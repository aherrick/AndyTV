using AndyTV.Maui.Controls;
using Foundation;
using Microsoft.Maui.Handlers;
using UIKit;
using WebKit;

namespace AndyTV.Maui;

public class NativeVideoPlayerHandler : ViewHandler<NativeVideoPlayer, WKWebView>
{
    private string _currentSource;

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

    protected override WKWebView CreatePlatformView()
    {
        var config = new WKWebViewConfiguration
        {
            AllowsInlineMediaPlayback = true,
            AllowsAirPlayForMediaPlayback = true,
            AllowsPictureInPictureMediaPlayback = true,
        };
        config.MediaTypesRequiringUserActionForPlayback = WKAudiovisualMediaTypes.None;

        var webView = new WKWebView(CoreGraphics.CGRect.Empty, config)
        {
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
            BackgroundColor = UIColor.Black,
            Opaque = false,
            ScrollView = { ScrollEnabled = false },
        };

        return webView;
    }

    protected override void DisconnectHandler(WKWebView platformView)
    {
        platformView.LoadHtmlString("<html><body style='background:black'></body></html>", null);
        base.DisconnectHandler(platformView);
    }

    private static string BuildHtml(string url) =>
        $$"""
        <!DOCTYPE html>
        <html>
        <head>
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { background: black; width: 100vw; height: 100vh; }
        video { width: 100%; height: 100%; object-fit: contain; }
        </style>
        </head>
        <body>
        <video id="v" src="{{url}}" autoplay playsinline controls></video>
        </body>
        </html>
        """;

    private static void MapSource(NativeVideoPlayerHandler handler, NativeVideoPlayer player)
    {
        if (string.IsNullOrEmpty(player.Source))
        {
            handler.PlatformView.LoadHtmlString("<html><body style='background:black'></body></html>", null);
            return;
        }

        handler._currentSource = player.Source;
        handler.PlatformView.LoadHtmlString(BuildHtml(player.Source), new NSUrl(player.Source));
        player.SetPaused(false);
    }

    private static void MapPlay(NativeVideoPlayerHandler handler, NativeVideoPlayer player, object args)
    {
        handler.PlatformView.EvaluateJavaScript("document.getElementById('v').play()", null);
        player.SetPaused(false);
    }

    private static void MapStop(NativeVideoPlayerHandler handler, NativeVideoPlayer player, object args)
    {
        handler.PlatformView.EvaluateJavaScript("document.getElementById('v').pause()", null);
        player.SetPaused(true);
    }
}
