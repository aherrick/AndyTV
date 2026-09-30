using AndyTV.Data.Services;
using AndyTV.Maui.Messages;
using AndyTV.Maui.Services;
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
        var services = IPlatformApplication.Current.Services;

        window.Created += async (_, _) =>
        {
            var lastChannel = services.GetRequiredService<ILastChannelService>().LoadLastChannel();
            if (!string.IsNullOrEmpty(lastChannel?.Url))
            {
                await Shell.Current.Navigation.PushAsync(new Views.PlayerPage(lastChannel), animated: false);
            }
        };

        window.Resumed += (_, _) =>
            WeakReferenceMessenger.Default.Send(new AppResumedMessage());

        // Backgrounding must NOT stop playback so audio keeps playing behind other apps (Spotify-style)
        window.Stopped += (_, _) =>
            WeakReferenceMessenger.Default.Send(new AppStoppedMessage());

        // Only kill the server-side stream when the app is actually torn down, not on background
        window.Destroying += (_, _) => _ = services.GetRequiredService<LocalPlaybackService>().Stop();

        return window;
    }
}