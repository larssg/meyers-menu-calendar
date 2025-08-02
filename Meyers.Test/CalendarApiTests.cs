using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Meyers.Test;

public class CalendarApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CalendarApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_Calendar_Returns_iCal_Content()
    {
        var response = await _client.GetAsync("/calendar");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/calendar; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("BEGIN:VCALENDAR", content);
        Assert.Contains("END:VCALENDAR", content);
    }

    [Fact]
    public async Task Get_Root_Returns_API_Description()
    {
        var response = await _client.GetAsync("/");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Meyers Menu Calendar API", content);
    }

    [Fact]
    public async Task Get_Calendar_Creates_Events_For_All_Weekdays()
    {
        var response = await _client.GetAsync("/calendar");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("BEGIN:VCALENDAR", content);
        Assert.Contains("END:VCALENDAR", content);
        
        // Verify we have events for all weekdays
        var weekdays = new[] { "Mandag", "Tirsdag", "Onsdag", "Torsdag", "Fredag" };
        foreach (var day in weekdays)
        {
            Assert.Contains($"SUMMARY:Meyers Menu - {day}", content);
        }
        
        // Verify we have different UIDs for different days (dates)
        Assert.Contains("UID:meyers-menu-", content);
        
        // Count the number of events - should be 5 for weekdays
        var eventCount = System.Text.RegularExpressions.Regex.Matches(content, "BEGIN:VEVENT").Count;
        Assert.Equal(5, eventCount);
        
        // Verify the calendar contains the correct dates from July 28 - August 1, 2025
        Assert.Contains("UID:meyers-menu-2025-07-28", content); // Monday
        Assert.Contains("UID:meyers-menu-2025-07-29", content); // Tuesday  
        Assert.Contains("UID:meyers-menu-2025-07-30", content); // Wednesday
        Assert.Contains("UID:meyers-menu-2025-07-31", content); // Thursday
        Assert.Contains("UID:meyers-menu-2025-08-01", content); // Friday
    }
}