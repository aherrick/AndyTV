namespace AndyTV.Data.Helpers;

public static class UrlHelper
{
    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme is "http" or "https" && !string.IsNullOrEmpty(uri.Host);
    }

    public static bool IsValidPlaylistSource(string source) =>
        IsValidUrl(source) || File.Exists(source);
}