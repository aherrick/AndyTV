using AndyTV.Data.Services;
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

        window.Created += async (_, _) =>
        {
            var lastChannelService = IPlatformApplication.Current?.Services.GetService<ILastChannelService>();
            var lastChannel = lastChannelService?.LoadLastChannel();
            if (lastChannel != null && !string.IsNullOrEmpty(lastChannel.Url))
            {
                var playerPage = new Views.PlayerPage(lastChannel.Url, lastChannel.DisplayName);
                await Shell.Current.Navigation.PushModalAsync(playerPage);
            }
        };

        window.Resumed += (_, _) =>
        {
            WeakReferenceMessenger.Default.Send(new AppResumedMessage());
        };

        return window;
    }
}