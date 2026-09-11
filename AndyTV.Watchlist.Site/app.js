// Alpine component for the permanent shell. The Function writes a precomputed latest.json
// (ET times, icons, picks, plan already formatted), so this just holds reactive state and
// maps the model onto declarative templates in index.html.

// Cross-origin: the storage account must allow GET from this site's origin (CORS).
const DATA_URL = "https://andytvwatchlist.blob.core.windows.net/andytv-watchlist/latest.json";

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
    shareOpen: false,
    copied: false,
    a2hs: null,

    init() {
      // Static control/tab icons are present at parse time; sport icons in the data are emojis.
      lucide.createIcons();
      this.initAddToHomeScreen();
      this.syncTabFromPath();
      window.addEventListener("popstate", () => this.syncTabFromPath());
      this.load();
    },

    async load() {
      try {
        const response = await fetch(DATA_URL, { cache: "no-store" });
        if (!response.ok) throw new Error(`Watchlist request failed: ${response.status}`);
        this.model = await response.json();
      } catch (err) {
        this.model.date = "Could not load the watchlist.";
      }
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

    // ---- share ----

    shareLinks() {
      const url = encodeURIComponent(location.origin + this.tabPath);
      const title = encodeURIComponent(document.title);
      return [
        { label: "X", href: `https://twitter.com/intent/tweet?url=${url}&text=${title}` },
        { label: "Facebook", href: `https://www.facebook.com/sharer/sharer.php?u=${url}` },
        { label: "Reddit", href: `https://www.reddit.com/submit?url=${url}&title=${title}` },
      ];
    },

    onShare() {
      if (navigator.share) {
        navigator.share({ title: document.title, url: location.href }).catch(() => {});
        return;
      }
      this.shareOpen = !this.shareOpen;
    },

    async copyLink() {
      try {
        await navigator.clipboard.writeText(location.href);
        this.copied = true;
        setTimeout(() => {
          this.copied = false;
        }, 1500);
      } catch {
        this.copied = false;
      }
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
        maxModalDisplayCount: 2,
      });
      // Prompt on load; show() no-ops if already installed and picks the device-specific guide.
      this.a2hs.show();
    },

    addToHomeScreen() {
      this.shareOpen = false;
      this.a2hs?.show();
    },
  };
}

window.andyTv = andyTv;
