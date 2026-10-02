using Foundation;
using MediaPlayer;

namespace AndyTV.Maui.Services;

// Lock screen / Control Center card with play/pause for the current live stream.
public static class NowPlaying
{
    private static readonly List<(MPRemoteCommand Command, NSObject Target)> Targets = [];
    private static bool _playing;

    public static void Register(Action play, Action pause)
    {
        Clear();
        var center = MPRemoteCommandCenter.Shared;
        Add(center.PlayCommand, play);
        Add(center.PauseCommand, pause);
        Add(center.TogglePlayPauseCommand, () => (_playing ? pause : play)());
    }

    public static void Update(string title, bool playing)
    {
        _playing = playing;
        MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = new MPNowPlayingInfo
        {
            Title = title,
            IsLiveStream = true,
            PlaybackRate = playing ? 1 : 0,
        };
    }

    public static void Clear()
    {
        foreach (var (command, target) in Targets)
        {
            command.RemoveTarget(target);
        }
        Targets.Clear();
        MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = null;
    }

    private static void Add(MPRemoteCommand command, Action action)
    {
        command.Enabled = true;
        var target = command.AddTarget(_ =>
        {
            MainThread.BeginInvokeOnMainThread(action);
            return MPRemoteCommandHandlerStatus.Success;
        });
        Targets.Add((command, target));
    }
}
