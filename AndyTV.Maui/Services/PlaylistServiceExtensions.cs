using AndyTV.Data.Services;

namespace AndyTV.Maui.Services;

public static class PlaylistServiceExtensions
{
    // Returns once menu playlists load; the inner task finishes when search-only playlists do.
    public static async Task<Task> RefreshMenuChannelsFirst(this IPlaylistService playlistService)
    {
        var playlists = playlistService.LoadPlaylists();
        var menu = await playlistService.LoadChannelsAsync([.. playlists.Where(p => p.ShowInMenu)]);
        playlistService.SetChannels(menu);

        return playlistService
            .LoadChannelsAsync([.. playlists.Where(p => !p.ShowInMenu)])
            .ContinueWith(t => playlistService.SetChannels([.. menu, .. t.Result]), TaskScheduler.Default);
    }
}
