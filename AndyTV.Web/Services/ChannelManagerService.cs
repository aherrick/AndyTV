using AndyTV.Data.Models;
using AndyTV.Data.Services;

namespace AndyTV.Web.Services;

public class ChannelManagerService(IPlaylistService playlistService)
{
    public List<Playlist> Playlists { get; private set; } = [];
    public List<Channel> Channels => playlistService.Channels;
    public bool IsLoading { get; private set; }

    public event Action OnStateChanged = () => { };

    public void LoadPlaylists()
    {
        Playlists = playlistService.LoadPlaylists();
        NotifyStateChanged();
    }

    public async Task AddPlaylistAsync(string name, string url)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
            return;

        var playlist = new Playlist { Name = name.Trim(), Url = url.Trim() };

        Playlists.Add(playlist);
        playlistService.SavePlaylists(Playlists);
        await RefreshChannelsAsync();
    }

    public async Task DeletePlaylistAsync(Playlist playlist)
    {
        Playlists.Remove(playlist);
        playlistService.SavePlaylists(Playlists);
        await RefreshChannelsAsync();
    }

    public async Task RefreshChannelsAsync()
    {
        IsLoading = true;
        NotifyStateChanged();

        try
        {
            await playlistService.RefreshChannelsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading channels: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    public int GetChannelCount(Playlist playlist)
    {
        return playlistService
                .PlaylistChannels.FirstOrDefault(x => x.Playlist.Url == playlist.Url)
                .Channels?.Count ?? 0;
    }

    public List<Channel> FilterChannels(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return Channels;

        return
        [
            .. Channels.Where(c =>
                c.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || (c.Group?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
            ),
        ];
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}