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
    Task DeactivateMenuTypesNotInAsync(IEnumerable<string> activeNames);

    Task<Dictionary<int, (MenuEntry? today, MenuEntry? tomorrow)>> GetAllMenuPreviewsAsync(DateTime today,
        DateTime tomorrow);

    Task<int> GetTotalMenuEntriesCountAsync();
    Task<DateTime?> GetFirstMenuDateAsync();
    Task<DateTime?> GetLastMenuDateAsync();

    Task LogScrapingOperationAsync(ScrapingLog scrapingLog);
    Task<List<ScrapingLog>> GetRecentScrapingLogsAsync(int count = 50);

    Task LogCalendarDownloadAsync(CalendarDownloadLog downloadLog);
    Task<List<CalendarDownloadLog>> GetRecentCalendarDownloadsAsync(int count = 50);
    Task<int> GetCalendarDownloadCountAsync(DateTime since);
    Task<int> GetCalendarDownloadTotalCountAsync();
    Task<int> GetUniqueCalendarDownloadIpsCountAsync(DateTime since);
    Task<Dictionary<DateTime, int>> GetDailyDownloadCountsAsync(DateTime since);
    Task<Dictionary<int, int>> GetHourlyDownloadCountsAsync(DateTime since);
    Task<List<(string Name, int Count)>> GetTopDownloadClientsAsync(DateTime since, int limit = 10);
    Task<List<(string Name, int Count)>> GetTopDownloadFeedsAsync(DateTime since, int limit = 10);
    Task<List<DownloadSubscriberSummary>> GetDownloadSubscribersAsync(DateTime since, int limit = 20);
}