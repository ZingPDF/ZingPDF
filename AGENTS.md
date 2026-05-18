# ZingPDF Agent Notes

## Project Shape

- ZingPDF is a .NET 8 PDF library for reading, editing, saving, and generating PDF files in C#.
- Core library code lives in `src/ZingPDF`.
- Companion packages live in `src/ZingPDF.FromHTML`, `src/ZingPDF.GoogleFonts`, `src/ZingPDF.OCR`, and `src/ZingPDF.Fonts`.
- Tests live in `tests`: unit, integration, smoke, common fixtures, and performance projects.
- Runnable examples live in `examples`.
- Product site, docs, guides, and generated API reference live in `website`.
- Release, benchmark, and maintenance scripts live in `scripts`.

## Default Workflow

1. Inspect relevant source and tests before editing.
2. Prefer the smallest change that fixes the root cause.
3. Preserve existing public API shape unless the task explicitly asks for an API change.
4. Add or update focused tests for parsing, object model, save, text extraction, form, signing, signature validation, encryption, redaction, and page-editing behavior.
5. Update public docs when the change affects public APIs, supported capabilities, package behavior, examples, guides, or product limits.
6. Run the narrowest relevant test project first, then broader checks when shared behavior changed.
7. Summarize changed files, verification commands, and any remaining risk.

## Common Commands

- Restore: `dotnet restore ZingPDF.sln`
- Build: `dotnet build ZingPDF.sln --configuration Release`
- All tests: `dotnet test ZingPDF.sln --configuration Release`
- Unit tests: `dotnet test tests/ZingPDF.Tests.Unit/ZingPDF.Tests.Unit.csproj`
- Integration tests: `dotnet test tests/ZingPDF.Tests.Integration/ZingPDF.Tests.Integration.csproj`
- Smoke tests: `dotnet test tests/ZingPDF.Tests.Smoke/ZingPDF.Tests.Smoke.csproj`
- Binary fixture check: `pwsh ./scripts/assert-binary-fixtures.ps1`
- Website copy check: `pwsh ./website/check-copy.ps1`
- Generate API reference: `pwsh ./website/generate-api-reference.ps1`
- Run website locally: `pwsh ./website/serve-local.ps1`
- Performance run: `pwsh ./scripts/run-performance.ps1`

## Engineering Preferences

- Keep changes scoped. Avoid broad refactors unless they directly reduce risk for the requested change.
- Favor PDF-specific facts over generic abstractions. When behavior depends on the PDF spec, name the object, dictionary entry, stream, filter, xref, trailer, page tree, annotation, or content stream operation involved.
- Preserve user or generated changes already in the working tree. Do not restore deleted files unless explicitly asked.
- Treat binary fixtures carefully. PDF, image, and font fixtures should not be touched casually.
- Prefer structured parsers, dictionaries, object wrappers, and existing helper APIs over ad hoc string manipulation.
- Avoid new dependencies unless the existing stack cannot reasonably solve the task.
- Public APIs should have XML documentation that names concrete behavior, constraints, and stream requirements.
- When changing save, encryption, xref, object stream, signature validation, signing, or page tree behavior, expect integration or smoke coverage in addition to unit tests.

## Website And Copy

- Follow `website/STYLE_GUIDE.md` for public-facing copy.
- The website voice is plain technical English. It should name real PDF operations, API calls, constraints, pricing facts, and measured benchmark results.
- Avoid generic SaaS language and soft adjectives such as "robust", "powerful", "flexible", "seamless", and "strong" unless backed by a concrete fact.
- Before finishing website copy changes, run `pwsh ./website/check-copy.ps1`.
- When public API or product capability changes, update `website/docs.html`, `website/capabilities.html`, relevant guides, package READMEs, and `docs/project/SUPPORT.md`; regenerate `website/api` when XML docs need to be published.
- Legal pages are not marketing copy. Do not rewrite them for tone unless the requested change preserves legal meaning.

## Release And CI Notes

- CI runs on Windows, Ubuntu, and macOS for pull requests.
- Release packaging is driven by `Directory.Build.props`, `CHANGELOG.md`, `scripts/prepare-release.ps1`, and `.github/workflows/release.yml`.
- `VersionBase` is the stable version root; default local builds append `-dev`.
- DocFX is restored through `dotnet-tools.json` and used for the generated API reference.
- Cloudflare Pages deploys the static `website` directory after API reference generation.

## Known Traps

- Default `SaveAsync` writes incremental updates. Calling `RemoveHistoryAsync()` rewrites the file with only latest live objects.
- Save paths require writable, seekable output streams; when saving to a different stream, the output stream must be empty.
- Page numbers in public APIs are 1-based.
- PDF fixture files are binary test inputs. Preserve Git attributes and avoid line-ending transformations.
- Generated API reference output should be previewed through a local server, not direct `file://`, because browser module/search behavior can break.
- Website performance claims should be tied to measured benchmark output, not adjectives.

## Durable Context Files

- Repo workflow memory: this file.
- Architecture map: `docs/project/ARCHITECTURE.md`.
- Recurring workflows: `docs/project/WORKFLOWS.md`.
- Website voice: `website/STYLE_GUIDE.md`.
- Package READMEs: `docs/packages`.
- Support and compatibility notes: `docs/project/SUPPORT.md`.
