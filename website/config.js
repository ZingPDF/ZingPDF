window.ZINGPDF_STORE_CONFIG = {
  provider: "stripe",
  supportEmail: "tom@zingpdf.dev",
  licenses: [
    {
      id: "solo",
      name: "Solo",
      price: "$49",
      cadence: "per month",
      subtitle: "For one developer using ZingPDF in commercial .NET software.",
      description: "For independent developers, consultants, and single-owner products.",
      ctaLabel: "Buy Solo",
      checkoutUrl: "https://buy.stripe.com/dRmbJ25R691Ibbo4wheAg00",
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
      subtitle: "For up to 5 developers.",
      description: "Up to 5 developer seats on the standard online plan.",
      ctaLabel: "Buy Team",
      checkoutUrl: "https://buy.stripe.com/6oU28scfub9Q5R4aUFeAg01",
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
      subtitle: "For up to 20 developers.",
      description: "Up to 20 seats with priority support.",
      ctaLabel: "Buy Business",
      checkoutUrl: "https://buy.stripe.com/cNicN6cfucdU4N02o9eAg02",
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
