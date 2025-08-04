using System.Reflection;
using Meyers.Infrastructure.Configuration;
using Meyers.Web.Handlers;
using Microsoft.Extensions.Options;

namespace Meyers.Test;

public class CacheDurationTests
{
    private readonly MenuCacheOptions _cacheOptions;
    private readonly CalendarEndpointHandler _handler;

    public CacheDurationTests()
    {
        _cacheOptions = new MenuCacheOptions
        {
            RefreshIntervalHours = 6,
            CheckIntervalMinutes = 30,
            StartupDelaySeconds = 30
        };

        var options = Options.Create(_cacheOptions);

        // We need to create a mock handler to test the private method
        _handler = new CalendarEndpointHandler(
            null!, null!, null!, options);
    }

    [Fact]
    public void CalculateCacheDuration_RecentUpdate_ReturnsTimeUntilNextRefresh()
    {
        // Arrange: Menu was updated 2 hours ago
        var lastModified = DateTime.UtcNow.AddHours(-2);

        // Act: Call private method via reflection
        var cacheDuration = CallCalculateCacheDuration(lastModified);

        // Assert: Should cache until next refresh (5.4 hours from last update) + 10 min buffer
        // So: 5.4 - 2 + 0.17 = ~3.57 hours = ~12,852 seconds
        Assert.True(cacheDuration > 12000, $"Cache duration {cacheDuration} should be > 12000 seconds");
        Assert.True(cacheDuration < 14000, $"Cache duration {cacheDuration} should be < 14000 seconds");
    }

    [Fact]
    public void CalculateCacheDuration_OldUpdate_ReturnsMinimumCache()
    {
        // Arrange: Menu was updated 8 hours ago (past refresh time)
        var lastModified = DateTime.UtcNow.AddHours(-8);

        // Act
        var cacheDuration = CallCalculateCacheDuration(lastModified);

        // Assert: Should return minimum cache time (5 minutes = 300 seconds)
        Assert.Equal(300, cacheDuration);
    }

    [Fact]
    public void CalculateCacheDuration_VeryRecentUpdate_ReturnsReasonableTime()
    {
        // Arrange: Menu was updated 30 minutes ago
        var lastModified = DateTime.UtcNow.AddMinutes(-30);

        // Act
        var cacheDuration = CallCalculateCacheDuration(lastModified);

        // Assert: Should cache until next refresh + buffer
        // 5.4 hours - 0.5 hours + 10 minutes = ~5 hours = ~18,000 seconds
        Assert.True(cacheDuration > 17000, $"Cache duration {cacheDuration} should be > 17000 seconds");
        Assert.True(cacheDuration < 19000, $"Cache duration {cacheDuration} should be < 19000 seconds");
    }

    [Fact]
    public void CalculateCacheDuration_FutureUpdate_ReturnsMinimumCache()
    {
        // Arrange: Menu "updated" in the future (edge case)
        var lastModified = DateTime.UtcNow.AddHours(1);

        // Act
        var cacheDuration = CallCalculateCacheDuration(lastModified);

        // Assert: Should return minimum cache time
        Assert.Equal(300, cacheDuration);
    }

    [Fact]
    public void CalculateCacheDuration_NeverExceedsMaximum()
    {
        // Arrange: Test various times
        var testTimes = new[]
        {
            DateTime.UtcNow.AddMinutes(-1), // Very recent
            DateTime.UtcNow.AddHours(-1), // 1 hour ago
            DateTime.UtcNow.AddHours(-3), // Mid-cycle
            DateTime.UtcNow.AddHours(-10) // Very old
        };

        foreach (var lastModified in testTimes)
        {
            // Act
            var cacheDuration = CallCalculateCacheDuration(lastModified);

            // Assert: Never exceed 6 hours (21,600 seconds)
            Assert.True(cacheDuration <= 21600,
                $"Cache duration {cacheDuration} should not exceed 21600 seconds for time {lastModified}");

            // Assert: Never below 5 minutes (300 seconds)
            Assert.True(cacheDuration >= 300,
                $"Cache duration {cacheDuration} should not be below 300 seconds for time {lastModified}");
        }
    }

    // Helper method to call private CalculateCacheDuration method via reflection
    private int CallCalculateCacheDuration(DateTime lastModified)
    {
        var method = typeof(CalendarEndpointHandler).GetMethod("CalculateCacheDuration",
            BindingFlags.NonPublic | BindingFlags.Instance);

        return (int)method!.Invoke(_handler, [lastModified])!;
    }
}