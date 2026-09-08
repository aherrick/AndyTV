namespace AndyTV.Guide.Shared;

// Bridges a "Watch" click in the shared guide to a host that can play it (the desktop app).
// Hosts without a player (e.g. the web guide) simply don't register one, so CanWatch is false.
public sealed class GuideWatchHandler
{
    public event Action<string> WatchRequested;

    public bool CanWatch => WatchRequested is not null;

    public void Watch(string streamingTvId) => WatchRequested?.Invoke(streamingTvId);
}
