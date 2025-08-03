using Microsoft.EntityFrameworkCore;
using Meyers.Web.Data;
using Meyers.Web.Models;

namespace Meyers.Web.Repositories;

public class MenuRepository(MenuDbContext context) : IMenuRepository
{
    public async Task<List<MenuEntry>> GetMenusForDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await context.MenuEntries
            .Where(m => m.Date >= startDate && m.Date <= endDate)
            .OrderBy(m => m.Date)
            .ToListAsync();
    }

    public async Task<MenuEntry?> GetMenuForDateAsync(DateTime date)
    {
        return await context.MenuEntries
            .FirstOrDefaultAsync(m => m.Date.Date == date.Date);
    }

    public async Task SaveMenuAsync(MenuEntry menuEntry)
    {
        var existing = await GetMenuForDateAsync(menuEntry.Date);

        if (existing != null)
        {
            existing.DayName = menuEntry.DayName;
            existing.MenuItems = menuEntry.MenuItems;
            existing.MainDish = menuEntry.MainDish;
            existing.Details = menuEntry.Details;
            existing.UpdatedAt = DateTime.UtcNow;
            context.MenuEntries.Update(existing);
        }
        else
        {
            await context.MenuEntries.AddAsync(menuEntry);
        }

        await context.SaveChangesAsync();
    }

    public async Task SaveMenusAsync(List<MenuEntry> menuEntries)
    {
        foreach (var menuEntry in menuEntries)
        {
            var existing = await GetMenuForDateAsync(menuEntry.Date);

            if (existing != null)
            {
                existing.DayName = menuEntry.DayName;
                existing.MenuItems = menuEntry.MenuItems;
                existing.MainDish = menuEntry.MainDish;
                existing.Details = menuEntry.Details;
                existing.UpdatedAt = DateTime.UtcNow;
                context.MenuEntries.Update(existing);
            }
            else
            {
                await context.MenuEntries.AddAsync(menuEntry);
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<DateTime?> GetLastUpdateTimeAsync()
    {
        return await context.MenuEntries
            .OrderByDescending(m => m.UpdatedAt)
            .Select(m => m.UpdatedAt)
            .FirstOrDefaultAsync();
    }
}
