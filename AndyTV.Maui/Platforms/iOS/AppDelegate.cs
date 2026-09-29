using AndyTV.Maui.Services;
using AVFoundation;
using Foundation;
using UIKit;

namespace AndyTV.Maui;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Playback);
        AVAudioSession.SharedInstance().SetActive(true);

        return base.FinishedLaunching(application, launchOptions);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1822:Mark members as static",
        Justification = "Exported iOS delegate callbacks must remain instance methods."
    )]
    [Export("application:supportedInterfaceOrientationsForWindow:")]
    public UIInterfaceOrientationMask GetSupportedInterfaceOrientations(
        UIApplication application,
        UIWindow forWindow
    )
    {
        _ = application;
        _ = forWindow;

        if (OrientationLockService.ActivePlaybackLockMode == LockMode.Landscape)
        {
            return UIInterfaceOrientationMask.Landscape;
        }

        if (OrientationLockService.ActivePlaybackLockMode == LockMode.Portrait)
        {
            return UIInterfaceOrientationMask.Portrait;
        }

        return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad
            ? UIInterfaceOrientationMask.All
            : UIInterfaceOrientationMask.AllButUpsideDown;
    }
}
