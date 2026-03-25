# ZingPDF Sales Site

This directory contains a very small static SPA for selling commercial access to ZingPDF.

## Why this approach

- no framework dependency
- no build step
- easy static hosting
- hosted checkout links keep payment maintenance low
- generated API reference is emitted as static HTML

## Recommended payment setup

The site is currently configured for Lemon Squeezy checkout overlays.

This is a strong low-maintenance default because Lemon Squeezy offers hosted checkout and acts as merchant of record.

## How to configure

Edit `config.js` and update:

- `supportEmail`
- `licenses[].checkoutUrl`
- pricing copy and plan names if needed

The page loads `https://assets.lemonsqueezy.com/lemon.js` once and automatically applies the `lemonsqueezy-button` class to non-enterprise checkout buttons.

If a checkout URL still contains `your-store`, the button will stay in placeholder mode.

## How to run locally

Any static server will work. For example:

```powershell
cd C:\Users\tom\dev\ZingPDF\website
python -m http.server 8080
```

Then open `http://localhost:8080`.

## API reference generation

The developer guide in `docs.html` is curated by hand.

The API reference in `api.html` is generated from the library XML docs plus source signatures:

```powershell
pwsh ./generate-api-reference.ps1
```

The script builds `ZingPDF` in `Release`, reads `ZingPDF.xml`, and writes a static `api.html` file that is safe to deploy on any static host.

## How to deploy

This site can be deployed to:

- GitHub Pages
- Netlify
- Cloudflare Pages
- Vercel static hosting

Because the site is static, there is no backend deployment requirement unless you later add license-key provisioning or CRM automation.

## Cloudflare Pages deployment

This repo includes a GitHub Actions workflow at `.github/workflows/cloudflare-pages.yml`.

It uses Cloudflare Pages Direct Upload rather than Cloudflare's Git-based build pipeline.

This is intentional:

- the site itself is static
- `api.html` is generated from the .NET XML docs before deploy
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

On every push to `main` that changes the website, library, or solution files, GitHub Actions will:

1. set up .NET 8
2. run `pwsh ./website/generate-api-reference.ps1`
3. deploy the `website/` folder to Cloudflare Pages

### Custom domain

After the first successful deployment:

1. open the Pages project in Cloudflare
2. add your custom domain such as `zingpdf.com`
3. optionally add `www.zingpdf.com` and redirect it to the apex domain

Cloudflare will guide you through the required DNS records if your DNS is already on Cloudflare.

## Suggested next steps

1. Replace the placeholder support email.
2. Confirm the displayed prices match the Lemon Squeezy products.
3. Decide whether Solo and Team should remain one-time purchases or move to subscriptions.
4. Add a privacy policy and commercial terms page before going live.
