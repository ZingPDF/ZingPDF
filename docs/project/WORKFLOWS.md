# ZingPDF Workflows

Use this file for repeatable repo workflows that are easy to forget between sessions.

## Making Code Changes

1. Read the relevant source and nearby tests.
2. Identify the behavior boundary: parser, syntax object, public API, save pipeline, forms, text extraction, website, release, or packaging.
3. Make the narrowest change that fixes the root cause.
4. Add or update focused tests near the changed behavior.
5. Update public docs when the change affects public APIs, supported capabilities, package behavior, examples, guides, or product limits.
6. Run the narrow test project first.
7. Run `dotnet test ZingPDF.sln --configuration Release` when shared behavior, public APIs, save behavior, or package wiring changed.

## Testing Matrix

- Parser, syntax object, helper, font, and copy changes: start with `dotnet test tests/ZingPDF.Tests.Unit/ZingPDF.Tests.Unit.csproj`.
- Trailer, xref, save, encrypted file, object stream, and fixture-driven behavior: include `dotnet test tests/ZingPDF.Tests.Integration/ZingPDF.Tests.Integration.csproj`.
- End-to-end document operations or package-level confidence: include `dotnet test tests/ZingPDF.Tests.Smoke/ZingPDF.Tests.Smoke.csproj`.
- Public signing or signature validation behavior: include smoke coverage because validation depends on saved PDF bytes, `/ByteRange`, `/Contents`, and CMS payloads.
- Signing and encryption changes need smoke coverage for both signature validation and authentication/decryption paths. Do not model generic "encrypt an already signed PDF" as a high-level workflow; see `docs/project/SIGNING_ENCRYPTION_PLAN.md`.
- Fixture checkout concerns: run `pwsh ./scripts/assert-binary-fixtures.ps1`.
- Broad release confidence: run restore, Release build, and Release test against `ZingPDF.sln`.

## Website Copy

1. Read `website/STYLE_GUIDE.md`.
2. Edit copy so headings and sentences name concrete PDF operations, API calls, stream requirements, pricing facts, or benchmark numbers.
3. Avoid process-facing text, generic marketing adjectives, and audience-observer phrasing.
4. Run `pwsh ./website/check-copy.ps1`.
5. For visual changes, run `pwsh ./website/serve-local.ps1` and inspect the changed pages in a browser.

## Documentation Updates

1. For public API changes, update XML documentation, `website/docs.html`, `website/capabilities.html`, relevant guides, package READMEs, and `docs/project/SUPPORT.md`.
2. For new guides, add the guide page, link it from `website/guides.html`, and cross-link related guides.
3. For capability limits, document both the available behavior and the not-yet-supported PDF cases.
4. Regenerate `website/api` with `pwsh ./website/generate-api-reference.ps1` before publishing API reference changes.

## API Reference

1. Update XML documentation in source if public API behavior changed.
2. Run `pwsh ./website/generate-api-reference.ps1`.
3. Preview generated output through a local static server. Do not rely on direct `file://` preview for `website/api`.

## Performance Work

1. Use `tests/ZingPDF.Performance` for benchmark scenarios.
2. Run `pwsh ./scripts/run-performance.ps1` for current results.
3. Use `pwsh ./scripts/compare-performance.ps1` when comparing against a baseline summary.
4. Update performance pages only from measured output.
5. The Performance PR workflow is path-filtered to core library, source generator, benchmark project, PDF fixtures, and benchmark scripts.

## Release Preparation

1. Confirm release-affecting changes are intentional.
2. Merge product changes to `main` through PRs.
3. When you are ready to publish one or more merged changes, run the Prepare Release workflow manually.
4. Merge release-preparation PRs with a `chore(release): prepare <version>` title and without `[skip release]`.
5. The Release workflow publishes only after release metadata has already been merged to `main`; it does not commit back to the protected branch.
6. Run `pwsh ./scripts/prepare-release.ps1` manually only when preparing release metadata outside automation.
7. The release workflow packs core, FromHTML, GoogleFonts, OCR, Templates, and Templates.LiquidHtml packages.
8. Use `[skip release]` on maintenance-only merge commits that should not become generated changelog entries.
9. Use `[skip deploy]` on maintenance-only merge commits that should not publish the website.
10. Cloudflare Pages deploys on `main` pushes unless `[skip deploy]` is present; use the marker when a maintenance merge should not publish the website.

## Capturing New Memory

- If a task reveals durable architecture knowledge, update `docs/project/ARCHITECTURE.md`.
- If a task reveals a recurring command or sequence, update this file.
- If a task reveals a rule for future agents, update `AGENTS.md`.
- If a task reveals public copy style guidance, update `website/STYLE_GUIDE.md`.
- Keep these notes short. Future agents need fast orientation, not a second codebase.
