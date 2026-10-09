# Document workflow verification

9 October 2026. Implementation is in draft [PR #14](https://github.com/ZingPDF/ZingPDF/pull/14), stacked on invoice PR #13. The delegated implementation agents used `gpt-6-luna` with high reasoning effort; the coordinator reviewed integration and independent evidence. See [the plan](DOCUMENT_WORKFLOW_PLAN.md) for ownership and acceptance criteria.

## Implemented behavior

- `PdfOcrMode.RenderedPage` supplies the rendered visible page to an `IOcrEngine`, including mixed vector/scanned content, tiled images and rotation. The existing image-candidate default remains. `Source` and `InputCoverage` describe the input used, not recognition completeness. Resolution is 72–600 DPI and the configurable pixel limit cannot exceed 50 million.
- External AcroForms preserve inherited text/choice settings and repeated widget appearances. Choice export values remain distinct from display labels; checkboxes and radio buttons regenerate selected appearances. Flattening validates appearances and geometry before removing fields/annotations. Unsupported XFA and missing/unusable normal appearances fail explicitly. An empty, unvalued, zero-area widget with no painting operations is removed without adding page content.
- Runnable acceptance examples and independent readers retain filled/flattened PDFs, previews, hashes and verification output. The Linux OCR recipe uses PDFium and a process-isolated Tesseract CLI adapter, with a read-only, offline, non-root worker and external deadline. This does not verify the built-in managed Tesseract wrapper on Linux.
- Default logger initialization no longer creates an unused debug file. Parsing therefore does not require a writable working directory; explicit `FileLogger` behavior remains unchanged.
- Plain-text extraction retains its positioned-run collector across a page's content streams, preserving baseline line breaks and horizontal gaps without inserting spaces into contiguous same-line fragments.

## Executed local checks

The final `dotnet test ZingPDF.sln --configuration Release --no-restore` passed 468 unit, 18 integration and 122 smoke tests (608 total), with no failures/skips. Focused OCR routing tests cover mixed/tiled inputs, default behavior, pixel limits, errors and cancellation. Form tests cover inheritance, repeated widgets, missing appearances and the existing complex-form fixture.

After the Linux startup finding, two focused logger tests passed, including first initialization in an isolated process. `pwsh ./scripts/assert-binary-fixtures.ps1` passed.

The five-field-type, seven-widget external-form example passed the independent `scripts/verify-form-appearances.py` checks. pypdf verified saved field values, selected appearance stream hashes, removal of interactive structure and extracted labels. Poppler at 144 DPI produced identical filled and flattened renders locally: 0.0000% changed pixels. This is a single synthetic template, not a general fidelity guarantee.

`pwsh ./website/check-copy.ps1` passed. `pwsh ./website/generate-api-reference.ps1` completed with zero warnings/errors; the OCR options page was previewed through the local HTTP server.

## Linux acceptance evidence

Initial [run 37888594888](https://github.com/ZingPDF/ZingPDF/actions/runs/37888594888) passed the independent external-form appearance job. The OCR container built and recorded native/model inventories, then exposed eager default debug-file creation in the library logger on the read-only filesystem. That unused initializer was removed; subsequent runs retain the same worker restrictions.

The logger repair allowed real recognition in [run 37888867018](https://github.com/ZingPDF/ZingPDF/actions/runs/37888867018), which then exposed a synthetic fixture layout error: the mixed-page image covered the lower half of the digital header. The retained preview confirmed the overlap. The fixture now separates the header and scan, with expected text and the 2% CER threshold unchanged. Failed pages persist recognition text and metrics before raising an acceptance failure.

The corrected fixture passed mixed-page and tiled recognition in [run 37889128266](https://github.com/ZingPDF/ZingPDF/actions/runs/37889128266), but Tesseract automatic orientation returned upside-down text for the rotated page. The CLI adapter now trials all four quarter-turn orientations using `--psm 3` and TSV output. It chooses the highest character-length-weighted word confidence among trials with at least three words and twelve letters, with deterministic ties. It has no access to the expected corpus answers. Selection is a heuristic, not a recognition guarantee; all four trial scores and the selected angle are retained and independently checked. Local TSV ordering, weighting, selection and clockwise pixel-rotation self-checks pass.

All four real-OCR cases passed the 2% gate in [run 37889467834](https://github.com/ZingPDF/ZingPDF/actions/runs/37889467834). Its final vector-text case failed because embedded extraction concatenated three distinct lines across content streams. The existing plain-text collector already handles positioned line breaks and gaps; the repair retains it for the page instead of resetting it per stream. Three focused tests pass for cross-stream line breaks, horizontal gaps and contiguous same-line fragments. Expected text and the corpus threshold remain unchanged.

The full native run at implementation commit `90588609faef134c15d8937f916df67ba6eb9f68` completed all five cases and the supervisor probe. Its [retained artifact](https://github.com/ZingPDF/ZingPDF/actions/runs/37889865275/artifacts/11597937112) was downloaded and passed the corrected independent Python verifier locally with pypdf 6.10.0. The workflow's original verifier failed only because Docker serializes enabled `no-new-privileges` without a `:true` suffix. The corrected verifier accepts equivalent enabled representations and explicitly rejects absent/false values. This first run is therefore marked failed in GitHub despite completed native recognition; a clean CI rerun uses the corrected verifier.

| Synthetic page | Input path | Character error rate | Page elapsed time |
| --- | --- | --- | --- |
| Digital header and scan body | Rendered page | 0.9709% | 2,912 ms |
| Three scan tiles | Rendered page | 0% | 2,764 ms |
| PDF page rotated 90 degrees | Rendered page | 0% | 2,788 ms |
| Clean printed English | Rendered page | 0% | 2,939 ms |
| Selectable vector text | Embedded PDF text, no OCR | 0% | 74 ms |

Times include OCR trials and preview rendering. They are single-run evidence, not capacity or percentile measurements. The deliberate three-second stall exited with code 124 in 3,136 ms. The worker used UID/GID 1654, no network, a read-only root, all capabilities dropped and enabled no-new-privileges, with 4 CPUs/8 GiB. Model/native hashes, package inventory, image identity and all four orientation scores are in the artifact. The independent Linux form job also passed for this implementation.

## Remaining procurement gates

Customer acceptance still needs the evaluated insurer template set, reference PDFs and scans, with approved recognition/fidelity thresholds, production concurrency and recovery measurements, dependency advisory disposition and approved licensing/support terms. JBIG2 and CCITT Group 4 are not exercised by this synthetic corpus. XFA remains unsupported. The build reports existing ImageSharp 3.1.11 dependency advisories; this change does not resolve them. Pin approved container digests and native package/model versions for a production release. These changes improve the technical POC decision; they do not establish a production procurement approval.
