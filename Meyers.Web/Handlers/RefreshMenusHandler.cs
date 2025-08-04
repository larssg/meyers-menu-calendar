using Meyers.Web.Services;

namespace Meyers.Web.Handlers;

public class RefreshMenusHandler(MenuScrapingService scrapingService)
{
    public async Task<IResult> RefreshMenusAsync(HttpContext context)
    {
        // Check for required secret parameter
        var secret = context.Request.Query["secret"].FirstOrDefault();
        var expectedSecret = Environment.GetEnvironmentVariable("REFRESH_SECRET");

        if (string.IsNullOrEmpty(expectedSecret))
        {
            return Results.Problem("REFRESH_SECRET environment variable not configured", statusCode: 503);
        }

        if (secret != expectedSecret)
        {
            return Results.Problem("Invalid or missing secret parameter", statusCode: 403);
        }

        try
        {
            var result = await scrapingService.ScrapeMenuAsync(forceRefresh: true);
            var count = result.Count;
            return Results.Ok(new
            {
                success = true,
                message = $"Successfully refreshed {count} menu entries",
                timestamp = DateTime.UtcNow,
                menuCount = count
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to refresh menus: {ex.Message}");
        }
    }
}