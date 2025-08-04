namespace Meyers.Web.Configuration;

public class MenuCacheOptions
{
    public const string SectionName = "MenuCache";

    public int CheckIntervalMinutes { get; set; } = 30;
    public int RefreshIntervalHours { get; set; } = 6;
    public int StartupDelaySeconds { get; set; } = 30;

    public TimeSpan CheckInterval => TimeSpan.FromMinutes(CheckIntervalMinutes);
    public TimeSpan RefreshInterval => TimeSpan.FromHours(RefreshIntervalHours);
    public TimeSpan StartupDelay => TimeSpan.FromSeconds(StartupDelaySeconds);
}