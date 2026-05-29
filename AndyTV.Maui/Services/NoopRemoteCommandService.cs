namespace AndyTV.Maui.Services;

public sealed class NoopRemoteCommandService : IRemoteCommandService
{
    public event EventHandler ToggleMuteRequested;
    public event EventHandler NextChannelRequested;
    public event EventHandler PreviousChannelRequested;

    public void Start() { }
    public void Stop() { }
    public void SetNowPlaying(string channelName, bool isMuted) { }
}
