namespace AndyTV.Maui.Services;

public sealed class NoopRemoteCommandService : IRemoteCommandService
{
    public event EventHandler<RemoteCommandEventArgs> CommandReceived
    {
        add { }
        remove { }
    }

    public void Start() { }
    public void Stop() { }
    public void SetNowPlaying(string channelName, bool isMuted) { }
}
