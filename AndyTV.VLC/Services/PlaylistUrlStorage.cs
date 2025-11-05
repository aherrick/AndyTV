using Microsoft.JSInterop;

namespace AndyTV.VLC.Services;

// Simple session storage wrapper (lives only for the lifetime of the tab)
public class PlaylistUrlStorage
{
    private const string Key = "andytv.vlc.playlistUrl";
    private readonly IJSRuntime _js;

    public PlaylistUrlStorage(IJSRuntime js)
    {
        _js = js;
    }

    public async Task SaveAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        await _js.InvokeVoidAsync("sessionStorage.setItem", Key, url.Trim());
    }

    public async Task<string?> LoadAsync()
    {
        try
        {
            var value = await _js.InvokeAsync<string?>("sessionStorage.getItem", Key);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch
        {
            return null;
        }
    }
}