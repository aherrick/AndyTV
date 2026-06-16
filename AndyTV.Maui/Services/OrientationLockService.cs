#if ANDROID
using Android.Content.PM;
#elif IOS
using Foundation;
using UIKit;
#endif

namespace AndyTV.Maui.Services;

public class OrientationLockService : IOrientationLockService
{
    internal static LockMode ActivePlaybackLockMode { get; private set; } = LockMode.Unlocked;

    public LockMode CurrentLockMode { get; private set; } = LockMode.Unlocked;

    public void CycleLockMode()
    {
        CurrentLockMode = CurrentLockMode switch
        {
            LockMode.Unlocked => LockMode.Landscape,
            LockMode.Landscape => LockMode.Portrait,
            _ => LockMode.Unlocked
        };
    }

    public void ApplyForPlayback()
    {
        ActivePlaybackLockMode = CurrentLockMode;

        switch (CurrentLockMode)
        {
            case LockMode.Landscape:
                LockLandscape();
                break;
            case LockMode.Portrait:
                LockPortrait();
                break;
            default:
                UnlockOrientation();
                break;
        }
    }

    public void UseDefaultOrientation()
    {
        ActivePlaybackLockMode = LockMode.Unlocked;
        UnlockOrientation();
    }

    private static void LockLandscape()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
#if ANDROID
            var activity = Platform.CurrentActivity;
            if (activity is null)
            {
                return;
            }

            activity.RequestedOrientation = ScreenOrientation.Landscape;
#elif IOS
            RequestIosOrientation(UIInterfaceOrientation.LandscapeRight);
#endif
        });
    }

    private static void LockPortrait()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
#if ANDROID
            var activity = Platform.CurrentActivity;
            if (activity is null)
            {
                return;
            }

            activity.RequestedOrientation = ScreenOrientation.Portrait;
#elif IOS
            RequestIosOrientation(UIInterfaceOrientation.Portrait);
#endif
        });
    }

    private static void UnlockOrientation()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
#if ANDROID
            var activity = Platform.CurrentActivity;
            if (activity is null)
            {
                return;
            }

            activity.RequestedOrientation = ScreenOrientation.Unspecified;
#elif IOS
            RequestIosOrientation(UIInterfaceOrientation.Unknown);
#endif
        });
    }

#if IOS
    private static void RequestIosOrientation(UIInterfaceOrientation orientation)
    {
        if (OperatingSystem.IsIOSVersionAtLeast(16))
        {
            UIApplication.SharedApplication.KeyWindow?.RootViewController
                ?.SetNeedsUpdateOfSupportedInterfaceOrientations();
        }

        if (orientation != UIInterfaceOrientation.Unknown)
        {
            UIDevice.CurrentDevice.SetValueForKey(
                new NSNumber((int)orientation),
                new NSString("orientation")
            );
        }

        UIViewController.AttemptRotationToDeviceOrientation();
    }
#endif
}

