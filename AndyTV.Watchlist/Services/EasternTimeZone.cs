namespace AndyTV.Watchlist.Services;

public static class EasternTimeZone
{
    public static TimeZoneInfo Get()
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
