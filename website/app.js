(function () {
  const config = window.ZINGPDF_STORE_CONFIG || {};
  const pricingGrid = document.getElementById("pricing-grid");
  const dialog = document.getElementById("contact-dialog");
  const emailText = document.getElementById("contact-email-text");
  const emailLink = document.getElementById("contact-email-link");
  const contactSalesTriggers = document.querySelectorAll("[data-contact-sales]");
  const faqDetails = document.querySelectorAll(".faq-list details");
  const guidesSearch = document.querySelector("[data-guides-search]");
  const guideCards = Array.from(document.querySelectorAll("[data-guide-card]"));
  const guidesCount = document.querySelector("[data-guides-count]");
  const guidesEmpty = document.querySelector("[data-guides-empty]");

  if (pricingGrid && dialog && emailText && emailLink) {
    const supportEmail = config.supportEmail || "sales@example.com";
    emailText.textContent = `Email ${supportEmail} to discuss custom licensing, redistribution, procurement, or support terms.`;
    emailLink.href = `mailto:${supportEmail}?subject=${encodeURIComponent("ZingPDF commercial licensing")}`;
    emailLink.textContent = `Email ${supportEmail}`;

    for (const license of config.licenses || []) {
      pricingGrid.appendChild(buildCard(license));
    }
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

  function buildCard(license) {
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
    price.innerHTML = `${escapeHtml(license.price || "")}${license.cadence ? ` <small>${escapeHtml(license.cadence)}</small>` : ""}`;
    article.appendChild(price);

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
    const button = document.createElement(license.contactOnly ? "button" : "a");
    button.className = "button button-primary";
    button.textContent = license.ctaLabel || "Continue";

    if (license.contactOnly) {
      button.type = "button";
      button.addEventListener("click", () => dialog.showModal());
      return button;
    }

    if (!license.checkoutUrl || license.checkoutUrl.includes("your-")) {
      button.classList.remove("button-primary");
      button.classList.add("button-ghost", "button-disabled");
      button.href = "#";
      button.setAttribute("aria-disabled", "true");
      button.title = "Set a real hosted checkout URL in website/config.js";
      button.addEventListener("click", (event) => {
        event.preventDefault();
        window.alert("Update website/config.js with your real hosted checkout URL first.");
      });
      return button;
    }

    button.href = license.checkoutUrl;
    button.target = "_blank";
    button.rel = "noopener";
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

  function highlightCodeBlocks() {
    const blocks = document.querySelectorAll("pre code");
    const pattern = /"(?:[^"\\]|\\.)*"|\/\/.*|\b(?:using|var|await|async|new|return|if|foreach|switch|case|break|class|public|private|internal|void|string|int|bool|null|is|not|for|try|finally)\b|\b(?:Task|File|Rectangle|Coordinate|DateTimeOffset|TimeSpan|Console|Stream|MemoryStream|Pdf|Page|Form|PdfFont|FontOptions|TextLayoutOptions|TextExtractionOptions|TextObject|TextFormField|ChoiceFormField|SignatureFormField|CheckboxFormField|RadioButtonFormField|PushButtonFormField|ChoiceItem|SelectableOption|StandardPdfFonts|RGBColour|TextOverflowMode|TextExtractionOutputKind)\b|\b(?:GetMetadataAsync|GetFormAsync|GetFieldsAsync|GetFieldAsync|GetPageCountAsync|GetPageAsync|GetOptionsAsync|GetValueAsync|SetValueAsync|ClearAsync|FlattenAsync|SelectAsync|DeselectAsync|SelectOptionByTextAsync|SelectOptionByValueAsync|AddWatermarkAsync|AddTextAsync|EncryptAsync|SaveAsync|Load|Create|AppendPageAsync|InsertPageAsync|DeletePageAsync|ExportPagesAsync|SplitAsync|AppendPdfAsync|ExtractTextAsync|Compress|AuthenticateAsync|DecryptAsync|RegisterStandardFontAsync|RegisterTrueTypeFontAsync|RemoveHistoryAsync|WriteLine|OpenRead|Create|FromDimensions|FromCoordinates|FirstOrDefault|Single|OfType)\b|\b\d+\b/g;

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

    if (/^(Task|File|Rectangle|Coordinate|DateTimeOffset|TimeSpan|Console|Stream|MemoryStream|Pdf|Page|Form|PdfFont|FontOptions|TextLayoutOptions|TextExtractionOptions|TextObject|TextFormField|ChoiceFormField|SignatureFormField|CheckboxFormField|RadioButtonFormField|PushButtonFormField|ChoiceItem|SelectableOption|StandardPdfFonts|RGBColour|TextOverflowMode|TextExtractionOutputKind)$/.test(token)) {
      return `<span class="token type">${escaped}</span>`;
    }

    return `<span class="token function">${escaped}</span>`;
  }
})();
