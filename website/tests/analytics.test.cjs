const assert = require("node:assert/strict");
const { readFileSync } = require("node:fs");
const path = require("node:path");
const test = require("node:test");
const vm = require("node:vm");

const script = readFileSync(path.join(__dirname, "..", "app.js"), "utf8");

function runPage(url, measurementId = "G-TEST") {
  const calls = [];
  const listeners = {};
  const observers = [];
  const banner = {
    hidden: true,
    setAttribute(name, value) { this[name] = value; }
  };
  const location = new URL(url);
  const window = {
    ZINGPDF_STORE_CONFIG: { googleAnalyticsMeasurementId: measurementId },
    location,
    history: {
      replaceState(_state, _title, nextUrl) {
        const next = new URL(nextUrl);
        location.href = next.href;
      }
    },
    gtag(...args) { calls.push(args); }
  };
  const document = {
    head: { appendChild() {} },
    getElementById(id) {
      return id === "checkout-banner" ? banner : id === "licenses" ? {} : null;
    },
    querySelectorAll() { return []; },
    querySelector() { return null; },
    createElement() { return {}; },
    addEventListener(name, listener) { listeners[name] = listener; }
  };
  class IntersectionObserver {
    constructor(callback) { this.callback = callback; observers.push(this); }
    observe() {}
    disconnect() { this.disconnected = true; }
  }
  window.IntersectionObserver = IntersectionObserver;
  vm.runInNewContext(script, { window, document, URL, IntersectionObserver });

  return {
    calls,
    banner,
    location,
    showPricing() {
      observers[0].callback([{ isIntersecting: true }]);
    },
    click(href, id = "") {
      listeners.click({ target: { closest: () => ({ href, id }) } });
    }
  };
}

test("production page records funnel signals without treating pricing as checkout", () => {
  const page = runPage("https://zingpdf.dev/");
  page.showPricing();
  page.showPricing();
  page.click("https://zingpdf.dev/#licenses");
  page.click("https://www.nuget.org/packages/ZingPDF");

  const events = page.calls.filter(([kind]) => kind === "event");
  assert.deepEqual(events.map(([, name]) => name), [
    "pricing_view", "pricing_link_click", "resource_click"
  ]);
  assert.equal(events[2][2].resource, "nuget");
});

test("local preview suppresses GA4 events", () => {
  const page = runPage("http://localhost:8080/?checkout=success");
  page.click("http://localhost:8080/#licenses");
  assert.equal(page.calls.length, 0);
  assert.equal(page.location.search, "");
});

test("a checkout return is counted once as a return, not a purchase", () => {
  const page = runPage("https://zingpdf.dev/?checkout=success");
  assert.equal(page.location.search, "");
  assert.equal(page.banner.hidden, false);
  assert.deepEqual(page.calls.filter(([kind]) => kind === "event").map(([, name]) => name), ["checkout_return"]);
  assert.equal(page.calls.find(([, name]) => name === "checkout_return")[2].result, "success");
});
