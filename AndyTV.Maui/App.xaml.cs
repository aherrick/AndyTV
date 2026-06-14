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

        window.Created += async (_, _) =>
        {
            // Pre-warm the channel list in the background so it's ready when the user navigates back
            var playlistService = IPlatformApplication.Current?.Services.GetService<IPlaylistService>();
            if (playlistService is not null)
            {
                _ = Task.Run(() => playlistService.RefreshChannelsAsync());
            }

            var lastChannelService = IPlatformApplication.Current?.Services.GetService<ILastChannelService>();
            var localPlaybackService =
                IPlatformApplication.Current?.Services.GetService<ILocalPlaybackService>();
            var lastChannel = lastChannelService?.LoadLastChannel();
            if (lastChannel != null && !string.IsNullOrEmpty(lastChannel.Url))
            {
                var playbackUrl =
                    localPlaybackService is null
                        ? lastChannel.Url
                        : await localPlaybackService.ResolvePlaybackUrl(lastChannel.Url);
                var playerPage = new Views.PlayerPage(playbackUrl, lastChannel.DisplayName);
                await Shell.Current.Navigation.PushAsync(playerPage, animated: false);
            }
        };

        window.Resumed += (_, _) =>
            WeakReferenceMessenger.Default.Send(new AppResumedMessage());

        return window;
    }
}