using Microsoft.EntityFrameworkCore;
using Meyers.Web;
using Meyers.Web.Configuration;
using Meyers.Web.Data;
using Meyers.Web.Handlers;
using Meyers.Web.Repositories;
using Meyers.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<MenuCacheOptions>(
    builder.Configuration.GetSection(MenuCacheOptions.SectionName));

// Database configuration
var databasePath = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DATABASE_PATH")
    ?? "Data Source=menus.db";

builder.Services.AddDbContext<MenuDbContext>(options =>
    options.UseSqlite(databasePath));

// Repository registration
builder.Services.AddScoped<IMenuRepository, MenuRepository>();

// Service registration
builder.Services.AddHttpClient<MenuScrapingService>();
builder.Services.AddScoped<CalendarService>();

// Handler registration
builder.Services.AddScoped<CalendarEndpointHandler>();
builder.Services.AddScoped<MenuPreviewHandler>();

// Blazor SSR services
builder.Services.AddRazorComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery();

// Background service registration
builder.Services.AddHostedService<MenuCacheBackgroundService>();

var app = builder.Build();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MenuDbContext>();
    context.Database.Migrate();
}

// Configure static files
app.UseStaticFiles();

// Add headers to prevent indexing (except for social media preview crawlers)
app.Use(async (context, next) =>
{
    var userAgent = context.Request.Headers["User-Agent"].ToString().ToLowerInvariant();
    var isSocialMediaCrawler = userAgent.Contains("facebookexternalhit") ||
                              userAgent.Contains("twitterbot") ||
                              userAgent.Contains("linkedinbot") ||
                              userAgent.Contains("whatsapp") ||
                              userAgent.Contains("telegrambot") ||
                              userAgent.Contains("skypeuripreview") ||
                              userAgent.Contains("slackbot") ||
                              userAgent.Contains("discordbot");

    if (!isSocialMediaCrawler)
    {
        context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive, nosnippet, noimageindex, notranslate, nocache";
    }

    await next();
});

// Configure routing
app.UseRouting();

// Add anti-forgery middleware for Blazor SSR
app.UseAntiforgery();

// Map Blazor components
app.MapRazorComponents<App>();

// Map calendar endpoints
app.MapGet("/calendar/{menuTypeSlug}.ics", async (string menuTypeSlug, CalendarEndpointHandler handler) => 
    await handler.GetCalendarAsync(menuTypeSlug));

// API endpoint for available menus
app.MapGet("/api/menu-types", async (IMenuRepository menuRepository) =>
{
    var menuTypes = await menuRepository.GetMenuTypesAsync();
    return Results.Ok(menuTypes.Select(mt => new { mt.Id, mt.Name, mt.Slug }).ToList());
});

// API endpoint for menu preview
app.MapGet("/api/menu-preview/{menuTypeId:int}", async (int menuTypeId, MenuPreviewHandler handler) =>
    await handler.GetMenuPreviewAsync(menuTypeId));

// Hidden endpoint for manual menu refresh (for development/troubleshooting)
app.MapGet("/admin/refresh-menus", async (HttpContext context, MenuScrapingService scrapingService) =>
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
        var result = await scrapingService.ScrapeMenuAsync();
        var count = result.Count;
        return Results.Ok(new { 
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
});

app.Run();

public partial class Program { }
