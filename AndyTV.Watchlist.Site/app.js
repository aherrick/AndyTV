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
      timeline: [],
      plan: { summary: "", steps: [] },
    },
    tabs: ["top", "timeline", "plan"],
    activeTab: "top",
    shareOpen: false,
    copied: false,

    init() {
      // Static control/tab icons are present at parse time; sport icons in the data are emojis.
      lucide.createIcons();
      this.syncTabFromPath();
      window.addEventListener("popstate", () => this.syncTabFromPath());
      this.load();
    },

    async load() {
      try {
        const response = await fetch(DATA_URL, { cache: "no-store" });
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
      history.pushState({ tab }, "", tab === "top" ? "/" : "/" + tab);
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

    onShare() {
      if (navigator.share) {
        navigator.share({ title: document.title, url: location.href }).catch(() => {});
        return;
      }
      this.shareOpen = !this.shareOpen;
    },

    copyLink() {
      if (navigator.clipboard) {
        navigator.clipboard.writeText(location.href);
        this.copied = true;
        setTimeout(() => {
          this.copied = false;
        }, 1500);
      }
      this.shareOpen = false;
    },

    xUrl() {
      return (
        "https://twitter.com/intent/tweet?url=" +
        encodeURIComponent(location.href) +
        "&text=" +
        encodeURIComponent(document.title)
      );
    },

    fbUrl() {
      return "https://www.facebook.com/sharer/sharer.php?u=" + encodeURIComponent(location.href);
    },

    redditUrl() {
      return (
        "https://www.reddit.com/submit?url=" +
        encodeURIComponent(location.href) +
        "&title=" +
        encodeURIComponent(document.title)
      );
    },
  };
}

window.andyTv = andyTv;
