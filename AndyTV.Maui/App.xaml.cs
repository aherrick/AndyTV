using AndyTV.Data.Services;

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

        return window;
    }
}