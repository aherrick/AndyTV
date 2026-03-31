namespace AndyTV.Maui.Views;

public partial class AVKitTestPage : ContentPage
{
    private bool _hasPromptedForUrl;

    public AVKitTestPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_hasPromptedForUrl)
        {
            return;
        }

        _hasPromptedForUrl = true;
        await PromptForUrlAndPlay();
    }

    private async void OnPromptClicked(object sender, EventArgs e)
    {
        await PromptForUrlAndPlay();
    }

    private async void OnPlayClicked(object sender, EventArgs e)
    {
        var url = UrlEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            await PromptForUrlAndPlay();
            return;
        }

        await PlayUrl(url);
    }

    private async Task PromptForUrlAndPlay()
    {
        var promptResult = await DisplayPromptAsync(
            "AVKit Test",
            "Paste a stream URL to play.",
            accept: "Play",
            cancel: "Cancel",
            placeholder: "https://...",
            initialValue: UrlEntry.Text?.Trim(),
            keyboard: Keyboard.Url);

        if (string.IsNullOrWhiteSpace(promptResult))
        {
            return;
        }

        var url = promptResult.Trim();
        UrlEntry.Text = url;
        await PlayUrl(url);
    }

    private async Task PlayUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            await DisplayAlertAsync("Invalid URL", "Enter a valid absolute URL.", "OK");
            return;
        }

#if IOS
        PlayNative(url);
#else
        await DisplayAlertAsync("Unsupported Platform", "AVKit playback is only available on iOS.", "OK");
#endif
    }

#if IOS
    private static void PlayNative(string streamUrl)
    {
        var url = Foundation.NSUrl.FromString(streamUrl);
        if (url == null)
        {
            return;
        }

        // Many IPTV/HLS providers block Apple's default UA; use a VLC-style UA to pass.
        var headers = Foundation.NSDictionary.FromObjectAndKey(
            new Foundation.NSString("VLC/3.0.0 LibVLC/3.0.0"),
            new Foundation.NSString("User-Agent"));

        var assetOptions = Foundation.NSDictionary.FromObjectAndKey(
            headers,
            new Foundation.NSString("AVURLAssetHTTPHeaderFieldsKey"));

        var asset = new AVFoundation.AVUrlAsset(url, assetOptions);
        var playerItem = new AVFoundation.AVPlayerItem(asset);
        var player = new AVFoundation.AVPlayer(playerItem);
        var playerVC = new AVKit.AVPlayerViewController { Player = player };

        var window = UIKit.UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIKit.UIWindowScene>()
            .SelectMany(s => s.Windows)
            .FirstOrDefault(w => w.IsKeyWindow);

        var rootVC = window?.RootViewController;
        while (rootVC?.PresentedViewController != null)
        {
            rootVC = rootVC.PresentedViewController;
        }

        rootVC?.PresentViewController(playerVC, true, () => player.Play());
    }
#endif
}
