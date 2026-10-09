"""Independent verifier for the synthetic full-page OCR acceptance corpus."""
import hashlib
import json
import re
import struct
import sys
import unicodedata
from pathlib import Path

from pypdf import PdfReader


EXPECTED_CASES = {
    "digital-header-scan-body": {
        "pages": 1,
        "text": "DIGITAL HEADER: INVOICE ZX-1042 SCAN BODY: AMOUNT 184.75 AUD CUSTOMER: ALICE EXAMPLE REFERENCE: Q7M-29K",
        "tokens": ["ZX-1042", "184.75", "ALICE", "Q7M-29K"],
        "ocr": True,
        "source": "RenderedPage",
        "coverage": "RenderedPage",
    },
    "tiled-scan-page": {
        "pages": 1,
        "text": "TILE ONE: NORTH 5831 TILE TWO: MIDDLE 2746 TILE THREE: SOUTH 9105",
        "tokens": ["NORTH", "5831", "MIDDLE", "2746", "SOUTH", "9105"],
        "ocr": True,
        "source": "RenderedPage",
        "coverage": "RenderedPage",
    },
    "rotated-scan-page": {
        "pages": 1,
        "text": "ROTATED PAGE: EAST 7712 DATE: 2026-10-09 STATUS: APPROVED",
        "tokens": ["ROTATED", "EAST", "7712", "APPROVED"],
        "ocr": True,
        "source": "RenderedPage",
        "coverage": "RenderedPage",
    },
    "clean-printed-english": {
        "pages": 1,
        "text": "CLEAN PRINTED ENGLISH THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG. INVOICE TOTAL: 245.60 AUD. PLEASE RETAIN THIS PAGE FOR YOUR RECORDS.",
        "tokens": ["QUICK", "BROWN", "245.60", "RECORDS"],
        "ocr": True,
        "source": "RenderedPage",
        "coverage": "RenderedPage",
    },
    "vector-text": {
        "pages": 1,
        "text": "VECTOR TEXT: CONTRACT 6308 THIS LINE IS SELECTABLE PDF TEXT. TOTAL 319.40 AUD",
        "tokens": ["VECTOR", "6308", "SELECTABLE", "319.40"],
        "ocr": False,
        "source": "EmbeddedText",
        "coverage": "None",
    },
}


def normalize(text: str) -> str:
    text = unicodedata.normalize("NFKC", text).upper()
    return " ".join(text.split())


def normalize_token(text: str) -> str:
    return "".join(ch for ch in unicodedata.normalize("NFKC", text).upper() if ch.isalnum())


def edit_distance(left: str, right: str) -> int:
    previous = list(range(len(right) + 1))
    for i, left_char in enumerate(left, 1):
        current = [i]
        for j, right_char in enumerate(right, 1):
            current.append(min(current[-1] + 1, previous[j] + 1, previous[j - 1] + (left_char != right_char)))
        previous = current
    return previous[-1]


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def sha256_text(text: str) -> str:
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def png_dimensions(path: Path) -> tuple[int, int]:
    data = path.read_bytes()
    assert data[:8] == b"\x89PNG\r\n\x1a\n", f"Not a PNG preview: {path.name}"
    assert data[12:16] == b"IHDR", f"Missing PNG IHDR: {path.name}"
    width, height = struct.unpack(">II", data[16:24])
    assert width > 0 and height > 0
    return width, height


def verify(root: Path) -> None:
    manifest = json.loads((root / "results.json").read_text(encoding="utf-8"))
    environment = json.loads((root / "worker-environment.json").read_text(encoding="utf-8"))
    assert manifest["OcrEngine"] == "tesseract-cli"
    assert manifest["Language"] == "eng"
    assert manifest["WorkerDeadlineSeconds"] == 60
    assert manifest["ApplicationCancellationSeconds"] == 55
    assert "JBIG2-encoded image streams" in manifest["NotCovered"]
    assert "CCITT Group 4-encoded image streams" in manifest["NotCovered"]
    assert environment["IsLinux"] is True
    assert environment["User"] not in ("root", "0")
    assert environment["CpuMax"] != "unavailable"
    assert environment["MemoryMax"] != "unavailable"
    assert environment["PidsMax"] != "unavailable"
    assert environment["OcrTempFilesAfterRun"] == 0

    packages = (root / "ocr-packages.tsv").read_text(encoding="utf-8")
    model_hashes = (root / "tessdata.sha256").read_text(encoding="utf-8").splitlines()
    pdfium_hashes = (root / "pdfium.sha256").read_text(encoding="utf-8").splitlines()
    package_rows = packages.splitlines()
    for package_name in ("tesseract-ocr", "tesseract-ocr-eng", "tesseract-ocr-osd"):
        assert any(re.fullmatch(re.escape(package_name) + r"\t[0-9][^\r\n]*", row) for row in package_rows), f"Missing valid version for {package_name}"
    assert len(model_hashes) == 2 and any(re.fullmatch(r"[0-9a-f]{64}  .+eng\.traineddata", line) for line in model_hashes)
    assert any(re.fullmatch(r"[0-9a-f]{64}  .+osd\.traineddata", line) for line in model_hashes)
    assert len(pdfium_hashes) == 1 and re.fullmatch(r"[0-9a-f]{64}  /app/runtimes/linux-x64/native/libpdfium\.so", pdfium_hashes[0])
    model_inventory = {Path(line.split("  ", 1)[1]).stem: line.split("  ", 1)[0] for line in model_hashes}
    assert {item["Name"]: item["Sha256"] for item in environment["TessdataHashes"]} == model_inventory
    assert environment["PdfiumPackage"].startswith("bblanchon.PDFium.Linux/")
    assert environment["PdfiumSha256"] == pdfium_hashes[0].split("  ", 1)[0]
    assert "tesseract 5" in (root / "tesseract-version.txt").read_text(encoding="utf-8").lower()
    languages = (root / "tesseract-languages.txt").read_text(encoding="utf-8").splitlines()
    assert "eng" in languages and "osd" in languages

    docker_state = json.loads((root / "container-inspect.json").read_text(encoding="utf-8"))[0]
    config = docker_state["Config"]
    host = docker_state["HostConfig"]
    assert config["User"] == "1654:1654"
    assert host["NetworkMode"] == "none" and host["ReadonlyRootfs"] is True
    assert "ALL" in host["CapDrop"] and "no-new-privileges:true" in host["SecurityOpt"]
    assert host["Memory"] == 8 * 1024**3 and host["NanoCpus"] == 4_000_000_000
    assert host["PidsLimit"] == 256 and host["ShmSize"] == 256 * 1024**2
    assert any(path == "/tmp" and ("size=512m" in options or "size=536870912" in options) for path, options in host["Tmpfs"].items())
    image = json.loads((root / "image-inspect.json").read_text(encoding="utf-8"))[0]
    assert image["Config"]["Entrypoint"] == ["/usr/bin/timeout", "--signal=TERM", "--kill-after=5s", "60s", "dotnet", "VerifyDocumentWorkflow.dll"]
    supervisor = json.loads((root / "supervisor-check.json").read_text(encoding="utf-8"))
    assert supervisor["ConfiguredWorkerDeadlineSeconds"] == 60
    assert supervisor["AttachExitCode"] == supervisor["ContainerExitCode"] == 124
    assert supervisor["ElapsedMilliseconds"] < 20_000

    inputs = {item["File"]: item["Sha256"] for item in manifest["Inputs"]}
    assert len(inputs) == len(manifest["Inputs"])
    cases = {case["Name"]: case for case in manifest["Results"]}
    assert set(cases) == set(EXPECTED_CASES), f"Corpus cases differ: {sorted(cases)}"
    for name, expected_case in EXPECTED_CASES.items():
        expected_pages = expected_case["pages"]
        case = cases[name]
        pdf_path = root / "corpus" / case["PdfFile"]
        assert pdf_path.is_file() and pdf_path.read_bytes().startswith(b"%PDF-"), f"Missing PDF {case['PdfFile']}"
        assert sha256(pdf_path) == case["PdfSha256"] == inputs[case["PdfFile"]], f"PDF hash mismatch: {name}"
        reader = PdfReader(pdf_path, strict=True)
        assert len(reader.pages) == case["PageCount"] == expected_pages, f"PDF page count mismatch: {name}"
        assert len(case["Pages"]) == expected_pages
        pdf_page = reader.pages[0]
        pdf_text = pdf_page.extract_text() or ""
        resources = pdf_page.get("/Resources").get_object()
        xobject_reference = resources.get("/XObject")
        image_objects = xobject_reference.get_object() if xobject_reference is not None else {}
        if name == "digital-header-scan-body":
            assert "DIGITAL HEADER: INVOICE ZX-1042" in pdf_text
            assert len(image_objects) == 1
        elif name == "tiled-scan-page":
            assert not pdf_text.strip() and len(image_objects) == 3
        elif name == "rotated-scan-page":
            assert int(pdf_page.get("/Rotate", 0)) == 90 and len(image_objects) == 1
        elif name == "clean-printed-english":
            assert not pdf_text.strip() and len(image_objects) == 1
        elif name == "vector-text":
            assert "VECTOR TEXT: CONTRACT 6308" in pdf_text and not image_objects
        page = case["Pages"][0]
        assert page["ExpectedText"] == expected_case["text"], f"Reference text mismatch: {name}"
        assert page["ExpectedTokens"] == expected_case["tokens"], f"Reference tokens mismatch: {name}"
        assert page["UsedOcr"] is expected_case["ocr"]
        assert page["Source"] == expected_case["source"]
        assert page["InputCoverage"] == expected_case["coverage"]
        trials = page["OrientationTrials"]
        if page["UsedOcr"]:
            assert [trial["RotationDegrees"] for trial in trials] == [0, 90, 180, 270]
            assert all(0 <= trial["LengthWeightedWordConfidence"] <= 100 for trial in trials)
            eligible = [trial for trial in trials if trial["RecognizedWordCount"] >= 3
                        and trial["RecognizedLetterCount"] >= 12]
            assert eligible, f"No eligible orientation in {name}"
            chosen = sorted(eligible, key=lambda trial: (-trial["LengthWeightedWordConfidence"], trial["RotationDegrees"]))[0]
            assert page["SelectedOcrRotationDegrees"] == chosen["RotationDegrees"]
            assert page["LengthWeightedWordConfidence"] == chosen["LengthWeightedWordConfidence"]
            assert page["RecognizedWordCount"] == chosen["RecognizedWordCount"]
        else:
            assert trials == [] and page["SelectedOcrRotationDegrees"] is None
        expected = normalize(page["ExpectedText"])
        actual = normalize(page["OcrText"])
        assert sha256_text(page["OcrText"]) == page["OcrTextSha256"], f"OCR result hash mismatch: {name}"
        cer = edit_distance(expected, actual) / max(1, len(expected))
        assert abs(cer - page["CharacterErrorRate"]) < 1e-9, f"CER mismatch: {name}"
        assert cer <= 0.02, f"CER too high for {name}: {cer:.3f}"
        for token in page["ExpectedTokens"]:
            assert normalize_token(token) in normalize_token(page["OcrText"]), f"Missing expected token {token!r} in {name}"
        assert page["MissingTokens"] == [] and len(page["MissingTokens"]) == 0
        if name == "vector-text":
            extracted = normalize(pdf_text)
            for token in page["ExpectedTokens"]:
                assert normalize_token(token) in normalize_token(extracted), f"PDF text extraction missed {token!r} in vector page"
        else:
            assert page["Source"] == "RenderedPage" and page["InputCoverage"] == "RenderedPage", f"Full-page OCR was skipped: {name}"
        preview = root / "previews" / page["PreviewFile"]
        assert preview.is_file() and sha256(preview) == page["PreviewSha256"], f"Preview hash mismatch: {name}"
        assert png_dimensions(preview) == (page["PreviewWidth"], page["PreviewHeight"])
        if name == "rotated-scan-page":
            assert page["PreviewWidth"] > page["PreviewHeight"], "The rotated PDF page was not rendered in landscape orientation"
        assert page["ElapsedMilliseconds"] > 0
        print(f"PASS {name}: {page['Source']}/{page['InputCoverage']}, CER={cer:.3f}, {page['PreviewWidth']}x{page['PreviewHeight']}, {page['ElapsedMilliseconds']} ms")

    if "digital-header-scan-body" in cases:
        page = cases["digital-header-scan-body"]["Pages"][0]
        assert normalize_token("DIGITAL HEADER INVOICE ZX-1042") in normalize_token(page["OcrText"]), "Mixed vector header missing from rendered OCR result"

    (root / "independent-verification.json").write_text(json.dumps({
        "Validator": "Python standard library + pypdf",
        "PypdfVersion": __import__("pypdf").__version__,
        "Cases": len(cases),
        "Pages": sum(case["PageCount"] for case in cases.values()),
        "Verified": True,
    }, indent=2), encoding="utf-8")


if __name__ == "__main__":
    verify(Path(sys.argv[1] if len(sys.argv) > 1 else ".tools/document-workflow"))
