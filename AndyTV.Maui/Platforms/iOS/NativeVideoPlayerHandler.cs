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
    private NSObject _playbackStalledObserver;

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
        _player = new AVPlayer
        {
            AutomaticallyWaitsToMinimizeStalling = false,
            ActionAtItemEnd = AVPlayerActionAtItemEnd.None,
        };

        _playerViewController = new AVPlayerViewController
        {
            Player = _player,
            ShowsPlaybackControls = true,
            AllowsPictureInPicturePlayback = true,
        };

        var interval = CMTime.FromSeconds(1, 1);
        _timeObserver = _player.AddPeriodicTimeObserver(interval, null, _ =>
        {
            var isActivelyPlaying = _player.TimeControlStatus == AVPlayerTimeControlStatus.Playing;
            var isWaitingForData = _player.TimeControlStatus == AVPlayerTimeControlStatus.WaitingToPlayAtSpecifiedRate;

            // Buffering is not a user pause. Keep the health monitor armed for stalled live HLS.
            VirtualView?.SetPaused(!isActivelyPlaying && !isWaitingForData);

            if (isActivelyPlaying)
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

    protected override void DisconnectHandler(UIView platformView)
    {
        if (_playbackStalledObserver != null)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(_playbackStalledObserver);
            _playbackStalledObserver.Dispose();
            _playbackStalledObserver = null;
        }

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
        if (string.IsNullOrEmpty(player.Source))
        {
            handler._player.ReplaceCurrentItemWithPlayerItem(null);
            player.SetPaused(true);
            return;
        }

        var url = NSUrl.FromString(player.Source);
        if (url == null)
        {
            handler._player.ReplaceCurrentItemWithPlayerItem(null);
            player.SetPaused(true);
            return;
        }

        if (handler._playbackStalledObserver != null)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(handler._playbackStalledObserver);
            handler._playbackStalledObserver.Dispose();
            handler._playbackStalledObserver = null;
        }

        var item = new AVPlayerItem(url)
        {
            PreferredForwardBufferDuration = 1,
            CanUseNetworkResourcesForLiveStreamingWhilePaused = true,
        };

        handler._player.ReplaceCurrentItemWithPlayerItem(item);
        player.SetPaused(false);

        handler._playbackStalledObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            AVPlayerItem.PlaybackStalledNotification,
            _ =>
            {
                if (handler._player?.CurrentItem == item)
                {
                    handler._player.PlayImmediatelyAtRate(1f);
                }
            },
            item
        );
    }

    private static void MapPlay(NativeVideoPlayerHandler handler, NativeVideoPlayer player, object args)
    {
        player.SetPaused(false);
        handler._player?.PlayImmediatelyAtRate(1f);
    }

    private static void MapStop(NativeVideoPlayerHandler handler, NativeVideoPlayer player, object args)
    {
        player.SetPaused(true);
        handler._player?.Pause();
        handler._player?.ReplaceCurrentItemWithPlayerItem(null);
    }
}
