# ZingPDF Sales Site

This directory contains a very small static SPA for selling commercial access to ZingPDF.

## Why this approach

- no framework dependency
- no build step
- easy static hosting
- Stripe Checkout Sessions can run through a small Pages Function without exposing secrets to the browser
- generated API reference is emitted as a static DocFX site

## Recommended payment setup

The site is currently configured for Stripe Checkout Sessions through a Cloudflare Pages Function.

This keeps payment handling off the site while still allowing monthly and annual billing from one pricing UI.

Stripe is now the source of truth for both pricing amounts and plan mapping. The frontend fetches display pricing from a Pages Function backed by Stripe metadata, and checkout uses that same metadata lookup.

## How to configure

Edit `config.js` and update:

- `supportEmail`
- plan names and marketing copy if needed

Then configure these Cloudflare Pages environment variables for the deploy:

- `STRIPE_SECRET_KEY`

Optional:

- `STRIPE_SUCCESS_URL`
- `STRIPE_CANCEL_URL`

The website pricing cards read their displayed amounts from Stripe through `/api/pricing-catalog`, so you do not need to maintain numeric prices in `config.js`.

Stripe metadata should be set like this:

- on each Stripe product:
  - `site_plan_id=solo|team|business`

The site infers monthly vs annual directly from the Stripe recurring interval on each price.

## How to run locally

Any static server will work. For example:

```powershell
cd C:\Users\tom\dev\ZingPDF\website
python -m http.server 8080
```

Then open `http://localhost:8080`.

You can also use the helper script:

```powershell
powershell -ExecutionPolicy Bypass -File .\serve-local.ps1
```

## Editorial guardrails

Public-facing copy should follow [`STYLE_GUIDE.md`](./STYLE_GUIDE.md).

Before deploying copy changes, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\check-copy.ps1
```

The script flags owner-facing copy, audience-observer phrasing, meta-writing filler, and a small set of weak marketing adjectives.

## API reference generation

The developer guide in `docs.html` is curated by hand.

The API reference in `api/` is generated from the library source and XML docs using DocFX:

```powershell
pwsh ./generate-api-reference.ps1
```

The script restores the local DocFX tool, generates metadata from `ZingPDF.csproj`, and writes a static site into `api/` that is safe to deploy on any static host.

Important: the generated DocFX API site should be previewed through a local web server such as `python -m http.server`. Opening `api/index.html` directly with `file://` will trigger browser CORS/module restrictions and break search and other frontend behavior.

## How to deploy

This site can be deployed to:

- GitHub Pages
- Netlify
- Cloudflare Pages
- Vercel static hosting

The site is mostly static, with one small Pages Function for Stripe Checkout Session creation.

## Cloudflare Pages deployment

This repo includes a GitHub Actions workflow at `.github/workflows/cloudflare-pages.yml`.

It uses Cloudflare Pages Direct Upload rather than Cloudflare's Git-based build pipeline.

This is intentional:

- the site itself is static
- `api/` is generated from the .NET source and XML docs before deploy
- GitHub Actions is a better place to run the .NET build step than Cloudflare's Pages build image

### One-time Cloudflare setup

1. In Cloudflare, go to Workers & Pages and create a new Pages project.
2. Choose **Direct Upload**.
3. Give the project a name such as `zingpdf`.
4. Do not upload files manually if you do not want to. The GitHub Actions workflow will handle future deploys.

### Cloudflare token setup

Create an API token in Cloudflare with permission to deploy to Pages.

The workflow expects these GitHub repository settings:

- secret: `CLOUDFLARE_API_TOKEN`
- secret: `CLOUDFLARE_ACCOUNT_ID`

The workflow deploys to a Pages project named `zingpdf`.

If you pick a different Pages project name, update `.github/workflows/cloudflare-pages.yml` to match.

### How deployment works

On every push to `main`, GitHub Actions will:

1. set up .NET 8
2. run `pwsh ./website/generate-api-reference.ps1`
3. deploy the `website/` folder to Cloudflare Pages

Cloudflare Pages will also deploy any functions under `website/functions/`.

### Custom domain

After the first successful deployment:

1. open the Pages project in Cloudflare
2. add your custom domain such as `zingpdf.dev`
3. optionally add `www.zingpdf.dev` and redirect it to the apex domain

Cloudflare will guide you through the required DNS records if your DNS is already on Cloudflare.

## Suggested next steps

1. Confirm the configured support email is correct.
2. Ensure each Stripe product has `site_plan_id`, and that the matching Stripe prices use standard monthly/yearly recurring intervals.
3. Confirm that Solo, Team, and Business pricing and subscription language match the commercial model.
4. Confirm the published legal pages match the current commercial terms.
