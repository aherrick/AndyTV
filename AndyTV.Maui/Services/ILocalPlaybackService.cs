namespace AndyTV.Maui.Services;

public interface ILocalPlaybackService
{
    Task<string> ResolvePlaybackUrl(string sourceUrl);
}