const assert = require("node:assert/strict");
const { readFileSync } = require("node:fs");
const path = require("node:path");
const test = require("node:test");
const vm = require("node:vm");

const script = readFileSync(path.join(__dirname, "..", "showcase.js"), "utf8");

function element(label = "") {
  return {
    dataset: {}, attributes: { "aria-label": label }, listeners: {}, hidden: false,
    addEventListener(name, listener) { this.listeners[name] = listener; },
    setAttribute(name, value) { this.attributes[name] = value; },
    getAttribute(name) { return this.attributes[name]; },
    fire(name, event = {}) { this.listeners[name]?.(event); }
  };
}

function setup(reducedMotion = false) {
  const slides = ["Author", "Output", "Transform"].map(element);
  const dots = slides.map(() => element());
  const status = element();
  const controls = element();
  controls.hidden = true;
  const carousel = element();
  carousel.querySelectorAll = (selector) => selector === "[data-slide]" ? slides : dots;
  carousel.querySelector = (selector) => ({
    "[data-slide-status]": status, "[data-carousel-controls]": controls
  })[selector];
  carousel.contains = (target) => dots.includes(target);
  const document = element();
  document.hidden = false;
  document.querySelectorAll = () => [carousel];
  const motion = element();
  motion.matches = reducedMotion;
  const timers = new Map();
  let timerId = 0;
  let visibility;
  const window = {
    matchMedia: () => motion,
    setInterval(callback, delay) { timers.set(++timerId, { callback, delay }); return timerId; },
    clearInterval(id) { timers.delete(id); },
    IntersectionObserver: class {}
  };
  class IntersectionObserver {
    constructor(callback) { visibility = callback; }
    observe() {}
  }
  vm.runInNewContext(script, { window, document, IntersectionObserver });
  const current = () => slides.findIndex((slide) => slide.dataset.active === "true");
  const tick = () => [...timers.values()].forEach(({ callback }) => callback());
  return { carousel, slides, dots, status, controls, document, motion,
    timers, current, tick, visibility: (value) => visibility([{ isIntersecting: value }]) };
}

test("autoplay advances every 6.5 seconds, wraps, and exposes only the active slide", () => {
  const c = setup();
  assert.equal(c.current(), 0);
  assert.equal(c.controls.hidden, false);
  assert.equal([...c.timers.values()][0].delay, 6500);
  c.tick();
  assert.equal(c.current(), 1);
  assert.equal(c.slides[0].inert, true);
  assert.equal(c.slides[0].attributes["aria-hidden"], "true");
  assert.equal(c.slides[1].inert, false);
  assert.equal(c.dots[1].attributes["aria-current"], "true");
  assert.equal(c.status.textContent, undefined, "autoplay must not repeatedly announce slides");
  c.tick(); c.tick();
  assert.equal(c.current(), 0);
});

test("manual controls, arrow keys, and announcements work while rotation is paused", () => {
  const c = setup();
  c.carousel.fire("focusin", { target: c.dots[0] });
  assert.equal(c.timers.size, 0);
  c.dots[2].fire("click");
  assert.equal(c.current(), 2);
  assert.match(c.status.textContent, /Slide 3 of 3: Transform/);
  c.dots[1].fire("click");
  assert.equal(c.current(), 1);
  let prevented = false;
  c.carousel.fire("keydown", { key: "ArrowRight", preventDefault() { prevented = true; } });
  assert.equal(c.current(), 2);
  assert.equal(prevented, true);
  assert.equal(c.timers.size, 0);
  c.carousel.fire("focusout", { relatedTarget: null });
  assert.equal(c.timers.size, 1);
});

test("hover, hidden tabs, and an offscreen carousel suspend autoplay", () => {
  const c = setup();
  c.carousel.fire("mouseenter"); assert.equal(c.timers.size, 0);
  c.carousel.fire("mouseleave"); assert.equal(c.timers.size, 1);
  c.document.hidden = true;
  c.document.fire("visibilitychange"); assert.equal(c.timers.size, 0);
  c.document.hidden = false;
  c.document.fire("visibilitychange"); assert.equal(c.timers.size, 1);
  c.visibility(false); assert.equal(c.timers.size, 0);
  c.visibility(true); assert.equal(c.timers.size, 1);
});

test("moving focus between dots keeps rotation paused until focus leaves the carousel", () => {
  const c = setup();
  c.carousel.fire("focusin", { target: c.dots[0] });
  assert.equal(c.timers.size, 0);
  c.carousel.fire("focusout", { relatedTarget: c.dots[1] });
  c.dots[1].fire("click");
  assert.equal(c.current(), 1);
  assert.equal(c.timers.size, 0);
  c.carousel.fire("focusout", { relatedTarget: null });
  assert.equal(c.timers.size, 1);
});

test("reduced motion disables autoplay but preserves manual navigation", () => {
  const c = setup(true);
  assert.equal(c.timers.size, 0);
  c.dots[1].fire("click");
  assert.equal(c.current(), 1);
  c.motion.matches = false;
  c.motion.fire("change");
  assert.equal(c.timers.size, 1);
  c.motion.matches = true;
  c.motion.fire("change");
  assert.equal(c.timers.size, 0);
});
