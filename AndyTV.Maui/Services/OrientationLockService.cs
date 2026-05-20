#if ANDROID
using Android.Content.PM;
#elif IOS
using Foundation;
using UIKit;
#endif
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace AndyTV.Maui.Services;

public class OrientationLockService : IOrientationLockService
{
    private const string LandscapeLockPreferenceKey = "LandscapeLockEnabled";

    internal static bool IsPlaybackLandscapeLocked { get; private set; }

    public bool IsLandscapeLockEnabled =>
        Preferences.Default.Get(LandscapeLockPreferenceKey, false);

    public void SetLandscapeLockEnabled(bool isEnabled)
    {
        Preferences.Default.Set(LandscapeLockPreferenceKey, isEnabled);

        if (!isEnabled)
        {
            UseDefaultOrientation();
        }
    }

    public void ApplyForPlayback()
    {
        if (!IsLandscapeLockEnabled)
        {
            UseDefaultOrientation();
            return;
        }

        IsPlaybackLandscapeLocked = true;
        LockLandscape();
    }

    public void UseDefaultOrientation()
    {
        IsPlaybackLandscapeLocked = false;
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
