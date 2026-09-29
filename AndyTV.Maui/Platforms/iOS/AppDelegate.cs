using AndyTV.Maui.Messages;
using AndyTV.Maui.Services;
using AVFoundation;
using CommunityToolkit.Mvvm.Messaging;
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

        AVAudioSession.Notifications.ObserveInterruption((_, e) =>
        {
            if (e.InterruptionType == AVAudioSessionInterruptionType.Began)
            {
                WeakReferenceMessenger.Default.Send(new AudioInterruptedMessage());
            }
            else if (e.Option.HasFlag(AVAudioSessionInterruptionOptions.ShouldResume))
            {
                WeakReferenceMessenger.Default.Send(new AudioResumableMessage());
            }
        });

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
