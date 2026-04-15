import {
  formatCadence,
  formatPrice,
  getCatalogPrice,
  getPricingCatalog
} from "../_lib/stripe-pricing.js";

const PLAN_IDS = ["solo", "team", "business"];

export async function onRequestGet(context) {
  const { env } = context;

  try {
    if (!env.STRIPE_SECRET_KEY) {
      return json({ error: "Stripe is not configured yet." }, 500);
    }

    const stripeCatalog = await getPricingCatalog(env);
    const catalog = {};

    for (const planId of PLAN_IDS) {
      const monthlyPrice = getCatalogPrice(stripeCatalog, planId, "monthly");
      const annualPrice = getCatalogPrice(stripeCatalog, planId, "annual");

      if (!monthlyPrice || !annualPrice) {
        continue;
      }

      catalog[planId] = {
        monthlyPrice: formatPrice(monthlyPrice),
        monthlyCadence: formatCadence(monthlyPrice),
        annualPrice: formatPrice(annualPrice),
        annualCadence: formatCadence(annualPrice)
      };
    }

    return json({ plans: catalog }, 200);
  } catch (error) {
    return json(
      { error: error instanceof Error ? error.message : "Unable to load pricing." },
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
