using Meyers.Web.Repositories;
using Meyers.Web.Services;

namespace Meyers.Web.Handlers;

public class CalendarEndpointHandler(
    MenuScrapingService menuScrapingService,
    CalendarService calendarService,
    IMenuRepository menuRepository)
{
    public async Task<IResult> GetCalendarAsync()
    {
        try
        {
            // Get current menu data (this will refresh the cache if needed)
            var currentMenuDays = await menuScrapingService.ScrapeMenuAsync();

            // Also get historical data from the last month plus any future items
            var startDate = DateTime.Today.AddMonths(-1);
            var endDate = DateTime.Today.AddMonths(1); // Get up to one month in the future
            var allCachedEntries = await menuRepository.GetMenusForDateRangeAsync(startDate, endDate);

            // Convert cached entries to MenuDay objects
            var historicalMenuDays = allCachedEntries
                .Where(entry => currentMenuDays.All(current => current.Date.Date != entry.Date.Date))
                .Select(entry => new MenuDay
                {
                    DayName = entry.DayName,
                    Date = entry.Date,
                    MenuItems = string.IsNullOrEmpty(entry.MenuItems)
                        ? []
                        : entry.MenuItems.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    MainDish = entry.MainDish,
                    Details = entry.Details
                })
                .ToList();

            // Combine current and historical data
            var allMenuDays = historicalMenuDays.Concat(currentMenuDays)
                .OrderBy(m => m.Date)
                .ToList();

            var icalContent = calendarService.GenerateCalendar(allMenuDays);

            return Results.Text(icalContent, "text/calendar; charset=utf-8");
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error generating calendar: {ex.Message}");
        }
    }
}
