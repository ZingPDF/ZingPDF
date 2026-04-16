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
    catalog[planId][billingPeriod] ??= {};

    for (const variant of expandPriceVariants(price)) {
      const currency = String(variant?.currency || "").toUpperCase();
      if (!currency) {
        continue;
      }

      catalog[planId][billingPeriod][currency] = variant;
    }
  }

  return catalog;
}

export function getCatalogPrice(catalog, planId, billingPeriod, request) {
  const pricesForPeriod = catalog?.[String(planId || "").toLowerCase()]?.[String(billingPeriod || "").toLowerCase()];
  if (!pricesForPeriod) {
    return null;
  }

  for (const currency of getPreferredCurrencies(request)) {
    const match = pricesForPeriod[currency];
    if (match) {
      return match;
    }
  }

  const availableCurrencies = Object.keys(pricesForPeriod).sort();
  return availableCurrencies.length > 0 ? pricesForPeriod[availableCurrencies[0]] : null;
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

function getPreferredCurrencies(request) {
  const country = getRequestCountry(request);

  if (country === "AU") {
    return ["AUD", "USD", "EUR", "GBP"];
  }

  if (country === "GB") {
    return ["GBP", "EUR", "USD", "AUD"];
  }

  if (country === "NZ") {
    return ["NZD", "AUD", "USD", "EUR"];
  }

  if (country === "CA") {
    return ["CAD", "USD", "EUR", "GBP"];
  }

  if (country === "US") {
    return ["USD", "EUR", "GBP", "AUD"];
  }

  if (EURO_COUNTRIES.has(country)) {
    return ["EUR", "USD", "GBP", "AUD"];
  }

  return ["USD", "EUR", "GBP", "AUD"];
}

function getRequestCountry(request) {
  const workerCountry = String(request?.cf?.country || "").toUpperCase();
  if (workerCountry) {
    return workerCountry;
  }

  const headerCountry = String(request?.headers?.get?.("CF-IPCountry") || "").toUpperCase();
  return headerCountry;
}

const EURO_COUNTRIES = new Set([
  "AT",
  "BE",
  "CY",
  "DE",
  "EE",
  "ES",
  "FI",
  "FR",
  "GR",
  "HR",
  "IE",
  "IT",
  "LT",
  "LU",
  "LV",
  "MT",
  "NL",
  "PT",
  "SI",
  "SK"
]);

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

    const detailedPrices = await Promise.all((payload.data || []).map((price) => fetchStripePrice(env, price.id)));
    prices.push(...detailedPrices);
    hasMore = payload.has_more === true && payload.data?.length > 0;
    startingAfter = hasMore ? payload.data[payload.data.length - 1].id : null;
  }

  return prices;
}

async function fetchStripePrice(env, priceId) {
  const params = new URLSearchParams();
  params.set("expand[]", "product");

  const response = await fetch(`https://api.stripe.com/v1/prices/${priceId}?${params.toString()}`, {
    headers: {
      Authorization: `Bearer ${env.STRIPE_SECRET_KEY}`
    }
  });

  const payload = await response.json();
  if (!response.ok) {
    const message = payload?.error?.message || "Unable to fetch Stripe price.";
    throw new Error(message);
  }

  return payload;
}

function expandPriceVariants(price) {
  const variants = [
    {
      ...price,
      currency: price.currency,
      unit_amount: price.unit_amount
    }
  ];

  const currencyOptions = price?.currency_options || {};
  for (const [currency, option] of Object.entries(currencyOptions)) {
    variants.push({
      ...price,
      currency,
      unit_amount: option?.unit_amount ?? price.unit_amount
    });
  }

  return variants;
}
