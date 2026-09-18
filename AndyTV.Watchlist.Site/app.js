// Alpine component for the permanent shell. The Function writes a precomputed latest.json
// (ET times, icons, picks, plan already formatted), so this just holds reactive state and
// maps the model onto declarative templates in index.html.

// Cross-origin: the storage account must allow GET from this site's origin (CORS).
const DATA_URL = "https://andytvwatchlist.blob.core.windows.net/andytv-watchlist/latest.json";

// The watchlist regenerates at 3:45 AM ET, so shift the clock back 3h45m before taking the ET
// date: anything before 3:45 AM still counts as the prior day's watchlist.
function watchlistDay() {
  const shifted = new Date(Date.now() - (3 * 60 + 45) * 60_000);
  return shifted.toLocaleDateString("en-US", { timeZone: "America/New_York" });
}

function andyTv() {
  return {
    model: {
      date: "",
      updated: "",
      top: { picks: [], games: [] },
      plan: { summary: "", steps: [] },
    },
    tabs: ["top", "timeline", "plan"],
    activeTab: "top",
    a2hs: null,
    loadedDay: "",

    init() {
      // Static control/tab icons are present at parse time; sport icons in the data are emojis.
      lucide.createIcons();
      this.initAddToHomeScreen();
      this.syncTabFromPath();
      window.addEventListener("popstate", () => this.syncTabFromPath());
      this.initResumeRefresh();
      this.load();
    },

    async load() {
      try {
        const response = await fetch(DATA_URL, { cache: "no-store" });
        if (!response.ok) throw new Error(`Watchlist request failed: ${response.status}`);
        this.model = await response.json();
        this.loadedDay = watchlistDay();
      } catch (err) {
        this.model.date = "Could not load the watchlist.";
      }
    },

    // Home-screen PWAs are suspended rather than reloaded, so a page opened yesterday would keep
    // showing yesterday's watchlist. The data only changes daily, so only refetch across the 3:45 AM ET cutover.
    initResumeRefresh() {
      const refreshIfNewDay = () => {
        if (document.visibilityState === "visible" && this.loadedDay && this.loadedDay !== watchlistDay()) {
          this.load();
        }
      };
      document.addEventListener("visibilitychange", refreshIfNewDay);
      window.addEventListener("pageshow", refreshIfNewDay);
    },

    // ---- clean-path tab routing (no '#') ----

    syncTabFromPath() {
      const segment = location.pathname.replace(/^\/+|\/+$/g, "");
      this.activeTab = this.tabs.includes(segment) ? segment : "top";
    },

    go(tab) {
      this.activeTab = tab;
      history.pushState({ tab }, "", this.tabPath);
    },

    get tabPath() {
      return this.activeTab === "top" ? "/" : "/" + this.activeTab;
    },

    // Timeline is the Top games re-sorted by start time (kept out of the JSON to avoid duplication).
    timeline() {
      return [...this.model.top.games].sort((a, b) => new Date(a.timeIso) - new Date(b.timeIso));
    },

    // ---- watch-plan alternates: up to two options plus a "+N more" overflow ----

    altText(alternates) {
      const max = 2;
      let chips = alternates
        .slice(0, max)
        .map((a) => `${a.icon} ${a.matchup}`)
        .join(" · ");
      const extra = alternates.length - max;
      if (extra > 0) {
        chips += ` · +${extra} more`;
      }
      return chips;
    },

    // ---- theme ----

    toggleTheme() {
      const next =
        document.documentElement.getAttribute("data-theme") === "light" ? "dark" : "light";
      document.documentElement.setAttribute("data-theme", next);
      localStorage.setItem("andytv-theme", next);
    },

    // ---- share (native Web Share only; button hidden when unsupported) ----

    canShare() {
      return typeof navigator.share === "function";
    },

    share() {
      navigator.share({ title: document.title, url: location.href }).catch(() => {});
    },

    // ---- add to home screen (pwa-add-to-homescreen) ----

    initAddToHomeScreen() {
      if (typeof window.AddToHomeScreen !== "function") {
        return;
      }
      this.a2hs = window.AddToHomeScreen({
        appName: "AndyTV Watchlist",
        appIconUrl: "/img/apple-touch-icon.png",
        assetUrl: "https://cdn.jsdelivr.net/npm/pwa-add-to-homescreen@4.4.0/dist/assets/img/",
        displayOptions: { showMobile: true, showDesktop: false },
      });
    },

    // Mobile-only install affordance; hidden on desktop and once installed.
    canInstall() {
      return (
        !!this.a2hs &&
        !this.a2hs.isStandAlone() &&
        (this.a2hs.isDeviceIOS() || this.a2hs.isDeviceAndroid())
      );
    },

    addToHomeScreen() {
      this.a2hs?.show();
    },
  };
}

window.andyTv = andyTv;
