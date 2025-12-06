using AndyTV.Data.Services;
using AndyTV.Maui.Services;
using AndyTV.Maui.ViewModels;
using AndyTV.Maui.Views;
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
            .UseLibVLCSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
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
        builder.Services.AddTransient<PlayerViewModel>();

        // Pages
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<ChannelsPage>();
        builder.Services.AddTransient<PlayerPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}