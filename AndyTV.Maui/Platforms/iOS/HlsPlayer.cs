using AVFoundation;
using AVKit;
using Foundation;
using UIKit;
using AndyTV.Maui.Services;

namespace AndyTV.Maui.Platforms.iOS;

public sealed class HlsPlayer : IHlsPlayer
{
    public Task<string> PlayHls(string url)
    {
        var tcs = new TaskCompletionSource<string>();

        if (string.IsNullOrWhiteSpace(url))
        {
            tcs.SetResult("URL is empty.");
            return tcs.Task;
        }

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            tcs.SetResult("URL uses http — iOS ATS may block this. Try https, or add ATS exception to Info.plist.");
            return tcs.Task;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var nsUrl = NSUrl.FromString(url);

                if (nsUrl is null)
                {
                    tcs.SetResult("Invalid URL — could not parse.");
                    return;
                }

                var item = new AVPlayerItem(nsUrl);

                NSNotificationCenter.DefaultCenter.AddObserver(
                    new NSString("AVPlayerItemFailedToPlayToEndTimeNotification"),
                    notification =>
                    {
                        var failedItem = notification.Object as AVPlayerItem;
                        var error = failedItem?.Error;
                        ShowNativeAlert("Playback Failed",
                            $"Error: {error?.LocalizedDescription ?? "unknown"}\n" +
                            $"Reason: {error?.LocalizedFailureReason ?? "none"}");
                    },
                    item);

                // Also observe item status for immediate load errors
                item.AddObserver(new ItemStatusObserver(tcs), "status", NSKeyValueObservingOptions.New, IntPtr.Zero);

                var player = new AVPlayer(item);
                var controller = new AVPlayerViewController
                {
                    Player = player,
                    ShowsPlaybackControls = true,
                    ModalPresentationStyle = UIModalPresentationStyle.FullScreen
                };

                var root = GetTopViewController();

                if (root is null)
                {
                    tcs.TrySetResult("Could not find a view controller to present from.");
                    return;
                }

                root.PresentViewController(controller, true, () =>
                {
                    player.Play();
                    tcs.TrySetResult("Started playback.");
                });
            }
            catch (Exception ex)
            {
                tcs.TrySetResult($"Unexpected error: {ex.Message}");
            }
        });

        return tcs.Task;
    }

    private static void ShowNativeAlert(string title, string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var root = GetTopViewController();
            if (root is null)
            {
                return;
            }

            var alert = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);
            alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
            root.PresentViewController(alert, true, null);
        });
    }

    private static UIViewController GetTopViewController()
    {
        var windowScene = UIApplication.SharedApplication
            .ConnectedScenes
            .OfType<UIWindowScene>()
            .FirstOrDefault(x => x.ActivationState == UISceneActivationState.ForegroundActive);

        var window = windowScene?.Windows.FirstOrDefault(x => x.IsKeyWindow);
        var vc = window?.RootViewController;

        while (vc?.PresentedViewController is not null)
        {
            vc = vc.PresentedViewController;
        }

        return vc;
    }
}

sealed class ItemStatusObserver(TaskCompletionSource<string> tcs) : NSObject
{
    public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, IntPtr context)
    {
        if (ofObject is not AVPlayerItem item)
        {
            return;
        }

        if (item.Status == AVPlayerItemStatus.Failed)
        {
            var error = item.Error;
            tcs.TrySetResult(
                $"Item failed to load.\n" +
                $"Error: {error?.LocalizedDescription ?? "unknown"}\n" +
                $"Reason: {error?.LocalizedFailureReason ?? "none"}");
        }
    }
}
