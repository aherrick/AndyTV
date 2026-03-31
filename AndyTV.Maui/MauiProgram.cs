using AndyTV.Data.Services;
using AndyTV.Maui.Services;
using AndyTV.Maui.ViewModels;
using AndyTV.Maui.Views;
using CommunityToolkit.Maui;
using LibVLCSharp.MAUI;
using Microsoft.Extensions.Logging;

namespace AndyTV.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseLibVLCSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("fa-light-300.ttf", nameof(FontAwesome.FontAwesomeLight));
                fonts.AddFont("fa-regular-400.ttf", nameof(FontAwesome.FontAwesomeRegular));
                fonts.AddFont("fa-solid-900.ttf", nameof(FontAwesome.FontAwesomeSolid));
            });

        // Services
        builder.Services.AddSingleton<IStorageProvider, MauiStorageProvider>();
        builder.Services.AddSingleton<IPlaylistService, PlaylistService>();
        builder.Services.AddSingleton<IRecentChannelService, RecentChannelService>();
        builder.Services.AddSingleton<ILastChannelService, LastChannelService>();
        builder.Services.AddSingleton<IFavoriteChannelService, FavoriteChannelService>();

        // ViewModels
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<ChannelsViewModel>();
        builder.Services.AddTransient<FavoritesViewModel>();

        // Pages
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<ChannelsPage>();
        builder.Services.AddTransient<FavoritesPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}