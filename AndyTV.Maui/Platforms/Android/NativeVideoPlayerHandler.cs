using Android.Widget;
using AndyTV.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace AndyTV.Maui;

public class NativeVideoPlayerHandler : ViewHandler<NativeVideoPlayer, VideoView>
{
    private Timer _activityTimer;

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

    protected override VideoView CreatePlatformView()
    {
        var videoView = new VideoView(Context);
        var mediaController = new MediaController(Context);
        mediaController.SetAnchorView(videoView);
        videoView.SetMediaController(mediaController);

        _activityTimer = new Timer(_ =>
        {
            try
            {
                var playing = videoView.IsPlaying;
                VirtualView?.SetPaused(!playing);
                if (playing)
                {
                    VirtualView?.OnPlaybackActivity();
                }
            }
            catch
            {
                // VideoView may already be disposed
            }
        }, null, 1000, 1000);

        return videoView;
    }

    protected override void DisconnectHandler(VideoView platformView)
    {
        _activityTimer?.Dispose();
        platformView.StopPlayback();
        base.DisconnectHandler(platformView);
    }

    private static void MapSource(NativeVideoPlayerHandler handler, NativeVideoPlayer player)
    {
        if (string.IsNullOrEmpty(player.Source))
        {
            return;
        }

        handler.PlatformView.SetVideoURI(Android.Net.Uri.Parse(player.Source));
    }

    private static void MapPlay(NativeVideoPlayerHandler handler, NativeVideoPlayer player, object args)
    {
        handler.PlatformView.Start();
    }

    private static void MapStop(NativeVideoPlayerHandler handler, NativeVideoPlayer player, object args)
    {
        handler.PlatformView.StopPlayback();
    }
}
