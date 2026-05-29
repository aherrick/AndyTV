namespace AndyTV.Maui.Services;

public interface IRemoteCommandService
{
    event EventHandler ToggleMuteRequested;
    event EventHandler NextChannelRequested;
    event EventHandler PreviousChannelRequested;

    void Start();
    void Stop();
    void SetNowPlaying(string channelName, bool isMuted);
}
