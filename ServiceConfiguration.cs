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

        // LibVLC & MediaPlayer
        services.AddSingleton(_ => new LibVLC(enableDebugLogs: false));
        services.AddSingleton(sp =>
        {
            var libVlc = sp.GetRequiredService<LibVLC>();
            return new MediaPlayer(libVlc)
            {
                EnableHardwareDecoding = true,
                EnableKeyInput = false,
                EnableMouseInput = false,
            };
        });

        // VideoView
        services.AddSingleton<VideoView>(sp =>
        {
            var mediaPlayer = sp.GetRequiredService<MediaPlayer>();
            return new VideoView
            {
                Dock = DockStyle.Fill,
                MediaPlayer = mediaPlayer,
            };
        });

        // App services
        services.AddSingleton<UpdateService>();

        // Forms
        services.AddTransient<Form1>();

        return services.BuildServiceProvider();
    }
}