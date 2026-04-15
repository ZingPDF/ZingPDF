(function () {
  const config = window.ZINGPDF_STORE_CONFIG || {};
  const pricingGrid = document.getElementById("pricing-grid");
  const billingToggle = document.getElementById("billing-toggle");
  const checkoutBanner = document.getElementById("checkout-banner");
  const dialog = document.getElementById("contact-dialog");
  const emailText = document.getElementById("contact-email-text");
  const emailLink = document.getElementById("contact-email-link");
  const contactSalesTriggers = document.querySelectorAll("[data-contact-sales]");
  const faqDetails = document.querySelectorAll(".faq-list details");
  const guidesSearch = document.querySelector("[data-guides-search]");
  const guideCards = Array.from(document.querySelectorAll("[data-guide-card]"));
  const guidesCount = document.querySelector("[data-guides-count]");
  const guidesEmpty = document.querySelector("[data-guides-empty]");

  let billingPeriod = "monthly";
  let checkoutInFlight = false;
  let pricingCatalog = {};

  if (pricingGrid && dialog && emailText && emailLink) {
    const supportEmail = config.supportEmail || "sales@example.com";
    emailText.textContent = `Email ${supportEmail} to discuss custom licensing, redistribution, procurement, or support terms.`;
    emailLink.href = `mailto:${supportEmail}?subject=${encodeURIComponent("ZingPDF commercial licensing")}`;
    emailLink.textContent = `Email ${supportEmail}`;

    if (billingToggle) {
      for (const button of billingToggle.querySelectorAll("[data-billing-period]")) {
        button.addEventListener("click", () => {
          billingPeriod = button.getAttribute("data-billing-period") || "monthly";
          syncBillingToggle();
          renderPricingCards();
        });
      }
      syncBillingToggle();
    }

    initializePricing().catch(() => {
      renderPricingCards();
    });
  }

  for (const trigger of contactSalesTriggers) {
    trigger.addEventListener("click", () => dialog?.showModal());
  }

  for (const detail of faqDetails) {
    detail.addEventListener("toggle", () => {
      if (!detail.open) {
        return;
      }

      for (const other of faqDetails) {
        if (other !== detail) {
          other.open = false;
        }
      }
    });
  }

  if (guidesSearch && guideCards.length > 0) {
    const applyGuideFilter = () => {
      const query = normalizeText(guidesSearch.value);
      let visibleCount = 0;

      for (const card of guideCards) {
        const haystack = normalizeText(
          `${card.getAttribute("data-guide-search-text") || ""} ${card.textContent || ""}`
        );
        const isVisible = query === "" || haystack.includes(query);
        card.hidden = !isVisible;
        if (isVisible) {
          visibleCount += 1;
        }
      }

      if (guidesCount) {
        guidesCount.textContent = `${visibleCount} guide${visibleCount === 1 ? "" : "s"}`;
      }

      if (guidesEmpty) {
        guidesEmpty.hidden = visibleCount !== 0;
      }
    };

    guidesSearch.addEventListener("input", applyGuideFilter);
    applyGuideFilter();
  }

  highlightCodeBlocks();
  hydrateCheckoutBanner();

  async function initializePricing() {
    pricingCatalog = await loadPricingCatalog();
    renderPricingCards();
  }

  async function loadPricingCatalog() {
    const response = await fetch("/api/pricing-catalog", {
      headers: {
        Accept: "application/json"
      }
    });

    const payload = await response.json().catch(() => ({}));
    if (!response.ok || !payload.plans) {
      throw new Error(payload.error || "Unable to load pricing.");
    }

    return payload.plans;
  }

  function renderPricingCards() {
    pricingGrid.innerHTML = "";
    for (const license of config.licenses || []) {
      pricingGrid.appendChild(buildCard(license));
    }
  }

  function syncBillingToggle() {
    if (!billingToggle) {
      return;
    }

    for (const button of billingToggle.querySelectorAll("[data-billing-period]")) {
      const isActive = button.getAttribute("data-billing-period") === billingPeriod;
      button.classList.toggle("is-active", isActive);
      button.setAttribute("aria-pressed", isActive ? "true" : "false");
    }
  }

  function buildCard(license) {
    const planPricing = pricingCatalog[license.id] || {};
    const article = document.createElement("article");
    article.className = `pricing-card${license.featured ? " featured" : ""}`;

    if (license.badge) {
      const badge = document.createElement("div");
      badge.className = "pricing-badge";
      badge.textContent = license.badge;
      article.appendChild(badge);
    }

    const title = document.createElement("h3");
    title.textContent = license.name;
    article.appendChild(title);

    const subtitle = document.createElement("p");
    subtitle.className = "pricing-subtitle";
    subtitle.textContent = license.subtitle || "";
    article.appendChild(subtitle);

    const price = document.createElement("div");
    price.className = "pricing-price";
    const activePrice = billingPeriod === "annual" ? planPricing.annualPrice : planPricing.monthlyPrice;
    const activeCadence = billingPeriod === "annual" ? planPricing.annualCadence : planPricing.monthlyCadence;
    price.innerHTML = `${escapeHtml(activePrice || "")}${activeCadence ? ` <small>${escapeHtml(activeCadence)}</small>` : ""}`;
    article.appendChild(price);

    if (planPricing.annualPrice) {
      const annualPrice = document.createElement("div");
      annualPrice.className = "pricing-annual-price";
      annualPrice.innerHTML = billingPeriod === "annual"
        ? `monthly option: ${escapeHtml(planPricing.monthlyPrice || "")}${planPricing.monthlyCadence ? ` <small>${escapeHtml(planPricing.monthlyCadence)}</small>` : ""}`
        : `or ${escapeHtml(planPricing.annualPrice)}${planPricing.annualCadence ? ` <small>${escapeHtml(planPricing.annualCadence)}</small>` : ""}`;
      article.appendChild(annualPrice);
    }

    const description = document.createElement("p");
    description.className = "pricing-description";
    description.textContent = license.description || "";
    article.appendChild(description);

    const list = document.createElement("ul");
    list.className = "pricing-list";

    for (const bullet of license.bullets || []) {
      const item = document.createElement("li");
      item.textContent = bullet;
      list.appendChild(item);
    }

    article.appendChild(list);
    article.appendChild(buildActionButton(license));

    return article;
  }

  function buildActionButton(license) {
    const button = document.createElement("button");
    button.className = "button button-primary";
    button.type = "button";
    button.textContent = `${license.ctaLabel || "Continue"}${billingPeriod === "annual" ? " Annual" : ""}`;

    if (license.contactOnly) {
      button.addEventListener("click", () => dialog.showModal());
      return button;
    }

    if (!pricingCatalog[license.id]) {
      button.disabled = true;
      button.title = "Pricing is not configured yet.";
      return button;
    }

    button.addEventListener("click", async () => {
      if (checkoutInFlight) {
        return;
      }

      checkoutInFlight = true;
      button.disabled = true;

      try {
        const response = await fetch("/api/create-checkout-session", {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({
            planId: license.id,
            billingPeriod
          })
        });

        const payload = await response.json().catch(() => ({}));
        if (!response.ok || !payload.url) {
          throw new Error(payload.error || "Unable to start checkout.");
        }

        window.location.href = payload.url;
      } catch (error) {
        window.alert(error instanceof Error ? error.message : "Unable to start checkout.");
      } finally {
        checkoutInFlight = false;
        button.disabled = false;
      }
    });

    return button;
  }

  function escapeHtml(value) {
    return String(value)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#39;");
  }

  function normalizeText(value) {
    return String(value || "")
      .toLowerCase()
      .replace(/\s+/g, " ")
      .trim();
  }

  function hydrateCheckoutBanner() {
    if (!checkoutBanner) {
      return;
    }

    const url = new URL(window.location.href);
    const state = url.searchParams.get("checkout");

    if (state === "success") {
      checkoutBanner.hidden = false;
      checkoutBanner.setAttribute("data-state", "success");
      checkoutBanner.textContent = "Thanks — your checkout completed successfully. Your subscription should be active shortly.";
      url.searchParams.delete("checkout");
      window.history.replaceState({}, "", url.toString());
      return;
    }

    if (state === "cancelled") {
      checkoutBanner.hidden = false;
      checkoutBanner.setAttribute("data-state", "cancelled");
      checkoutBanner.textContent = "Checkout cancelled. You can review the plans below and try again whenever you're ready.";
      url.searchParams.delete("checkout");
      window.history.replaceState({}, "", url.toString());
    }
  }

  function highlightCodeBlocks() {
    const blocks = document.querySelectorAll("pre code");
  const pattern = /"(?:[^"\\]|\\.)*"|\/\/.*|\b(?:using|var|await|async|new|return|if|foreach|switch|case|break|class|public|private|internal|void|string|int|bool|null|is|not|for|try|finally)\b|\b(?:Task|File|Rectangle|Coordinate|DateTimeOffset|TimeSpan|Console|Stream|MemoryStream|Pdf|Page|Form|PdfFont|FontOptions|TextLayoutOptions|TextExtractionOptions|TextObject|TextFormField|ChoiceFormField|SignatureFormField|CheckboxFormField|RadioButtonFormField|PushButtonFormField|ChoiceItem|SelectableOption|StandardPdfFonts|RGBColour|TextOverflowMode|TextExtractionOutputKind|TesseractOcrEngine|PdfOcrOptions|PdfEncryptionPermissions|PdfSignatureOptions|PdfRedactionPlan|PdfRedactionOptions|PdfRedactionReport|PdfRedactionMark|PdfRedactionKind|PdfAuthoringBuilder|PdfPageEditingBuilder|PdfPagesBuilder|Regex|X509Certificate2)\b|\b(?:GetMetadataAsync|GetFormAsync|GetFieldsAsync|GetFieldAsync|GetPageCountAsync|GetPageAsync|GetOptionsAsync|GetValueAsync|SetValueAsync|ClearAsync|FlattenAsync|SignAsync|SignInvisibleAsync|SelectAsync|DeselectAsync|SelectOptionByTextAsync|SelectOptionByValueAsync|AddWatermarkAsync|AddTextAsync|EncryptAsync|SaveAsync|SaveToFileAsync|ApplyAsync|Load|Create|New|Pages|Page|Append|Insert|Remove|Delete|AppendPageAsync|InsertPageAsync|DeletePageAsync|ExportPagesAsync|SplitAsync|AppendPdfAsync|ExtractTextAsync|ExtractTextWithOcrAsync|ExtractPlainTextWithOcrAsync|RedactionAsync|MarkTextAsync|MarkRegion|Compress|AuthenticateAsync|DecryptAsync|RegisterStandardFontAsync|RegisterTrueTypeFontAsync|RemoveHistoryAsync|Wrap|InBox|AlignStart|AlignCenter|AlignEnd|AlignTop|AlignMiddle|AlignBottom|ShrinkToFit|ClipOverflow|WithTrueTypeFont|WriteLine|OpenRead|Create|FromDimensions|FromCoordinates|FirstOrDefault|Single|OfType)\b|\b\d+\b/g;

    for (const block of blocks) {
      const text = block.textContent || "";
      let cursor = 0;
      let highlighted = "";

      for (const match of text.matchAll(pattern)) {
        const token = match[0];
        const index = match.index ?? 0;

        highlighted += escapeHtml(text.slice(cursor, index));
        highlighted += wrapToken(token);
        cursor = index + token.length;
      }

      highlighted += escapeHtml(text.slice(cursor));
      block.innerHTML = highlighted;
    }
  }

  function wrapToken(token) {
    const escaped = escapeHtml(token);

    if (token.startsWith("//")) {
      return `<span class="token comment">${escaped}</span>`;
    }

    if (token.startsWith('"')) {
      return `<span class="token string">${escaped}</span>`;
    }

    if (/^\d+$/.test(token)) {
      return `<span class="token number">${escaped}</span>`;
    }

    if (/^(using|var|await|async|new|return|if|foreach|switch|case|break|class|public|private|internal|void|null|is|not)$/.test(token)) {
      return `<span class="token keyword">${escaped}</span>`;
    }

    if (/^(string|int|bool)$/.test(token)) {
      return `<span class="token builtin">${escaped}</span>`;
    }

    if (/^(Task|File|Rectangle|Coordinate|DateTimeOffset|TimeSpan|Console|Stream|MemoryStream|Pdf|Page|Form|PdfFont|FontOptions|TextLayoutOptions|TextExtractionOptions|TextObject|TextFormField|ChoiceFormField|SignatureFormField|CheckboxFormField|RadioButtonFormField|PushButtonFormField|ChoiceItem|SelectableOption|StandardPdfFonts|RGBColour|TextOverflowMode|TextExtractionOutputKind|TesseractOcrEngine|PdfOcrOptions|PdfSignatureOptions|PdfRedactionPlan|PdfRedactionOptions|PdfRedactionReport|PdfRedactionMark|PdfRedactionKind|PdfAuthoringBuilder|PdfPageEditingBuilder|PdfPagesBuilder|Regex|X509Certificate2)$/.test(token)) {
      return `<span class="token type">${escaped}</span>`;
    }

    return `<span class="token function">${escaped}</span>`;
  }
})();
