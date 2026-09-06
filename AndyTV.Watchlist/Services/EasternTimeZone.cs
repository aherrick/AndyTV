namespace AndyTV.Watchlist.Services;

public static class EasternTimeZone
{
    public static TimeZoneInfo Zone { get; } = Resolve();

    public static DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Zone);

    public static DateTimeOffset Convert(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, Zone);

    private static TimeZoneInfo Resolve()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        }
    }
}
