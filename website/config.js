window.ZINGPDF_STORE_CONFIG = {
  provider: "stripe",
  supportEmail: "tom@zingpdf.dev",
  licenses: [
    {
      id: "solo",
      name: "Solo",
      price: "$49",
      cadence: "per month",
      subtitle: "For one developer shipping commercial .NET software.",
      description: "A good fit for independent developers, consultants, and single-owner products that need commercial coverage.",
      ctaLabel: "Buy Solo",
      checkoutUrl: "https://buy.stripe.com/test_cNicN6cfucdU4N02o9eAg02",
      featured: false,
      bullets: [
        "1 developer seat",
        "Commercial use",
        "Core, Fonts, GoogleFonts, and FromHTML packages included",
        "Email-based support"
      ]
    },
    {
      id: "team",
      name: "Team",
      price: "$149",
      cadence: "per month",
      subtitle: "For small product teams that want a straightforward commercial license.",
      description: "The default choice for companies that need multi-seat usage without custom procurement or enterprise overhead.",
      ctaLabel: "Buy Team",
      checkoutUrl: "https://buy.stripe.com/test_4gMaEY5R6guabbo0g1eAg03",
      featured: true,
      badge: "Most Popular",
      bullets: [
        "Up to 5 developer seats",
        "Core, Fonts, GoogleFonts, and FromHTML packages included",
        "Commercial use",
        "Priority email support"
      ]
    },
    {
      id: "business",
      name: "Business",
      price: "$399",
      cadence: "per month",
      subtitle: "For growing companies that need broader team coverage.",
      description: "Best for organizations that want wider internal usage, stronger support expectations, and a simpler path than enterprise procurement.",
      ctaLabel: "Buy Business",
      checkoutUrl: "https://buy.stripe.com/test_8x23cw93iguadjw5AleAg04",
      featured: false,
      bullets: [
        "Up to 20 developer seats",
        "Core, Fonts, GoogleFonts, and FromHTML packages included",
        "Commercial use",
        "Priority email support"
      ]
    }
  ]
};
