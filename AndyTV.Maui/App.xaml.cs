using AndyTV.Data.Services;
using AndyTV.Maui.Messages;
using AndyTV.Maui.Services;
using CommunityToolkit.Mvvm.Messaging;

namespace AndyTV.Maui;

public partial class App : Application
{
    public App()
    {
        Console.WriteLine("[AndyTV] App ctor start");
        InitializeComponent();
        Console.WriteLine("[AndyTV] App InitializeComponent done");
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        Console.WriteLine("[AndyTV] CreateWindow start");
        var window = new Window(new AppShell());
        Console.WriteLine("[AndyTV] AppShell created");

        window.Created += async (_, _) =>
        {
            try
            {
                Console.WriteLine("[AndyTV] Window created");

                // Pre-warm the channel list in the background so it's ready when the user navigates back
                var playlistService = IPlatformApplication.Current?.Services.GetService<IPlaylistService>();
                if (playlistService is not null)
                {
                    _ = Task.Run(() => playlistService.RefreshMenuChannelsFirst());
                }

                var lastChannelService = IPlatformApplication.Current?.Services.GetService<ILastChannelService>();
                var localPlaybackService =
                    IPlatformApplication.Current?.Services.GetService<LocalPlaybackService>();
                var lastChannel = lastChannelService?.LoadLastChannel();
                Console.WriteLine($"[AndyTV] Last channel: {lastChannel?.DisplayName ?? "(none)"}");
                if (lastChannel != null && !string.IsNullOrEmpty(lastChannel.Url))
                {
                    var playbackUrl =
                        localPlaybackService is null
                            ? lastChannel.Url
                            : await localPlaybackService.ResolvePlaybackUrl(lastChannel.Url);
                    Console.WriteLine("[AndyTV] Opening PlayerPage");
                    var playerPage = new Views.PlayerPage(playbackUrl, lastChannel.DisplayName);
                    await Shell.Current.Navigation.PushAsync(playerPage, animated: false);
                    Console.WriteLine("[AndyTV] PlayerPage pushed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AndyTV] Window.Created FAILED: {ex}");
                throw;
            }
        };

        window.Resumed += (_, _) =>
            WeakReferenceMessenger.Default.Send(new AppResumedMessage());

        // Backgrounding must NOT stop playback so audio keeps playing behind other apps (Spotify-style)
        window.Stopped += (_, _) =>
            WeakReferenceMessenger.Default.Send(new AppStoppedMessage());

        // Only kill the server-side stream when the app is actually torn down, not on background
        window.Destroying += (_, _) =>
        {
            var localPlaybackService =
                IPlatformApplication.Current?.Services.GetService<LocalPlaybackService>();
            _ = localPlaybackService?.StopPlayback();
        };

        return window;
    }
}