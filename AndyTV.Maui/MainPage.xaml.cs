using AndyTV.Maui.Services;

namespace AndyTV.Maui;

public partial class MainPage : ContentPage
{
    private readonly IHlsPlayer _hlsPlayer;

    public MainPage(IHlsPlayer hlsPlayer)
    {
        InitializeComponent();
        _hlsPlayer = hlsPlayer;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await PlayUrl();
    }

    private async void OnPlayClicked(object sender, EventArgs e)
    {
        await PlayUrl();
    }

    private async Task PlayUrl()
    {
        try
        {
            var url = HlsUrlEntry.Text?.Trim() ?? string.Empty;
            var result = await _hlsPlayer.PlayHls(url);
            if (!result.StartsWith("Started"))
            {
                await DisplayAlertAsync("HLS Diagnostic", result, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Playback Error", ex.Message, "OK");
        }
    }
}