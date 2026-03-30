using AndyTV.Maui.Controls;
using AVFoundation;
using AVKit;
using CoreMedia;
using Foundation;
using Microsoft.Maui.Handlers;
using UIKit;

namespace AndyTV.Maui;

public class NativeVideoPlayerHandler : ViewHandler<NativeVideoPlayer, UIView>
{
    private AVPlayer _player;
    private AVPlayerViewController _playerViewController;
    private NSObject _timeObserver;

    public static IPropertyMapper<NativeVideoPlayer, NativeVideoPlayerHandler> Mapper =
        new PropertyMapper<NativeVideoPlayer, NativeVideoPlayerHandler>(ViewMapper)
        {
            [nameof(NativeVideoPlayer.Source)] = MapSource,
        };

    public static CommandMapper<NativeVideoPlayer, NativeVideoPlayerHandler> Commands =
        new(ViewCommandMapper)
        {
            [nameof(NativeVideoPlayer.Play)] = MapPlay,
            [nameof(NativeVideoPlayer.Stop)] = MapStop,
        };

    public NativeVideoPlayerHandler() : base(Mapper, Commands) { }

    protected override UIView CreatePlatformView()
    {
        _player = new AVPlayer();
        _playerViewController = new AVPlayerViewController
        {
            Player = _player,
            ShowsPlaybackControls = true,
            AllowsPictureInPicturePlayback = true,
        };

        AttachTimeObserver();

        var view = _playerViewController.View;
        view.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        return view;
    }

    private void AttachTimeObserver()
    {
        if (_timeObserver != null)
        {
            _player?.RemoveTimeObserver(_timeObserver);
            _timeObserver = null;
        }

        var interval = CMTime.FromSeconds(1, 1);
        _timeObserver = _player.AddPeriodicTimeObserver(interval, null, _ =>
        {
            var status = _player.TimeControlStatus;
            var active = status != AVPlayerTimeControlStatus.Paused;

            VirtualView?.SetPaused(!active);
            if (status == AVPlayerTimeControlStatus.Playing)
            {
                VirtualView?.OnPlaybackActivity();
            }
        });
    }

    protected override void ConnectHandler(UIView platformView)
    {
        base.ConnectHandler(platformView);

        // Proper VC containment is required for PiP to function
        var window = UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>()
            .SelectMany(s => s.Windows)
            .FirstOrDefault(w => w.IsKeyWindow);

        var parentVC = window?.RootViewController;
        while (parentVC?.PresentedViewController != null)
        {
            parentVC = parentVC.PresentedViewController;
        }

        if (parentVC != null)
        {
            parentVC.AddChildViewController(_playerViewController);
            _playerViewController.DidMoveToParentViewController(parentVC);
        }
    }

    protected override void DisconnectHandler(UIView platformView)
    {
        if (_timeObserver != null)
        {
            _player?.RemoveTimeObserver(_timeObserver);
            _timeObserver = null;
        }

        _player?.Pause();
        _playerViewController?.WillMoveToParentViewController(null);
        _playerViewController?.RemoveFromParentViewController();
        base.DisconnectHandler(platformView);
    }

    private static void MapSource(NativeVideoPlayerHandler handler, NativeVideoPlayer player)
    {
        if (string.IsNullOrEmpty(player.Source))
        {
            handler._player?.Pause();
            return;
        }

        var url = NSUrl.FromString(player.Source);
        if (url == null)
        {
            return;
        }

        // Create a brand new AVPlayer with the URL. This is the pattern Apple
        // recommends and the only one that reliably bootstraps HLS playback.
        handler._player?.Pause();
        if (handler._timeObserver != null)
        {
            handler._player?.RemoveTimeObserver(handler._timeObserver);
            handler._timeObserver = null;
        }

        handler._player = new AVPlayer(url);
        handler._playerViewController.Player = handler._player;
        handler.AttachTimeObserver();
        handler._player.Play();
    }

    private static void MapPlay(NativeVideoPlayerHandler handler, NativeVideoPlayer player, object args)
    {
        handler._player?.Play();
    }

    private static void MapStop(NativeVideoPlayerHandler handler, NativeVideoPlayer player, object args)
    {
        handler._player?.Pause();
    }
}
