using System.Net.NetworkInformation;
using System.Net.Sockets;
using AndyTV.Data.Services;

namespace AndyTV.Maui.Services;

public class LocalPlaybackService(LocalConfigService localConfigService)
{
    // Tailscale is the only route to the server, so streams go through it whenever Tailscale is connected.
    // A new session id makes the server kill the previous stream and start a fresh one on the first playlist fetch.
    public string GetUrl(string sourceUrl)
    {
        var config = localConfigService.Load();
        if (string.IsNullOrWhiteSpace(config.ServerUrl) || !IsTailscaleConnected())
        {
            return sourceUrl;
        }

        var quality = string.IsNullOrWhiteSpace(config.Quality) ? "320" : config.Quality;
        return $"{config.ServerUrl.TrimEnd('/')}/live.m3u8?session={Guid.NewGuid():N}&quality={quality}&url={Uri.EscapeDataString(sourceUrl)}";
    }

    // Connected, Tailscale is an iOS tunnel (utun) holding an address in 100.64.0.0/10. Carriers hand out
    // that range on cellular too, so only tunnels count. A failed lookup plays direct rather than not at all.
    private static bool IsTailscaleConnected()
    {
        try
        {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n => n.Name.StartsWith("utun", StringComparison.Ordinal))
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Any(a =>
                    a.Address.AddressFamily == AddressFamily.InterNetwork
                    && a.Address.GetAddressBytes() is [100, >= 64 and < 128, _, _]
                );
        }
        catch (Exception e) when (e is NetworkInformationException or PlatformNotSupportedException)
        {
            return false;
        }
    }
}
