# ZingPDF

ZingPDF is a proprietary .NET library for reading, editing, and saving PDF files.

This repository contains the source for the core library, companion packages, website, and test suites. Customer-facing documentation lives in the website docs and generated API reference.

## Contents

- `ZingPDF`: core PDF APIs
- `ZingPDF.Fonts`: font metrics and font-provider support
- `ZingPDF.FromHTML`: HTML-to-PDF helpers
- `website/`: product site, docs, and generated API reference
- `Tests/`: unit, integration, smoke, and performance coverage

## Documentation

- website docs: `website/docs.html`
- generated API reference: `website/api.html`
- support and compatibility notes: `SUPPORT.md`

## Build

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

## Release Automation

The single release workflow at `.github/workflows/release.yml` handles both automatic and manual releases.

Pushes to `main` create an automatic patch release, update `CHANGELOG.md`, tag the repo, publish a GitHub release, and push packages to NuGet.

If you need to override the version manually, trigger the workflow with a `version` input such as `2.0.0`. That is intended for deliberate major or minor bumps.

Before NuGet publishing will work, configure trusted publishing on nuget.org for this workflow file:

- `release.yml`

Also add a repository secret named `NUGET_USER` containing the nuget.org profile name that owns the packages.

## Licensing

ZingPDF is proprietary software. Commercial use requires an active subscription or a separate commercial agreement. See `LICENSE.txt` for the current license terms.
