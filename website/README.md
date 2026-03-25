# ZingPDF Sales Site

This directory contains a very small static SPA for selling commercial access to ZingPDF.

## Why this approach

- no framework dependency
- no build step
- easy static hosting
- hosted checkout links keep payment maintenance low

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

## How to deploy

This site can be deployed to:

- GitHub Pages
- Netlify
- Cloudflare Pages
- Vercel static hosting

Because the site is static, there is no backend deployment requirement unless you later add license-key provisioning or CRM automation.

## Suggested next steps

1. Replace the placeholder support email.
2. Confirm the displayed prices match the Lemon Squeezy products.
3. Decide whether Solo and Team should remain one-time purchases or move to subscriptions.
4. Add a privacy policy and commercial terms page before going live.
