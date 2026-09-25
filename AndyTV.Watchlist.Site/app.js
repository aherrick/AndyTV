// Alpine component for the permanent shell. The Function writes a precomputed latest.json
// (ET times, icons, picks, plan already formatted), so this just holds reactive state and
// maps the model onto declarative templates in index.html.

// Cross-origin: the storage account must allow GET from this site's origin (CORS).
const DATA_URL = "https://andytvwatchlist.blob.core.windows.net/andytv-watchlist/latest.json";
const WEEKEND_DATA_URL = DATA_URL.replace("latest.json", "latest_weekend.json");
const TIME_ZONE = "America/New_York";

// Friday at 4 AM through Sunday before 4 AM, in Eastern time.
function isWeekendWindow(now = new Date()) {
  const { weekday, hour } = Object.fromEntries(
    new Intl.DateTimeFormat("en-US", {
      timeZone: TIME_ZONE, weekday: "short", hour: "numeric", hourCycle: "h23",
    }).formatToParts(now).map(({ type, value }) => [type, value])
  );
  return (weekday === "Fri" && hour >= 4) || weekday === "Sat" || (weekday === "Sun" && hour < 4);
}

// The daily watchlist rolls over at 3:30 AM ET, including on daylight-saving days.
function watchlistDay(now = new Date()) {
  const { year, month, day, hour, minute } = Object.fromEntries(
    new Intl.DateTimeFormat("en-US", {
      timeZone: TIME_ZONE, year: "numeric", month: "2-digit", day: "2-digit",
      hour: "2-digit", minute: "2-digit", hourCycle: "h23",
    }).formatToParts(now).map(({ type, value }) => [type, Number(value)])
  );
  return new Date(Date.UTC(year, month - 1, day, hour, minute - (3 * 60 + 30))).toISOString().slice(0, 10);
}

const weekdayFormat = new Intl.DateTimeFormat("en-US", { timeZone: TIME_ZONE, weekday: "long" });

async function fetchJson(url) {
  const response = await fetch(url, { cache: "no-store" });
  if (!response.ok) throw new Error(`Request failed: ${response.status}`);
  return response.json();
}

const byTime = (games) => [...games].sort((a, b) => new Date(a.timeIso) - new Date(b.timeIso));

function andyTv() {
  return {
    model: {
      date: "",
      updated: "",
      top: { picks: [], games: [] },
      plan: { summary: "", steps: [] },
    },
    weekendModel: null,
    get tabs() {
      return ["top", "timeline", "plan", ...(this.weekendModel ? ["weekend"] : [])];
    },
    get displayedModel() {
      return this.activeTab === "weekend" ? this.weekendModel : this.model;
    },
    // The feed arrives in rank order; Weekend can flip it to start-time order and back.
    get rankedGames() {
      const games = this.bySport(this.displayedModel.top.games);
      return this.activeTab === "weekend" && this.weekendView === "timeline" ? byTime(games) : games;
    },
    // Sports in the current list, most games first, for the filter chips.
    get sports() {
      const counts = new Map();
      for (const game of this.displayedModel.top.games) {
        const entry = counts.get(game.sport) ?? { name: game.sport, icon: game.icon, count: 0 };
        entry.count++;
        counts.set(game.sport, entry);
      }
      return [...counts.values()].sort((a, b) => b.count - a.count);
    },
    sport: "",
    activeTab: "top",
    weekendView: "top",
    a2hs: null,
    loadedDay: "",

    init() {
      // Static control/tab icons are present at parse time; sport icons in the data are emojis.
      lucide.createIcons();
      this.initAddToHomeScreen();
      this.syncTabFromPath();
      this.load();
      this.loadWeekend();
    },

    async load() {
      const day = watchlistDay();
      try {
        this.model = await fetchJson(DATA_URL);
        this.loadedDay = day;
      } catch {
        this.model.date = "Could not load the watchlist.";
      }
    },

    // Called only on page load. Outside the window, never request the weekend file.
    async loadWeekend() {
      if (!isWeekendWindow()) return;
      try {
        this.weekendModel = await fetchJson(WEEKEND_DATA_URL);
        this.syncTabFromPath();
      } catch {
        // The weekend file is optional; leave its tab hidden if unavailable.
      }
    },

    // Resume refresh is only for the existing daily watchlist.
    refreshIfNewDay() {
      if (document.visibilityState === "visible" && this.loadedDay && this.loadedDay !== watchlistDay()) this.load();
    },

    eventTime(event) {
      if (this.activeTab !== "weekend") return event.time;
      return `${weekdayFormat.format(new Date(event.timeIso))} · ${event.time}`;
    },

    // ---- clean-path tab routing (no '#') ----

    syncTabFromPath() {
      const segment = location.pathname.replace(/^\/+|\/+$/g, "");
      this.activeTab = this.tabs.includes(segment) ? segment : "top";
      this.sport = "";
    },

    go(tab) {
      this.activeTab = tab;
      this.sport = "";
      history.pushState({ tab }, "", tab === "top" ? "/" : "/" + tab);
    },

    bySport(games) {
      return this.sport ? games.filter((game) => game.sport === this.sport) : games;
    },

    // Timeline is the Top games re-sorted by start time (kept out of the JSON to avoid duplication).
    get timelineGames() {
      return byTime(this.bySport(this.model.top.games));
    },

    // ---- watch-plan alternates: up to two options plus a "+N more" overflow ----

    altText(alternates) {
      const chips = alternates
        .slice(0, 2)
        .map((a) => `${a.icon} ${a.matchup}`)
        .join(" · ");
      const extra = alternates.length - 2;
      return chips + (extra > 0 ? ` · +${extra} more` : "");
    },

    // ---- theme ----

    toggleTheme() {
      const next =
        document.documentElement.getAttribute("data-theme") === "light" ? "dark" : "light";
      document.documentElement.setAttribute("data-theme", next);
      localStorage.setItem("andytv-theme", next);
    },

    // Native Web Share only; the button is hidden when unsupported.
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
  };
}

window.andyTv = andyTv;
