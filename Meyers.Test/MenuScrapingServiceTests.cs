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
        
        // Verify we have all weekday entries for two weeks (10 days total)
        var expectedDays = new[] { "Mandag", "Tirsdag", "Onsdag", "Torsdag", "Fredag" };
        var foundDays = result.Select(r => r.DayName).ToList();
        
        Assert.Equal(10, foundDays.Count); // Two weeks of weekdays
        
        foreach (var expectedDay in expectedDays)
        {
            Assert.Contains(expectedDay, foundDays);
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
        var mondayMenu = result.FirstOrDefault(r => r.DayName == "Mandag");
        Assert.NotNull(mondayMenu);
        
        // Monday should be July 28, 2025 based on the HTML
        Assert.Equal(new DateTime(2025, 7, 28), mondayMenu.Date);
        
        // Monday should contain "Oksekødboller i krydret tomatsauce" as mentioned by user (check both encoded and decoded)
        var hasCorrectDish = mondayMenu.MenuItems.Any(item => 
            item.Contains("Oksekødboller i krydret tomatsauce", StringComparison.OrdinalIgnoreCase) ||
            item.Contains("Oksek&#248;dboller i krydret tomatsauce", StringComparison.OrdinalIgnoreCase));
            
        Assert.True(hasCorrectDish, 
            $"Expected Monday (July 28) menu to contain 'Oksekødboller i krydret tomatsauce'. Found items: {string.Join("; ", mondayMenu.MenuItems)}");
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
        Assert.Equal(10, result.Count);
        
        var expectedDates = new[]
        {
            // First week
            (DayName: "Mandag", Date: new DateTime(2025, 7, 28)),
            (DayName: "Tirsdag", Date: new DateTime(2025, 7, 29)),
            (DayName: "Onsdag", Date: new DateTime(2025, 7, 30)),
            (DayName: "Torsdag", Date: new DateTime(2025, 7, 31)),
            (DayName: "Fredag", Date: new DateTime(2025, 8, 1)),
            // Second week
            (DayName: "Mandag", Date: new DateTime(2025, 8, 4)),
            (DayName: "Tirsdag", Date: new DateTime(2025, 8, 5)),
            (DayName: "Onsdag", Date: new DateTime(2025, 8, 6)),
            (DayName: "Torsdag", Date: new DateTime(2025, 8, 7)),
            (DayName: "Fredag", Date: new DateTime(2025, 8, 8))
        };
        
        for (int i = 0; i < expectedDates.Length; i++)
        {
            var actualDay = result[i];
            var expectedDay = expectedDates[i];
            
            Assert.Equal(expectedDay.DayName, actualDay.DayName);
            Assert.Equal(expectedDay.Date, actualDay.Date);
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
        for (int i = 0; i < firstResult.Count; i++)
        {
            Assert.Equal(firstResult[i].DayName, secondResult[i].DayName);
            Assert.Equal(firstResult[i].Date, secondResult[i].Date);
            Assert.Equal(firstResult[i].MenuItems.Count, secondResult[i].MenuItems.Count);
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