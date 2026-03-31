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
    private IDisposable _itemStatusObserver;

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

        var view = _playerViewController.View;
        view.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        return view;
    }

    protected override void ConnectHandler(UIView platformView)
    {
        base.ConnectHandler(platformView);

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
        _itemStatusObserver?.Dispose();
        _itemStatusObserver = null;

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

    private static void ShowAlert(string title, string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var alert = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);
            alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));

            var vc = UIApplication.SharedApplication.ConnectedScenes
                .OfType<UIWindowScene>()
                .SelectMany(s => s.Windows)
                .FirstOrDefault(w => w.IsKeyWindow)
                ?.RootViewController;

            while (vc?.PresentedViewController != null)
            {
                vc = vc.PresentedViewController;
            }

            vc?.PresentViewController(alert, true, null);
        });
    }

    private static void MapSource(NativeVideoPlayerHandler handler, NativeVideoPlayer player)
    {
        handler._itemStatusObserver?.Dispose();
        handler._itemStatusObserver = null;

        if (handler._timeObserver != null)
        {
            handler._player?.RemoveTimeObserver(handler._timeObserver);
            handler._timeObserver = null;
        }

        handler._player?.Pause();

        if (string.IsNullOrEmpty(player.Source))
        {
            return;
        }

        var url = NSUrl.FromString(player.Source);
        if (url == null)
        {
            ShowAlert("Player Error", $"Invalid URL: {player.Source}");
            return;
        }

        ShowAlert("Debug", $"Loading: {player.Source}");

        // IPTV servers commonly block iOS's default AVPlayer User-Agent.
        // Use AVURLAsset with a custom User-Agent that matches VLC so the
        // server accepts the request and returns a valid HLS playlist.
        var headers = NSDictionary.FromObjectsAndKeys(
            new NSObject[] { new NSString("VLC/3.0.21 LibVLC/3.0.21") },
            new NSObject[] { new NSString("User-Agent") }
        );
        var asset = new AVUrlAsset(url, new AVUrlAssetOptions(headers));
        var item = new AVPlayerItem(asset);
        var newPlayer = new AVPlayer(item);
        handler._player = newPlayer;
        handler._playerViewController.Player = newPlayer;

        // Observe item status to catch errors
        handler._itemStatusObserver = item.AddObserver(
            "status",
            NSKeyValueObservingOptions.New | NSKeyValueObservingOptions.Initial,
            _ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (item.Status == AVPlayerItemStatus.Failed)
                    {
                        var err = item.Error;
                        var logLines = $"Code: {err?.Code}\nDomain: {err?.Domain}\n{err?.LocalizedDescription}\n{err?.LocalizedFailureReason}";

                        var errorLog = item.ErrorLog;
                        if (errorLog?.Events != null && errorLog.Events.Length > 0)
                        {
                            foreach (var evt in errorLog.Events)
                            {
                                logLines += $"\n---\nURI: {evt.Uri}\nHTTP: {evt.ErrorStatusCode}\n{evt.ErrorComment}";
                            }
                        }
                        else
                        {
                            logLines += "\n(no error log events)";
                        }

                        ShowAlert("AVPlayerItem Failed", logLines);
                    }
                    else if (item.Status == AVPlayerItemStatus.ReadyToPlay)
                    {
                        ShowAlert("Debug", "ReadyToPlay — calling Play()");
                        newPlayer.Play();
                    }
                });
            });

        // Time observer for activity
        var interval = CMTime.FromSeconds(1, 1);
        handler._timeObserver = newPlayer.AddPeriodicTimeObserver(interval, null, _ =>
        {
            handler.VirtualView?.SetPaused(newPlayer.TimeControlStatus == AVPlayerTimeControlStatus.Paused);
            if (newPlayer.TimeControlStatus == AVPlayerTimeControlStatus.Playing)
            {
                handler.VirtualView?.OnPlaybackActivity();
            }
        });
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
