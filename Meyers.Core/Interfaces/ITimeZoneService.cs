namespace Meyers.Core.Interfaces;

public interface ITimeZoneService
{
    TimeZoneInfo CopenhagenTimeZone { get; }
    DateTime GetCopenhagenNow();
    DateTime GetCopenhagenDate();
}