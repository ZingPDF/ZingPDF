"""Independent acceptance checks for the GenerateInvoices example (test dependency: pypdf 6.10.0)."""
import json
import re
import sys
import unicodedata
from pathlib import Path

import pypdf


def verify(directory: Path) -> None:
    manifest = json.loads((directory / "manifest.json").read_text(encoding="utf-8-sig"))
    results = []
    for case in manifest["Results"]:
        reader = pypdf.PdfReader(directory / case["File"], strict=True)
        pages = [unicodedata.normalize("NFC", page.extract_text(extraction_mode="layout")) for page in reader.pages]
        text = "\n".join(pages)
        assert re.findall(r"LINE\d{4}", text) == case["References"], f"Missing, duplicated or reordered rows: {case['File']}"
        assert len(case["References"]) == case["Rows"]
        assert re.search(r"Total AUD\s+" + re.escape(case["Total"]) + r"(?!\d)", text), f"Missing total: {case['File']}"
        assert len(re.findall(r"Total AUD", text)) == 1, f"Duplicated totals: {case['File']}"
        for required in ["José Müller", "Montréal", "€", "£", "$"]:
            assert required in text, f"Missing Unicode text {required!r}: {case['File']}"
        fonts = set()
        for page, page_text in zip(reader.pages, pages):
            assert abs(float(page.mediabox.width) - 595.28) < 1
            assert abs(float(page.mediabox.height) - 841.89) < 1
            if "LINE" in page_text:
                assert "Line reference" in page_text and "Amount AUD" in page_text, f"Missing repeated header: {case['File']}"
            for font_ref in page["/Resources"].get("/Font", {}).values():
                font = font_ref.get_object()
                if font.get("/Subtype") == "/Type3":
                    # Type 3 fonts embed their glyph programs in CharProcs rather than a FontFile stream.
                    char_procs = font.get("/CharProcs", {})
                    assert char_procs and all(proc.get_object().get_data() for proc in char_procs.values()), f"Missing embedded Type 3 glyphs: {case['File']}"
                    fonts.add("Type3 embedded glyph programs")
                    continue
                descendant = font["/DescendantFonts"][0].get_object() if "/DescendantFonts" in font else font
                descriptor = descendant.get("/FontDescriptor")
                assert descriptor is not None, f"Font descriptor missing: {case['File']}"
                descriptor = descriptor.get_object()
                assert any(key in descriptor for key in ["/FontFile", "/FontFile2", "/FontFile3"]), f"Unembedded font: {case['File']}"
                fonts.add(str(font.get("/BaseFont")))
        assert fonts, f"No embedded fonts: {case['File']}"
        if case["Rows"] >= 40:
            assert 1 < len(pages) <= 50
        results.append({"File": case["File"], "Rows": case["Rows"], "Pages": len(pages), "EmbeddedFonts": sorted(fonts)})
        print(f"PASS {case['File']}: {case['Rows']} rows, {len(pages)} pages, ordered rows, Unicode, headers, one total, embedded fonts")
    (directory / "verification.json").write_text(json.dumps({"Validator": f"pypdf {pypdf.__version__}", "Results": results}, indent=2), encoding="utf-8")


if __name__ == "__main__":
    verify(Path(sys.argv[1] if len(sys.argv) > 1 else "output/invoices"))
