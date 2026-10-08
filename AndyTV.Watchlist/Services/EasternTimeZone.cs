namespace AndyTV.Watchlist.Services;

public static class EasternTimeZone
{
    // .NET resolves IANA ids on both Windows and Linux.
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public static DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Zone);

    public static DateOnly Today => Date(DateTimeOffset.UtcNow);

    public static DateTimeOffset Convert(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, Zone);

    public static DateOnly Date(DateTimeOffset value) => DateOnly.FromDateTime(Convert(value).DateTime);
}
