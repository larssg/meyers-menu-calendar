using Meyers.Web.Models;

namespace Meyers.Web.Repositories;

public interface IMenuRepository
{
    Task<List<MenuEntry>> GetMenusForDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<MenuEntry?> GetMenuForDateAsync(DateTime date);
    Task SaveMenuAsync(MenuEntry menuEntry);
    Task SaveMenusAsync(List<MenuEntry> menuEntries);
    Task<DateTime?> GetLastUpdateTimeAsync();
}