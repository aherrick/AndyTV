using AndyTV.Maui.Controls;
using AVFoundation;
using AVKit;
using Foundation;
using Microsoft.Maui.Handlers;
using UIKit;

namespace AndyTV.Maui.Platforms.iOS.Handlers;

public class NativeVideoPlayerHandler : ViewHandler<NativeVideoPlayer, UIView>
{
    private AVPlayer? _player;
    private AVPlayerViewController? _playerViewController;

    public static IPropertyMapper<NativeVideoPlayer, NativeVideoPlayerHandler> PropertyMapper =
        new PropertyMapper<NativeVideoPlayer, NativeVideoPlayerHandler>(ViewMapper)
        {
            [nameof(NativeVideoPlayer.Source)] = MapSource
        };

    public static CommandMapper<NativeVideoPlayer, NativeVideoPlayerHandler> CommandMapper =
        new(ViewCommandMapper)
        {
            [nameof(NativeVideoPlayer.Play)] = MapPlay,
            [nameof(NativeVideoPlayer.Stop)] = MapStop
        };

    public NativeVideoPlayerHandler() : base(PropertyMapper, CommandMapper)
    {
    }

    protected override UIView CreatePlatformView()
    {
        _playerViewController = new AVPlayerViewController
        {
            AllowsPictureInPicturePlayback = true,
            ShowsPlaybackControls = true
        };

        return _playerViewController.View!;
    }

    private static void MapSource(NativeVideoPlayerHandler handler, NativeVideoPlayer player)
    {
        if (string.IsNullOrEmpty(player.Source))
            return;

        var url = NSUrl.FromString(player.Source);
        if (url == null)
            return;

        handler._player = new AVPlayer(url);
        handler._playerViewController!.Player = handler._player;
        handler._player.Play();
    }

    private static void MapPlay(NativeVideoPlayerHandler handler, NativeVideoPlayer player, object? args)
    {
        handler._player?.Play();
    }

    private static void MapStop(NativeVideoPlayerHandler handler, NativeVideoPlayer player, object? args)
    {
        handler._player?.Pause();
    }

    protected override void DisconnectHandler(UIView platformView)
    {
        _player?.Pause();
        _player?.Dispose();
        _playerViewController?.Dispose();
        base.DisconnectHandler(platformView);
    }
}
