export async function getPricingCatalog(env) {
  const prices = await listActiveRecurringPrices(env);
  const catalog = {};

  for (const price of prices) {
    const planId = String(price?.product?.metadata?.site_plan_id || "").toLowerCase();
    const billingPeriod = getBillingPeriod(price);

    if (!planId || !billingPeriod) {
      continue;
    }

    catalog[planId] ??= {};
    catalog[planId][billingPeriod] = price;
  }

  return catalog;
}

export function getCatalogPrice(catalog, planId, billingPeriod) {
  return catalog?.[String(planId || "").toLowerCase()]?.[String(billingPeriod || "").toLowerCase()] || null;
}

export function formatPrice(price) {
  const unitAmount = Number(price?.unit_amount ?? 0);
  const currency = String(price?.currency || "usd").toUpperCase();

  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(unitAmount / 100);
}

export function formatCadence(price) {
  const interval = price?.recurring?.interval;
  const intervalCount = Number(price?.recurring?.interval_count ?? 1);

  if (!interval) {
    return "";
  }

  if (interval === "year" && intervalCount === 1) {
    return "per year";
  }

  if (interval === "month" && intervalCount === 1) {
    return "per month";
  }

  return `every ${intervalCount} ${interval}${intervalCount === 1 ? "" : "s"}`;
}

function getBillingPeriod(price) {
  const interval = price?.recurring?.interval;
  const intervalCount = Number(price?.recurring?.interval_count ?? 1);

  if (interval === "month" && intervalCount === 1) {
    return "monthly";
  }

  if (interval === "year" && intervalCount === 1) {
    return "annual";
  }

  return "";
}

async function listActiveRecurringPrices(env) {
  const prices = [];
  let hasMore = true;
  let startingAfter = null;

  while (hasMore) {
    const params = new URLSearchParams();
    params.set("active", "true");
    params.set("type", "recurring");
    params.set("limit", "100");
    params.set("expand[]", "data.product");

    if (startingAfter) {
      params.set("starting_after", startingAfter);
    }

    const response = await fetch(`https://api.stripe.com/v1/prices?${params.toString()}`, {
      headers: {
        Authorization: `Bearer ${env.STRIPE_SECRET_KEY}`
      }
    });

    const payload = await response.json();
    if (!response.ok) {
      const message = payload?.error?.message || "Unable to list Stripe prices.";
      throw new Error(message);
    }

    prices.push(...(payload.data || []));
    hasMore = payload.has_more === true && payload.data?.length > 0;
    startingAfter = hasMore ? payload.data[payload.data.length - 1].id : null;
  }

  return prices;
}
