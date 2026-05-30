namespace AndyTV.Maui.Services;

public enum RemoteCommandKind
{
    ToggleMute,
    VolumeUp,
    VolumeDown,
    RecentNext,
    RecentPrevious,
    Unknown
}

public sealed class RemoteCommandEventArgs(
    RemoteCommandKind kind,
    string source,
    string details = null
) : EventArgs
{
    public RemoteCommandKind Kind { get; } = kind;
    public string Source { get; } = source;
    public string Details { get; } = details;
}

public interface IRemoteCommandService
{
    event EventHandler<RemoteCommandEventArgs> CommandReceived;

    void Start();
    void Stop();
    void SetNowPlaying(string channelName, bool isMuted);
}
