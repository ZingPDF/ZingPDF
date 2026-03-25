# Commercialization Checklist

This document is a working checklist for taking ZingPDF from "technically usable" to "commercially sellable".

## Recommended commercial model

For a developer-facing library like ZingPDF, the default recommendation is:

- publish the packages publicly on NuGet
- keep the binaries proprietary and source-closed
- sell commercial licenses directly
- allow evaluation without heavy technical enforcement
- enforce primarily through contract, support entitlement, and account management rather than hard runtime blocking

This is the best fit when adoption speed matters, integration should feel simple, and the product is a library rather than a hosted service.

### Why this is the recommended default

For a .NET library, every anti-abuse control has a developer-experience cost:

- private feeds complicate restore, CI, containers, caching, air-gapped installs, and renewals
- runtime license checks create deployment risk and support burden
- obfuscation makes debugging worse and does not stop determined abuse
- online activation is unpopular in build systems and regulated environments

That friction is paid by legitimate customers first.

In the early commercial stage, lost conversions from setup friction are usually a bigger risk than unpaid usage. Abuse will happen, but if the package is easy to trial, easy to adopt, and backed by clear legal terms, some "grey" usage often converts later through exposure, production reliance, procurement, or support needs.

### Recommended trust model

Use a light-trust commercial model first:

- package acquisition is frictionless
- usage rights are governed by license terms
- paid customers receive support, updates, and commercial assurances
- evaluation use is allowed under a clear trial/evaluation policy
- no mandatory phone-home requirement for normal operation

This keeps the product easy to adopt while preserving a path to monetization through:

- legal clarity
- support entitlement
- upgrade entitlement
- procurement approval
- enterprise trust

## What not to do for the first paid release

Unless abuse is already severe, avoid making these mandatory on day one:

- private NuGet feeds as the only delivery mechanism
- runtime network validation
- aggressive obfuscation as the core protection strategy
- machine-bound activation
- frequent license renewals that require redeployment

These controls add operational cost to:

- local development
- CI/CD
- containers
- offline environments
- customer support
- sales engineering

They also make trials materially harder.

## Practical model for ZingPDF

The recommended packaging and entitlement shape is:

### Distribution

- publish `ZingPDF` and related packages publicly on NuGet
- sign packages and document the official package IDs and publisher identity
- keep premium/support terms outside the package transport mechanism

### Licensing

- commercial proprietary license
- free evaluation allowed for non-production or time-limited trial use
- paid license required for production/commercial use
- support and updates included for a defined maintenance term

### Enforcement

- no hard runtime blocking initially
- optional license key or license file only for entitlement tracking, not for breaking execution in normal customer environments
- if a key is used, allow offline validation and long-lived renewals

### Trial experience

- same package and mostly same install path as paid customers
- no separate "trial build" if it can be avoided
- clear trial terms and upgrade path

### Enterprise option

- offer private-feed delivery only as an enterprise convenience feature, not as the default
- support offline license files for customers who require internal artifact control

## Decision framework

Use this framework when choosing whether to add stricter controls.

### Public NuGet with trust-first licensing

Best when:

- growth and adoption matter most
- customers are developers and want frictionless install
- product value becomes clear only after hands-on use
- support and updates are meaningful differentiators

Main downside:

- some unpaid production use will occur

Main upside:

- fastest path to trials, samples, blog posts, and organic adoption

### Private NuGet feed

Best when:

- most customers are already enterprise buyers
- procurement expects authenticated artifact access
- the buyer count is low and contract value is high

Main downside:

- significantly worse first-run experience
- extra work for CI, containers, token rotation, mirrors, and incident handling

This should usually be optional rather than mandatory.

### Built-in runtime license enforcement

Best when:

- abuse is measurable and materially harming revenue
- the product is mature enough to absorb support overhead
- customers can tolerate activation and entitlement workflows

Main downside:

- every edge case becomes your problem: offline builds, expired keys, clock drift, rotation, renewals, emergency deployments

This is more appropriate as a later-stage escalation than an initial default.

### Obfuscation

Useful only as a speed bump.

- it may deter casual inspection or trivial repackaging
- it will not stop determined abuse
- it reduces debuggability and can worsen customer trust

Use sparingly if at all, and not as the main business model.

## Escalation policy

Start with the low-friction model and add controls only when data justifies it.

Escalate from trust-first to stronger enforcement only if one or more of these become true:

- clear evidence of widespread unpaid production use
- support load from non-customers becomes expensive
- major customers explicitly require stronger entitlement controls
- channel abuse undermines pricing or partner relationships

Escalation order should be:

1. clearer legal terms and sales process
2. support/update entitlement gating
3. optional offline license files or customer portal issuance
4. selective enterprise private-feed delivery
5. hard runtime enforcement only if earlier steps are insufficient

This keeps the commercial burden proportional to actual abuse.

## Suggested first release model

For the first paid release of ZingPDF:

1. Publish public NuGet packages.
2. Keep installation identical for trial and paid users.
3. Ship a proper commercial license agreement and trial terms.
4. Sell per-developer or per-organisation commercial licenses, with support and upgrade entitlement bundled.
5. Do not require online activation.
6. Do not require a private feed for normal customers.
7. Track customers in CRM/invoicing rather than inside the runtime.
8. Revisit technical enforcement only after real usage data is available.

If a lightweight entitlement artifact is desired, prefer a license file that:

- is optional in evaluation mode
- is validated offline
- renews infrequently
- does not suddenly break production workloads because a renewal was missed

## Packaging and product recommendations specific to this repo

Based on the current repository shape:

- keep `ZingPDF` as the primary public package
- consider keeping higher-cost add-ons such as HTML conversion as separate packages or separate commercial tiers
- document exactly which packages are production-supported and under what terms
- avoid coupling package restore to entitlement unless a strong business reason emerges

The package structure already supports a good commercial story:

- a clean public API
- packaged metadata
- documentation and examples
- CI and release groundwork

That means the next leverage is more in licensing, positioning, and onboarding than in DRM.

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
  recommended default is per-developer seats for SDK use, with OEM/redistribution terms negotiated separately
- Sales channel:
  recommended default is direct sales plus public NuGet distribution
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

The current `LICENSE.txt` is a concise commercial license notice. A fuller customer-facing EULA and evaluation agreement are still recommended before broad release.

### 5. Decide delivery and entitlement enforcement

Options include:

- ship via private NuGet feed with authenticated access
- ship via public NuGet but require a purchased license key
- deliver source access separately for premium tiers

Recommended starting position:

- ship via public NuGet
- rely on contract and support entitlement first
- add optional offline license artifacts later only if needed

If you want enforcement in the library itself, that should be treated as a later-stage product decision, not a prerequisite for launch.

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

1. Finalize a commercial EULA plus explicit evaluation terms.
2. Launch with public NuGet distribution and no hard runtime activation.
3. Define support and upgrade entitlement for paying customers.
4. Clean the highest-risk nullable warnings in the core document/save/encryption paths.
5. Prepare a first tagged prerelease package for external evaluation.
6. Measure trial-to-paid conversion and evidence of abuse before adding technical controls.
