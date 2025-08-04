using Microsoft.EntityFrameworkCore;
using Meyers.Web.Data;
using Meyers.Web.Models;

namespace Meyers.Web.Repositories;

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
        var existing = await GetMenuForDateAsync(menuEntry.Date, menuEntry.MenuTypeId);

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
            var existing = await GetMenuForDateAsync(menuEntry.Date, menuEntry.MenuTypeId);

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
}