#if IOS
using AVFoundation;
using Foundation;
using MediaPlayer;

namespace AndyTV.Maui.Services;

public sealed class RemoteCommandService : IRemoteCommandService
{
    private NSObject _toggleToken;
    private NSObject _playToken;
    private NSObject _pauseToken;
    private NSObject _nextToken;
    private NSObject _previousToken;

    public event EventHandler ToggleMuteRequested;
    public event EventHandler NextChannelRequested;
    public event EventHandler PreviousChannelRequested;

    public void Start()
    {
        var commandCenter = MPRemoteCommandCenter.Shared;

        commandCenter.TogglePlayPauseCommand.Enabled = true;
        commandCenter.PlayCommand.Enabled = true;
        commandCenter.PauseCommand.Enabled = true;
        commandCenter.NextTrackCommand.Enabled = true;
        commandCenter.PreviousTrackCommand.Enabled = true;

        _toggleToken = commandCenter.TogglePlayPauseCommand.AddTarget(_ =>
        {
            ToggleMuteRequested?.Invoke(this, EventArgs.Empty);
            return MPRemoteCommandHandlerStatus.Success;
        });

        _playToken = commandCenter.PlayCommand.AddTarget(_ =>
        {
            ToggleMuteRequested?.Invoke(this, EventArgs.Empty);
            return MPRemoteCommandHandlerStatus.Success;
        });

        _pauseToken = commandCenter.PauseCommand.AddTarget(_ =>
        {
            ToggleMuteRequested?.Invoke(this, EventArgs.Empty);
            return MPRemoteCommandHandlerStatus.Success;
        });

        _nextToken = commandCenter.NextTrackCommand.AddTarget(_ =>
        {
            NextChannelRequested?.Invoke(this, EventArgs.Empty);
            return MPRemoteCommandHandlerStatus.Success;
        });

        _previousToken = commandCenter.PreviousTrackCommand.AddTarget(_ =>
        {
            PreviousChannelRequested?.Invoke(this, EventArgs.Empty);
            return MPRemoteCommandHandlerStatus.Success;
        });

        SetNowPlaying("Andy TV", false);
    }

    public void Stop()
    {
        var commandCenter = MPRemoteCommandCenter.Shared;

        if (_toggleToken is not null)
        {
            commandCenter.TogglePlayPauseCommand.RemoveTarget(_toggleToken);
            _toggleToken = null;
        }

        if (_playToken is not null)
        {
            commandCenter.PlayCommand.RemoveTarget(_playToken);
            _playToken = null;
        }

        if (_pauseToken is not null)
        {
            commandCenter.PauseCommand.RemoveTarget(_pauseToken);
            _pauseToken = null;
        }

        if (_nextToken is not null)
        {
            commandCenter.NextTrackCommand.RemoveTarget(_nextToken);
            _nextToken = null;
        }

        if (_previousToken is not null)
        {
            commandCenter.PreviousTrackCommand.RemoveTarget(_previousToken);
            _previousToken = null;
        }
    }

    public void SetNowPlaying(string channelName, bool isMuted)
    {
        var info = new MPNowPlayingInfo
        {
            Title = channelName ?? "Andy TV",
            Artist = isMuted ? "Muted" : "Playing",
            PlaybackRate = isMuted ? 0 : 1
        };

        MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = info;
    }
}
#endif
