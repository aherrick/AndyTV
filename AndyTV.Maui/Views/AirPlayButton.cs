using AVKit;
using Microsoft.Maui.Handlers;
using UIKit;

namespace AndyTV.Maui.Views;

// Apple's AirPlay picker; routes audio only since VLC can't hand video to an Apple TV.
public class AirPlayButton : View;

public class AirPlayButtonHandler() : ViewHandler<AirPlayButton, AVRoutePickerView>(ViewMapper)
{
    protected override AVRoutePickerView CreatePlatformView() => new() { TintColor = UIColor.White };
}
