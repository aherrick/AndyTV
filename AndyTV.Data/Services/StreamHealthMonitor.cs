namespace AndyTV.Data.Services;

public sealed class StreamHealthMonitor(Func<bool> isPaused, Action restart, int stallSeconds = 4, Action<string>? logger = null)
{
    public const int DefaultStallSeconds = 4;

    private readonly long _stallThresholdTicks = TimeSpan.FromSeconds(stallSeconds).Ticks;
    private readonly Func<bool> _isPaused = isPaused;
    private readonly Action _restart = restart;
    private readonly Action<string>? _logger = logger;

    private long _lastActivityUtcTicks = DateTime.UtcNow.Ticks;
    private int _isRestarting;

    public void MarkActivity()
    {
        Interlocked.Exchange(ref _lastActivityUtcTicks, DateTime.UtcNow.Ticks);
    }

    public void Tick()
    {
        // Called periodically by a timer (UI timer in MAUI/WinForms).
        // Purpose: detect "no playback activity for N seconds" and trigger a restart.

        if (_isPaused())
            return;

        var nowTicks = DateTime.UtcNow.Ticks;

        var lastTicks = Interlocked.Read(ref _lastActivityUtcTicks);
        var inactiveTicks = nowTicks - lastTicks;
        var inactiveSeconds = TimeSpan.FromTicks(inactiveTicks).TotalSeconds;

        // If we've seen any activity recently, don't restart.
        if (inactiveTicks < _stallThresholdTicks)
        {
            if (inactiveSeconds > 1)
            {
                _logger?.Invoke($"[Health] Tick: Inactive for {inactiveSeconds:F2}s (Threshold: {stallSeconds}s)");
            }
            return;
        }

        _logger?.Invoke($"[Health] Stalled! Inactive for {inactiveSeconds:F2}s. Attempting restart...");

        // Prevent overlapping restart attempts.
        if (Interlocked.CompareExchange(ref _isRestarting, 1, 0) != 0)
        {
            _logger?.Invoke("[Health] Restart already in progress");
            return;
        }

        try
        {
            // "Claim" this stall window so we don't spam restarts every Tick() until activity resumes.
            Interlocked.Exchange(ref _lastActivityUtcTicks, nowTicks);
            _restart();
        }
        finally
        {
            Interlocked.Exchange(ref _isRestarting, 0);
        }
    }
}