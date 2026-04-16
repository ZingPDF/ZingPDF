window.ZINGPDF_STORE_CONFIG = {
  provider: "stripe",
  checkoutMode: "sessions",
  supportEmail: "tom@zingpdf.dev",
  licenses: [
    {
      id: "solo",
      name: "Solo",
      subtitle: "For one developer using ZingPDF in commercial .NET software.",
      description: "For independent developers, consultants, and single-owner products.",
      ctaLabel: "Buy Solo",
      featured: false,
      bullets: [
        "1 developer seat",
        "Commercial use",
        "Email-based support"
      ]
    },
    {
      id: "team",
      name: "Team",
      subtitle: "For up to 5 developers.",
      description: "Best value for small teams on the standard online plan.",
      ctaLabel: "Buy Team",
      featured: true,
      badge: "Most Popular",
      bullets: [
        "Up to 5 developer seats",
        "Commercial use",
        "Priority email support"
      ]
    },
    {
      id: "business",
      name: "Business",
      subtitle: "For up to 20 developers.",
      description: "Up to 20 seats with priority support.",
      ctaLabel: "Buy Business",
      featured: false,
      bullets: [
        "Up to 20 developer seats",
        "Commercial use",
        "Priority email support"
      ]
    }
  ]
};
