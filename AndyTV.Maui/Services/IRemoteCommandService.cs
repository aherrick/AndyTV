namespace AndyTV.Maui.Services;

public enum RemoteCommandKind
{
    ToggleMute,
    VolumeUp,
    VolumeDown,
    RecentNext,
    RecentPrevious,
}

public sealed class RemoteCommandEventArgs(RemoteCommandKind kind) : EventArgs
{
    public RemoteCommandKind Kind { get; } = kind;
}

public interface IRemoteCommandService
{
    event EventHandler<RemoteCommandEventArgs> CommandReceived;

    void Start();
    void Stop();
    void SetNowPlaying(string channelName, bool isMuted);
}
