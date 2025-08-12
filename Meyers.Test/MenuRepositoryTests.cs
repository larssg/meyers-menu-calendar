using System.Globalization;
using Meyers.Core.Models;
using Meyers.Infrastructure.Data;
using Meyers.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Meyers.Test;

public class MenuRepositoryTests
{
    private MenuDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<MenuDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        var context = new MenuDbContext(options);
        context.Database.OpenConnection();
        context.Database.Migrate();
        return context;
    }

    private async Task<MenuType> CreateTestMenuType(MenuDbContext context, string name = "Test Menu")
    {
        var menuType = new MenuType
        {
            Name = name,
            Slug = name.ToLowerInvariant().Replace(" ", "-"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.MenuTypes.Add(menuType);
        await context.SaveChangesAsync();
        return menuType;
    }

    private async Task<MenuEntry> CreateTestMenuEntry(MenuDbContext context, MenuType menuType, DateTime date)
    {
        var entry = new MenuEntry
        {
            Date = date,
            DayName = date.ToString("dddd", new CultureInfo("da-DK")),
            MenuItems = "Test dish; Test side",
            MainDish = "Test main dish",
            Details = "Test details",
            MenuTypeId = menuType.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.MenuEntries.Add(entry);
        await context.SaveChangesAsync();
        return entry;
    }

    [Fact]
    public async Task GetTotalMenuEntriesCountAsync_WithNoEntries_ReturnsZero()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);

        // Act
        var count = await repository.GetTotalMenuEntriesCountAsync();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetTotalMenuEntriesCountAsync_WithMultipleEntries_ReturnsCorrectCount()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);
        var menuType = await CreateTestMenuType(context);

        // Create 5 test entries
        for (var i = 0; i < 5; i++) await CreateTestMenuEntry(context, menuType, DateTime.Today.AddDays(i));

        // Act
        var count = await repository.GetTotalMenuEntriesCountAsync();

        // Assert
        Assert.Equal(5, count);
    }

    [Fact]
    public async Task GetFirstMenuDateAsync_WithNoEntries_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);

        // Act
        var firstDate = await repository.GetFirstMenuDateAsync();

        // Assert
        Assert.Null(firstDate);
    }

    [Fact]
    public async Task GetFirstMenuDateAsync_WithSingleEntry_ReturnsCorrectDate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);
        var menuType = await CreateTestMenuType(context);
        var testDate = new DateTime(2024, 1, 15);

        await CreateTestMenuEntry(context, menuType, testDate);

        // Act
        var firstDate = await repository.GetFirstMenuDateAsync();

        // Assert
        Assert.NotNull(firstDate);
        Assert.Equal(testDate.Date, firstDate.Value.Date);
    }

    [Fact]
    public async Task GetFirstMenuDateAsync_WithMultipleEntries_ReturnsEarliestDate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);
        var menuType = await CreateTestMenuType(context);

        var dates = new[]
        {
            new DateTime(2024, 3, 15),
            new DateTime(2024, 1, 10), // This should be the earliest
            new DateTime(2024, 2, 20),
            new DateTime(2024, 4, 5)
        };

        foreach (var date in dates) await CreateTestMenuEntry(context, menuType, date);

        // Act
        var firstDate = await repository.GetFirstMenuDateAsync();

        // Assert
        Assert.NotNull(firstDate);
        Assert.Equal(new DateTime(2024, 1, 10).Date, firstDate.Value.Date);
    }

    [Fact]
    public async Task GetLastMenuDateAsync_WithNoEntries_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);

        // Act
        var lastDate = await repository.GetLastMenuDateAsync();

        // Assert
        Assert.Null(lastDate);
    }

    [Fact]
    public async Task GetLastMenuDateAsync_WithSingleEntry_ReturnsCorrectDate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);
        var menuType = await CreateTestMenuType(context);
        var testDate = new DateTime(2024, 6, 20);

        await CreateTestMenuEntry(context, menuType, testDate);

        // Act
        var lastDate = await repository.GetLastMenuDateAsync();

        // Assert
        Assert.NotNull(lastDate);
        Assert.Equal(testDate.Date, lastDate.Value.Date);
    }

    [Fact]
    public async Task GetLastMenuDateAsync_WithMultipleEntries_ReturnsLatestDate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);
        var menuType = await CreateTestMenuType(context);

        var dates = new[]
        {
            new DateTime(2024, 3, 15),
            new DateTime(2024, 1, 10),
            new DateTime(2024, 2, 20),
            new DateTime(2024, 4, 5) // This should be the latest
        };

        foreach (var date in dates) await CreateTestMenuEntry(context, menuType, date);

        // Act
        var lastDate = await repository.GetLastMenuDateAsync();

        // Assert
        Assert.NotNull(lastDate);
        Assert.Equal(new DateTime(2024, 4, 5).Date, lastDate.Value.Date);
    }

    [Fact]
    public async Task GetFirstAndLastMenuDateAsync_WithMultipleMenuTypes_ReturnsCorrectDatesAcrossAllTypes()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);

        var menuType1 = await CreateTestMenuType(context, "Menu Type 1");
        var menuType2 = await CreateTestMenuType(context, "Menu Type 2");

        // Add entries for menu type 1
        await CreateTestMenuEntry(context, menuType1, new DateTime(2024, 2, 15));
        await CreateTestMenuEntry(context, menuType1, new DateTime(2024, 3, 20));

        // Add entries for menu type 2 with wider date range
        await CreateTestMenuEntry(context, menuType2, new DateTime(2024, 1, 5)); // Earliest overall
        await CreateTestMenuEntry(context, menuType2, new DateTime(2024, 5, 10)); // Latest overall

        // Act
        var firstDate = await repository.GetFirstMenuDateAsync();
        var lastDate = await repository.GetLastMenuDateAsync();

        // Assert
        Assert.NotNull(firstDate);
        Assert.NotNull(lastDate);
        Assert.Equal(new DateTime(2024, 1, 5).Date, firstDate.Value.Date);
        Assert.Equal(new DateTime(2024, 5, 10).Date, lastDate.Value.Date);
    }

    [Fact]
    public async Task GetFirstAndLastMenuDateAsync_WithSameDate_ReturnsSameDate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);
        var menuType1 = await CreateTestMenuType(context, "Menu Type 1");
        var menuType2 = await CreateTestMenuType(context, "Menu Type 2");
        var testDate = new DateTime(2024, 3, 15);

        // Create multiple entries for the same date but different menu types
        await CreateTestMenuEntry(context, menuType1, testDate);
        await CreateTestMenuEntry(context, menuType2, testDate);

        // Act
        var firstDate = await repository.GetFirstMenuDateAsync();
        var lastDate = await repository.GetLastMenuDateAsync();

        // Assert
        Assert.NotNull(firstDate);
        Assert.NotNull(lastDate);
        Assert.Equal(testDate.Date, firstDate.Value.Date);
        Assert.Equal(testDate.Date, lastDate.Value.Date);
    }

    [Fact]
    public async Task DateRangeQueries_WithTimeComponents_HandleDateOnlyComparison()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new MenuRepository(context);
        var menuType = await CreateTestMenuType(context);

        // Create entries with different time components but same date
        var baseDate = new DateTime(2024, 3, 15);
        var entry1 = new MenuEntry
        {
            Date = baseDate.AddHours(8), // 8 AM
            DayName = "Friday",
            MenuItems = "Morning dish",
            MainDish = "Morning main",
            Details = "Morning details",
            MenuTypeId = menuType.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var entry2 = new MenuEntry
        {
            Date = baseDate.AddHours(14), // 2 PM
            DayName = "Friday",
            MenuItems = "Afternoon dish",
            MainDish = "Afternoon main",
            Details = "Afternoon details",
            MenuTypeId = menuType.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.MenuEntries.AddRange(entry1, entry2);
        await context.SaveChangesAsync();

        // Act
        var firstDate = await repository.GetFirstMenuDateAsync();
        var lastDate = await repository.GetLastMenuDateAsync();
        var count = await repository.GetTotalMenuEntriesCountAsync();

        // Assert
        Assert.NotNull(firstDate);
        Assert.NotNull(lastDate);
        Assert.Equal(baseDate.Date, firstDate.Value.Date);
        Assert.Equal(baseDate.Date, lastDate.Value.Date);
        Assert.Equal(2, count);
    }
}