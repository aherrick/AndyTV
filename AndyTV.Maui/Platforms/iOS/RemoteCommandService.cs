#if IOS
using AVFoundation;
using CoreGraphics;
using Foundation;
using MediaPlayer;
using UIKit;

namespace AndyTV.Maui.Services;

public sealed class RemoteCommandService : IRemoteCommandService
{
    private const int HidKeyboardReturnOrEnter = 40;
    private const int HidKeyboardSpacebar = 44;
    private const int HidKeyboardRightArrow = 79;
    private const int HidKeyboardLeftArrow = 80;
    private const int HidKeyboardDownArrow = 81;
    private const int HidKeyboardUpArrow = 82;

    private NSObject _toggleToken;
    private NSObject _playToken;
    private NSObject _pauseToken;
    private NSObject _nextToken;
    private NSObject _previousToken;
    private HardwareInputView _hardwareInputView;
    private bool _started;

    public event EventHandler<RemoteCommandEventArgs> CommandReceived;

    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        ConfigureAudioSession();

        var commandCenter = MPRemoteCommandCenter.Shared;

        commandCenter.TogglePlayPauseCommand.Enabled = true;
        commandCenter.PlayCommand.Enabled = true;
        commandCenter.PauseCommand.Enabled = true;
        commandCenter.NextTrackCommand.Enabled = true;
        commandCenter.PreviousTrackCommand.Enabled = true;

        _toggleToken = commandCenter.TogglePlayPauseCommand.AddTarget(_ =>
        {
            Publish(RemoteCommandKind.ToggleMute, "media-command:TogglePlayPause");
            return MPRemoteCommandHandlerStatus.Success;
        });

        _playToken = commandCenter.PlayCommand.AddTarget(_ =>
        {
            Publish(RemoteCommandKind.ToggleMute, "media-command:Play");
            return MPRemoteCommandHandlerStatus.Success;
        });

        _pauseToken = commandCenter.PauseCommand.AddTarget(_ =>
        {
            Publish(RemoteCommandKind.ToggleMute, "media-command:Pause");
            return MPRemoteCommandHandlerStatus.Success;
        });

        _nextToken = commandCenter.NextTrackCommand.AddTarget(_ =>
        {
            Publish(RemoteCommandKind.RecentNext, "media-command:NextTrack");
            return MPRemoteCommandHandlerStatus.Success;
        });

        _previousToken = commandCenter.PreviousTrackCommand.AddTarget(_ =>
        {
            Publish(RemoteCommandKind.RecentPrevious, "media-command:PreviousTrack");
            return MPRemoteCommandHandlerStatus.Success;
        });

        MainThread.BeginInvokeOnMainThread(AttachHardwareInputView);
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

        MainThread.BeginInvokeOnMainThread(DetachHardwareInputView);
        _started = false;
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

    private void ConfigureAudioSession()
    {
        try
        {
            var session = AVAudioSession.SharedInstance();
            session.SetCategory(AVAudioSessionCategory.Playback);
            session.SetActive(true);
        }
        catch (Exception ex)
        {
            Publish(RemoteCommandKind.Unknown, "audio-session", ex.Message);
        }
    }

    private void AttachHardwareInputView()
    {
        if (_hardwareInputView is not null)
        {
            _hardwareInputView.BecomeFirstResponder();
            return;
        }

        var rootView = GetRootView();
        if (rootView is null)
        {
            Publish(RemoteCommandKind.Unknown, "hardware-input", "Root view missing");
            return;
        }

        _hardwareInputView = new HardwareInputView(HandleHardwarePress)
        {
            Frame = CGRect.Empty,
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth
                | UIViewAutoresizing.FlexibleHeight,
            BackgroundColor = UIColor.Clear,
            UserInteractionEnabled = true,
            AccessibilityElementsHidden = true
        };

        rootView.AddSubview(_hardwareInputView);
        _hardwareInputView.BecomeFirstResponder();
    }

    private void DetachHardwareInputView()
    {
        if (_hardwareInputView is null)
        {
            return;
        }

        _hardwareInputView.ResignFirstResponder();
        _hardwareInputView.RemoveFromSuperview();
        _hardwareInputView.Dispose();
        _hardwareInputView = null;
    }

    private bool HandleHardwarePress(UIPress press)
    {
        var key = press.Key;
        var keyCode = key is null ? -1 : (int)key.KeyCode;

        // Up/Down = channel change
        // Left/Right = volume
        // Select/PlayPause/Enter/Space = toggle mute
        switch (press.Type)
        {
            case UIPressType.UpArrow:
                Publish(RemoteCommandKind.RecentNext, "hardware-press:UpArrow");
                return true;
            case UIPressType.DownArrow:
                Publish(RemoteCommandKind.RecentPrevious, "hardware-press:DownArrow");
                return true;
            case UIPressType.LeftArrow:
                Publish(RemoteCommandKind.VolumeDown, "hardware-press:LeftArrow");
                return true;
            case UIPressType.RightArrow:
                Publish(RemoteCommandKind.VolumeUp, "hardware-press:RightArrow");
                return true;
            case UIPressType.Select:
            case UIPressType.PlayPause:
                Publish(RemoteCommandKind.ToggleMute, $"hardware-press:{press.Type}");
                return true;
            case UIPressType.Menu:
                return false; // let system handle Home/Menu
        }

        switch (keyCode)
        {
            case HidKeyboardUpArrow:
                Publish(RemoteCommandKind.RecentNext, "hid-key:UpArrow");
                return true;
            case HidKeyboardDownArrow:
                Publish(RemoteCommandKind.RecentPrevious, "hid-key:DownArrow");
                return true;
            case HidKeyboardRightArrow:
                Publish(RemoteCommandKind.VolumeUp, "hid-key:RightArrow");
                return true;
            case HidKeyboardLeftArrow:
                Publish(RemoteCommandKind.VolumeDown, "hid-key:LeftArrow");
                return true;
            case HidKeyboardReturnOrEnter:
            case HidKeyboardSpacebar:
                Publish(RemoteCommandKind.ToggleMute, $"hid-key:{keyCode}");
                return true;
        }

        return false;
    }

    private static UIView GetRootView()
    {
        foreach (var scene in UIApplication.SharedApplication.ConnectedScenes)
        {
            if (scene is not UIWindowScene windowScene)
            {
                continue;
            }

            foreach (var window in windowScene.Windows)
            {
                if (window.IsKeyWindow)
                {
                    return window.RootViewController?.View;
                }
            }
        }

        return null;
    }

    private void Publish(RemoteCommandKind kind, string source, string details = null)
    {
        CommandReceived?.Invoke(this, new RemoteCommandEventArgs(kind, source, details));
    }

    private sealed class HardwareInputView(Func<UIPress, bool> handlePress) : UIView(CGRect.Empty)
    {
        public override bool CanBecomeFirstResponder => true;

        public override void MovedToWindow()
        {
            base.MovedToWindow();

            if (Window is not null)
            {
                BecomeFirstResponder();
            }
        }

        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            var handled = false;

            foreach (var press in presses)
            {
                handled |= handlePress(press);
            }

            if (!handled)
            {
                base.PressesBegan(presses, evt);
            }
        }
    }
}
#endif
