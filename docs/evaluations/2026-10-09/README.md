# PDF library procurement evaluation

Evaluation date: 9 October 2026 (Australia/Sydney).

Implementation update, 11 October 2026: [invoice remediation PR #13](https://github.com/ZingPDF/ZingPDF/pull/13) and [document workflow remediation PR #14](https://github.com/ZingPDF/ZingPDF/pull/14) are merged. The reports below retain their original evaluation snapshot. Subsequent synthetic verification is recorded in [invoice evidence](../../project/INVOICE_RENDERING_VERIFICATION.md) and [workflow evidence](../../project/DOCUMENT_WORKFLOW_VERIFICATION.md); customer-corpus and commercial approval gates remain open.

These are three fictional company briefs built around real software workflows. Company sizes, document volumes, budgets, and acceptance thresholds are evaluation assumptions, not customer research or measured library performance. Three separate evaluators investigated the scenarios; the lead evaluator reviewed the evidence and decisions. All evaluators are AI agents, not independent certification bodies or customers.

## Decisions

| Complexity and company workflow | Selected library for a pilot | Why it leads | Implication for ZingPDF |
| --- | --- | --- | --- |
| [1. Invoice and report generation](01-invoice-generation.md) | QuestPDF Professional | Document layout, tables, and pagination are central; editing third-party PDFs is outside scope. | Demonstrate invoice pagination and reproducible HTML deployment, rather than relying on a broad feature list. |
| [2. Insurance document processing](02-document-workflow.md) | Commercial Syncfusion Document SDK stack | Documented forms, rendering, and OCR coverage align with an end-to-end processing pipeline. | Prove full-page OCR and rendered form fidelity on customer-style inputs and publish a reproducible Linux deployment. |
| [3. Regulated records and signatures](03-regulated-records.md) | Commercial iText with relevant add-ons | Documented archival, accessibility, advanced signing, typography, and redaction capabilities align with mandatory gates. | Current documented gaps exclude this scope. Make narrower signature and font boundaries explicit. |

These are conditional pilot selections. No competitor SDK trial, negotiated quote, independent compliance validation, or production approval was completed. A feature documented by a vendor earns a place in the pilot, not a pass on the customer's acceptance test. A cheaper product cannot compensate for a failed mandatory requirement.

## Procurement process and evidence

The completed stages were: define the company brief and requirements; identify mandatory gates; inspect ZingPDF source, tests, support and commercial terms; research alternatives using official vendor documentation; choose a pilot candidate and challenger; document costs, unresolved questions, and acceptance plans. Each report includes the reasoning and the conditions that would change the choice.

The remaining stages are executable trials against the same corpus; procurement and security review; written licence/SaaS/deployment and support scope; contract approval; and a controlled production rollout with rollback. These stages are explicitly pending. No vendor was contacted or paid.

Evidence has four levels:

1. **Executed:** a test actually run here, with environment and scope recorded.
2. **Inspected:** implementation or test code examined, but the specific end-to-end behavior not executed.
3. **Documented:** a published vendor capability or commercial term, subject to verification and the applicable order.
4. **Unknown:** no adequate evidence; do not treat as supported, unsupported, or a zero-cost item.

ZingPDF received deeper source inspection than its competitors. This creates an evidence asymmetry: its known limits are more visible, while rivals may have undiscovered limits. The reports do not interpret vendor claims as proof that rival implementations are superior. The pilot must apply equal acceptance tests to all surviving candidates, pin exact package versions and add-ons, and record discrepancies. Conversely, an explicit ZingPDF product limit is enough to reject it for a mandatory requirement without building a comparative benchmark.

## Executed ZingPDF verification

Starting checkout: `5c421a9828dc3f88b891ed81e25976303720ea4a`; working tree initially clean. `Directory.Build.props` specifies `VersionBase=1.0.58`, with local default `1.0.58-dev`. The [public NuGet listing](https://www.nuget.org/packages/ZingPDF) showed version 1.0.58 when checked on the evaluation date; matching version numbers do not prove published-binary equivalence to this checkout.

Environment: Windows, .NET SDK 10.0.301, test project targeting `net8.0`.

```powershell
dotnet test tests/ZingPDF.Tests.Smoke/ZingPDF.Tests.Smoke.csproj --configuration Release --logger "trx;LogFileName=technical-evaluation-smoke.trx"
```

Result: **104 passed, 0 failed, 0 skipped; test runner duration 16 seconds.** This is existing repository coverage, not a new customer acceptance suite. The locally generated TRX is at `tests/ZingPDF.Tests.Smoke/TestResults/technical-evaluation-smoke.trx` and is not committed.

The generation evaluator also ran `dotnet test tests/ZingPDF.Tests.Unit/ZingPDF.Tests.Unit.csproj --filter FullyQualifiedName~LiquidHtmlPdfTemplateTests --no-restore --verbosity quiet`: **4 passed, 0 failed**, reported test duration 217 ms. This was a local Debug test run, not Chromium PDF generation. An unrelated existing CS8604 test-code warning was emitted; see report 1 for its scope.

Coverage includes parsing/page access, page editing and save round trips, content authoring, text extraction, encryption, signing/validation, forms and redaction. Some assertions are structural or self round trips; OCR smoke cases use a fake recognition engine. This run does not prove real OCR accuracy, independent signature validation, Chromium invoice fidelity, Linux runtime compatibility, malformed-input isolation, compliance, or comparative speed. No performance ranking is inferred from its duration.

## Commercial comparison

Compare three-year total cost as licence purchases and renewals plus mandatory add-ons/support, engineering integration and regression work, infrastructure, external services, and expected maintenance. Keep currencies separate until procurement fixes an exchange-rate date. Include developer growth, contractor access, related legal entities, OEM/distribution and SaaS rights, CI/offline activation, and cancellation rights. An unknown quote is an unresolved procurement gate, not an estimate of zero.

ZingPDF's current local [commercial terms](../../legal/COMMERCIAL_TERMS.md), [EULA](../../legal/EULA.md), and [support policy](../../project/SUPPORT_POLICY.md) were inspected. Covered versions retain commercial rights within purchased scope after a paid term ends; new releases/support need an active subscription. Default support has no fixed contractual turnaround. Public source availability does not grant fork or modification rights. A document-processing service should obtain written confirmation of its SaaS use under the embedded-application permission and standalone/competing-service restriction. No legal interpretation is presented as contractual approval.

Older pricing notes were used only to identify what needed rechecking. ZingPDF amounts from those notes are not used in the evaluation: its applicable quote or current rendered checkout must establish price and seats. Vendor pricing and packaging in the linked reports are date-specific and should be confirmed before purchase.

## Priorities suggested by these scenarios

1. **Publish deployable evidence:** pinned Linux container recipes, offline dependency provisioning, and realistic examples with outputs and visual comparisons. This benefits both generation and editing buyers.
2. **Close workflow gaps before broadening claims:** full-page OCR composition, mixed scans, form appearance preservation, and demonstrated table pagination each remove a specific evaluation objection.
3. **Make procurement easy to resolve:** an unambiguous SaaS scope, seat-growth quotes, dependency/security inventory, vulnerability-response policy, and explicit support commitments reduce unknowns without adding PDF features.
4. **Keep regulated capabilities bounded:** integrity checks are different from timestamp/revocation/LTV validation; encryption and rewriting can invalidate signatures; structural redaction has supported-content limits. PDF/A and PDF/UA require independent validation and cannot be inferred from ordinary PDF output.

The most plausible nearer-term position from these three briefs is a scoped .NET editing workflow using supported forms, page operations, extraction, and rewritten saves. That is a hypothesis to test against customer files and purchasing decisions, not evidence of product-market fit. The selections would change if a company changes its requirements or a candidate fails the common pilot corpus.
