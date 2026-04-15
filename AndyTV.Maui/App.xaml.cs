using AndyTV.Maui.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace AndyTV.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = new Window(new AppShell());

        window.Resumed += (_, _) =>
            WeakReferenceMessenger.Default.Send(new AppResumedMessage());

        return window;
    }
}