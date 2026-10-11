# Document workflow remediation plan

Started 9 October 2026. This follows the invoice rendering change in PR #13. Work is on `codex/document-workflow-remediation-20261009`; the draft PR will target the invoice branch until that change is merged.

## Scope and acceptance

1. Add an explicit rendered-page OCR mode while preserving the existing image-candidate default. A digital header must not bypass OCR of a scanned body. Report the input source and coverage without claiming recognition accuracy or complete recovered text.
2. Prevent flattening from silently discarding widgets whose normal appearance cannot be placed on the page. Validate before destructive changes. Fix supported inherited form properties and repeated-widget appearances where concrete tests establish defects.
3. Execute real PDFium rendering and native OCR in an offline non-root Linux worker, using a fixed corpus with mixed text/images, tiled scans, rotation and clean printed English. Preserve artifacts, model hashes, dependency inventory, timings and recognition metrics.
4. Publish public API and deployment documentation, independent form appearance comparisons, and a verification report that distinguishes executed checks from procurement gates still requiring customer data.

The acceptance corpus is synthetic and repository-owned. It does not substitute for 50 insurer templates, 100 reference PDFs or 100 customer scans. No support commitment, licensing expansion, OCR accuracy claim across arbitrary documents or production capacity claim follows from a small corpus pass.

## Delegation

All three implementation agents use `gpt-6-luna` with high reasoning effort, as requested for cheaper-model execution. The coordinating agent reviews integration and evidence.

| Agent | Owned changes | Required evidence |
| --- | --- | --- |
| `workflow_ocr` | OCR package and focused routing/resource/cancellation tests | Mixed and tiled pages use the rendered input; legacy defaults survive; options and pixel limits fail explicitly; renderer/engine errors and cancellation propagate |
| `workflow_forms` | Form implementation, synthetic external dictionaries, appearance acceptance example | Inheritance/repeated widgets preserve values and appearances; missing/unusable appearances reject before mutation; supported fill/save/reopen/flatten output renders correctly |
| `workflow_linux` | Native document-workflow example, Linux image, verification scripts and CI | Real OCR, not a delegate, runs without runtime downloads or network; native/library/model versions and hashes recorded; deadline terminates the worker; exact corpus comparisons are retained |
| Coordinator | Integration, public documentation, API generation, draft PR and CI review | Narrow checks first, broader checks for shared behavior, independent PDF inspection and an explicit list of unverified gates |

Agents must read `AGENTS.md`, preserve existing changes and binary fixtures, avoid broad refactors or dependencies, coordinate shared files and builds, and report commands/results. They must not commit or push independently. Public APIs need XML documentation. The coordinator owns package READMEs, support notes, website copy and generated API metadata.

## Delivery process

Inspect implementation and tests, agree additive API contracts, execute independent changes, review diffs and run focused checks. Then run unit/integration/smoke coverage as appropriate, website copy checks and API generation. Push a draft PR through the available Git credentials and execute the Linux workflow. Repair failures and retain output evidence before reporting completion. Keep the purchasing decision conditional on customer-corpus fidelity, load measurements and approved licensing/support terms.
