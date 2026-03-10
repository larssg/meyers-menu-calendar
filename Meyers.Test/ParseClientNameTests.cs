using System.Reflection;
using Meyers.Web.Handlers;

namespace Meyers.Test;

public class ParseClientNameTests
{
    private static string InvokeParseClientName(string userAgent)
    {
        var method = typeof(CalendarEndpointHandler).GetMethod("ParseClientName",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, [userAgent])!;
    }

    [Theory]
    [InlineData("", "Unknown")]
    [InlineData("Mozilla/5.0 (Macintosh) CalendarAgent/950", "Apple Calendar")]
    [InlineData("dataaccessd/1.0", "Apple Calendar")]
    [InlineData("CoreDAV/1.0", "Apple Calendar")]
    [InlineData("Google-Calendar-Importer", "Google Calendar")]
    [InlineData("Microsoft Outlook 16.0", "Outlook")]
    [InlineData("Mozilla/5.0 Thunderbird/115.0", "Thunderbird")]
    [InlineData("CalDAV-Client/1.0", "CalDAV Client")]
    [InlineData("curl/8.0", "curl")]
    [InlineData("Wget/1.21", "wget")]
    [InlineData("python-requests/2.31", "Python")]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64; rv:120.0) Gecko/20100101 Firefox/120.0", "Firefox")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36", "Chrome")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36 Edg/120.0", "Edge")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 14_0) AppleWebKit/605.1.15 Safari/605.1.15", "Safari")]
    public void ParseClientName_IdentifiesClients(string userAgent, string expected)
    {
        Assert.Equal(expected, InvokeParseClientName(userAgent));
    }

    [Fact]
    public void ParseClientName_TruncatesLongUnknownUserAgents()
    {
        var longAgent = new string('X', 100);
        var result = InvokeParseClientName(longAgent);

        Assert.Equal(53, result.Length); // 50 chars + "..."
        Assert.EndsWith("...", result);
    }
}
