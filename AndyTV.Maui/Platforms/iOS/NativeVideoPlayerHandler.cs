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

        var interval = CMTime.FromSeconds(1, 1);
        _timeObserver = _player.AddPeriodicTimeObserver(interval, null, _ =>
        {
            var playing = _player.Rate > 0f;
            VirtualView?.SetPaused(!playing);
            if (playing)
            {
                VirtualView?.OnPlaybackActivity();
            }
        });

        var view = _playerViewController.View;
        view.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        return view;
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

    private IDisposable _statusObserver;

    protected override void DisconnectHandler(UIView platformView)
    {
        _statusObserver?.Dispose();
        _statusObserver = null;

        if (_timeObserver != null)
        {
            _player?.RemoveTimeObserver(_timeObserver);
            _timeObserver = null;
        }

        _player?.Pause();
        _player?.ReplaceCurrentItemWithPlayerItem(null);
        _playerViewController?.WillMoveToParentViewController(null);
        _playerViewController?.RemoveFromParentViewController();
        base.DisconnectHandler(platformView);
    }

    private static void MapSource(NativeVideoPlayerHandler handler, NativeVideoPlayer player)
    {
        handler._statusObserver?.Dispose();
        handler._statusObserver = null;

        if (string.IsNullOrEmpty(player.Source))
        {
            handler._player.ReplaceCurrentItemWithPlayerItem(null);
            return;
        }

        var url = NSUrl.FromString(player.Source);
        var item = new AVPlayerItem(AVAsset.FromUrl(url));
        
        handler._statusObserver = item.AddObserver(
            "status",
            NSKeyValueObservingOptions.New | NSKeyValueObservingOptions.Initial,
            change =>
            {
                if (item.Status == AVPlayerItemStatus.ReadyToPlay)
                {
                    handler._player.Play();
                }
            });

        handler._player.ReplaceCurrentItemWithPlayerItem(item);
    }

    private static void MapPlay(NativeVideoPlayerHandler handler, NativeVideoPlayer player, object args)
    {
        handler._player?.Play();
    }

    private static void MapStop(NativeVideoPlayerHandler handler, NativeVideoPlayer player, object args)
    {
        handler._player?.Pause();
        handler._player?.ReplaceCurrentItemWithPlayerItem(null);
    }
}
