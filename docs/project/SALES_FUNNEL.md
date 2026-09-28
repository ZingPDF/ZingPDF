# ZingPDF sales funnel measurement

## What the site records

`website/app.js` uses the GA4 ID in `website/config.js` on the production site. Localhost traffic is excluded. Page views are automatic. These custom events add buying-path signals:

| Event | Trigger | What it means |
| --- | --- | --- |
| `pricing_link_click` | A site link to `#licenses` is clicked | Intent to inspect pricing; the section may not yet have been seen |
| `pricing_view` | At least 25% of the pricing section enters the viewport | Pricing section was exposed once on that page load; it does not prove the Stripe iframe rendered |
| `resource_click` | A site link to ZingPDF on NuGet or GitHub is clicked | Interest in package or source; `resource` is `nuget` or `github` |
| `contact_sales_click` | The direct-contact button is clicked | Contact dialog opened; no email is sent yet |
| `sales_email_click` | The email link inside that dialog is clicked | Email client requested; no email delivery is confirmed |
| `checkout_return` | A recognised `?checkout=success` or `?checkout=cancelled` URL is loaded | A return URL was loaded; `result` holds the status, but neither status verifies a Stripe payment |

The site does **not** record a `begin_checkout` or `purchase` GA4 event. Subscribe buttons are inside Stripe's cross-origin Pricing Table iframe, so this static page cannot reliably observe those clicks. A return URL can be opened without a payment. Stripe payment and subscription records are the source of truth for purchases.

No event sends email addresses, payment details, Checkout Session IDs, or arbitrary URL query strings as custom parameters. The site removes `checkout` from a recognised return URL before initializing GA4's page view.

## Stripe configuration

On 28 September 2026, the live Stripe Pricing Table `prctbl_1TMfzpL6fCXAz4BdbX1euTjF` was configured to redirect successful payments for all six Solo, Team, and Business prices to `https://zingpdf.dev/?checkout=success`. The Stripe product feature lists were updated to say `Keep paid-term versions indefinitely`. The table preview showed the feature on all three plans at their existing AUD monthly prices. Check these settings again if the table or prices are replaced.

The embedded table's text is managed in Stripe, not in this repository. Stripe payment and subscription records remain the authority for completed sales. Do not mark `checkout_return` as a purchase or key event without server-side Checkout Session verification. The site has a `?checkout=cancelled` banner, but this Pricing Table has no corresponding cancelled-payment redirect configured.

## Weekly diagnosis

Use one date range and time zone for all sources. In GA4, compare landing pages and traffic sources with `pricing_view`, `pricing_link_click`, `resource_click`, and `contact_sales_click`. GA4 Realtime or DebugView can verify that the events arrive after deployment. Create event-scoped custom dimensions for `resource` and `result` only if their breakdowns are needed in reports; the event counts work without them.

Compare that with Stripe's completed subscription payments and available Checkout Session records for the same period. NuGet downloads are useful for interest over time, but are not unique people or evidence of commercial use. Do not divide purchases by NuGet downloads as a conversion rate.

| Observation | First question to investigate |
| --- | --- |
| Few relevant landing-page visits | Is distribution reaching developers with a commercial PDF workflow? |
| Many visits, few `pricing_view` events | Are guide visitors reaching the commercial offer? |
| Pricing views, few Stripe checkout sessions | Is the offer, license wording, or Stripe table blocking intent? |
| Checkout sessions, few paid subscriptions | Where are checkout sessions being abandoned or rejected? |
| Paid subscriptions, no `checkout_return` | Is Stripe using its own confirmation page rather than the optional redirect? |

Read patterns across several weeks and look at absolute counts. A zero-sales period with a small number of qualified pricing views is weak evidence about price or product-market fit.
