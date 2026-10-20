"""Check the shipped example PDFs with pypdf, independently of ZingPDF."""

import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import platform

from pypdf import PdfReader


def verify(website: Path) -> dict:
    cases = [
        ("assets/examples/first-pdf/original.pdf", ["Delivery report"]),
        ("assets/examples/first-pdf/edited.pdf", ["Delivery report\nApproved for delivery", "Delivery checklist"]),
        ("assets/showcase/project-status-report.pdf", ["Northwind ERP Modernisation", "Operational detail"]),
    ]
    checks = []
    for relative, expected_pages in cases:
        reader = PdfReader(website / relative)
        if len(reader.pages) != len(expected_pages):
            raise ValueError(f"Unexpected page count: {relative}")
        expected_text = []
        for page, required in zip(reader.pages, expected_pages):
            actual = " ".join((page.extract_text() or "").split())
            for text in required.splitlines():
                if text not in actual:
                    raise ValueError(f"Missing {text!r}: {relative}")
                expected_text.append(text)
        checks.append({"file": relative, "pages": len(reader.pages), "required_text": expected_text, "passed": True})

    extracted = (website / "assets/examples/first-pdf/extracted-text.txt").read_text(encoding="utf-8")
    if not all(text in extracted for text in ("Delivery report", "Approved for delivery", "Delivery checklist")):
        raise ValueError("The shipped extracted-text output is incomplete.")

    return {
        "verified_at_utc": datetime.now(timezone.utc).isoformat(),
        "platform": platform.system(),
        "package": "ZingPDF 1.0.59",
        "independent_reader": "pypdf",
        "checks": checks,
        "scope": "Generated text-based inputs and a fixed-layout report. Validate your own documents and deployment environment.",
    }


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("website", type=Path)
    args = parser.parse_args()
    result = verify(args.website)
    destination = args.website / "assets/examples/verification.json"
    destination.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(f"Verified {len(result['checks'])} PDFs with pypdf; wrote {destination}.")
