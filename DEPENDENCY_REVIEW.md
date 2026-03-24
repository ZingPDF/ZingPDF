# Dependency Review

This document captures the current dependency recommendations for ZingPDF from a commercial distribution perspective.

## Summary

Recommendation: keep third-party dependencies as NuGet packages rather than embedding their source into the repository.

Embedding usually makes dependency management harder by increasing update cost, license-tracking burden, review noise, and support responsibility.

For ZingPDF, the better path is:

- keep normal package references for stable low-risk dependencies
- reduce or remove unnecessary internal runtime dependencies over time
- review commercially sensitive licenses before shipping paid access

## Current package guidance

### Keep as package references

#### Microsoft.Extensions.DependencyInjection

Project:

- `ZingPDF/ZingPDF.csproj`

Assessment:

- low license risk
- broadly used and predictable
- currently used as an internal construction/detail-of-implementation choice

Recommendation:

- acceptable to keep short term
- consider removing later if you want a leaner runtime dependency surface for customers

Reason:

- this dependency is not a product differentiator and does add another runtime package to support

#### Nito.AsyncEx.Coordination

Project:

- `ZingPDF/ZingPDF.csproj`

Assessment:

- low license risk
- used heavily for `AsyncLazy`
- operationally straightforward

Recommendation:

- keep for now
- only replace if you decide to simplify async infrastructure later

#### SkiaSharp

Projects:

- `ZingPDF/ZingPDF.csproj`
- `ZingPDF.Fonts/ZingPDF.Fonts.csproj`

Assessment:

- acceptable from a licensing perspective
- meaningful operational/runtime dependency because it brings native concerns

Recommendation:

- keep as a package reference
- do not embed source
- document any platform/runtime expectations clearly for customers

### Review before commercial release

#### SixLabors.ImageSharp

Project:

- `ZingPDF/ZingPDF.csproj`

Assessment:

- highest commercial review priority in the current dependency set
- used in image metadata handling, JPEG recompression, and DCT/image paths

Recommendation:

- perform a dedicated legal/license review before selling ZingPDF commercially
- if the license terms are not acceptable for your business model, replace it before release

Reason:

- this is the clearest current dependency with potentially material commercial licensing implications

#### MorseCode.ITask

Project:

- `ZingPDF/ZingPDF.csproj`

Assessment:

- niche dependency
- appears old and no longer strategically desirable
- parser layer depends on it broadly

Recommendation:

- plan a migration to `Task` or `ValueTask`
- remove this dependency from the core library when practical

Reason:

- even if licensing is acceptable, it increases maintenance risk and makes the codebase feel less standard for new contributors and customers

### Fine as isolated dependencies

#### Microsoft.CodeAnalysis.CSharp

Project:

- `ZingPDF.InheritableSourceGenerator/ZingPDF.InheritableSourceGenerator.csproj`

Assessment:

- build-time only
- not part of the runtime dependency story for library consumers

Recommendation:

- fine to keep as-is

#### PuppeteerSharp

Project:

- `ZingPDF.FromHTML/ZingPDF.FromHTML.csproj`

Assessment:

- optional component
- operationally heavier because it drives browser automation

Recommendation:

- keep isolated in the separate `ZingPDF.FromHTML` package/project
- do not fold this into the core `ZingPDF` package unless the business case is very strong

#### ABCpdf

Project:

- `Tester/Tester.csproj`

Assessment:

- local/tester utility concern rather than customer-facing runtime dependency

Recommendation:

- no action needed for the core commercial package

## General recommendation on embedding dependencies

Do not vendor or embed third-party package source into the repository by default.

This would usually make life harder, not easier, because it:

- slows security and bug-fix updates
- increases license compliance obligations
- adds maintenance overhead
- makes future upstream merges and audits harder

Only consider embedding dependency source if there is a very specific reason such as:

- the dependency is tiny and extremely stable
- you need a permanent patch upstream will not accept
- you must avoid host-environment dependency conflicts

## Suggested follow-up actions

1. Review the commercial implications of `SixLabors.ImageSharp`.
2. Replace `MorseCode.ITask` with standard .NET async primitives.
3. Consider removing `Microsoft.Extensions.DependencyInjection` from the core library if you want a smaller runtime surface.
4. Keep browser automation isolated in `ZingPDF.FromHTML`.
5. Consider introducing `Directory.Packages.props` for centralized package version management.
