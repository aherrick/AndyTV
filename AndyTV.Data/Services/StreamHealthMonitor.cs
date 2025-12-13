namespace AndyTV.Data.Services;

public sealed class StreamHealthMonitor(Func<bool> isPaused, Action restart, int stallSeconds = 4)
{
    public const int DefaultStallSeconds = 4;

    private readonly long _stallThresholdTicks = TimeSpan.FromSeconds(stallSeconds).Ticks;
    private readonly Func<bool> _isPaused = isPaused;
    private readonly Action _restart = restart;

    private long _lastActivityUtcTicks = DateTime.UtcNow.Ticks;
    private int _isRestarting;

    public void MarkActivity()
    {
        Interlocked.Exchange(ref _lastActivityUtcTicks, DateTime.UtcNow.Ticks);
    }

    public void MarkPlaying()
    {
        Interlocked.Exchange(ref _isRestarting, 0);
        Interlocked.Exchange(ref _lastActivityUtcTicks, DateTime.UtcNow.Ticks);
    }

    public void Tick()
    {
        if (_isPaused())
            return;

        var nowTicks = DateTime.UtcNow.Ticks;
        var lastTicks = Interlocked.Read(ref _lastActivityUtcTicks);
        var inactiveTicks = nowTicks - lastTicks;

        if (inactiveTicks < _stallThresholdTicks)
            return;

        if (Interlocked.CompareExchange(ref _isRestarting, 1, 0) != 0)
            return;

        Interlocked.Exchange(ref _lastActivityUtcTicks, nowTicks);
        _restart();
    }
}
