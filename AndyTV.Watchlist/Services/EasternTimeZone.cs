namespace AndyTV.Watchlist.Services;

public static class EasternTimeZone
{
    // .NET resolves IANA ids on both Windows and Linux.
    public static TimeZoneInfo Zone { get; } = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public static DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Zone);

    public static DateTimeOffset Convert(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, Zone);
}
