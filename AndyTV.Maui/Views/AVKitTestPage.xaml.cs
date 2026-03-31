namespace AndyTV.Maui.Views;

public partial class AVKitTestPage : ContentPage
{
    public AVKitTestPage()
    {
        InitializeComponent();
    }

    private void OnPlayClicked(object sender, EventArgs e)
    {
        var url = UrlEntry.Text?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

#if IOS
        PlayNative(url);
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

        var player = new AVFoundation.AVPlayer(url);
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
