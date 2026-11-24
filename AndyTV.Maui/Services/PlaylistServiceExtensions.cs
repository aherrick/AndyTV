using AndyTV.Data.Services;

namespace AndyTV.Maui.Services;

public static class PlaylistServiceExtensions
{
    // Returns once menu playlists load; the inner task finishes when search-only playlists do.
    public static async Task<Task> RefreshMenuChannelsFirst(this PlaylistService playlistService)
    {
        var playlists = playlistService.LoadPlaylists();
        var menu = await PlaylistService.LoadChannelsAsync([.. playlists.Where(p => p.ShowInMenu)]);
        playlistService.SetChannels(menu);

        return PlaylistService
            .LoadChannelsAsync([.. playlists.Where(p => !p.ShowInMenu)])
            .ContinueWith(t => playlistService.SetChannels([.. menu, .. t.Result]), TaskScheduler.Default);
    }
}
