using System.Diagnostics;

namespace AndyTV.VLC.Services;

public class VlcService(IConfiguration config, ILogger<VlcService> logger)
{
    private readonly string _vlcPath =
        config.GetValue<string>("VLC:Path") ?? "C:/Program Files/VideoLAN/VLC/vlc.exe";

    public bool Launch(string streamUrl)
    {
        if (string.IsNullOrWhiteSpace(streamUrl))
        {
            return false;
        }
        if (!File.Exists(_vlcPath))
        {
            logger.LogWarning("VLC executable not found at {Path}", _vlcPath);
            return false;
        }
        try
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = _vlcPath,
                    Arguments = $"\"{streamUrl}\"",
                    UseShellExecute = true,
                }
            );
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to launch VLC for {Url}", streamUrl);
            return false;
        }
    }
}