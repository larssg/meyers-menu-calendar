using Meyers.Web.Services;
using Meyers.Web.Data;
using Meyers.Web.Repositories;
using Microsoft.EntityFrameworkCore;
using HtmlAgilityPack;

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
            .UseSqlite($"Data Source=:memory:")
            .Options;
        
        var context = new MenuDbContext(options);
        context.Database.OpenConnection();
        context.Database.Migrate();
        return context;
    }
    
    private MenuScrapingService CreateService(MenuDbContext context)
    {
        var mockHttpClient = new MockHttpClient(_testHtmlPath);
        var repository = new MenuRepository(context);
        return new MenuScrapingService(mockHttpClient, repository);
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
        
        // Verify we have entries for all menu types and all weekdays (8 menu types × 10 days = 80 total)
        var expectedDays = new[] { "Mandag", "Tirsdag", "Onsdag", "Torsdag", "Fredag" };
        var foundDays = result.Select(r => r.DayName).ToList();
        
        Assert.Equal(80, foundDays.Count); // 8 menu types × 10 weekdays
        
        // Verify each weekday appears multiple times (once per menu type)
        foreach (var expectedDay in expectedDays)
        {
            var dayCount = foundDays.Count(d => d == expectedDay);
            Assert.True(dayCount >= 8, $"Expected at least 8 entries for {expectedDay}, but found {dayCount}");
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
        
        // Verify Monday contains actual menu content (not specific dishes as they change daily)
        var hasMenuContent = mondayMenu.MenuItems.Any(item => 
            item.Contains("Varm ret", StringComparison.OrdinalIgnoreCase) ||
            item.Contains("Alm./Halal:", StringComparison.OrdinalIgnoreCase) ||
            item.Contains("Vegetarisk", StringComparison.OrdinalIgnoreCase));
            
        Assert.True(hasMenuContent, 
            $"Expected Monday menu to contain valid menu structure. Found items: {string.Join("; ", mondayMenu.MenuItems)}");
        
        // Log the actual content for debugging
        foreach (var item in mondayMenu.MenuItems)
        {
            Console.WriteLine($"Menu item: {item}");
        }
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
        for (int i = 0; i < result.Count - 1; i++)
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

        // Assert
        var mondayMenus = result.Where(r => r.DayName == "Mandag" && r.Date == new DateTime(2025, 7, 28)).ToList();
        Assert.NotEmpty(mondayMenus);
        Assert.Equal(8, mondayMenus.Count); // Should have 8 menu types for Monday
        
        // Find "Det velkendte" menu for Monday July 28, 2025 
        var detVelkendteMonday = mondayMenus.FirstOrDefault(m => 
            m.MenuItems.Any(item => item.Contains("Kylling og champignon", StringComparison.OrdinalIgnoreCase)));
        
        Assert.NotNull(detVelkendteMonday);
        Assert.NotEmpty(detVelkendteMonday.MenuItems);
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
        Assert.Equal(80, result.Count); // 8 menu types × 10 weekdays
        
        // Expected dates for each week
        var expectedDates = new[]
        {
            new DateTime(2025, 7, 28), // Monday week 1
            new DateTime(2025, 7, 29), // Tuesday week 1
            new DateTime(2025, 7, 30), // Wednesday week 1
            new DateTime(2025, 7, 31), // Thursday week 1
            new DateTime(2025, 8, 1),  // Friday week 1
            new DateTime(2025, 8, 4),  // Monday week 2
            new DateTime(2025, 8, 5),  // Tuesday week 2
            new DateTime(2025, 8, 6),  // Wednesday week 2
            new DateTime(2025, 8, 7),  // Thursday week 2
            new DateTime(2025, 8, 8)   // Friday week 2
        };
        
        // Verify that all expected dates appear (8 times each, once per menu type)
        foreach (var expectedDate in expectedDates)
        {
            var entriesForDate = result.Where(r => r.Date == expectedDate).ToList();
            Assert.Equal(8, entriesForDate.Count); // Should have 8 menu types for each date
        }
    }
    
    [Fact]
    public async Task ScrapeMenuAsync_WithCachedData_ReturnsCachedResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var scrapingService = CreateService(context);
        
        // First call should scrape from website and cache the data
        var firstResult = await scrapingService.ScrapeMenuAsync();
        Assert.NotEmpty(firstResult);
        
        // Verify data was cached
        var cachedEntries = await context.MenuEntries.ToListAsync();
        Assert.NotEmpty(cachedEntries);
        
        // Second call should return cached data (since cache is fresh)
        var secondResult = await scrapingService.ScrapeMenuAsync();
        
        // Results should be identical
        Assert.Equal(firstResult.Count, secondResult.Count);
        
        // Sort both results by date, day name, and menu content for consistent comparison
        var firstSorted = firstResult.OrderBy(r => r.Date).ThenBy(r => r.DayName).ThenBy(r => string.Join("|", r.MenuItems)).ToList();
        var secondSorted = secondResult.OrderBy(r => r.Date).ThenBy(r => r.DayName).ThenBy(r => string.Join("|", r.MenuItems)).ToList();
        
        for (int i = 0; i < firstSorted.Count; i++)
        {
            Assert.Equal(firstSorted[i].DayName, secondSorted[i].DayName);
            Assert.Equal(firstSorted[i].Date, secondSorted[i].Date);
            Assert.Equal(firstSorted[i].MenuItems.Count, secondSorted[i].MenuItems.Count);
        }
    }
}

// Mock HttpClient for testing
public class MockHttpClient : HttpClient
{
    private readonly string _filePath;

    public MockHttpClient(string filePath)
    {
        _filePath = filePath;
    }

    public new async Task<string> GetStringAsync(string requestUri)
    {
        if (File.Exists(_filePath))
        {
            return await File.ReadAllTextAsync(_filePath);
        }
        throw new FileNotFoundException($"Test file not found: {_filePath}");
    }
}