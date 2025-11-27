using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public interface IPlaylistService
{
    List<(Playlist Playlist, List<Channel> Channels)> PlaylistChannels { get; }
    List<Channel> Channels { get; }

    List<Playlist> LoadPlaylists();
    void SavePlaylists(List<Playlist> playlists);
    Task RefreshChannelsAsync();
    Task<List<(Playlist Playlist, List<Channel> Channels)>> LoadChannelsAsync(List<Playlist> playlists);
}
