#if IOS
using AVFoundation;
using CoreGraphics;
using Foundation;
using MediaPlayer;
using UIKit;

namespace AndyTV.Maui.Services;

// Button mapping for the R2 / TikTok scroll ring on iOS:
//   Up    -> HID Volume Up   -> VolumeUp        (caught by AVAudioSession outputVolume KVO)
//   Down  -> HID Volume Down -> VolumeDown      (caught by AVAudioSession outputVolume KVO)
//   Left  -> Arrow Left      -> RecentPrevious  (caught by UIPress)
//   Right -> Arrow Right     -> RecentNext      (caught by UIPress)
//   OK    -> Select press    -> ToggleMute      (caught by UIPress)
//   Camera-> HID Volume Up   -> same as Up (iOS camera shutter convention)
public sealed class RemoteCommandService : IRemoteCommandService
{
    private HardwareInputView _inputView;
    private MPVolumeView _hiddenVolumeView;
    private VolumeObserver _volumeObserver;
    private float _lastVolume;
    private bool _suppressVolume;
    private bool _started;

    public event EventHandler<RemoteCommandEventArgs> CommandReceived;

    public void Start()
    {
        if (_started)
            return;
        _started = true;

        var session = AVAudioSession.SharedInstance();
        session.SetCategory(AVAudioSessionCategory.Playback);
        session.SetActive(true);

        MainThread.BeginInvokeOnMainThread(AttachToRootView);
        SetNowPlaying("Andy TV", false);
    }

    public void Stop()
    {
        if (!_started)
            return;
        _started = false;
        MainThread.BeginInvokeOnMainThread(DetachFromRootView);
    }

    public void SetNowPlaying(string channelName, bool isMuted)
    {
        MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = new MPNowPlayingInfo
        {
            Title = channelName ?? "Andy TV",
            Artist = isMuted ? "Muted" : "Playing",
            PlaybackRate = isMuted ? 0 : 1,
        };
    }

    private void AttachToRootView()
    {
        var rootView = GetRootView();
        if (rootView is null)
            return;

        _inputView = new HardwareInputView(HandlePress)
        {
            Frame = CGRect.Empty,
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
            BackgroundColor = UIColor.Clear,
            UserInteractionEnabled = true,
        };
        rootView.AddSubview(_inputView);
        _inputView.BecomeFirstResponder();

        // Hidden MPVolumeView suppresses the system volume HUD so the ring's
        // up/down (which send HID Volume Up/Down) don't show iOS volume bars.
        _hiddenVolumeView = new MPVolumeView(new CGRect(-1000, -1000, 1, 1)) { Alpha = 0.01f };
        rootView.AddSubview(_hiddenVolumeView);

        _lastVolume = AVAudioSession.SharedInstance().OutputVolume;
        _volumeObserver = new VolumeObserver(AVAudioSession.SharedInstance(), OnVolumeChanged);
    }

    private void DetachFromRootView()
    {
        _volumeObserver?.Dispose();
        _volumeObserver = null;

        if (_hiddenVolumeView is not null)
        {
            _hiddenVolumeView.RemoveFromSuperview();
            _hiddenVolumeView.Dispose();
            _hiddenVolumeView = null;
        }

        if (_inputView is not null)
        {
            _inputView.ResignFirstResponder();
            _inputView.RemoveFromSuperview();
            _inputView.Dispose();
            _inputView = null;
        }
    }

    private bool HandlePress(UIPress press)
    {
        switch (press.Type)
        {
            case UIPressType.LeftArrow:
                Publish(RemoteCommandKind.RecentPrevious);
                return true;
            case UIPressType.RightArrow:
                Publish(RemoteCommandKind.RecentNext);
                return true;
            case UIPressType.Select:
            case UIPressType.PlayPause:
                Publish(RemoteCommandKind.ToggleMute);
                return true;
            default:
                return false;
        }
    }

    private void OnVolumeChanged()
    {
        if (_suppressVolume)
            return;

        var newVolume = AVAudioSession.SharedInstance().OutputVolume;
        var delta = newVolume - _lastVolume;
        if (Math.Abs(delta) < 0.001f)
            return;

        // Restore system volume so the ring doesn't drift it up/down.
        RestoreSystemVolume(_lastVolume);

        Publish(delta > 0 ? RemoteCommandKind.VolumeUp : RemoteCommandKind.VolumeDown);
    }

    private void RestoreSystemVolume(float target)
    {
        _suppressVolume = true;
        if (_hiddenVolumeView is not null)
        {
            foreach (var sub in _hiddenVolumeView.Subviews)
            {
                if (sub is UISlider slider)
                {
                    slider.Value = target;
                    break;
                }
            }
        }
        Task.Delay(150).ContinueWith(_ =>
        {
            _lastVolume = AVAudioSession.SharedInstance().OutputVolume;
            _suppressVolume = false;
        });
    }

    private void Publish(RemoteCommandKind kind)
    {
        CommandReceived?.Invoke(this, new RemoteCommandEventArgs(kind));
    }

    private static UIView GetRootView()
    {
        foreach (var scene in UIApplication.SharedApplication.ConnectedScenes)
        {
            if (scene is UIWindowScene ws)
            {
                foreach (var window in ws.Windows)
                {
                    if (window.IsKeyWindow)
                        return window.RootViewController?.View;
                }
            }
        }
        return null;
    }

    private sealed class HardwareInputView(Func<UIPress, bool> handlePress) : UIView(CGRect.Empty)
    {
        public override bool CanBecomeFirstResponder => true;

        public override void MovedToWindow()
        {
            base.MovedToWindow();
            if (Window is not null)
                BecomeFirstResponder();
        }

        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            var handled = false;
            foreach (var p in presses)
            {
                if (handlePress(p))
                    handled = true;
            }
            if (!handled)
                base.PressesBegan(presses, evt);
        }
    }

    private sealed class VolumeObserver : NSObject
    {
        private readonly AVAudioSession _session;
        private readonly Action _onChanged;

        public VolumeObserver(AVAudioSession session, Action onChanged)
        {
            _session = session;
            _onChanged = onChanged;
            _session.AddObserver(this, "outputVolume", NSKeyValueObservingOptions.New, nint.Zero);
        }

        public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, nint context)
        {
            if (string.Equals(keyPath, "outputVolume", StringComparison.Ordinal))
                _onChanged();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { _session.RemoveObserver(this, "outputVolume"); } catch { }
            }
            base.Dispose(disposing);
        }
    }
}
#endif
