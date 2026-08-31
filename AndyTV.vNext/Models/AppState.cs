namespace AndyTV.vNext;

sealed class AppState
{
    public List<PlaylistRef> Playlists { get; set; } = [];
    public List<ChannelRef> Recent { get; set; } = [];
    public ChannelRef Last { get; set; }
}
