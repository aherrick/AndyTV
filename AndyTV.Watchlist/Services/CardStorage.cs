using System.Globalization;

namespace AndyTV.Watchlist.Services;

// Persistent per-day storage for rendered card images. On Azure Functions, %HOME% is the
// Azure Files-backed share that survives restarts and is shared across instances.
public static class CardStorage
{
    public static string Root { get; } = Resolve();

    public static string DayFolderName(DateOnly date) =>
        date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    public static string DayDir(DateOnly date) => Path.Combine(Root, DayFolderName(date));

    private static string Resolve()
    {
        var home = Environment.GetEnvironmentVariable("HOME");
        var baseDir = string.IsNullOrWhiteSpace(home)
            ? Path.GetTempPath()
            : Path.Combine(home, "data");
        return Path.Combine(baseDir, "andytv-insta");
    }
}
