using AndyTV.Maui.Services;

namespace AndyTV.Maui;

public partial class MainPage(IHlsPlayer hlsPlayer) : ContentPage
{
    private async void OnPlayClicked(object sender, EventArgs e)
    {
        try
        {
            var url = HlsUrlEntry.Text?.Trim() ?? string.Empty;
            var result = await hlsPlayer.PlayHls(url);
            await DisplayAlert("HLS Diagnostic", result, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Playback Error", ex.Message, "OK");
        }
    }
}