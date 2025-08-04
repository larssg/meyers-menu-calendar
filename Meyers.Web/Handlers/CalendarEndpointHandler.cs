using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Meyers.Core.Interfaces;
using Meyers.Core.Models;
using Meyers.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Meyers.Web.Handlers;

public partial class CalendarEndpointHandler(
    IMenuScrapingService menuScrapingService,
    ICalendarService calendarService,
    IMenuRepository menuRepository,
    IOptions<MenuCacheOptions> cacheOptions)
{
    public async Task<IResult> GetCalendarAsync(string menuTypeSlug, HttpContext httpContext)
    {
        try
        {
            // Get current menu data (this will refresh the cache if needed)
            var currentMenuDays = await menuScrapingService.ScrapeMenuAsync();

            // Also get historical data from the last month plus any future items
            var startDate = DateTime.Today.AddMonths(-1);
            var endDate = DateTime.Today.AddMonths(1); // Get up to one month in the future

            // Get specific menu type
            var menuType = await menuRepository.GetMenuTypeBySlugAsync(menuTypeSlug);
            if (menuType == null) return Results.NotFound($"Menu '{menuTypeSlug}' not found");

            var menuTypeName = menuType.Name;

            // Get cached entries for this specific menu type
            var allCachedEntries = await menuRepository.GetMenusForDateRangeAsync(startDate, endDate, menuType.Id);

            // Convert cached entries to MenuDay objects
            var historicalMenuDays = allCachedEntries
                .Where(entry => currentMenuDays.All(current =>
                    current.Date.Date != entry.Date.Date || current.MenuType != menuType.Name))
                .Select(entry => new MenuDay
                {
                    DayName = entry.DayName,
                    Date = entry.Date,
                    MenuItems = string.IsNullOrEmpty(entry.MenuItems)
                        ? []
                        : entry.MenuItems.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    MainDish = entry.MainDish,
                    Details = entry.Details,
                    MenuType = entry.MenuType?.Name ?? ""
                })
                .ToList();

            // Filter current menu days by menu type
            var filteredCurrentMenuDays = currentMenuDays.Where(m => m.MenuType == menuType.Name).ToList();

            // Combine current and historical data
            var allMenuDays = historicalMenuDays.Concat(filteredCurrentMenuDays)
                .OrderBy(m => m.Date)
                .ToList();

            var icalContent = calendarService.GenerateCalendar(allMenuDays, menuTypeName);
            var lastModified = await menuRepository.GetLastUpdateTimeAsync() ?? DateTime.UtcNow;

            return CreateCachedCalendarResponse(icalContent, lastModified, httpContext);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error generating calendar: {ex.Message}");
        }
    }

    public async Task<IResult> GetCustomCalendarAsync(string config, HttpContext httpContext)
    {
        try
        {
            // Decode the configuration
            var weekdayMenuConfig = DecodeCustomConfig(config);
            if (weekdayMenuConfig == null) return Results.BadRequest("Invalid calendar configuration");

            // Get current menu data
            var currentMenuDays = await menuScrapingService.ScrapeMenuAsync();

            // Get historical data from the last month plus any future items
            var startDate = DateTime.Today.AddMonths(-1);
            var endDate = DateTime.Today.AddMonths(1);

            // Get all menu types we need
            var allMenuTypes = await menuRepository.GetMenuTypesAsync();
            var menuTypeDict = allMenuTypes.ToDictionary(mt => mt.Id, mt => mt);

            // Collect all menu days based on the configuration
            var customMenuDays = new List<MenuDay>();

            foreach (var (dayOfWeek, menuTypeId) in weekdayMenuConfig)
            {
                if (!menuTypeDict.TryGetValue(menuTypeId, out var menuType)) continue;

                // Get cached entries for this menu type
                var cachedEntries = await menuRepository.GetMenusForDateRangeAsync(startDate, endDate, menuType.Id);

                // Convert cached entries to MenuDay objects and filter by day of week
                var historicalMenuDays = cachedEntries
                    .Where(entry => entry.Date.DayOfWeek == dayOfWeek)
                    .Where(entry => currentMenuDays.All(current =>
                        current.Date.Date != entry.Date.Date || current.MenuType != menuType.Name))
                    .Select(entry => new MenuDay
                    {
                        DayName = entry.DayName,
                        Date = entry.Date,
                        MenuItems = string.IsNullOrEmpty(entry.MenuItems)
                            ? []
                            : entry.MenuItems.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
                        MainDish = entry.MainDish,
                        Details = entry.Details,
                        MenuType = entry.MenuType?.Name ?? ""
                    });

                // Filter current menu days by menu type and day of week
                var currentMenuDaysForType = currentMenuDays
                    .Where(m => m.MenuType == menuType.Name && m.Date.DayOfWeek == dayOfWeek);

                customMenuDays.AddRange(historicalMenuDays);
                customMenuDays.AddRange(currentMenuDaysForType);
            }

            // Sort by date
            customMenuDays = customMenuDays.OrderBy(m => m.Date).ToList();

            var icalContent = calendarService.GenerateCalendar(customMenuDays, "Custom Menu Selection");
            var lastModified = await menuRepository.GetLastUpdateTimeAsync() ?? DateTime.UtcNow;

            return CreateCachedCalendarResponse(icalContent, lastModified, httpContext);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error generating custom calendar: {ex.Message}");
        }
    }

    [GeneratedRegex(@"([MTWRF])(\d+)", RegexOptions.Compiled)]
    private static partial Regex DayMenuConfigRegex();

    private static Dictionary<DayOfWeek, int>? DecodeCustomConfig(string config)
    {
        try
        {
            // Enhanced encoding: supports multi-digit menu type IDs
            // Format: M1T10W3R25F1 (M=Monday, T=Tuesday, W=Wednesday, R=Thursday, F=Friday, numbers=menu type IDs)
            var result = new Dictionary<DayOfWeek, int>();
            var matches = DayMenuConfigRegex().Matches(config);

            if (matches.Count == 0) return null;

            // Ensure the entire config string is consumed by the matches (no invalid characters)
            var totalMatchLength = matches.Sum(m => m.Length);
            if (totalMatchLength != config.Length) return null;

            foreach (Match match in matches)
            {
                var dayChar = match.Groups[1].Value[0];
                if (!int.TryParse(match.Groups[2].Value, out var menuTypeId))
                    return null;

                var dayOfWeek = dayChar switch
                {
                    'M' => DayOfWeek.Monday,
                    'T' => DayOfWeek.Tuesday,
                    'W' => DayOfWeek.Wednesday,
                    'R' => DayOfWeek.Thursday,
                    'F' => DayOfWeek.Friday,
                    _ => (DayOfWeek?)null
                };

                if (dayOfWeek.HasValue) result[dayOfWeek.Value] = menuTypeId;
            }

            return result.Count > 0 ? result : null;
        }
        catch
        {
            return null;
        }
    }

    public static string EncodeCustomConfig(Dictionary<DayOfWeek, int> weekdayMenuConfig)
    {
        var sb = new StringBuilder();

        var orderedDays = new[]
            { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

        foreach (var day in orderedDays)
            if (weekdayMenuConfig.TryGetValue(day, out var menuTypeId))
            {
                var dayChar = day switch
                {
                    DayOfWeek.Monday => 'M',
                    DayOfWeek.Tuesday => 'T',
                    DayOfWeek.Wednesday => 'W',
                    DayOfWeek.Thursday => 'R',
                    DayOfWeek.Friday => 'F',
                    _ => throw new ArgumentException($"Unsupported day: {day}")
                };
                sb.Append(dayChar);
                sb.Append(menuTypeId);
            }

        return sb.ToString();
    }

    private IResult CreateCachedCalendarResponse(string icalContent, DateTime lastModified, HttpContext httpContext)
    {
        // Generate ETag from content hash for conditional requests
        var contentBytes = Encoding.UTF8.GetBytes(icalContent);
        var hashBytes = SHA256.HashData(contentBytes);
        var etag = $"\"{Convert.ToHexString(hashBytes)[..16]}\"";

        // Calculate dynamic cache duration based on refresh cycle
        var cacheDuration = CalculateCacheDuration(lastModified);

        // Add caching headers to the response
        httpContext.Response.Headers.CacheControl = $"public, max-age={cacheDuration}";
        httpContext.Response.Headers.ETag = etag;
        httpContext.Response.Headers["Last-Modified"] = lastModified.ToString("R"); // RFC 1123 format
        httpContext.Response.Headers.Vary = "Accept-Encoding"; // Vary on encoding for better caching

        return Results.Text(icalContent, "text/calendar; charset=utf-8");
    }

    private int CalculateCacheDuration(DateTime lastModified)
    {
        var options = cacheOptions.Value;
        var now = DateTime.UtcNow;

        // Handle edge case: if lastModified is in the future, use minimum cache
        if (lastModified > now) return (int)TimeSpan.FromMinutes(5).TotalSeconds;

        // Calculate when the next refresh will happen (90% of 6-hour interval = 5.4 hours)
        var proactiveThreshold = TimeSpan.FromTicks((long)(options.RefreshInterval.Ticks * 0.9));
        var nextRefreshTime = lastModified.Add(proactiveThreshold);

        // Add a buffer time after the refresh to allow for processing
        var bufferTime = TimeSpan.FromMinutes(10);
        var cacheExpiryTime = nextRefreshTime.Add(bufferTime);

        // Calculate seconds until cache should expire
        var timeUntilExpiry = cacheExpiryTime - now;

        // Ensure minimum cache time of 5 minutes and maximum of 6 hours
        var minCacheSeconds = (int)TimeSpan.FromMinutes(5).TotalSeconds;
        var maxCacheSeconds = (int)options.RefreshInterval.TotalSeconds;

        var cacheSeconds = Math.Max(minCacheSeconds, (int)timeUntilExpiry.TotalSeconds);
        cacheSeconds = Math.Min(maxCacheSeconds, cacheSeconds);

        return cacheSeconds;
    }
}