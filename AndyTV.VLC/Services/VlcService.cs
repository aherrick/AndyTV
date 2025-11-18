using System.Diagnostics;

namespace AndyTV.VLC.Services;

public class VlcService
{
    private readonly string _vlcPath;
    private readonly ILogger<VlcService> _logger;

    public VlcService(IConfiguration config, ILogger<VlcService> logger)
    {
        _vlcPath = config.GetValue<string>("VLC:Path") ?? "C:/Program Files/VideoLAN/VLC/vlc.exe";
        _logger = logger;
    }

    public bool Launch(string streamUrl)
    {
        if (string.IsNullOrWhiteSpace(streamUrl))
            return false;
        if (!File.Exists(_vlcPath))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("VLC executable not found at {Path}", _vlcPath);
            }
            return false;
        }
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _vlcPath,
                Arguments = $"\"{streamUrl}\"",
                UseShellExecute = true,
            };
            Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Failed to launch VLC for {Url}", streamUrl);
            }
            return false;
        }
    }
}