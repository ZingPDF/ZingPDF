# Report 1: invoice and account-report generation

Evaluation date: 9 October 2026. Perspective: a technical evaluator employed by the purchasing company, with no preference for ZingPDF.

**Decision: select QuestPDF Professional as the preferred library for a time-boxed proof of concept, with PDFsharp/MigraDoc Core as the competing finalist. Production approval remains conditional on the acceptance tests below.** ZingPDF can create these documents, but its inspected PDF-native API leaves document pagination to application code; its HTML route introduces browser lifecycle and deployment concerns that are not yet answered by the inspected interface. Neither claim establishes that ZingPDF output is defective.

## Company and workflow

A privately owned B2B subscription-software company has USD 2 million annual gross revenue and three .NET developers. One legal entity runs an ASP.NET service and queue worker in Debian-based Linux x64 containers. It sends invoices and account summaries to English-speaking customers in Australia, the US and Western Europe. Finance supplies validated decimal totals and tax amounts; the PDF code formats them without recalculating tax. The business expects 30,000 documents per month and occasional bursts of eight parallel jobs.

There are three company-owned layouts: invoice, credit note and monthly account report. Inputs are structured records, not arbitrary customer HTML or uploaded PDFs. Most outputs have 1–5 pages; long reports have up to 50 pages. Names include Latin diacritics and the currency symbols €, £ and $. Unsupported scripts must produce an explicit validation error rather than missing glyphs. Signing, PDF/A, PDF/UA certification, existing-PDF editing and interactive forms are outside this purchase. If those become requirements, reopen the selection.

These are plausible scenario assumptions, not findings about an actual customer.

## Requirements and acceptance gates

Hard gates apply before preferences or price scores. A low subscription price cannot compensate for a missing invoice line or unusable deployment.

| ID | Requirement | Acceptance test |
| --- | --- | --- |
| G1 | Proprietary SaaS use and redistribution in the worker are licensed | Record the exact selected package versions, legal entity, developer coverage, dependency notices and commercial terms; purchasing approves them before release |
| G2 | .NET integration on the production Linux image | Clean image starts with bundled fonts/assets, non-root user and runtime outbound networking disabled; create all three document types without runtime downloads |
| G3 | Correct variable-length document layout | Approved corpus covers 0, 1, 40 and 500 rows, long descriptions, long addresses, credit values and awkward page boundaries; every expected line appears exactly once, totals remain attached to their label, column headers repeat and no text overlaps or clips |
| G4 | Searchable readable text and stable typography | Embed approved fonts; extract expected invoice number, accented names, amounts and symbols with an independent reader; finance checks rendered pages and print samples in Acrobat and Chromium |
| G5 | Bounded execution and no cross-customer content | On a declared 2-vCPU/2-GiB container: warmed p95 under 1 second for the representative 5-page invoice at eight concurrent jobs, maximum 10 seconds for 50 pages, container peak RSS below 1.5 GiB; 10,000 seeded jobs have zero missing rows, failures or tenant-data leaks |
| G6 | Predictable operations | Cold startup and first invoice within 15 seconds; repeated restart and disabled-egress tests pass; timeout/supervisor terminates a stalled job without holding the queue indefinitely; no sensitive document content in logs |

Thresholds are proposed procurement targets, not measured results. The load corpus must have a fixed distribution and each finalist gets the same resources. Byte-identical PDFs are not required: timestamps and internal identifiers may vary. Text, pagination and rendered appearance are compared instead.

Preferences after gates: engineer effort to create and change all three templates, understandable layout failures, small dependency footprint, documented update process, responsive defect support and three-year total cost. The invoice arithmetic remains owned and tested by the application.

## Evidence and procurement process

Evidence grades used here: **A** means a relevant check executed in this evaluation; **B** means inspected implementation or a current official document establishes the stated behavior; **C** means evaluator inference; **U** means unmeasured or not verified. A vendor feature page is B for a documented capability, not A for working on our corpus.

1. Finance and engineering sign off the corpus, target image and gates before a vendor demonstration.
2. Engineering checks official APIs, support limits, package licensing and architecture. Purchasing requests a three-developer ZingPDF quote and captures QuestPDF's applicable legal terms.
3. Two engineers implement the same first invoice independently in QuestPDF and MigraDoc, with equal two-day budgets. Record time spent, layout exceptions and review defects; do not substitute a vendor's sample invoice for the agreed corpus.
4. Complete the second template, run the gates and archive the package lock files, Dockerfiles, outputs, independent text extraction and rendered comparisons. Failure of any gate stops adoption until a documented correction passes.
5. Finance accepts the outputs, operations accepts the image and rollback procedure, and purchasing signs the order. Pin the accepted versions, release behind a feature flag, and retain the previous renderer during rollout.

This report completes desk screening and one narrow ZingPDF unit check. No comparative implementation, Linux deployment, document inspection or load experiment has been run. It is a shortlist decision, not a claim of production certification.

Evidence is asymmetric: ZingPDF source and a narrow test were inspected locally; alternative-library capabilities were checked in official documentation without running their implementations. The preference for QuestPDF is an architectural-fit hypothesis for the pilot. It is not evidence that QuestPDF has fewer defects or better throughput than ZingPDF. Repository evidence is from the working tree at HEAD `5c421a9828dc3f88b891ed81e25976303720ea4a`; the targeted test ran on Windows x64 using .NET SDK 10.0.301 and targeted .NET 8.

## Candidates and reasons

| Candidate | Relevant established evidence | Main uncertainty or cost |
| --- | --- | --- |
| QuestPDF Professional | Official API documents tables, headers and cell layout (B). The official pricing page lists a USD 1,999 company-wide Professional license with an initial update/support term (B). This company's USD 2m revenue makes free Community eligibility inappropriate (B). | Linux image, native dependency compatibility, fonts, performance and actual developer effort are U. A commercial license and update renewal must be budgeted. |
| PDFsharp/MigraDoc Core | MigraDoc documents a paragraph/table document model with automatic page breaks (B); PDFsharp alone supplies only basic text layout. Core supports Linux and recommends an application font resolver. MIT licensing is published by the project (B). | Actual pagination edge cases, project effort and performance are U. Contracted support is separate; license-free does not mean maintenance-free. |
| ZingPDF PDF-native authoring | `PdfAuthoringBuilder` explicitly constructs configured pages and places text in coordinates or bounds (B). Support lists text, images, paths and embedded TrueType fonts; high-level font registration is limited to WinAnsi/Windows-1252 (B). | No document-level flowing table/pagination contract was found in the inspected authoring API (B limited to that surface). Implementing row measurement and page breaks adds application work (C). Controlled character scope makes the stated font limit manageable, subject to G4. |
| ZingPDF Liquid HTML | README and source establish Fluid model rendering, includes and HTML escaping, followed by PuppeteerSharp conversion (B). Four targeted tests passed (A). | Real-browser pagination, cold starts and throughput are U. Current wrapper calls browser download preparation and launches a fresh browser for every conversion (B); production controls need clarification or changes. |
| Direct Playwright .NET + owned HTML templates | Official API exposes Chromium PDF generation and print configuration (B). Relevant if there is an existing approved HTML print design. | Own the template engine, browser packaging, pooling, sandbox configuration and update process. No existing HTML investment is assumed here, so this is a reserve option, not a preferred finalist (C). |

Sources: [QuestPDF table API](https://www.questpdf.com/api-reference/table/basics.html), [QuestPDF pricing](https://www.questpdf.com/pricing.html), [QuestPDF Community terms](https://www.questpdf.com/license/community.html), [PDFsharp/MigraDoc reference](https://docs.pdfsharp.net/), [PDFsharp specifications](https://docs.pdfsharp.net/PDFsharp/Overview/Specifications.html), [PDFsharp FAQ](https://docs.pdfsharp.net/General/Overview/FAQ.html), [PDFsharp current MIT license](https://github.com/empira/PDFsharp/blob/master/LICENSE), [Playwright Page API](https://playwright.dev/dotnet/docs/api/class-page) and [browser installation](https://playwright.dev/dotnet/docs/browsers). Pricing and terms were checked on the evaluation date and should be captured again at purchase. Older QuestPDF per-developer pricing or MIT descriptions are not used.

Other commercial suites are deferred because this workflow does not need editing, OCR or signing. This is a procurement-scope choice, not evidence that those products are incapable. Buying an extensive PDF suite becomes reasonable if a roadmap introduces those operations or procurement requires a negotiated support agreement.

## Ranking and the purchasing decision

Use this ordinal order after all gates pass; it avoids giving numerical precision to unrun experiments:

1. **QuestPDF Professional:** first POC choice for a new C# invoice implementation. Tables and page-content controls directly match the work; the application need not develop its own document paginator. This predicts lower implementation effort but does not prove it (C).
2. **PDFsharp/MigraDoc Core:** viable alternative with a documented document-layout model and zero library license fee. It becomes the chosen library if its POC matches visual/layout gates and demonstrates lower total cost or the company prioritizes MIT rights over vendor email support.
3. **ZingPDF Liquid HTML / direct browser route:** move up if engineers already maintain approved HTML templates and the runtime gates pass. Direct browser integration deserves comparison because the rendering engine is the same architectural category; ZingPDF must justify its added licensed dependency through template support and predictable deployment.
4. **ZingPDF native drawing:** suitable for a fixed one-page form, but not the first choice for these variable-length invoice tables without application pagination code.

No candidate currently has demonstrated G2–G6 against this company's corpus. The choice remains reversible. If QuestPDF fails a gate, evaluate MigraDoc rather than rationalizing the failure with a preference score. If both fail, extend the shortlist to a commercial generation product and reset the POC budget.

## Three-year cost

For candidate `v`, use `TCO_v = license_and_renewals_v + loaded_hourly_rate × (integration_hours_v + template_change_hours_v + upgrade_hours_v + incident_hours_v) + 36 × monthly_runtime_cost_v + optional_support_v + exit_cost_v`. Apply tax and foreign-exchange assumptions consistently; avoid counting support twice if included in the license.

At currently published prices, QuestPDF Professional's license component is **USD 5,997 before tax** with two annual renewals at USD 1,999 and continuous price-lock eligibility. It is USD 1,999 if the company purchases once and stays on covered versions, but that scenario excludes later updates and support and must be judged separately. These are license scenarios, not complete TCO. [QuestPDF pricing and renewal terms](https://www.questpdf.com/pricing.html).

PDFsharp/MigraDoc's library license component is zero under MIT; support quotes and engineer hours are unknown. ZingPDF's component is the accepted three-seat quote plus renewals for the chosen term: local commercial terms deliberately defer pricing and seat counts to checkout or the order. Do not infer a price from stale marketing text or an uninspected Stripe table. Browser routes may add compute and patching work, but no cost delta has been measured.

For illustration only, at USD 100/hour a USD 5,997 license difference corresponds to about 60 engineering hours over three years. That is a break-even question for the POC, not a claim that QuestPDF saves those hours.

## ZingPDF findings and implications

Inspected repository evidence:

- `docs/project/SUPPORT.md`: managed .NET 8 core, dependency-specific deployment requirements, WinAnsi high-level text boundary and stream requirements.
- `src/ZingPDF/PdfAuthoringBuilder.cs`: page plans and coordinate/bounds text operations; bounded wrapping is not a whole-document table paginator.
- `src/ZingPDF.FromHTML/Converter.cs`: `BrowserFetcher.DownloadAsync()` is invoked on browser preparation; each conversion launches and disposes a browser; the string path uses screen media and default PDF options. The URI path explicitly waits for `document.fonts.ready`; the HTML string path has no such explicit wait. Library defaults may also wait, so this does not prove a missing-font failure.
- `src/ZingPDF.Templates.LiquidHtml/HtmlToPdfConverter.cs`: the default converter does not forward its cancellation token into `Converter.ToPdfAsync`. The public template factories install this default converter; the custom-converter factory overload inspected is internal, despite the converter interface being public.
- `tests/ZingPDF.Tests.Unit/Templates/LiquidHtml/LiquidHtmlPdfTemplateTests.cs`: loop, include, invalid-template and output-copy behavior. The PDF-output test injects `%PDF-test` bytes; it is not evidence of a valid generated PDF.
- `docs/legal/COMMERCIAL_TERMS.md` and `docs/project/SUPPORT_POLICY.md`: paid developer seats with continuing rights for covered releases, and no default guaranteed turnaround SLA.

Executed: `dotnet test tests/ZingPDF.Tests.Unit/ZingPDF.Tests.Unit.csproj --filter FullyQualifiedName~LiquidHtmlPdfTemplateTests --no-restore --verbosity quiet`. Result: **4 passed, 0 failed**, 217 ms reported test duration on the local Windows host. Build emitted an unrelated CS8604 warning in `StrictContentStreamParserTests.cs`. This establishes template behavior only, not Linux browser deployment or invoice quality.

To compete for this purchase, ZingPDF should publish and test a production Linux invoice example with long tables, repeated headers, embedded fonts, disabled runtime networking and declared browser installation. Browser configuration, print settings, bounded concurrency and cancellation should have documented supported controls. Measure cold and warm behavior instead of claiming general generation speed. A public converter injection seam could help customers use an already-managed browser worker; that is a proposed product change, not an available capability asserted here.

Adding a second native layout engine is not necessary to win every generation use case. An honest boundary between coordinate authoring and browser-driven document layout, followed by reproducible deployment evidence, is a smaller and more immediately testable improvement. ZingPDF's editing and inspection features offer little purchasing value in this particular workflow; bundling them alone does not outweigh layout and operational fit.
