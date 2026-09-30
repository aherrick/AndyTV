using AndyTV.Maui.Messages;
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
}
