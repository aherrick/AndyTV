// Inlined at the end of index.html by SitePageRenderer, so the right tab is shown before first paint.
// The page already holds every tab and list; this only switches panels, filters lists by sport,
// and wires up the header buttons.
const $$ = (selector, root = document) => root.querySelectorAll(selector);
const tabs = [...$$(".tabs [data-go]")].map((button) => button.dataset.go);
const views = { timeline: "top", weekend: "top" };

// target is a tab ("plan") or tab:view ("timeline:all"); unknown tabs fall back to Top.
function show(target, push) {
  let [tab, view] = target.split(":");
  if (!tabs.includes(tab)) tab = "top";
  if (view) views[tab] = view;
  const panel = tab in views ? `${tab}:${views[tab]}` : tab;

  document.body.dataset.tab = tab;
  for (const button of $$(".tabs [data-go]")) {
    button.classList.toggle("active", button.dataset.go === tab);
    button.setAttribute("aria-selected", button.dataset.go === tab);
  }
  for (const section of $$(".panel")) {
    section.classList.toggle("active", section.dataset.panel === panel);
    filter(section, "");
    section.querySelector(".sport-filter")?.scrollTo(0, 0);
  }
  if (push) history.pushState(null, "", tab === "top" ? "/" : `/${tab}`);
}

function filter(panel, sport) {
  for (const chip of $$("[data-filter]", panel)) chip.classList.toggle("active", chip.dataset.filter === sport);
  for (const item of $$("[data-sport]", panel)) item.hidden = sport !== "" && item.dataset.sport !== sport;
}

const pathTab = () => location.pathname.replace(/^\/+|\/+$/g, "");

document.addEventListener("click", (e) => {
  const go = e.target.closest("[data-go]");
  if (go) show(go.dataset.go, !go.dataset.go.includes(":"));
  const chip = e.target.closest("[data-filter]");
  if (chip) filter(chip.closest(".panel"), chip.dataset.filter);
});
addEventListener("popstate", () => show(pathTab(), false));
show(pathTab(), false);

// Mouse wheel scrolls a one-line sport filter sideways; at either end the page scrolls as usual.
for (const bar of $$(".sport-filter")) {
  bar.addEventListener("wheel", (e) => {
    if (Math.abs(e.deltaX) >= Math.abs(e.deltaY)) return;
    const max = bar.scrollWidth - bar.clientWidth;
    if ((e.deltaY > 0 && bar.scrollLeft < max) || (e.deltaY < 0 && bar.scrollLeft > 0)) {
      e.preventDefault();
      bar.scrollLeft += e.deltaY;
    }
  }, { passive: false });
}

document.getElementById("theme-toggle").addEventListener("click", () => {
  const next = document.documentElement.dataset.theme === "light" ? "dark" : "light";
  document.documentElement.dataset.theme = next;
  localStorage.setItem("andytv-theme", next);
});

const shareButton = document.getElementById("share-btn");
if ("share" in navigator) {
  shareButton.hidden = false;
  shareButton.addEventListener("click", () => navigator.share({ title: document.title, url: location.href }).catch(() => {}));
}

// Mobile-only install button, once the deferred pwa-add-to-homescreen script has loaded.
addEventListener("DOMContentLoaded", () => {
  const a2hs = window.AddToHomeScreen?.({
    appName: "AndyTV Watchlist",
    appIconUrl: "/img/apple-touch-icon.png",
    assetUrl: "https://cdn.jsdelivr.net/npm/pwa-add-to-homescreen@4.4.0/dist/assets/img/",
    displayOptions: { showMobile: true, showDesktop: false },
  });
  if (a2hs && !a2hs.isStandAlone() && (a2hs.isDeviceIOS() || a2hs.isDeviceAndroid())) {
    const installButton = document.getElementById("install-btn");
    installButton.hidden = false;
    installButton.addEventListener("click", () => a2hs.show());
  }
});

// An installed home-screen app has no reload button; fetch a fresh page when it resumes after an hour.
const loadedAt = Date.now();
function reloadIfStale() {
  if (document.visibilityState === "visible" && Date.now() - loadedAt > 60 * 60 * 1000) location.reload();
}
document.addEventListener("visibilitychange", reloadIfStale);
addEventListener("pageshow", reloadIfStale);
