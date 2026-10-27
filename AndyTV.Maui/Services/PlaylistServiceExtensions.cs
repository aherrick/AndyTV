using AndyTV.Data.Services;
using CommunityToolkit.Maui.Alerts;

namespace AndyTV.Maui.Services;

public static class PlaylistServiceExtensions
{
    // Returns once menu playlists load; search-only playlists keep loading in the background.
    public static async Task RefreshMenuChannelsFirst(this IPlaylistService playlistService)
    {
        var playlists = playlistService.LoadPlaylists();
        var menu = await playlistService.LoadChannelsAsync([.. playlists.Where(p => p.ShowInMenu)]);
        playlistService.SetChannels(menu);

        _ = playlistService
            .LoadChannelsAsync([.. playlists.Where(p => !p.ShowInMenu)])
            .ContinueWith(
                t =>
                {
                    playlistService.SetChannels([.. menu, .. t.Result]);
                    if (t.Result.Count > 0)
                    {
                        var loaded = string.Join(" · ", t.Result.Select(x => $"{x.Playlist.Name} {x.Channels.Count:N0}"));
                        MainThread.BeginInvokeOnMainThread(() => _ = Toast.Make($"Loaded {loaded}").Show());
                    }
                },
                TaskScheduler.Default
            );
    }
}
