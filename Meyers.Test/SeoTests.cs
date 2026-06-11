using System.Net;

namespace Meyers.Test;

public class SeoTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SeoTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RobotsTxt_Allows_Search_Engines_But_Disallows_NonContent_Paths()
    {
        var response = await _client.GetAsync("/robots.txt");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("User-agent: *", content);
        Assert.DoesNotContain("User-agent: *\nDisallow: /\n", content.Replace("\r\n", "\n"));
        Assert.Contains("Disallow: /admin", content);
        Assert.Contains("Disallow: /api", content);
        Assert.Contains("Disallow: /calendar", content);
    }

    [Fact]
    public async Task RobotsTxt_Still_Blocks_AI_Training_Crawlers()
    {
        var response = await _client.GetAsync("/robots.txt");

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("User-agent: GPTBot", content);
        Assert.Contains("User-agent: CCBot", content);
        Assert.Contains("User-agent: Google-Extended", content);
    }

    [Fact]
    public async Task RobotsTxt_References_Sitemap_With_Absolute_Url()
    {
        var response = await _client.GetAsync("/robots.txt");

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sitemap: http://localhost/sitemap.xml", content);
    }

    [Fact]
    public async Task Sitemap_Returns_Homepage_Url()
    {
        var response = await _client.GetAsync("/sitemap.xml");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("application/xml", response.Content.Headers.ContentType?.ToString());

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">", content);
        Assert.Contains("<loc>http://localhost/</loc>", content);
    }

    [Fact]
    public async Task HomePage_Is_Indexable()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("X-Robots-Tag"));

        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("noindex", content);
    }

    [Fact]
    public async Task HomePage_Contains_Meta_Description_Canonical_And_StructuredData()
    {
        var response = await _client.GetAsync("/");

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("<meta name=\"description\"", content);
        Assert.Contains("<link rel=\"canonical\" href=\"http://localhost/\"", content);
        Assert.Contains("<meta property=\"og:title\"", content);
        Assert.Contains("<meta property=\"og:url\"", content);
        Assert.Contains("application/ld+json", content);
        Assert.Contains("https://schema.org", content);
    }

    [Fact]
    public async Task Admin_Api_And_Calendar_Responses_Have_Noindex_Header()
    {
        foreach (var path in new[] { "/admin/refresh-menus", "/api/menu-types", "/calendar/det-velkendte.ics" })
        {
            var response = await _client.GetAsync(path);

            Assert.True(response.Headers.TryGetValues("X-Robots-Tag", out var values),
                $"Expected X-Robots-Tag header on {path}");
            Assert.Contains("noindex", values.Single());
        }
    }

    [Fact]
    public async Task HomePage_ServerRenders_WeeklyPreview_Content()
    {
        // Populate menu data first by requesting a calendar feed
        await _client.GetAsync("/calendar/det-velkendte.ics");

        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // The week-ahead preview must be in the server-rendered HTML, not only built by JavaScript
        Assert.Contains("menu-row-today", content);
        Assert.Contains("tag-today", content);
        Assert.Contains("class=\"menu-dish\"", content);
        Assert.Contains("Weekend &mdash; the kitchen rests", content);
    }
}
