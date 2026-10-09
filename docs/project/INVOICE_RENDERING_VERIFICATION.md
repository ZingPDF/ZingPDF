# Invoice rendering remediation

Evaluation follow-up: 9 October 2026. Draft implementation: [PR #13](https://github.com/ZingPDF/ZingPDF/pull/13).

The smallest procurement remediation is an operationally controlled HTML invoice route. `HtmlToPdfRenderer` accepts an installed browser, copies its configuration, reuses one browser process, gives each job a separate context, limits concurrency, waits for fonts and forwards cancellation. Its default is print media. Existing `Converter` helpers retain screen media and automatic provisioning. Liquid templates can use the public converter injection factory and `RendererHtmlToPdfConverter`.

`examples/GenerateInvoices` supplies an application-owned A4 template with repeated table headers, embedded Noto Sans, long descriptions and credit values. Its six cases contain 0, 1, 40 and 500 rows. The independent pypdf verifier checks ordered references, totals, searchable accented names and currency symbols, page size, repeated headers and font embedding. The font fixture is unchanged and the example includes its OFL notice.

## Executed Windows checks

| Check | Result |
| --- | --- |
| Release unit tests | 463 passed, zero failed or skipped |
| Release integration tests against installed Chrome | 18 passed, zero failed or skipped |
| Release smoke tests | 104 passed, zero failed or skipped |
| Six generated PDFs read with pypdf 6.10.0 | All passed; page counts 1, 1, 3, 26, 3 and 26 |
| Poppler rendering and visual review | 40-row invoice first page and 500-row invoice final page inspected; headers, wrapped rows and totals visible |
| Website copy check | Passed |
| API generation | Passed with zero warnings or errors; renderer and options pages served over HTTP |

The browser was Chrome 154.0.8037.99 and the application runtime was .NET 8.0.28. The example's first render took 908 ms; subsequent renders took 319–485 ms. These are observations from one sequential Windows run, not percentile results or comparative performance claims.

The new integration cases exercise print options, option snapshots, isolated concurrent contexts, offline embedded fonts, delayed font readiness, navigation waits, queued and active cancellation, timeout recovery, disposal, absolute data URLs and cancellation of stalled download headers/bodies. Real Chromium output also exposed an extraction loop on inline marked-content property dictionaries. The content scanner now consumes names and otherwise makes progress on stray delimiters; focused unit cases and the smoke suite cover the correction.

The signing smoke fixture previously calculated a leaf certificate's expiry using a later clock read than its issuer, intermittently exceeding the issuer's expiry by a second. It now uses the issuer's validity dates. Signing implementation behavior is unchanged.

## Linux evidence

The dedicated workflow runs browser integration tests and the corpus in a Debian .NET 8 container under UID 1654, with no networking, a read-only root filesystem, dropped capabilities, a 2-CPU/2-GiB limit and bounded temporary/shared memory. Browser installation and NuGet restoration happen during image build. The build stage uses the .NET 10 SDK because the existing source generator requires C# 13.

The workflow retains generated PDFs, independent validation, image/container configuration and installed package inventory. [Linux run 37875590324](https://github.com/ZingPDF/ZingPDF/actions/runs/37875590324) passed on implementation commit `b440a277768f35688c6b22bc738c49a59bc52413`. All 16 browser test cases passed with no skips, followed by all six independent PDF checks in the offline container. Page counts matched Windows. The downloaded artifact was verified again locally, and the Linux 500-row final page was rendered and visually inspected.

The worker used Debian 12, .NET 8.0.31 and Chromium package `154.0.8037.92-1~deb12u1`. Its first render took 1,290 ms; subsequent sequential renders took 420–810 ms. Built image ID: `sha256:9ca3e51a506b8b8acfe8fff576f1c2947bea42b19bacf72a9b3d387f1d92621b`. These times do not establish the eight-job p95 target.

[Evidence artifact 11591799870](https://github.com/ZingPDF/ZingPDF/actions/runs/37875590324/artifacts/11591799870) has archive digest `sha256:bcb718622797b86d39158206632ad38abe008a49ed13dad81e04a97dddeffb60`. GitHub retains it until 7 January 2027; archive an accepted release's evidence separately before that expiry.

## Procurement boundary

This supplies and tests the previously missing browser controls and concrete pagination/deployment path. It makes ZingPDF's HTML route a candidate for an invoice pilot. It does not complete the original company's purchase: finance must accept print samples, purchasing must accept licensing and support terms, and engineers must run the declared eight-job/10,000-job performance and isolation experiment against the competing finalists. The six-file corpus does not establish those load targets, three-year cost or zero operational failures.

CI restoration also reported existing ImageSharp 3.1.11 advisory warnings. This change does not upgrade that dependency; production approval requires disposition of those advisories alongside the existing dependency inventory.
