# Evaluation 2: insurance onboarding document workflow

Evaluation date: 9 October 2026. Independent desk evaluation of a hypothetical buyer; requirements and targets below are scenario assumptions, not customer research or measured vendor results.

## Recommendation

Choose **commercial Syncfusion Document SDK, including its PDF, PDF-to-image and OCR components, as the preferred proof-of-concept candidate**. Keep Aspose.PDF plus Aspose.OCR as the runner-up. No production purchase is approved by this report: representative-document fidelity, deployment, security, licensing and cost gates remain open for every vendor.

This buyer needs to manipulate PDFs received from other organizations, not merely generate its own templates. Syncfusion documents the complete required workflow within one vendor's .NET product family, including Linux OCR and rendering. That reduces integration uncertainty relative to ZingPDF's documented image-only OCR boundary and iText's separate rendering CLI. This is an inference about implementation effort, not a demonstrated reliability or speed advantage.

ZingPDF remains worth a small challenge trial if price is substantially lower and the buyer accepts a custom full-page OCR adapter. Its current packaged OCR workflow does not satisfy the coverage requirement directly. The decision can change if corpus tests or commercial quotes favor another stack.

## Buyer and workload

An insurance onboarding SaaS has eight developers and US$10 million annual revenue. Customers upload insurer proposal forms, identity documents and supporting evidence. The application fills approved AcroForm templates, flattens completed forms, combines them with supporting PDFs, rotates pages, applies an application-reference watermark, generates previews and extracts text for search. Scanned documents require OCR. Documents remain inside the buyer's confidential-data environment.

Assume 20,000 onboarding cases per month, 100,000 input PDFs and 600,000 pages; 30% of pages need OCR. Ordinary documents contain 1–30 pages; large uploads reach 200 pages or 100 MB. Deployment is Linux x64 .NET 8 in Debian-based containers, with four 4-vCPU/8-GiB workers. Production can process documents with outbound network denied after deployment. These are a capacity-planning envelope to validate, not claims about any library.

Dynamic XFA, handwriting recognition, browser form editing, PDF/A, PDF/UA and PAdES are outside scope. Already digitally signed uploads are retained unchanged as originals; a transformed working copy is labelled as such. Flattening is neither cryptographic integrity protection nor a guarantee that PDF content cannot subsequently be edited. This workflow does not promise sanitization or redaction of uploaded documents.

## Requirements and acceptance gates

| ID | Requirement | Acceptance evidence |
|---|---|---|
| G1 | Proprietary multitenant SaaS licensing | Written scope for all eight developers, staging, CI, autoscaling workers and customer access; dependency notices and renewal/expiry rights accepted. Commercial iText only for this buyer. |
| G2 | Confidential processing in Linux containers | All steps pass in the pinned production image without egress, including licensing and OCR model loading. Network capture shows no document transmission. Native packages and fonts are included in the inventory. |
| G3 | Correct AcroForm results | On 50 insurer templates, fill text, checkbox, radio, combo and list fields, save, reopen and flatten. Required values and selections survive; no blank, clipped or stale required-field appearances in Acrobat and an independent renderer. Test long names, accents, multiline text, inherited fields and repeated widgets. Unsupported forms must be detected and routed to review. |
| G4 | Preserve document content | Merge, reorder, rotate and watermark 150 varied PDFs. Page count/order/geometry match the requested operation; no missing text, images or annotations outside the declared transformation. Detect encrypted and signed inputs explicitly. Compare original/transformed renders, allowing only intended changes. |
| G5 | Preview and search coverage | Render every accepted page at 150 dpi. On 100 digital-text PDFs, recover at least 99.5% of normalized reference tokens. On 100 clean printed 300-dpi English scans, OCR character error rate is at most 2%; adverse scans go to review. Mixed vector/image, tiled scan, rotated, CCITT and JBIG2 cases must be exercised through full-page rendering where image extraction is insufficient. No page silently reported complete after processing only a logo or one tile. |
| G6 | Controlled resource use and failure | Native pipeline, excluding OCR, p95 under 5 seconds for the ordinary 20-page case; OCR p95 under 3 seconds per clean 300-dpi page. Measure on the declared worker profile with four concurrent jobs. Worker RSS below 2 GiB per ordinary job; 100-MB/200-page jobs finish below 6 GiB or fail explicitly. A 60-second job deadline can terminate a stuck worker process. |
| G7 | Operational support | Agreed business-day response target for reproducible blocking defects, a security-reporting route, and documented supported package/runtime versions. Logs contain job references and errors, not document text or images. Retrying cannot duplicate or overwrite a completed case artifact. |

G3–G5 require zero critical output failures in the acceptance corpus, with each rejection accounted for. A small corpus pass does not prove universal PDF compatibility; supported upload limits and manual-review routing remain product controls. G6 targets must be renegotiated before procurement if representative scans exceed them for all candidates. Isolation, retention and idempotency belong to the application rather than the PDF API.

## Evidence and shortlist

Evidence grades: **A** means a test or experiment was executed; **B** means an implementation was inspected or an official capability/license statement was checked, with its scope stated; **C** means evaluator inference; **U** means unverified buyer-specific behavior. A smoke-suite pass is narrower evidence than a representative corpus pass, even though both would receive grade A.

ZingPDF observations concern checkout commit `5c421a9828dc3f88b891ed81e25976303720ea4a`, not a confirmed released NuGet build. No competing library was installed or benchmarked here. No confidential customer corpus was available.

| Candidate | Fit and evidence | Remaining gate / shortlist result |
|---|---|---|
| Syncfusion commercial Document SDK | B: AcroForm fill/flatten, text extraction, merge/watermarks, Linux rendering and OCR are documented. Separate PDF-to-image and OCR packages are required; native deployment is still relevant. | Preferred POC candidate. U: real templates, OCR accuracy, offline operation, support terms and SaaS quote. Community licensing is inapplicable. |
| Aspose.PDF + Aspose.OCR | B: form flattening and .NET 8 Docker are documented; OCR is a separately licensed product with Linux support. OEM license categories explicitly cover SaaS. | Runner-up. Validate PDF and OCR together; request suitable eight-developer/OEM scope. No measured reason to claim inferior fidelity. |
| Commercial iText Core + rendering + OCR | B: commercial/AGPL choice and volume subscription licensing are documented. pdfOCR has .NET installation guidance; current pdfRender .NET guidance uses its CLI rather than a native C# renderer. | Eligible alternate if already standardized on iText. Extra renderer process and licensing scope create implementation work; this scenario does not need its advanced compliance capabilities. No inferred incompatibility with Linux. |
| ZingPDF + ZingPDF.OCR | B: forms, render API and OCR implementation were inspected; support matrix documents forms, merge, rotation and text watermarks. OCR is restricted to usable image XObjects. A: existing Windows smoke suite passes, within the scope described below. | Not a direct G5 pass. Trial only with declared application-owned rendering/OCR adaptation and written SaaS/support clarification. All remaining gates U. |

Syncfusion's [AcroForm documentation](https://help.syncfusion.com/document-processing/pdf/pdf-library/net/working-with-forms) describes filling and flattening, including text auto-resizing. Its [text extraction documentation](https://help.syncfusion.com/document-processing/pdf/pdf-library/net/working-with-text-extraction) describes page/document extraction. [PDF overview](https://help.syncfusion.com/document-processing/pdf/pdf-library/net/overview) documents merge and watermark capabilities. Its [Linux PDF-to-image guidance](https://help.syncfusion.com/document-processing/pdf/conversions/pdf-to-image/net/convert-pdf-file-to-image-in-asp-net-core) requires Linux SkiaSharp assets. Its [OCR getting-started guidance](https://help.syncfusion.com/document-processing/data-extraction/net/ocr-processor/getting-started-overview) covers Linux and Docker and lists native Linux prerequisites. These establish feature routes, not acceptance performance.

Aspose supplies [form flattening examples](https://docs.aspose.com/pdf/net/manipulate-pdf-document/) and a [.NET 8 Docker example](https://docs.aspose.com/pdf/net/docker/dotnet8/). [Aspose.OCR system requirements](https://docs.aspose.com/ocr/net/system-requirements/) include Linux. Its [license-type policy](https://purchase.aspose.com/policies/license-types) differentiates internal and OEM/SaaS use and distinguishes included support from optional paid support. The purchase must name both products and the actual developer coverage.

iText's [buying guide](https://itextpdf.com/how-buy) offers commercial volume and OEM arrangements. Its [AGPL guidance](https://itextpdf.com/how-buy/AGPLv3-license) says proprietary applications that cannot meet those terms need commercial licensing. Its [.NET pdfRender guidance](https://kb.itextpdf.com/itext/how-do-i-use-pdfrender-in-a-net-environment) describes CLI integration; [.NET pdfOCR installation](https://kb.itextpdf.com/itext/installing-itext-pdfocr-for-net-developers) flags platform runtime dependencies. A license for Core alone is not assumed to cover all required components.

## What the ZingPDF checkout actually establishes

The [support matrix](../../project/SUPPORT.md) lists .NET 8, AcroForms, flattening, page operations, text extraction and PDFium-backed PNG rendering. It explicitly asks buyers to validate rendering on each platform, describes limited high-level font writing and richer text-field support, and advises against sharing a `Pdf` instance between concurrent operations.

[Form.FlattenAsync](../../../src/ZingPDF/Elements/Forms/Form.cs) moves existing widget appearances into ordinary page content and removes form structure. The smoke test `Form_FlattenAsync_RemovesInteractiveFormStructure` in [PdfTests.cs](../../../tests/ZingPDF.Tests.Smoke/PdfTests.cs) fills a field, saves, reopens, and checks that forms/widget annotations are removed. That test does not establish rendered-value fidelity across external insurer forms. Other inspected tests cover checkbox/radio/choice selections surviving save and canonical button appearance structure. These are useful implementation evidence, not corpus-wide results.

[PdfOcrExtensions.cs](../../../src/ZingPDF.OCR/PdfOcrExtensions.cs) returns existing embedded text when any nonblank text exists under the default option. Otherwise [PageImageExtractor.cs](../../../src/ZingPDF.OCR/PageImageExtractor.cs) selects the single image candidate with the largest pixel area from the page resources. It does not render the complete page. A page containing a digital header and scanned body can therefore return only the header; a tiled scan can process only one tile. The [OCR package README](../../packages/README.OCR.md) states this boundary. Raw conversion handles common 8-bit RGB/grayscale streams, with JPEG/JPEG 2000 passthrough; this is not general full-page support for every scan encoding. These are inspected implementation limitations, not results from fabricated failure files.

The OCR smoke tests use a `DelegateOcrEngine` and establish routing/image-selection behavior. They do not verify native Tesseract accuracy or Linux packaging. The shared evaluation ran `dotnet test tests/ZingPDF.Tests.Smoke/ZingPDF.Tests.Smoke.csproj --configuration Release --logger "trx;LogFileName=technical-evaluation-smoke.trx"` on Windows with .NET SDK 10.0.301: **104 passed, 0 failed, 0 skipped**, reported test duration 16 seconds. This confirms the existing suite for the inspected checkout; it does not close Linux, visual-corpus or real OCR gates.

ZingPDF's [commercial terms](../../legal/COMMERCIAL_TERMS.md) cover developer seats, generally offered companion packages and continuing rights to versions released during paid terms. The [EULA](../../legal/EULA.md) restricts offering the software as a service or competing offering, so this customer-facing application should obtain written scope clarification. The [support policy](../../project/SUPPORT_POLICY.md) does not guarantee default turnaround times. Neither pricing labels nor an informal support statement close those contract gates.

## Procurement process and decision rule

1. Have engineering, operations, security and procurement agree the requirements and rejection policy before demos. Freeze an anonymized corpus and expected values/renders with input hashes. Do not accept a vendor's chosen sample as the only test.
2. Request package/version lists, temporary full-capability licenses, native dependency inventories and written SaaS/support quotes for Syncfusion and Aspose. Keep commercial iText as a reserve. Ask ZingPDF for the same facts plus a sample showing full-page OCR and external-form appearance handling.
3. Build a narrow adapter with identical operations for each finalist. Run the same corpus in isolated containers. Preserve input/output hashes, values, independent renders, token/OCR comparisons, timings and peak RSS. Record rejects and defects rather than hiding unsupported inputs.
4. Ask a reviewer who did not implement either adapter to examine blind-labelled output pages. Reproduce every critical difference. Submit at least one anonymized defect/support question to each vendor and record the actual response; vendor age or product breadth is not a measured support SLA.
5. Eliminate candidates failing any gate. Only then score fidelity/coverage 40%, deployment/operations 25%, support/legal scope 15%, three-year cost 15% and integration effort 5%. Fix weights before running tests. No numeric vendor scores are assigned today because the necessary measurements and quotes do not exist.
6. Choose the lowest-risk passing stack, sign its exact quote and run a limited production canary with manual review and rollback. If Syncfusion fails G3–G5 and Aspose passes, choose Aspose even at higher license cost. If all fail, retain manual processing and revise the architecture or accepted input scope.

## Three-year cost model

For each candidate calculate `TCO36 = license and renewal payments + component/add-on charges + support contract + implementation and upgrade hours × loaded hourly rate + incremental worker/storage cost + documented manual-review cost + funded exit cost`. Quote all amounts in one currency, state taxes and FX dates, and avoid assuming future price stability.

Syncfusion currently advertises Document SDK at **US$1,199 per developer per year** on its [pricing page](https://www.syncfusion.com/sales/pricing). At unchanged list price, eight seats over three years yield an illustrative **US$28,776** license subtotal. This is not a binding eight-seat SaaS/OCR/rendering quote: package coverage, any volume agreement, renewal rights and support must be confirmed. A preview PNG does not require a licensed interactive PDF viewer, which is separately priced. The [Community License rules](https://www.syncfusion.com/products/communitylicense) require under US$1 million annual gross revenue and at most five developers among other conditions; this buyer fails both thresholds.

For Aspose obtain `OEM PDF upfront + OEM OCR upfront + year-2/year-3 update subscriptions + paid support`; confirm whether a site offer is cheaper than eight individual entitlements. For iText obtain volume pricing on the actual licensed counting unit, duplicate processing/retries, growth and Core/OCR/rendering coverage. For ZingPDF obtain sufficient seats from current checkout or a quote, rather than infer an eight-seat price from Solo/Team/Business names; add the cost to build, validate and maintain full-page OCR adaptation. No measured labor or worker-cost estimates are available. Continuing-use rights can lower future license spending but freezing versions also freezes bug and security fixes.

## Implications for ZingPDF

The purchase obstacle is evidence and whole-workflow coverage, not the mere absence of a feature checklist. The next useful changes are:

- Supply a reproducible Linux container example with PDFium, Tesseract and language data pinned, plus an offline run and dependency inventory. Distinguish native OCR execution from the current delegate tests.
- Offer a documented rendered-page OCR path for mixed text/scans and tiled/unsupported image encodings, or clearly declare the required application adapter. Make partial extraction observable so a header or logo cannot masquerade as complete page text.
- Publish visual fill/save/flatten comparisons for external forms, covering inherited fields, missing/stale `/AP` appearances, checkbox/radio export values, clipped multiline text and rotation. Structural widget removal alone does not answer the buyer's concern.
- Clarify multitenant document-workflow SaaS rights, eight-developer licensing and optional support response commitments in a quote buyers can approve.
- Compete on measured total implementation cost after the same corpus passes. A smaller license bill cannot compensate for silent missing content in an insurance application.

ZingPDF could win a narrower approved-template workflow today if external scans are routed through a separately validated renderer/OCR pipeline and the legal/support gates close. For this broader scenario, Syncfusion has the clearer documented route into the POC; production selection remains contingent on evidence.
