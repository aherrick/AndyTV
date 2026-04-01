using AndyTV.Data.Services;
using AndyTV.Services;
using LibVLCSharp.WinForms;
using Microsoft.Extensions.DependencyInjection;

namespace AndyTV;

public static class ServiceConfiguration
{
    public static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // VideoView (MediaPlayer wired after deferred LibVLC init in Form1)
        services.AddSingleton(_ => new VideoView { Dock = DockStyle.Fill });

        // Shared services
        services.AddSingleton<IStorageProvider, WinFormsStorageProvider>();
        services.AddSingleton<IPlaylistService, PlaylistService>();
        services.AddSingleton<IRecentChannelService, RecentChannelService>();
        services.AddSingleton<ILastChannelService, LastChannelService>();
        services.AddSingleton<IFavoriteChannelService, FavoriteChannelService>();

        // App services
        services.AddSingleton<UpdateService>();

        // Forms
        services.AddTransient<Form1>();

        return services.BuildServiceProvider();
    }
}