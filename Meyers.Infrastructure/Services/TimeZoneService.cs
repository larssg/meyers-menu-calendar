using Meyers.Core.Interfaces;

namespace Meyers.Infrastructure.Services;

public class TimeZoneService : ITimeZoneService
{
    public TimeZoneInfo CopenhagenTimeZone { get; } = GetCopenhagenTimeZoneInfo();

    public DateTime GetCopenhagenNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CopenhagenTimeZone);
    }

    public DateTime GetCopenhagenDate()
    {
        return GetCopenhagenNow().Date;
    }

    private static TimeZoneInfo GetCopenhagenTimeZoneInfo()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
    }
}