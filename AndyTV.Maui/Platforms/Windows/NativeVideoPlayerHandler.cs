using AndyTV.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace AndyTV.Maui;

public class NativeVideoPlayerHandler : ViewHandler<NativeVideoPlayer, MediaPlayerElement>
{
    private MediaPlayer _mediaPlayer;

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

    protected override MediaPlayerElement CreatePlatformView()
    {
        _mediaPlayer = new MediaPlayer { AutoPlay = false };
        _mediaPlayer.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;

        var element = new MediaPlayerElement { AreTransportControlsEnabled = true };
        element.SetMediaPlayer(_mediaPlayer);
        return element;
    }

    private void OnPlaybackStateChanged(MediaPlaybackSession session, object args)
    {
        var playing = session.PlaybackState == MediaPlaybackState.Playing;
        VirtualView?.SetPaused(!playing);
        if (playing)
        {
            VirtualView?.OnPlaybackActivity();
        }
    }

    protected override void DisconnectHandler(MediaPlayerElement platformView)
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.PlaybackSession.PlaybackStateChanged -= OnPlaybackStateChanged;
            _mediaPlayer.Pause();
            _mediaPlayer.Dispose();
            _mediaPlayer = null;
        }

        base.DisconnectHandler(platformView);
    }

    private static void MapSource(NativeVideoPlayerHandler handler, NativeVideoPlayer player)
    {
        if (string.IsNullOrEmpty(player.Source))
        {
            return;
        }

        handler._mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(player.Source));
    }

    private static void MapPlay(NativeVideoPlayerHandler handler, NativeVideoPlayer player, object args)
    {
        handler._mediaPlayer?.Play();
    }

    private static void MapStop(NativeVideoPlayerHandler handler, NativeVideoPlayer player, object args)
    {
        if (handler._mediaPlayer != null)
        {
            handler._mediaPlayer.Pause();
            handler._mediaPlayer.Source = null;
        }
    }
}
