using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Meyers.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Meyers.Test;

public class CalendarApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public CalendarApiTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_Calendar_DetVelkendte_Returns_iCal_Content()
    {
        var response = await _client.GetAsync("/calendar/det-velkendte.ics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/calendar; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("BEGIN:VCALENDAR", content);
        Assert.Contains("END:VCALENDAR", content);
    }

    [Fact]
    public async Task Get_Root_Returns_HTML_Page()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Meyers Menu Calendar", content);
        Assert.Contains("<!DOCTYPE html>", content);
    }

    [Fact]
    public async Task Get_Calendar_IncludesRefreshIntervalProperties()
    {
        var response = await _client.GetAsync("/calendar/det-velkendte.ics");

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("X-PUBLISHED-TTL:PT6H", content);
        Assert.Contains("REFRESH-INTERVAL;VALUE=DURATION:PT6H", content);
    }

    [Fact]
    public async Task Get_Calendar_DetVelkendte_Creates_Events_For_All_Weekdays()
    {
        var response = await _client.GetAsync("/calendar/det-velkendte.ics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("BEGIN:VCALENDAR", content);
        Assert.Contains("END:VCALENDAR", content);

        // Verify we have events with main dish titles (simplified format)
        Assert.Contains("SUMMARY:", content);

        // Verify the summary contains actual menu content, not just the template
        var summaryLines = content.Split('\n').Where(line => line.StartsWith("SUMMARY:")).ToArray();
        Assert.True(summaryLines.Length >= 5, "Should have at least 5 summary lines for weekdays");

        // Each summary should contain menu content, not just "Meyers Menu"
        foreach (var summaryLine in summaryLines.Take(5))
        {
            Assert.DoesNotContain("Meyers Menu -", summaryLine);
            Assert.True(summaryLine.Length > 10, $"Summary should contain actual menu content: {summaryLine}");
        }

        // Verify we have different UIDs for different days (dates) with menu type suffix
        Assert.Contains("UID:meyers-menu-", content);

        // Count the number of events - should be 10 for two weeks of weekdays
        var eventCount = Regex.Matches(content, "BEGIN:VEVENT").Count;
        Assert.Equal(10, eventCount);

        // Verify the calendar contains the correct dates for both weeks with Det velkendte suffix
        // Uge 11: March 9-13, Uge 12: March 16-20, 2026
        Assert.Contains("UID:meyers-menu-2026-03-09-det-velkendte", content); // Monday
        Assert.Contains("UID:meyers-menu-2026-03-10-det-velkendte", content); // Tuesday
        Assert.Contains("UID:meyers-menu-2026-03-11-det-velkendte", content); // Wednesday
        Assert.Contains("UID:meyers-menu-2026-03-12-det-velkendte", content); // Thursday
        Assert.Contains("UID:meyers-menu-2026-03-13-det-velkendte", content); // Friday
        // Second week
        Assert.Contains("UID:meyers-menu-2026-03-16-det-velkendte", content); // Monday
        Assert.Contains("UID:meyers-menu-2026-03-17-det-velkendte", content); // Tuesday
        Assert.Contains("UID:meyers-menu-2026-03-18-det-velkendte", content); // Wednesday
        Assert.Contains("UID:meyers-menu-2026-03-19-det-velkendte", content); // Thursday
        Assert.Contains("UID:meyers-menu-2026-03-20-det-velkendte", content); // Friday
    }

    [Fact]
    public async Task Get_HomePage_Includes_EmbeddedMenuData()
    {
        // First, ensure we have menu data by calling the calendar endpoint
        await _client.GetAsync("/calendar/det-velkendte.ics");

        // Get the home page
        var response = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Check that the menu data is embedded in the page
        Assert.Contains("window.menuData", content);
        Assert.Contains("weeklyData", content);

        // The data should include menu type information
        Assert.Contains("\"menuTypes\"", content);
        Assert.Contains("\"startDate\"", content);

        // Should include title property for menus
        Assert.Contains("\"title\"", content);
    }

    [Fact]
    public async Task Get_AdminRefreshMenus_Returns_Success()
    {
        var response = await _client.GetAsync("/admin/refresh-menus?secret=test-secret-123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // Verify the response structure
        Assert.True(result.TryGetProperty("success", out var successProperty));
        Assert.True(successProperty.GetBoolean());

        Assert.True(result.TryGetProperty("message", out var messageProperty));
        Assert.Contains("Successfully refreshed", messageProperty.GetString());

        Assert.True(result.TryGetProperty("timestamp", out var timestampProperty));
        Assert.True(DateTime.TryParse(timestampProperty.GetString(), out _));

        Assert.True(result.TryGetProperty("menuCount", out var menuCountProperty));
        var menuCount = menuCountProperty.GetInt32();
        Assert.True(menuCount > 0, $"Expected menu count > 0, but got {menuCount}");

        // Should have 60 entries (6 menu types × 10 weekdays)
        Assert.Equal(60, menuCount);
    }

    [Fact]
    public async Task Get_AdminRefreshMenus_Multiple_Calls_Work()
    {
        // First call
        var response1 = await _client.GetAsync("/admin/refresh-menus?secret=test-secret-123");
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        var content1 = await response1.Content.ReadAsStringAsync();
        var result1 = JsonSerializer.Deserialize<JsonElement>(content1);
        var count1 = result1.GetProperty("menuCount").GetInt32();

        // Second call should work without issues
        var response2 = await _client.GetAsync("/admin/refresh-menus?secret=test-secret-123");
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var content2 = await response2.Content.ReadAsStringAsync();
        var result2 = JsonSerializer.Deserialize<JsonElement>(content2);
        var count2 = result2.GetProperty("menuCount").GetInt32();

        // Both calls should return the same count (data gets updated/replaced)
        Assert.Equal(count1, count2);
    }

    [Fact]
    public async Task Get_AdminRefreshMenus_WithInvalidSecret_Returns_Forbidden()
    {
        var response = await _client.GetAsync("/admin/refresh-menus?secret=wrong-secret");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid or missing secret parameter", content);
    }

    [Fact]
    public async Task Get_AdminRefreshMenus_WithoutSecret_Returns_Forbidden()
    {
        var response = await _client.GetAsync("/admin/refresh-menus");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid or missing secret parameter", content);
    }

    [Fact]
    public async Task Get_Root_DoesNotInclude_ServerHeader()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify that the Server header is not present
        Assert.False(response.Headers.Contains("Server"), "Server header should not be present");
    }

    [Fact]
    public async Task Get_Root_Uses_HTTPS_URLs_When_Behind_Proxy()
    {
        // Add X-Forwarded-Proto header to simulate being behind a reverse proxy
        _client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");

        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Verify that calendar URLs use https:// even when the request appears to be http
        Assert.Contains("https://", content);
        Assert.DoesNotContain("http://localhost", content); // Should not contain http URLs

        // Clean up
        _client.DefaultRequestHeaders.Remove("X-Forwarded-Proto");
    }

    [Fact]
    public async Task Get_Root_Includes_JavaScript_File()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Verify that the JavaScript file is included
        // @Assets directive adds fingerprinting so we check for the base filename
        Assert.Contains("menu-app", content);
        Assert.Contains("js", content);
    }

    [Fact]
    public async Task Get_JavaScript_File_Returns_Correct_Content()
    {
        // Request the JavaScript file directly
        var jsResponse = await _client.GetAsync("/js/menu-app.js");

        Assert.Equal(HttpStatusCode.OK, jsResponse.StatusCode);
        Assert.Equal("text/javascript", jsResponse.Content.Headers.ContentType?.ToString());

        var jsContent = await jsResponse.Content.ReadAsStringAsync();

        // Verify the JavaScript contains our functions
        Assert.Contains("function copyToClipboard", jsContent);
        Assert.Contains("function toggleMenuMode", jsContent);
        Assert.Contains("TOAST_DURATION", jsContent);
    }

    [Fact]
    public async Task Get_CustomCalendar_Returns_iCal_Content()
    {
        // Config: M1T1W1R2F1 (Mon/Tue/Wed/Fri = menu type 1, Thu = menu type 2)
        var response = await _client.GetAsync("/calendar/custom/M1T1W1R2F1.ics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/calendar; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("BEGIN:VCALENDAR", content);
        Assert.Contains("END:VCALENDAR", content);
        Assert.Contains("Custom Menu Selection", content);
    }

    [Fact]
    public async Task Get_CustomCalendar_WithInvalidConfig_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/calendar/custom/invalid.ics");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_CustomCalendar_FiltersCorrectDays()
    {
        // Config: M1F1 (Only Monday and Friday from menu type 1)
        var response = await _client.GetAsync("/calendar/custom/M1F1.ics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Should contain events, but only for Monday and Friday
        var eventCount = Regex.Matches(content, "BEGIN:VEVENT").Count;
        Assert.True(eventCount > 0, "Should have some events for Monday and Friday");

        // Should be fewer events than a full week calendar
        var fullWeekResponse = await _client.GetAsync("/calendar/det-velkendte.ics");
        var fullWeekContent = await fullWeekResponse.Content.ReadAsStringAsync();
        var fullWeekEventCount = Regex.Matches(fullWeekContent, "BEGIN:VEVENT").Count;

        Assert.True(eventCount < fullWeekEventCount, "Custom calendar should have fewer events than full week");
    }

    [Fact]
    public async Task Get_Calendar_LogsDownload()
    {
        var response = await _client.GetAsync("/calendar/det-velkendte.ics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MenuDbContext>();
        var logs = await context.CalendarDownloadLogs
            .Where(dl => dl.FeedPath == "det-velkendte")
            .ToListAsync();

        Assert.True(logs.Count > 0, "Calendar download should be logged");
        Assert.All(logs, log =>
        {
            Assert.NotEmpty(log.IpHash);
            Assert.NotEmpty(log.ClientName);
        });
    }

    [Fact]
    public async Task Get_CustomCalendar_LogsDownload()
    {
        var response = await _client.GetAsync("/calendar/custom/M1T1W1R2F1.ics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MenuDbContext>();
        var logs = await context.CalendarDownloadLogs
            .Where(dl => dl.FeedPath == "custom/M1T1W1R2F1")
            .ToListAsync();

        Assert.True(logs.Count > 0, "Custom calendar download should be logged");
    }
}
