using Meyers.Core.Models;

namespace Meyers.Core.Interfaces;

public interface IMenuRepository
{
    Task<List<MenuEntry>> GetMenusForDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<List<MenuEntry>> GetMenusForDateRangeAsync(DateTime startDate, DateTime endDate, int menuTypeId);
    Task<MenuEntry?> GetMenuForDateAsync(DateTime date);
    Task<MenuEntry?> GetMenuForDateAsync(DateTime date, int menuTypeId);
    Task SaveMenuAsync(MenuEntry menuEntry);
    Task SaveMenusAsync(List<MenuEntry> menuEntries);
    Task<DateTime?> GetLastUpdateTimeAsync();
    Task<List<MenuType>> GetMenuTypesAsync();
    Task<MenuType?> GetMenuTypeBySlugAsync(string slug);
    Task<MenuType> GetOrCreateMenuTypeAsync(string name);

    Task<Dictionary<int, (MenuEntry? today, MenuEntry? tomorrow)>> GetAllMenuPreviewsAsync(DateTime today,
        DateTime tomorrow);
}