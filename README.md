# ZingPDF

ZingPDF is a proprietary .NET PDF library for reading, editing, and saving PDF files.

This repository README is intentionally maintainer-focused. End-user integration guidance lives in the website docs and generated API reference.

## Licensing

ZingPDF is proprietary software.

Commercial use requires an active paid subscription with sufficient seats or a separate commercial agreement. Modification, redistribution of source, and resale are not permitted unless expressly granted in writing. See `LICENSE.txt` for the current commercial license notice.

## Repository Purpose

This repository contains the source for:

- `ZingPDF`: the core PDF library
- `ZingPDF.Fonts`: font metrics and font-provider support
- `ZingPDF.FromHTML`: optional HTML-to-PDF helpers
- `website/`: the sales site, developer docs, and generated API reference
- `Tests/`: unit, integration, and smoke test coverage

## Developer Docs

Customer/developer-facing documentation lives outside this README:

- website docs: `website/docs.html`
- generated API reference: `website/api.html`
- support and compatibility notes: `SUPPORT.md`

## Build And Test

Common commands:

```powershell
dotnet build ZingPDF.sln
dotnet test ZingPDF.sln
dotnet pack ZingPDF\ZingPDF.csproj -o artifacts\pack-audit
./scripts/run-performance.ps1
./scripts/run-competitive-performance.ps1
```

Generate the website API reference with:

```powershell
pwsh ./website/generate-api-reference.ps1
```

Performance snapshots are written to `artifacts/performance/latest-summary.json` and `artifacts/performance/latest-summary.md`. Side-by-side library comparisons are written to `artifacts/performance-competitive/latest-summary.md` and `artifacts/performance-competitive/competitive-summary.md`. The dedicated GitHub Actions `Performance` workflow benchmarks both the pull request and its base branch on the same Windows runner, then fails if a scenario regresses by more than 10%.

## Packaging Notes

- the core NuGet package is `ZingPDF`
- companion packages `ZingPDF.Fonts` and `ZingPDF.FromHTML` also pack independently
- package license/readme assets are sourced from the repository root

## Status

Before broad commercial release, keep these assets aligned:

- `LICENSE.txt`
- `SUPPORT.md`
- package metadata in the `.csproj` files
- website docs and generated API reference
- `CHANGELOG.md`
