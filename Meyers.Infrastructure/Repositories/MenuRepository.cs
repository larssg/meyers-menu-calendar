using Meyers.Core.Interfaces;
using Meyers.Core.Models;
using Meyers.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Meyers.Infrastructure.Repositories;

public class MenuRepository(MenuDbContext context) : IMenuRepository
{
    public async Task<List<MenuEntry>> GetMenusForDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await context.MenuEntries
            .Include(m => m.MenuType)
            .Where(m => m.Date >= startDate && m.Date <= endDate)
            .OrderBy(m => m.Date)
            .ToListAsync();
    }

    public async Task<List<MenuEntry>> GetMenusForDateRangeAsync(DateTime startDate, DateTime endDate, int menuTypeId)
    {
        return await context.MenuEntries
            .Include(m => m.MenuType)
            .Where(m => m.Date >= startDate && m.Date <= endDate && m.MenuTypeId == menuTypeId)
            .OrderBy(m => m.Date)
            .ToListAsync();
    }

    public async Task<MenuEntry?> GetMenuForDateAsync(DateTime date)
    {
        // For backward compatibility, get the first menu entry for the date (likely "Det velkendte")
        return await context.MenuEntries
            .Include(m => m.MenuType)
            .FirstOrDefaultAsync(m => m.Date.Date == date.Date);
    }

    public async Task<MenuEntry?> GetMenuForDateAsync(DateTime date, int menuTypeId)
    {
        return await context.MenuEntries
            .Include(m => m.MenuType)
            .FirstOrDefaultAsync(m => m.Date.Date == date.Date && m.MenuTypeId == menuTypeId);
    }

    public async Task SaveMenuAsync(MenuEntry menuEntry)
    {
        var existing = await context.MenuEntries
            .AsTracking()
            .Include(m => m.MenuType)
            .FirstOrDefaultAsync(m => m.Date.Date == menuEntry.Date.Date && m.MenuTypeId == menuEntry.MenuTypeId);

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
            var existing = await context.MenuEntries
                .AsTracking()
                .Include(m => m.MenuType)
                .FirstOrDefaultAsync(m => m.Date.Date == menuEntry.Date.Date && m.MenuTypeId == menuEntry.MenuTypeId);

            if (existing != null)
            {
                existing.DayName = menuEntry.DayName;
                existing.MenuItems = menuEntry.MenuItems;
                existing.MainDish = menuEntry.MainDish;
                existing.Details = menuEntry.Details;
                existing.MenuTypeId = menuEntry.MenuTypeId;
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

    public async Task<List<MenuType>> GetMenuTypesAsync()
    {
        return await context.MenuTypes
            .Where(mt => mt.IsActive)
            .OrderBy(mt => mt.Name)
            .ToListAsync();
    }

    public async Task<MenuType?> GetMenuTypeBySlugAsync(string slug)
    {
        return await context.MenuTypes
            .FirstOrDefaultAsync(mt => mt.Slug == slug && mt.IsActive);
    }

    public async Task<MenuType> GetOrCreateMenuTypeAsync(string name)
    {
        var slug = MenuType.GenerateSlug(name);
        var existing = await context.MenuTypes
            .AsTracking()
            .FirstOrDefaultAsync(mt => mt.Slug == slug);

        if (existing != null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.UpdatedAt = DateTime.UtcNow;
                context.MenuTypes.Update(existing);
                await context.SaveChangesAsync();
            }

            return existing;
        }

        var menuType = new MenuType
        {
            Name = name,
            Slug = slug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await context.MenuTypes.AddAsync(menuType);
        await context.SaveChangesAsync();
        return menuType;
    }

    public async Task<Dictionary<int, (MenuEntry? today, MenuEntry? tomorrow)>> GetAllMenuPreviewsAsync(DateTime today,
        DateTime tomorrow)
    {
        // Fetch all menus for today and tomorrow in a single query
        var menus = await context.MenuEntries
            .Include(m => m.MenuType)
            .Where(m => m.MenuType.IsActive && (m.Date.Date == today.Date || m.Date.Date == tomorrow.Date))
            .ToListAsync();

        // Group by menu type and organize into today/tomorrow pairs
        var result = new Dictionary<int, (MenuEntry? today, MenuEntry? tomorrow)>();

        var menusByType = menus.GroupBy(m => m.MenuTypeId);
        foreach (var group in menusByType)
        {
            var todayMenu = group.FirstOrDefault(m => m.Date.Date == today.Date);
            var tomorrowMenu = group.FirstOrDefault(m => m.Date.Date == tomorrow.Date);
            result[group.Key] = (todayMenu, tomorrowMenu);
        }

        // Include menu types that have no entries for today/tomorrow
        var activeMenuTypes = await GetMenuTypesAsync();
        foreach (var menuType in activeMenuTypes)
            if (!result.ContainsKey(menuType.Id))
                result[menuType.Id] = (null, null);

        return result;
    }
}