using Foundation;
using AVFoundation;

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
}