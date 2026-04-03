using AndyTV.Data.Services;
using AndyTV.Services;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using Microsoft.Extensions.DependencyInjection;

namespace AndyTV;

public static class ServiceConfiguration
{
    public static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // LibVLC
        services.AddSingleton(_ => new LibVLC(enableDebugLogs: false));

        // VideoView
        services.AddSingleton(sp =>
        {
            var libVlc = sp.GetRequiredService<LibVLC>();
            return new VideoView
            {
                Dock = DockStyle.Fill,
                MediaPlayer = new MediaPlayer(libVlc)
                {
                    EnableHardwareDecoding = true,
                    EnableKeyInput = false,
                    EnableMouseInput = false,
                },
            };
        });

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