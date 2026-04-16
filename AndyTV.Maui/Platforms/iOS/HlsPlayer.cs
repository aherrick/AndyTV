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
            tcs.SetResult("URL uses http — iOS App Transport Security may block this. Try https or add ATS exception.");
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

                var asset = AVUrlAsset.Create(nsUrl);
                var keys = new[] { "playable", "hasProtectedContent" };

                asset.LoadValuesAsynchronously(keys, () =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            var playableStatus = asset.GetStatusOfValue("playable", out var playableError);
                            var protectedStatus = asset.GetStatusOfValue("hasProtectedContent", out _);

                            if (playableStatus != AVKeyValueStatus.Loaded)
                            {
                                tcs.TrySetResult(
                                    $"Asset not playable.\n" +
                                    $"Status: {playableStatus}\n" +
                                    $"Error: {playableError?.LocalizedDescription ?? "none"}");
                                return;
                            }

                            var item = new AVPlayerItem(asset);

                            NSNotificationCenter.DefaultCenter.AddObserver(
                                AVPlayerItem.FailedToPlayToEndTimeNotification,
                                notification =>
                                {
                                    var failedItem = notification.Object as AVPlayerItem;
                                    var error = failedItem?.Error;
                                    var message =
                                        $"Playback failed mid-stream.\n" +
                                        $"Error: {error?.LocalizedDescription ?? "unknown"}\n" +
                                        $"Reason: {error?.LocalizedFailureReason ?? "none"}";
                                    ShowNativeAlert("Playback Failed", message);
                                },
                                item);

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

                                var protectedValue = protectedStatus == AVKeyValueStatus.Loaded
                                    ? asset.HasProtectedContent.ToString()
                                    : "unknown";

                                tcs.TrySetResult(
                                    $"Started playback.\n" +
                                    $"Playable: yes\n" +
                                    $"Protected: {protectedValue}");
                            });
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetResult($"Asset load error: {ex.Message}");
                        }
                    });
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
