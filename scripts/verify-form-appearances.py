"""Independently check parsed field values and Poppler renders before/after flattening."""
import argparse
import hashlib
import json
import subprocess
from collections import Counter
from pathlib import Path

from pypdf import PdfReader


def read_ppm(path):
    data = path.read_bytes()
    position = 0

    def token():
        nonlocal position
        while data[position:position + 1].isspace() or data[position:position + 1] == b"#":
            if data[position:position + 1] == b"#":
                position = data.index(b"\n", position) + 1
            else:
                position += 1
        start = position
        while not data[position:position + 1].isspace():
            position += 1
        return data[start:position]

    assert token() == b"P6", "Expected Poppler RGB PPM"
    width, height, maximum = int(token()), int(token()), int(token())
    assert maximum == 255
    position += 2 if data[position:position + 2] == b"\r\n" else 1
    pixels = data[position:]
    assert len(pixels) == width * height * 3
    return width, height, pixels


def verify(root, poppler):
    filled_path, flattened_path = root / "filled-and-reopened.pdf", root / "flattened.pdf"
    filled, flattened = PdfReader(filled_path, strict=True), PdfReader(flattened_path, strict=True)
    assert len(filled.pages) == len(flattened.pages) == 1
    fields = filled.get_fields()
    expected_values = {
        "profile.name": "Zoë Lovelace\nAnalytical Engine",
        "agreement": "/accept",
        "delivery": "/pickup",
        "country": "ca",
        "regions": ["nsw", "vic"],
    }
    for name, expected in expected_values.items():
        assert name in fields, f"Missing field: {name}"
        actual = fields[name].get("/V")
        actual = list(actual) if isinstance(expected, list) else str(actual)
        assert actual == expected, f"Field value mismatch: {name}"
    widgets = [ref.get_object() for ref in filled.pages[0].get("/Annots", [])]
    assert len(widgets) == 7 and all(widget.get("/Subtype") == "/Widget" for widget in widgets)
    assert "/AcroForm" not in flattened.trailer["/Root"]
    assert not flattened.pages[0].get("/Annots")
    selected_streams = []
    for widget in widgets:
        appearance = widget["/AP"]["/N"].get_object()
        if not hasattr(appearance, "get_data"):
            appearance = appearance[widget["/AS"]].get_object()
        selected_streams.append(hashlib.sha256(appearance.get_data()).hexdigest())
    page = flattened.pages[0]
    placed_names = [operands[0] for operands, operation in page.get_contents().operations if operation == b"Do"]
    assert len(placed_names) == 7, "A widget appearance was not placed on the page"
    placed_streams = [hashlib.sha256(page["/Resources"]["/XObject"][name].get_data()).hexdigest()
                      for name in placed_names]
    assert Counter(placed_streams) == Counter(selected_streams), "Flattened appearance content differs from selected widget streams"
    text = flattened.pages[0].extract_text() or ""
    assert text.count("Zoë Lovelace") == 2 and text.count("Analytical Engine") == 2, "Repeated text widgets disappeared"
    assert "Canada" in text and "Victoria" in text, "Choice display labels disappeared"

    ppm_paths = []
    for pdf_path, prefix in [(filled_path, "independent-filled"), (flattened_path, "independent-flattened")]:
        output = root / prefix
        subprocess.run([poppler, "-r", "144", "-singlefile", str(pdf_path), str(output)],
                       check=True, timeout=30, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
        ppm_paths.append(output.with_suffix(".ppm"))
    width, height, before = read_ppm(ppm_paths[0])
    after_width, after_height, after = read_ppm(ppm_paths[1])
    assert (width, height) == (after_width, after_height)
    changed = sum(any(abs(before[index + channel] - after[index + channel]) > 8 for channel in range(3))
                  for index in range(0, len(before), 3))
    fraction = changed / (width * height)
    assert fraction <= 0.002, f"Visible appearance changed after flattening: {fraction:.3%} of pixels"
    assert sum(byte < 100 for byte in after) > 1000, "Rendered output is blank"
    result = {
        "Validator": "pypdf + independent Poppler RGB render",
        "WidgetCount": len(widgets), "Width": width, "Height": height,
        "ChangedPixelsAbove8Levels": changed, "ChangedPixelFraction": fraction,
        "MaximumChangedPixelFraction": 0.002,
        "Files": {path.name: hashlib.sha256(path.read_bytes()).hexdigest()
                  for path in [filled_path, flattened_path, *ppm_paths]},
    }
    (root / "independent-form-verification.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(f"PASS external form: five field types, seven widgets, preserved appearances ({fraction:.4%} changed pixels)")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    parser.add_argument("--pdftoppm", default="pdftoppm")
    arguments = parser.parse_args()
    verify(arguments.directory, arguments.pdftoppm)
