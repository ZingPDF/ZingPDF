# ZingPDF Workflows

Use this file for repeatable repo workflows that are easy to forget between sessions.

## Making Code Changes

1. Read the relevant source and nearby tests.
2. Identify the behavior boundary: parser, syntax object, public API, save pipeline, forms, text extraction, website, release, or packaging.
3. Make the narrowest change that fixes the root cause.
4. Add or update focused tests near the changed behavior.
5. Run the narrow test project first.
6. Run `dotnet test ZingPDF.sln --configuration Release` when shared behavior, public APIs, save behavior, or package wiring changed.

## Testing Matrix

- Parser, syntax object, helper, font, and copy changes: start with `dotnet test tests/ZingPDF.Tests.Unit/ZingPDF.Tests.Unit.csproj`.
- Trailer, xref, save, encrypted file, object stream, and fixture-driven behavior: include `dotnet test tests/ZingPDF.Tests.Integration/ZingPDF.Tests.Integration.csproj`.
- End-to-end document operations or package-level confidence: include `dotnet test tests/ZingPDF.Tests.Smoke/ZingPDF.Tests.Smoke.csproj`.
- Fixture checkout concerns: run `pwsh ./scripts/assert-binary-fixtures.ps1`.
- Broad release confidence: run restore, Release build, and Release test against `ZingPDF.sln`.

## Website Copy

1. Read `website/STYLE_GUIDE.md`.
2. Edit copy so headings and sentences name concrete PDF operations, API calls, stream requirements, pricing facts, or benchmark numbers.
3. Avoid process-facing text, generic marketing adjectives, and audience-observer phrasing.
4. Run `pwsh ./website/check-copy.ps1`.
5. For visual changes, run `pwsh ./website/serve-local.ps1` and inspect the changed pages in a browser.

## API Reference

1. Update XML documentation in source if public API behavior changed.
2. Run `pwsh ./website/generate-api-reference.ps1`.
3. Preview generated output through a local static server. Do not rely on direct `file://` preview for `website/api`.

## Performance Work

1. Use `tests/ZingPDF.Performance` for benchmark scenarios.
2. Run `pwsh ./scripts/run-performance.ps1` for current results.
3. Use `pwsh ./scripts/compare-performance.ps1` when comparing against a baseline summary.
4. Update performance pages only from measured output.

## Release Preparation

1. Confirm release-affecting changes are intentional.
2. Update `CHANGELOG.md` under `## [Unreleased]` with meaningful notes, or let `scripts/prepare-release.ps1` derive notes from commits.
3. Run `pwsh ./scripts/prepare-release.ps1` only when preparing release metadata.
4. The release workflow packs core, FromHTML, GoogleFonts, and OCR packages.

## Capturing New Memory

- If a task reveals durable architecture knowledge, update `docs/project/ARCHITECTURE.md`.
- If a task reveals a recurring command or sequence, update this file.
- If a task reveals a rule for future agents, update `AGENTS.md`.
- If a task reveals public copy style guidance, update `website/STYLE_GUIDE.md`.
- Keep these notes short. Future agents need fast orientation, not a second codebase.
