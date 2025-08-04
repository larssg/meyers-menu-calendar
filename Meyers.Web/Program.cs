using Meyers.Core.Interfaces;
using Meyers.Infrastructure.Configuration;
using Meyers.Infrastructure.Data;
using Meyers.Infrastructure.Repositories;
using Meyers.Infrastructure.Services;
using Meyers.Web;
using Meyers.Web.Handlers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to remove Server header
builder.WebHost.ConfigureKestrel(options => { options.AddServerHeader = false; });

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
builder.Services.AddScoped<IMenuScrapingService, MenuScrapingService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<ITimeZoneService, TimeZoneService>();

// Handler registration
builder.Services.AddScoped<CalendarEndpointHandler>();
builder.Services.AddScoped<RefreshMenusHandler>();

// Blazor SSR services
builder.Services.AddRazorComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery();

// Background service registration
builder.Services.AddHostedService<MenuCacheBackgroundService>();

// Add response caching
builder.Services.AddResponseCaching();

// Add response compression (gzip/brotli)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    
    // Compress these MIME types
    options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "text/calendar",
        "application/javascript",
        "text/css"
    });
});

// Configure compression levels
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

var app = builder.Build();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MenuDbContext>();
    context.Database.Migrate();
}

// Configure static files
app.MapStaticAssets();
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
        context.Response.Headers["X-Robots-Tag"] =
            "noindex, nofollow, noarchive, nosnippet, noimageindex, notranslate, nocache";

    await next();
});

// Configure routing
app.UseRouting();

// Use response compression middleware (must be before static files and caching)
app.UseResponseCompression();

// Use response caching middleware
app.UseResponseCaching();

// Add anti-forgery middleware for Blazor SSR
app.UseAntiforgery();

// Map Blazor components
app.MapRazorComponents<App>();

// Map calendar endpoints
app.MapGet("/calendar/{menuTypeSlug}.ics",
    async (string menuTypeSlug, CalendarEndpointHandler handler, HttpContext httpContext) =>
        await handler.GetCalendarAsync(menuTypeSlug, httpContext));

app.MapGet("/calendar/custom/{config}.ics",
    async (string config, CalendarEndpointHandler handler, HttpContext httpContext) =>
        await handler.GetCustomCalendarAsync(config, httpContext));

// API endpoint for available menus
app.MapGet("/api/menu-types", async (IMenuRepository menuRepository) =>
{
    var menuTypes = await menuRepository.GetMenuTypesAsync();
    return Results.Ok(menuTypes.Select(mt => new { mt.Id, mt.Name, mt.Slug }).ToList());
});

// Hidden endpoint for manual menu refresh (for development/troubleshooting)
app.MapGet("/admin/refresh-menus", async (HttpContext context, RefreshMenusHandler handler) =>
    await handler.RefreshMenusAsync(context));

app.Run();

public partial class Program
{
}