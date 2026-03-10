using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Meyers.Infrastructure.Data;
using Meyers.Infrastructure.Repositories;
using Meyers.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Meyers.Test;

public class MenuScrapingServiceTests
{
    private readonly string _testHtmlPath;

    public MenuScrapingServiceTests()
    {
        _testHtmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "meyers-menu-page.html");
    }

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

    private MenuScrapingService CreateService(MenuDbContext context)
    {
        var mockHttpClient = CreateMockHttpClient(_testHtmlPath);
        var repository = new MenuRepository(context);
        return new MenuScrapingService(mockHttpClient, repository);
    }

    private HttpClient CreateMockHttpClient(string filePath)
    {
        var mockHandler = new MockHttpMessageHandler(filePath);
        return new HttpClient(mockHandler);
    }

    [Fact]
    public async Task ScrapeMenuAsync_WithRealPageData_ReturnsAllWeekdays()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var scrapingService = CreateService(context);

        // Act
        var result = await scrapingService.ScrapeMenuAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify we have entries for all menu types and all weekdays (6 menu types × 10 days = 60 total)
        var expectedDays = new[] { "Mandag", "Tirsdag", "Onsdag", "Torsdag", "Fredag" };
        var foundDays = result.Select(r => r.DayName).ToList();

        Assert.Equal(60, foundDays.Count); // 6 menu types × 10 weekdays

        // Verify each weekday appears multiple times (once per menu type per week)
        foreach (var expectedDay in expectedDays)
        {
            var dayCount = foundDays.Count(d => d == expectedDay);
            Assert.True(dayCount >= 6, $"Expected at least 6 entries for {expectedDay}, but found {dayCount}");
        }

        // Verify menu items contain expected patterns
        foreach (var menuDay in result)
        {
            Assert.NotEmpty(menuDay.MenuItems);
            Assert.True(menuDay.MenuItems.Any(item =>
                    item.Contains(":", StringComparison.OrdinalIgnoreCase)), // Should have structured content
                $"Expected structured menu content for {menuDay.DayName}, but found: {string.Join("; ", menuDay.MenuItems)}");
        }
    }

    [Fact]
    public async Task ScrapeMenuAsync_WithRealPageData_ContainsValidMenuContent()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var scrapingService = CreateService(context);

        // Act
        var result = await scrapingService.ScrapeMenuAsync();

        // Assert
        var mondayMenu = result.FirstOrDefault(r =>
            r.DayName.Contains("Mandag", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(mondayMenu);
        Assert.NotEmpty(mondayMenu.MenuItems);

        // Verify Monday contains actual menu content
        var hasMenuContent = mondayMenu.MenuItems.Any(item =>
            item.Contains("Varm ret", StringComparison.OrdinalIgnoreCase) ||
            item.Contains("Alm.", StringComparison.OrdinalIgnoreCase) ||
            item.Contains("Vegetarisk", StringComparison.OrdinalIgnoreCase));

        Assert.True(hasMenuContent,
            $"Expected Monday menu to contain valid menu structure. Found items: {string.Join("; ", mondayMenu.MenuItems)}");
    }

    [Fact]
    public async Task ScrapeMenuAsync_WithTestData_EachDayHasDifferentContent()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var scrapingService = CreateService(context);

        // Act
        var result = await scrapingService.ScrapeMenuAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count >= 2, "Should have at least 2 days to compare");

        // Verify that different days have different content
        for (var i = 0; i < result.Count - 1; i++)
        {
            var currentDay = result[i];
            var nextDay = result[i + 1];

            Assert.NotEmpty(currentDay.MenuItems);
            Assert.NotEmpty(nextDay.MenuItems);

            // Check that at least some menu items are different between days
            var currentMenuText = string.Join(" ", currentDay.MenuItems);
            var nextMenuText = string.Join(" ", nextDay.MenuItems);

            Assert.NotEqual(currentMenuText, nextMenuText);
        }
    }

    [Fact]
    public async Task ScrapeMenuAsync_WithTestData_MondayHasCorrectDateAndContent()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var scrapingService = CreateService(context);

        // Act
        var result = await scrapingService.ScrapeMenuAsync();

        // Assert - Uge 11 2026: March 9-13 (Monday March 9)
        var mondayMenus = result.Where(r => r.DayName == "Mandag" && r.Date == new DateTime(2026, 3, 9)).ToList();
        Assert.NotEmpty(mondayMenus);
        Assert.Equal(6, mondayMenus.Count); // Should have 6 menu types for Monday

        // Find "Almanak" menu for Monday with Cassoulet
        var almanakMonday = mondayMenus.FirstOrDefault(m =>
            m.MenuItems.Any(item => item.Contains("Cassoulet", StringComparison.OrdinalIgnoreCase)));

        Assert.NotNull(almanakMonday);
        Assert.NotEmpty(almanakMonday.MenuItems);
    }

    [Fact]
    public async Task ScrapeMenuAsync_WithTestData_AllDaysHaveCorrectDates()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var scrapingService = CreateService(context);

        // Act
        var result = await scrapingService.ScrapeMenuAsync();

        // Assert
        Assert.Equal(60, result.Count); // 6 menu types × 10 weekdays

        // Expected dates: Uge 11 (March 9-13) and Uge 12 (March 16-20), 2026
        var expectedDates = new[]
        {
            new DateTime(2026, 3, 9), // Monday week 1
            new DateTime(2026, 3, 10), // Tuesday week 1
            new DateTime(2026, 3, 11), // Wednesday week 1
            new DateTime(2026, 3, 12), // Thursday week 1
            new DateTime(2026, 3, 13), // Friday week 1
            new DateTime(2026, 3, 16), // Monday week 2
            new DateTime(2026, 3, 17), // Tuesday week 2
            new DateTime(2026, 3, 18), // Wednesday week 2
            new DateTime(2026, 3, 19), // Thursday week 2
            new DateTime(2026, 3, 20) // Friday week 2
        };

        // Verify that all expected dates appear (6 times each, once per menu type)
        foreach (var expectedDate in expectedDates)
        {
            var entriesForDate = result.Where(r => r.Date == expectedDate).ToList();
            Assert.Equal(6, entriesForDate.Count); // Should have 6 menu types for each date
        }
    }

    [Fact]
    public async Task ScrapeMenuAsync_WithCachedData_UsesCache()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var callCount = 0;
        var mockHandler = new MockHttpMessageHandler(_testHtmlPath)
        {
            OnSendAsync = () => callCount++,
            UseCurrentWeekDates = true
        };
        var httpClient = new HttpClient(mockHandler);
        var repository = new MenuRepository(context);
        var scrapingService = new MenuScrapingService(httpClient, repository);

        // Act - First call should scrape from website
        var firstResult = await scrapingService.ScrapeMenuAsync();
        Assert.NotEmpty(firstResult);
        Assert.Equal(1, callCount);

        // Update timestamps to ensure cache is considered fresh
        var entries = await context.MenuEntries.ToListAsync();
        foreach (var entry in entries) entry.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Act - Second call should use cache (not call HTTP)
        var secondResult = await scrapingService.ScrapeMenuAsync();
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task ScrapeMenuAsync_WithExpiredCache_RefreshesData()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var callCount = 0;
        var mockHandler = new MockHttpMessageHandler(_testHtmlPath)
        {
            OnSendAsync = () => callCount++,
            UseCurrentWeekDates = true
        };
        var httpClient = new HttpClient(mockHandler);
        var repository = new MenuRepository(context);
        var scrapingService = new MenuScrapingService(httpClient, repository);

        // Act - First call should scrape from website
        var firstResult = await scrapingService.ScrapeMenuAsync();
        Assert.NotEmpty(firstResult);
        Assert.Equal(1, callCount);

        // Make cache appear expired by setting UpdatedAt to 7 hours ago
        var entries = await context.MenuEntries.ToListAsync();
        foreach (var entry in entries) entry.UpdatedAt = DateTime.UtcNow.AddHours(-7);
        await context.SaveChangesAsync();

        // Act - Second call should scrape again because cache is expired
        var secondResult = await scrapingService.ScrapeMenuAsync();
        Assert.Equal(2, callCount);
        Assert.NotEmpty(secondResult);
    }

    [Fact]
    public void ParseNuxtData_ExtractsAllMenuTypes()
    {
        var html = File.ReadAllText(_testHtmlPath);
        var result = MenuScrapingService.ParseNuxtData(html);

        var groups = result.GroupBy(r => r.MenuType).OrderBy(g => g.Key).ToList();
        foreach (var g in groups)
        {
            Console.WriteLine($"{g.Key}: {g.Count()} entries");
        }
        Console.WriteLine($"Total: {result.Count}");

        var menuTypes = groups.Select(g => g.Key).ToList();
        Assert.Equal(6, menuTypes.Count);
        Assert.Contains("Almanak", menuTypes);
        Assert.Contains("Den Grønne", menuTypes);
        Assert.Contains("Det Velkendte", menuTypes);
        Assert.Contains("Meyers til frokost Aarhus", menuTypes);
        Assert.Contains("En Bid Grønnere", menuTypes);
        Assert.Contains("Det Velkendte - Portionspakket", menuTypes);

        // 6 menu types × 2 weeks × 5 days = 60
        Assert.Equal(60, result.Count);
    }

    [Fact]
    public async Task ScrapeMenuAsync_DeactivatesStaleMenuTypes()
    {
        // Arrange - create a menu type that won't appear in the scraped data
        using var context = CreateInMemoryContext();
        context.MenuTypes.Add(new Meyers.Core.Models.MenuType
        {
            Name = "Det friske",
            Slug = "det-friske",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var scrapingService = CreateService(context);

        // Act
        await scrapingService.ScrapeMenuAsync(forceRefresh: true);

        // Assert - "Det friske" should be deactivated
        var detFriske = await context.MenuTypes.FirstAsync(mt => mt.Slug == "det-friske");
        Assert.False(detFriske.IsActive);

        // Active scraped types should still be active
        var activeTypes = await context.MenuTypes.Where(mt => mt.IsActive).ToListAsync();
        Assert.Equal(6, activeTypes.Count);
    }

    [Fact]
    public void ParseWeekDates_ParsesCorrectly()
    {
        var dates = MenuScrapingService.ParseWeekDates("Uge 11");
        Assert.Equal(5, dates.Count);
        Assert.Equal(new DateTime(2026, 3, 9), dates[0]); // Monday
        Assert.Equal(new DateTime(2026, 3, 13), dates[4]); // Friday
    }

    [Fact]
    public void ParseNuxtData_MainDishDoesNotContainDietPrefix()
    {
        var html = File.ReadAllText(_testHtmlPath);
        var result = MenuScrapingService.ParseNuxtData(html);

        foreach (var day in result)
        {
            // MainDish should never be just "Alm." or start with diet prefixes
            Assert.False(day.MainDish == "Alm.",
                $"{day.MenuType} {day.DayName}: MainDish is just 'Alm.' — diet prefix not stripped");
            Assert.DoesNotMatch(@"^Alm\.?\s*/?\s*(halal)?\s*:", day.MainDish);
            Assert.DoesNotMatch(@"^Vegetarisk\s*:", day.MainDish);
            Assert.DoesNotMatch(@"^Vegansk\s*:", day.MainDish);
        }
    }

    [Fact]
    public void ParseNuxtData_MainDishHasNoTrailingPeriod()
    {
        var html = File.ReadAllText(_testHtmlPath);
        var result = MenuScrapingService.ParseNuxtData(html);

        foreach (var day in result)
        {
            Assert.False(day.MainDish.EndsWith('.'),
                $"{day.MenuType} {day.DayName}: MainDish ends with period: \"{day.MainDish}\"");
        }
    }
}

// Mock HttpMessageHandler for testing
public partial class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _filePath;

    public MockHttpMessageHandler(string filePath)
    {
        _filePath = filePath;
    }

    public Action? OnSendAsync { get; set; }

    /// <summary>
    /// When true, replaces hardcoded week numbers with current-week equivalents.
    /// This prevents cache tests from becoming flaky as time passes.
    /// </summary>
    public bool UseCurrentWeekDates { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        OnSendAsync?.Invoke();

        if (File.Exists(_filePath))
        {
            var content = await File.ReadAllTextAsync(_filePath, cancellationToken);
            if (UseCurrentWeekDates)
            {
                content = ReplaceWithCurrentWeekNumbers(content);
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "text/html")
            };
        }

        throw new FileNotFoundException($"Test file not found: {_filePath}");
    }

    /// <summary>
    /// Replaces hardcoded "Uge N" labels with current and next week numbers.
    /// The fixture has two distinct week numbers — map them to current and next week.
    /// </summary>
    private static string ReplaceWithCurrentWeekNumbers(string content)
    {
        var today = DateTime.Today;
        var currentWeek = System.Globalization.ISOWeek.GetWeekOfYear(today);
        var nextWeek = System.Globalization.ISOWeek.GetWeekOfYear(today.AddDays(7));

        // Find the two distinct week numbers in the fixture
        var fixtureWeeks = WeekLabelRegex().Matches(content)
            .Select(m => int.Parse(m.Groups[1].Value))
            .Distinct()
            .OrderBy(w => w)
            .ToList();

        if (fixtureWeeks.Count >= 2)
        {
            // Replace first fixture week with current, second with next
            content = content.Replace($"Uge {fixtureWeeks[0]}", $"Uge {currentWeek}");
            content = content.Replace($"Uge {fixtureWeeks[1]}", $"Uge {nextWeek}");
        }

        return content;
    }

    [GeneratedRegex(@"Uge (\d+)")]
    private static partial Regex WeekLabelRegex();
}
