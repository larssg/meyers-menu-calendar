using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Meyers.Test;

public class CalendarApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

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
        var eventCount = System.Text.RegularExpressions.Regex.Matches(content, "BEGIN:VEVENT").Count;
        Assert.Equal(10, eventCount);
        
        // Verify the calendar contains the correct dates for both weeks with Det velkendte suffix
        // UIDs now include menu type: "meyers-menu-2025-07-28-det-velkendte"
        Assert.Contains("UID:meyers-menu-2025-07-28-det-velkendte", content); // Monday
        Assert.Contains("UID:meyers-menu-2025-07-29-det-velkendte", content); // Tuesday  
        Assert.Contains("UID:meyers-menu-2025-07-30-det-velkendte", content); // Wednesday
        Assert.Contains("UID:meyers-menu-2025-07-31-det-velkendte", content); // Thursday
        Assert.Contains("UID:meyers-menu-2025-08-01-det-velkendte", content); // Friday
        // Second week: August 4 - August 8, 2025
        Assert.Contains("UID:meyers-menu-2025-08-04-det-velkendte", content); // Monday
        Assert.Contains("UID:meyers-menu-2025-08-05-det-velkendte", content); // Tuesday
        Assert.Contains("UID:meyers-menu-2025-08-06-det-velkendte", content); // Wednesday
        Assert.Contains("UID:meyers-menu-2025-08-07-det-velkendte", content); // Thursday
        Assert.Contains("UID:meyers-menu-2025-08-08-det-velkendte", content); // Friday
    }
}