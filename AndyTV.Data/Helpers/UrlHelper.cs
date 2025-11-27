namespace AndyTV.Data.Helpers;

public static class UrlHelper
{
    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var u))
            return false;

        return u.Scheme is "http" or "https" && !string.IsNullOrEmpty(u.Host);
    }
}
