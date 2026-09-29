using AndyTV.Data.Services;

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
            .ContinueWith(t => playlistService.SetChannels([.. menu, .. t.Result]), TaskScheduler.Default);
    }
}
