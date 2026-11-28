// Inlined at the end of index.html by SitePageRenderer, so the right tab is shown before first paint.
// The page already holds every tab and list; this only switches panels, filters lists by sport,
// league and network, and wires up the header buttons.
const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
const tabs = $$(".tabs [data-go]").map((button) => button.dataset.go);
const views = { timeline: "top", weekend: "top" };
const archived = "archived" in document.body.dataset;

// Per panel with chips: the picked sport, league and network ("" = all, net "*" = any network listed),
// and whether its network row is open. A tab switch clears the picks but leaves the row open or closed.
const picks = new Map(
  $$(".panel")
    .filter((panel) => panel.querySelector(".sport-filter"))
    .map((panel) => [panel, { sport: "", league: "", net: "", open: false, rowKey: "" }])
);

// target is a tab ("plan") or tab:view ("timeline:all"); unknown tabs fall back to Top.
function show(target, push) {
  let [tab, view] = target.split(":");
  if (!tabs.includes(tab)) tab = "top";
  if (view) views[tab] = view;
  const panel = tab in views ? `${tab}:${views[tab]}` : tab;

  document.body.dataset.tab = tab;
  for (const button of $$(".tabs [data-go]")) button.classList.toggle("active", button.dataset.go === tab);
  for (const section of $$(".panel")) {
    section.hidden = section.dataset.panel !== panel;
    const s = picks.get(section);
    if (s) {
      s.sport = s.league = s.net = "";
      update(section);
    }
    section.querySelector(".sport-filter")?.scrollTo(0, 0);
  }
  // Archived pages live at /yyyy/mm/dd/, so tab switches there don't touch the URL.
  if (push && !archived) history.pushState(null, "", tab === "top" ? "/" : `/${tab}`);
}

// Shows the items that match the panel's picks, then recounts each chip row for the other picks: sports and
// leagues within the picked network, networks within the picked sport and league. A league row shows under
// its sport chip when that sport spans 2+ leagues; the network row (All lists only) opens from the TV toggle.
function update(panel) {
  const s = picks.get(panel);
  const items = $$("[data-sport]", panel).map((el) => ({
    el,
    sport: el.dataset.sport,
    league: el.dataset.league,
    nets: el.dataset.nets?.split("|") ?? [],
  }));
  const onNet = (item) => s.net === "" || (s.net === "*" ? item.nets.length > 0 : item.nets.includes(s.net));
  const inSport = (item) => (s.sport === "" || item.sport === s.sport) && (s.league === "" || item.league === s.league);
  const set = (chip, count, active) => {
    chip.querySelector(".count").textContent = count;
    chip.classList.toggle("active", active);
    chip.classList.toggle("zero", count === 0);
  };

  let shown = 0;
  for (const item of items) {
    item.el.hidden = !(onNet(item) && inSport(item));
    if (!item.el.hidden) shown++;
  }

  const onNetItems = items.filter(onNet);
  for (const chip of $$("[data-filter]", panel)) {
    const sport = chip.dataset.filter;
    set(chip, onNetItems.filter((item) => sport === "" || item.sport === sport).length, sport === s.sport);
  }
  for (const row of $$(".league-filter", panel)) {
    row.hidden = row.dataset.for !== s.sport;
    if (row.hidden) row.scrollLeft = 0;
    for (const chip of $$("[data-league]", row)) {
      const league = chip.dataset.league;
      const count = onNetItems.filter((item) => item.sport === row.dataset.for && (league === "" || item.league === league)).length;
      set(chip, count, league === s.league);
    }
  }

  const networkRow = panel.querySelector(".network-filter");
  if (networkRow) {
    const inSportItems = items.filter(inSport);
    const perNet = new Map();
    for (const item of inSportItems) for (const net of item.nets) perNet.set(net, (perNet.get(net) ?? 0) + 1);
    const chips = $$("[data-net]", networkRow);
    for (const chip of chips) {
      const net = chip.dataset.net;
      const count = net === "" ? inSportItems.length : net === "*" ? inSportItems.filter((item) => item.nets.length).length : perNet.get(net) ?? 0;
      set(chip, count, net === s.net);
      // Networks with no games for this sport and league drop out, so no pick there can come up empty.
      chip.hidden = count === 0 && net !== "" && net !== s.net;
    }
    // After All networks and On TV: the most games first. Picking a network doesn't move its chip, so it stays
    // under the pointer or finger.
    const games = (chip) => perNet.get(chip.dataset.net) ?? 0;
    const named = chips.filter((chip) => chip.dataset.net !== "" && chip.dataset.net !== "*");
    networkRow.append(...named.sort((a, b) => games(b) - games(a) || a.dataset.net.localeCompare(b.dataset.net)));
    networkRow.hidden = !s.open;
    // Opening the row or picking a new sport or league starts the row back at its start, scrolled just far
    // enough to keep the picked network in view. Wrapped rows don't scroll, so this only moves a swiped row.
    const rowKey = s.open ? `${s.sport}|${s.league}` : "";
    if (rowKey !== s.rowKey) {
      networkRow.scrollLeft = 0;
      const picked = named.find((chip) => chip.dataset.net === s.net);
      if (picked && s.open) {
        networkRow.scrollLeft = picked.getBoundingClientRect().right - networkRow.getBoundingClientRect().right + 24;
      }
      s.rowKey = rowKey;
    }

    const toggle = panel.querySelector(".net-toggle");
    toggle.setAttribute("aria-expanded", s.open);
    toggle.classList.toggle("on", s.net !== "");
    // "All ESPN+ 11", like the league row's "All Basketball": says what the count covers, even with the row closed.
    panel.querySelector(".all-label").textContent = s.net === "" ? "All" : `All ${s.net === "*" ? "on TV" : s.net}`;
  }

  // A sport chip dimmed to 0 can still be picked; say why the list is empty and offer the way out.
  const empty = panel.querySelector(".empty");
  if (empty) {
    empty.hidden = shown > 0 || s.net === "";
    if (!empty.hidden) {
      const what = s.league || s.sport;
      empty.textContent = `No ${what ? `${what} ` : ""}games ${s.net === "*" ? "on TV" : `on ${s.net}`}.`;
      empty.insertAdjacentHTML("beforeend", '<button type="button">Show all networks</button>');
    }
  }

  const reset = panel.querySelector(".reset");
  if (reset) reset.hidden = !(s.sport || s.league || s.net);

  // New counts and reordered chips change a row's content width without resizing the row itself.
  for (const bar of $$(".sport-filter", panel)) edge(bar);
}

// Fades a chip row's right edge while more chips are off screen; none at the end or when they all fit.
function edge(bar) {
  const max = bar.scrollWidth - bar.clientWidth;
  bar.classList.toggle("more", max > 1 && bar.scrollLeft < max - 1);
}

function pick(panel, target) {
  const s = picks.get(panel);
  const sportChip = target.closest("[data-filter]");
  if (sportChip) {
    s.sport = sportChip.dataset.filter;
    s.league = "";
  }
  const leagueChip = target.closest(".league-filter [data-league]");
  if (leagueChip) s.league = leagueChip.dataset.league;
  // Tapping the picked network again clears it.
  const netChip = target.closest("[data-net]");
  if (netChip) s.net = netChip.dataset.net === s.net ? "" : netChip.dataset.net;
  if (target.closest(".net-toggle")) s.open = !s.open;
  if (target.closest(".empty button")) s.net = "";
  // Back to how the list looks on a fresh load: nothing picked, network row closed, rows scrolled to the start.
  if (target.closest(".reset")) {
    s.sport = s.league = s.net = "";
    s.open = false;
    for (const bar of $$(".sport-filter", panel)) bar.scrollLeft = 0;
    // Reset hides itself, so keep keyboard focus on the list's filters.
    panel.querySelector('[data-filter=""]').focus({ preventScroll: true });
  }
  update(panel);
}

const pathTab = () => location.pathname.replace(/^\/+|\/+$/g, "");
const header = document.querySelector(".site-header");
const main = document.querySelector("main");

document.addEventListener("click", (e) => {
  const go = e.target.closest("[data-go]");
  if (go) {
    main.classList.toggle("tall", scrollY > 0);
    show(go.dataset.go, !go.dataset.go.includes(":"));
    // Scrolled past the header: keep the tab bar pinned and start the new list at its top.
    scrollTo(0, Math.min(scrollY, header.offsetTop + header.offsetHeight));
    return;
  }
  const share = e.target.closest(".card-share");
  if (share) return shareGame(share);
  const panel = e.target.closest(".panel");
  if (picks.has(panel) && e.target.closest(".sport-filter button, .reset, .empty button")) pick(panel, e.target);
});
addEventListener("popstate", () => show(pathTab(), false));
show(pathTab(), false);

// Native share sheet, else copy the link: the dated page for this list (body's data-path, /yyyy/mm/dd/,
// on the homepage too) plus the card's #slug. That page is published with the homepage and stays up,
// so the link opens the card today and after today, even when the slug's date is the next day
// (a game after midnight ET). Sharing doesn't change this page's own URL.
function shareGame(button) {
  const card = button.closest(".game");
  const url = `${location.origin}${document.body.dataset.path}#${card.dataset.slug}`;
  if ("share" in navigator) return navigator.share({ title: card.querySelector(".title").textContent, url }).catch(() => {});
  navigator.clipboard.writeText(url).then(() => {
    button.classList.add("copied");
    setTimeout(() => button.classList.remove("copied"), 1500);
  });
}

// #slug opens the card's tab, scrolls to it and flashes it; a slug that isn't on this page does nothing.
function reveal() {
  const slug = decodeURIComponent(location.hash.slice(1));
  const card = slug && document.querySelector(`[data-slug="${CSS.escape(slug)}"]`);
  if (!card) return;
  show(card.closest(".panel").dataset.panel, false);
  card.classList.add("flash");
  card.addEventListener("animationend", () => card.classList.remove("flash"), { once: true });
  // Again after load, once the banner image above has shifted the layout.
  const center = () => card.scrollIntoView({ block: "center" });
  center();
  addEventListener("load", center, { once: true });
}
addEventListener("hashchange", reveal);
reveal();

// Chip rows wrap with a mouse (styles.css), so this matters only for a mouse on a touch-first device,
// e.g. a tablet: the wheel scrolls a one-line row sideways, and at either end the page scrolls as usual.
// Scrolling and resizing (which includes a row or its panel being shown) update the row's edge fade.
const resized = new ResizeObserver((entries) => entries.forEach((entry) => edge(entry.target)));
for (const bar of $$(".sport-filter")) {
  bar.addEventListener("wheel", (e) => {
    if (Math.abs(e.deltaX) >= Math.abs(e.deltaY)) return;
    const max = bar.scrollWidth - bar.clientWidth;
    if ((e.deltaY > 0 && bar.scrollLeft < max) || (e.deltaY < 0 && bar.scrollLeft > 0)) {
      e.preventDefault();
      bar.scrollLeft += e.deltaY;
    }
  }, { passive: false });
  bar.addEventListener("scroll", () => edge(bar), { passive: true });
  resized.observe(bar);
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
