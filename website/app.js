(function () {
  const config = window.ZINGPDF_STORE_CONFIG || {};
  const checkoutBanner = document.getElementById("checkout-banner");
  const checkoutReturnState = hydrateCheckoutBanner();
  const analyticsEnabled = initializeGoogleAnalytics(config);
  const dialog = document.getElementById("contact-dialog");
  const emailText = document.getElementById("contact-email-text");
  const emailLink = document.getElementById("contact-email-link");
  const contactSalesTriggers = document.querySelectorAll("[data-contact-sales]");
  const faqDetails = document.querySelectorAll(".faq-list details");
  const guidesSearch = document.querySelector("[data-guides-search]");
  const guideCards = Array.from(document.querySelectorAll("[data-guide-card]"));
  const guidesCount = document.querySelector("[data-guides-count]");
  const guidesEmpty = document.querySelector("[data-guides-empty]");

  if (dialog && emailText && emailLink) {
    const supportEmail = config.supportEmail || "sales@example.com";
    emailText.textContent = `Email ${supportEmail} to discuss custom licensing, redistribution, procurement, or support terms.`;
    emailLink.href = `mailto:${supportEmail}?subject=${encodeURIComponent("ZingPDF commercial licensing")}`;
    emailLink.textContent = `Email ${supportEmail}`;
  }

  for (const trigger of contactSalesTriggers) {
    trigger.addEventListener("click", () => {
      trackEvent("contact_sales_click");
      dialog?.showModal();
    });
  }

  initializeFunnelTracking();

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

  function trackEvent(name, parameters) {
    if (analyticsEnabled) {
      window.gtag("event", name, parameters || {});
    }
  }

  function initializeFunnelTracking() {
    if (checkoutReturnState) {
      // A return URL is directional evidence, not a verified Stripe payment.
      trackEvent("checkout_return", { result: checkoutReturnState });
    }

    const pricingSection = document.getElementById("licenses");
    if (pricingSection && analyticsEnabled && "IntersectionObserver" in window) {
      let pricingViewed = false;
      const observer = new IntersectionObserver((entries) => {
        if (!pricingViewed && entries.some((entry) => entry.isIntersecting)) {
          pricingViewed = true;
          trackEvent("pricing_view");
          observer.disconnect();
        }
      }, { threshold: 0.25 });
      observer.observe(pricingSection);
    }

    document.addEventListener("click", (event) => {
      const link = event.target?.closest?.("a");
      if (!link) {
        return;
      }

      const destination = new URL(link.href, window.location.href);
      if (destination.origin === window.location.origin && destination.hash === "#licenses") {
        trackEvent("pricing_link_click");
      } else if (destination.hostname === "www.nuget.org") {
        trackEvent("resource_click", { resource: "nuget" });
      } else if (destination.hostname === "github.com" && destination.pathname.startsWith("/ZingPDF/ZingPDF")) {
        trackEvent("resource_click", { resource: "github" });
      } else if (link.id === "contact-email-link") {
        trackEvent("sales_email_click");
      }
    });
  }

  function initializeGoogleAnalytics(storeConfig) {
    const measurementId = String(storeConfig.googleAnalyticsMeasurementId || "").trim();

    if (measurementId === "" || isLocalDevelopmentHost(window.location.hostname)) {
      return false;
    }

    window.dataLayer = window.dataLayer || [];
    window.gtag = window.gtag || function gtag() {
      window.dataLayer.push(arguments);
    };

    window.gtag("js", new Date());
    window.gtag("config", measurementId, {
      anonymize_ip: true
    });

    const script = document.createElement("script");
    script.async = true;
    script.src = `https://www.googletagmanager.com/gtag/js?id=${encodeURIComponent(measurementId)}`;
    document.head.appendChild(script);
    return true;
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

  function isLocalDevelopmentHost(hostname) {
    return hostname === "localhost" || hostname === "127.0.0.1" || hostname === "[::1]";
  }

  function hydrateCheckoutBanner() {
    if (!checkoutBanner) {
      return null;
    }

    const url = new URL(window.location.href);
    const state = url.searchParams.get("checkout");

    if (state === "success") {
      checkoutBanner.hidden = false;
      checkoutBanner.setAttribute("data-state", "success");
      checkoutBanner.textContent = "Stripe returned you to ZingPDF after checkout. Check your Stripe confirmation email for your subscription details.";
      url.searchParams.delete("checkout");
      window.history.replaceState({}, "", url.toString());
      return state;
    }

    if (state === "cancelled") {
      checkoutBanner.hidden = false;
      checkoutBanner.setAttribute("data-state", "cancelled");
      checkoutBanner.textContent = "Checkout cancelled. You can review the plans below and try again whenever you're ready.";
      url.searchParams.delete("checkout");
      window.history.replaceState({}, "", url.toString());
      return state;
    }

    return null;
  }

  function highlightCodeBlocks() {
    const blocks = document.querySelectorAll("pre code");
  const pattern = /"(?:[^"\\]|\\.)*"|\/\/.*|\b(?:using|var|await|async|new|return|if|foreach|switch|case|break|class|public|private|internal|void|string|int|bool|null|is|not|for|try|finally)\b|\b(?:Task|File|Uri|Rectangle|Coordinate|DateTimeOffset|TimeSpan|Console|Stream|TextWriter|MemoryStream|Pdf|Page|Form|PdfFont|FontOptions|TextLayoutOptions|TextExtractionOptions|TextObject|TextFormField|ChoiceFormField|SignatureFormField|CheckboxFormField|RadioButtonFormField|PushButtonFormField|RadioButtonFieldOption|ChoiceFieldOption|TextFormFieldCreationOptions|CheckboxFormFieldCreationOptions|RadioButtonFormFieldCreationOptions|ChoiceFormFieldCreationOptions|SignatureFormFieldCreationOptions|ChoiceItem|SelectableOption|StandardPdfFonts|RGBColour|TextOverflowMode|TextExtractionOutputKind|TesseractOcrEngine|PdfOcrOptions|PdfEncryptionPermissions|PdfSignatureOptions|PdfSignatureValidationOptions|PdfSignatureValidationStatus|PdfSignatureValidationProfile|PdfSignatureCheckStatus|PdfRedactionPlan|PdfRedactionOptions|PdfRedactionReport|PdfRedactionMark|PdfRedactionKind|PdfAuthoringBuilder|PdfPageEditingBuilder|PdfPagesBuilder|Converter|NavigationOptions|WaitUntil|PerformanceTrace|IPdfObjectCollection|IndirectObject|IndirectObjectReference|DocumentCatalogDictionary|Regex|X509Certificate2|X509Certificate2Collection)\b|\b(?:GetMetadataAsync|GetFormAsync|GetOrCreateFormAsync|GetFieldsAsync|GetFieldAsync|GetSignaturesAsync|GetPageCountAsync|GetPageAsync|GetOptionsAsync|GetValueAsync|SetValueAsync|ClearAsync|FlattenAsync|SignAsync|SignInvisibleAsync|ValidateIntegrityAsync|ValidateAsync|ValidateSignatureAsync|SelectAsync|DeselectAsync|SelectOptionByTextAsync|SelectOptionByValueAsync|GetCaptionAsync|HasActionAsync|GetActionTypeAsync|GetActionUriAsync|GetNamedActionAsync|GetAdditionalActionTriggersAsync|AddWatermarkAsync|AddTextAsync|AddTextFieldAsync|AddCheckboxFieldAsync|AddRadioButtonFieldAsync|AddComboBoxFieldAsync|AddListBoxFieldAsync|AddSignatureFieldAsync|EncryptAsync|SaveAsync|SaveToFileAsync|CopyToAsync|ToPdfAsync|GetDocumentCatalogAsync|GetLatestTrailerDictionaryAsync|EnumeratePagesAsync|GetSummary|SetEnabled|Reset|Measure|WriteSummary|ApplyAsync|Load|Create|New|Pages|Page|Append|Insert|Remove|Delete|AppendPageAsync|InsertPageAsync|DeletePageAsync|ExportPagesAsync|SplitAsync|AppendPdfAsync|ExtractTextAsync|ExtractTextWithOcrAsync|ExtractPlainTextWithOcrAsync|RedactionAsync|MarkTextAsync|MarkRegion|Compress|AuthenticateAsync|DecryptAsync|RegisterStandardFontAsync|RegisterTrueTypeFontAsync|RemoveHistoryAsync|Wrap|InBox|AlignStart|AlignCenter|AlignEnd|AlignTop|AlignMiddle|AlignBottom|ShrinkToFit|ClipOverflow|WithTrueTypeFont|WriteLine|OpenRead|Create|FromDimensions|FromCoordinates|FirstOrDefault|Single|OfType)\b|\b\d+\b/g;

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

    if (/^(Task|File|Uri|Rectangle|Coordinate|DateTimeOffset|TimeSpan|Console|Stream|TextWriter|MemoryStream|Pdf|Page|Form|PdfFont|FontOptions|TextLayoutOptions|TextExtractionOptions|TextObject|TextFormField|ChoiceFormField|SignatureFormField|CheckboxFormField|RadioButtonFormField|PushButtonFormField|RadioButtonFieldOption|ChoiceFieldOption|TextFormFieldCreationOptions|CheckboxFormFieldCreationOptions|RadioButtonFormFieldCreationOptions|ChoiceFormFieldCreationOptions|SignatureFormFieldCreationOptions|ChoiceItem|SelectableOption|StandardPdfFonts|RGBColour|TextOverflowMode|TextExtractionOutputKind|TesseractOcrEngine|PdfOcrOptions|PdfSignatureOptions|PdfSignatureValidationOptions|PdfSignatureValidationStatus|PdfSignatureValidationProfile|PdfSignatureCheckStatus|PdfRedactionPlan|PdfRedactionOptions|PdfRedactionReport|PdfRedactionMark|PdfRedactionKind|PdfAuthoringBuilder|PdfPageEditingBuilder|PdfPagesBuilder|Converter|NavigationOptions|WaitUntil|PerformanceTrace|IPdfObjectCollection|IndirectObject|IndirectObjectReference|DocumentCatalogDictionary|Regex|X509Certificate2|X509Certificate2Collection)$/.test(token)) {
      return `<span class="token type">${escaped}</span>`;
    }

    return `<span class="token function">${escaped}</span>`;
  }
})();
