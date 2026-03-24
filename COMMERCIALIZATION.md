# Commercialization Checklist

This document is a working checklist for taking ZingPDF from "technically usable" to "commercially sellable".

## What is now in place

- Packaged library metadata in `ZingPDF/ZingPDF.csproj`
- NuGet package readme inclusion
- Package license file inclusion
- XML documentation for the main public API
- End-to-end README examples for the core document, form, encryption, and metadata APIs
- Tests covering page editing, save behavior, encryption, forms, and metadata
- `CHANGELOG.md` for release notes
- GitHub Actions CI workflow for build, test, and pack validation
- GitHub Actions release workflow that stamps an auto-incremented main-branch version into DLLs and the NuGet package
- `DEPENDENCY_REVIEW.md` with commercial dependency recommendations

## Decisions still required

These are business or legal decisions rather than coding tasks:

- License model:
  single commercial license, per-developer seats, per-server/runtime, OEM, or dual licensing
- Sales channel:
  direct invoicing, private NuGet feed, public NuGet with license key enforcement, or bundled SDK delivery
- Support policy:
  response-time targets, maintenance window, and upgrade entitlement period
- Version policy:
  what counts as patch, minor, and major compatibility changes
- Compatibility promise:
  supported .NET versions, supported OSes, and supported PDF feature scope

## Engineering work still recommended before first paid release

### 1. Reduce warning debt

The library builds and tests successfully, but the main project still has a meaningful nullable and XML-comment warning backlog. Before charging customers, it would be sensible to either:

- clean these up, or
- explicitly scope which warnings are accepted and why

### 2. Publish a support matrix

Document at least:

- target framework support
- Windows/Linux/macOS expectations
- encrypted PDF support scope
- form support scope
- known unsupported PDF features such as signing

### 3. Maintain package versioning and release notes

Now that `CHANGELOG.md` exists, keep it current for every release:

- publish SemVer-based versions
- note breaking changes prominently
- tag releases consistently with package versions

### 4. Add a customer-facing legal set

Common documents:

- commercial EULA or license agreement
- privacy policy if telemetry or license activation is ever added
- support policy / SLA document

The current `LICENSE.txt` is only a minimal placeholder notice, not a full commercial agreement.

### 5. Decide delivery and entitlement enforcement

Options include:

- ship via private NuGet feed with authenticated access
- ship via public NuGet but require a purchased license key
- deliver source access separately for premium tiers

If you want enforcement in the library itself, that needs a deliberate product decision before implementation.

### 6. Extend CI into release automation

The baseline CI workflow is now in place. Recommended next steps:

- publish symbols to the intended distribution channel
- publish packages to the intended feed
- archive package artifacts for each release
- enable branch protection on `main` if you want releases to happen only through reviewed merges

### 7. Add customer onboarding docs

Useful docs for buyers:

- installation guide
- upgrade guide
- supported scenarios / limitations
- troubleshooting guide

### 8. Act on dependency review findings

See `DEPENDENCY_REVIEW.md` for the current package-by-package recommendations.

Highest-priority follow-up items:

- review `SixLabors.ImageSharp` licensing for commercial distribution
- replace `MorseCode.ITask` with standard .NET async primitives
- consider shrinking the runtime dependency surface in the core package

## Sensible near-term next steps

1. Finalize the commercial license model.
2. Decide whether distribution is private-feed, public-feed-plus-license, or direct delivery.
3. Clean the highest-risk nullable warnings in the core document/save/encryption paths.
4. Add changelog and release workflow.
5. Prepare a first tagged prerelease package for external evaluation.
