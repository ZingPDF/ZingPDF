import { getCatalogPrice, getPricingCatalog } from "../_lib/stripe-pricing.js";

export async function onRequestPost(context) {
  const { request, env } = context;

  try {
    if (!env.STRIPE_SECRET_KEY) {
      return json({ error: "Stripe is not configured yet." }, 500);
    }

    const body = await request.json().catch(() => null);
    const planId = typeof body?.planId === "string" ? body.planId : "";
    const billingPeriod = body?.billingPeriod === "annual" ? "annual" : "monthly";
    const catalog = await getPricingCatalog(env);
    const price = getCatalogPrice(catalog, planId, billingPeriod);
    const priceId = price?.id || null;

    if (!priceId) {
      return json({ error: "This plan is not configured for checkout yet." }, 400);
    }

    const origin = new URL(request.url).origin;
    const successUrl = env.STRIPE_SUCCESS_URL || `${origin}/?checkout=success#licenses`;
    const cancelUrl = env.STRIPE_CANCEL_URL || `${origin}/?checkout=cancelled#licenses`;

    const params = new URLSearchParams();
    params.set("mode", "subscription");
    params.set("success_url", successUrl);
    params.set("cancel_url", cancelUrl);
    params.set("allow_promotion_codes", "true");
    params.set("billing_address_collection", "auto");
    params.set("line_items[0][price]", priceId);
    params.set("line_items[0][quantity]", "1");
    params.set("metadata[plan_id]", planId);
    params.set("metadata[billing_period]", billingPeriod);

    const stripeResponse = await fetch("https://api.stripe.com/v1/checkout/sessions", {
      method: "POST",
      headers: {
        Authorization: `Bearer ${env.STRIPE_SECRET_KEY}`,
        "Content-Type": "application/x-www-form-urlencoded"
      },
      body: params.toString()
    });

    const session = await stripeResponse.json();
    if (!stripeResponse.ok) {
      const message = session?.error?.message || "Stripe checkout session creation failed.";
      return json({ error: message }, 502);
    }

    return json({ url: session.url }, 200);
  } catch (error) {
    return json(
      { error: error instanceof Error ? error.message : "Unable to create checkout session." },
      500
    );
  }
}

function json(payload, status) {
  return new Response(JSON.stringify(payload), {
    status,
    headers: {
      "Content-Type": "application/json; charset=utf-8",
      "Cache-Control": "no-store"
    }
  });
}
