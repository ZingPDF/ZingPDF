(function () {
  for (const carousel of document.querySelectorAll("[data-carousel]")) {
    const slides = Array.from(carousel.querySelectorAll("[data-slide]"));
    const dots = Array.from(carousel.querySelectorAll("[data-slide-to]"));
    const status = carousel.querySelector("[data-slide-status]");
    const motion = window.matchMedia("(prefers-reduced-motion: reduce)");
    let active = 0;
    let focused = false;
    let hovered = false;
    let visible = true;
    let timer;

    function schedule() {
      window.clearInterval(timer);
      if (!focused && !hovered && visible && !document.hidden && !motion.matches) {
        timer = window.setInterval(() => show(active + 1), 6500);
      }
    }

    function show(index, manual = false) {
      active = (index + slides.length) % slides.length;
      slides.forEach((slide, i) => {
        slide.hidden = false;
        slide.dataset.active = String(i === active);
        slide.setAttribute("aria-hidden", String(i !== active));
        slide.inert = i !== active;
      });
      dots.forEach((dot, i) => dot.setAttribute("aria-current", String(i === active)));
      // Announce navigation on request, without interrupting readers during autoplay.
      if (manual) status.textContent = `Slide ${active + 1} of ${slides.length}: ${slides[active].getAttribute("aria-label")}`;
    }

    function navigate(index) {
      show(index, true);
      schedule();
    }

    dots.forEach((dot, i) => dot.addEventListener("click", () => navigate(i)));
    carousel.addEventListener("mouseenter", () => { hovered = true; schedule(); });
    carousel.addEventListener("mouseleave", () => { hovered = false; schedule(); });
    carousel.addEventListener("focusin", () => { focused = true; schedule(); });
    carousel.addEventListener("focusout", (event) => {
      focused = carousel.contains(event.relatedTarget);
      schedule();
    });
    carousel.addEventListener("keydown", (event) => {
      if (event.key === "ArrowLeft" || event.key === "ArrowRight") {
        event.preventDefault();
        navigate(active + (event.key === "ArrowRight" ? 1 : -1));
      }
    });
    document.addEventListener("visibilitychange", schedule);
    motion.addEventListener("change", schedule);
    if ("IntersectionObserver" in window) {
      new IntersectionObserver(([entry]) => {
        visible = entry.isIntersecting;
        schedule();
      }).observe(carousel);
    }

    show(0);
    carousel.dataset.ready = "true";
    carousel.querySelector("[data-carousel-controls]").hidden = false;
    schedule();
  }
})();
