# Invoice generation with an installed browser

This .NET 8 example renders six PDFs: invoices with 0, 1, 40 and 500 rows, a 40-row credit note and a 500-row account report. The company-controlled Liquid HTML template repeats table headers, keeps each line together and embeds Noto Sans Regular. The font travels with the published application under the SIL Open Font License in `OFL.txt`.

The renderer uses an explicitly installed Chromium executable, print media, A4 CSS page sizing, background printing, bounded concurrency and a render timeout. It blocks page networking and forwards cancellation from the template. No browser download occurs at runtime. Finance supplies the amounts; this example does not calculate tax.

Run from the repository root with an installed Chrome or Chromium browser:

```powershell
$env:ZINGPDF_BROWSER_PATH = 'C:\Program Files\Google\Chrome\Application\chrome.exe'
dotnet run --project examples/GenerateInvoices --configuration Release -- output/invoices
python -m pip install pypdf==6.10.0
python scripts/verify-invoices.py output/invoices
```

`manifest.json` records expected rows, totals and elapsed render times. The independent Python reader checks the PDFs rather than trusting the rendering library. Its dependency is for verification only.

## Offline Linux container verification

Install Docker with Linux-container support, PowerShell 7 and Python, then run from the repository root:

```powershell
python -m pip install pypdf==6.10.0
./scripts/verify-invoices-linux.ps1
```

The Dockerfile builds with the .NET 10 SDK because the repository source generator requires C# 13. The published application targets .NET 8 and runs on the Debian .NET 8 runtime image. Debian Chromium is installed during the image build. Image building requires access to NuGet, the image registry and Debian package repositories. Rendering runs as UID 1654 with networking disabled, a read-only root filesystem, dropped capabilities, `no-new-privileges`, a writable output volume and temporary storage. The worker is limited to 2 CPUs, 2 GiB memory, 256 processes and 256 MiB shared memory. Its temporary filesystem is limited to 512 MiB.

The wrapper explicitly sets `ZINGPDF_NO_SANDBOX=1` for this isolated container because Chromium's sandbox can require privileges unavailable under those restrictions. This is an opt-in, not the application default. Use the browser sandbox for ordinary deployments; review the worker isolation before accepting untrusted HTML. This corpus uses an application-owned template and escaped model text.

Evidence is copied into a new `.tools/invoice-linux/<run-id>` directory: PDFs, manifest, independent verification output, the image/container configuration and installed package versions. The wrapper removes only its uniquely named container and volume. It keeps the image and evidence for inspection.

For a release candidate, pass the approved Chromium package version and immutable base-image digests:

```powershell
./scripts/verify-invoices-linux.ps1 -ChromiumVersion '<approved Debian version>' `
  -SdkImage 'mcr.microsoft.com/dotnet/sdk@sha256:<approved digest>' `
  -RuntimeImage 'mcr.microsoft.com/dotnet/runtime@sha256:<approved digest>'
```

The default .NET tags and Debian repositories change over time. Pinning an apt version fails if that version has left the configured repository; use an approved repository snapshot or retained built image when reproducibility is required. Record the built image ID or registry digest, package inventory and successful corpus before deployment. Apply browser security updates through a rebuilt image and rerun verification. The default recipe does not promise byte-for-byte reproducible builds.

The `Invoice rendering` workflow runs browser integration tests and this offline container corpus on pull requests affecting the implementation, and supports manual runs. Local Windows verification does not establish a Linux result: the Linux recipe must run successfully on a Docker Linux host before recording that gate as passed.

## Acceptance limits

The corpus tests searchable text, embedded fonts and table pagination. Review rendered pages for clipping and overlap, including page boundaries, before approving a template. Elapsed times in the manifest describe one run and do not establish the procurement targets for eight concurrent jobs, 10,000 seeded jobs, peak memory or comparative three-year cost. Record those measurements and finance's visual acceptance separately.
