using Meyers.Web.Repositories;
using Meyers.Web.Services;

namespace Meyers.Web.Handlers;

public class MenuPreviewHandler(IMenuRepository menuRepository)
{
    public async Task<IResult> GetMenuPreviewAsync(int menuTypeId)
    {
        try
        {
            // Get Copenhagen timezone
            TimeZoneInfo copenhagenTimeZone;
            try
            {
                copenhagenTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            }
            catch
            {
                try
                {
                    copenhagenTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");
                }
                catch
                {
                    copenhagenTimeZone = TimeZoneInfo.Utc;
                }
            }

            var copenhagenNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, copenhagenTimeZone);
            var today = copenhagenNow.Date;
            var tomorrow = today.AddDays(1);

            // Get both today's and tomorrow's menu in a single query
            var menus = await menuRepository.GetMenusForDateRangeAsync(today, tomorrow, menuTypeId);
            var todayMenu = menus.FirstOrDefault(m => m.Date.Date == today);
            var tomorrowMenu = menus.FirstOrDefault(m => m.Date.Date == tomorrow);

            return Results.Ok(new
            {
                today = todayMenu != null
                    ? new
                    {
                        title = CalendarService.CleanupTitle(todayMenu.MainDish)
                    }
                    : null,
                tomorrow = tomorrowMenu != null
                    ? new
                    {
                        title = CalendarService.CleanupTitle(tomorrowMenu.MainDish)
                    }
                    : null
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error fetching menu preview: {ex.Message}");
        }
    }
}
