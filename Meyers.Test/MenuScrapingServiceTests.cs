using System.Net;
using System.Text;
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
        foreach (var item in mondayMenu.MenuItems) Console.WriteLine($"Menu item: {item}");
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

        // Assert
        var mondayMenus = result.Where(r => r.DayName == "Mandag" && r.Date == new DateTime(2025, 11, 10)).ToList();
        Assert.NotEmpty(mondayMenus);
        Assert.Equal(8, mondayMenus.Count); // Should have 8 menu types for Monday

        // Find "Det velkendte" menu for Monday November 10, 2025 
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
            new DateTime(2025, 11, 10), // Monday week 1
            new DateTime(2025, 11, 11), // Tuesday week 1
            new DateTime(2025, 11, 12), // Wednesday week 1
            new DateTime(2025, 11, 13), // Thursday week 1
            new DateTime(2025, 11, 14), // Friday week 1
            new DateTime(2025, 11, 17), // Monday week 2
            new DateTime(2025, 11, 18), // Tuesday week 2
            new DateTime(2025, 11, 19), // Wednesday week 2
            new DateTime(2025, 11, 20), // Thursday week 2
            new DateTime(2025, 11, 21) // Friday week 2
        };

        // Verify that all expected dates appear (8 times each, once per menu type)
        foreach (var expectedDate in expectedDates)
        {
            var entriesForDate = result.Where(r => r.Date == expectedDate).ToList();
            Assert.Equal(8, entriesForDate.Count); // Should have 8 menu types for each date
        }
    }

    [Fact]
    public async Task ScrapeMenuAsync_WithCachedData_UsesCache()
    {
        // This test verifies that when data is cached and fresh, it doesn't scrape again
        // We'll test this by checking that the HTTP client is not called on the second request

        // Arrange
        using var context = CreateInMemoryContext();
        var callCount = 0;
        var mockHandler = new MockHttpMessageHandler(_testHtmlPath)
        {
            OnSendAsync = () => callCount++
        };
        var httpClient = new HttpClient(mockHandler);
        var repository = new MenuRepository(context);
        var scrapingService = new MenuScrapingService(httpClient, repository);

        // Act - First call should scrape from website
        var firstResult = await scrapingService.ScrapeMenuAsync();
        Assert.NotEmpty(firstResult);
        Assert.Equal(1, callCount); // HTTP client should be called once

        // Update timestamps to ensure cache is considered fresh
        // Also shift entry dates to be within the valid cache range (today -7 to +14 days)
        // since the test HTML has fixed dates that may be outside the current date window
        var entries = await context.MenuEntries.ToListAsync();
        var minDate = entries.Min(e => e.Date);
        var dateOffset = DateTime.Today - minDate; // Shift all dates to start from today
        foreach (var entry in entries)
        {
            entry.UpdatedAt = DateTime.UtcNow;
            entry.Date = entry.Date.Add(dateOffset);
        }
        await context.SaveChangesAsync();

        // Act - Second call should use cache (not call HTTP)
        var secondResult = await scrapingService.ScrapeMenuAsync();
        Assert.Equal(1, callCount); // HTTP client should NOT be called again

        // The results might differ in count due to date filtering in GetCachedMenusAsync
        // but the important thing is that no new HTTP request was made
    }

    [Fact]
    public async Task ScrapeMenuAsync_WithExpiredCache_RefreshesData()
    {
        // This test verifies that when cache is expired, it scrapes fresh data

        // Arrange
        using var context = CreateInMemoryContext();
        var callCount = 0;
        var mockHandler = new MockHttpMessageHandler(_testHtmlPath)
        {
            OnSendAsync = () => callCount++
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
        Assert.Equal(2, callCount); // HTTP client should be called again
        Assert.NotEmpty(secondResult);
    }
}

// Mock HttpMessageHandler for testing
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _filePath;

    public MockHttpMessageHandler(string filePath)
    {
        _filePath = filePath;
    }

    public Action? OnSendAsync { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        OnSendAsync?.Invoke();

        if (File.Exists(_filePath))
        {
            var content = await File.ReadAllTextAsync(_filePath, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "text/html")
            };
        }

        throw new FileNotFoundException($"Test file not found: {_filePath}");
    }
}