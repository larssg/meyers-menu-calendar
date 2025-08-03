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

            var todayMenu = await menuRepository.GetMenuForDateAsync(today, menuTypeId);
            var tomorrowMenu = await menuRepository.GetMenuForDateAsync(tomorrow, menuTypeId);

            return Results.Ok(new
            {
                today = todayMenu != null ? new
                {
                    title = CalendarService.CleanupTitle(todayMenu.MainDish),
                    details = todayMenu.Details
                } : null,
                tomorrow = tomorrowMenu != null ? new
                {
                    title = CalendarService.CleanupTitle(tomorrowMenu.MainDish),
                    details = tomorrowMenu.Details
                } : null
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error fetching menu preview: {ex.Message}");
        }
    }
}