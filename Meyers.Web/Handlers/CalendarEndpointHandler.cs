using System.Text;
using Meyers.Core.Interfaces;
using Meyers.Core.Models;

namespace Meyers.Web.Handlers;

public class CalendarEndpointHandler(
    IMenuScrapingService menuScrapingService,
    ICalendarService calendarService,
    IMenuRepository menuRepository)
{
    public async Task<IResult> GetCalendarAsync(string menuTypeSlug)
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

            return Results.Text(icalContent, "text/calendar; charset=utf-8");
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error generating calendar: {ex.Message}");
        }
    }

    public async Task<IResult> GetCustomCalendarAsync(string config)
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

            return Results.Text(icalContent, "text/calendar; charset=utf-8");
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error generating custom calendar: {ex.Message}");
        }
    }

    private static Dictionary<DayOfWeek, int>? DecodeCustomConfig(string config)
    {
        try
        {
            // Simple encoding: M1T1W1R2F1 (M=Monday, T=Tuesday, W=Wednesday, R=Thursday, F=Friday, numbers=menu type IDs)
            var result = new Dictionary<DayOfWeek, int>();

            for (var i = 0; i < config.Length - 1; i += 2)
            {
                var dayChar = config[i];
                if (!int.TryParse(config[i + 1].ToString(), out var menuTypeId))
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
}