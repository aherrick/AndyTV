namespace AndyTV.Maui.Views;

public partial class PlayerPage : ContentPage
{
    public PlayerPage(string url, string channelName)
    {
        InitializeComponent();
        Title = channelName;
    }
}