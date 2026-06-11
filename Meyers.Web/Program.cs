using System.IO.Compression;
using Meyers.Core.Interfaces;
using Meyers.Infrastructure.Configuration;
using Meyers.Infrastructure.Data;
using Meyers.Infrastructure.Repositories;
using Meyers.Infrastructure.Services;
using Meyers.Web;
using Meyers.Web.Handlers;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
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
{
    options.UseSqlite(databasePath);
    // Default to NoTracking for better performance - write operations will explicitly use tracking
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

// Repository registration
builder.Services.AddScoped<IMenuRepository, MenuRepository>();

// Service registration
builder.Services.AddHttpClient<MenuScrapingService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MeyersMenuCalendar/1.0");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.All
    })
    .AddStandardResilienceHandler(options =>
    {
        // Configure retry policy for transient failures
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.Retry.UseJitter = true;

        // Configure circuit breaker
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.MinimumThroughput = 5;

        // Configure total request timeout (includes retries)
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
    });
builder.Services.AddScoped<IMenuScrapingService, MenuScrapingService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<ITimeZoneService, TimeZoneService>();

// Handler registration
builder.Services.AddScoped<CalendarEndpointHandler>();
builder.Services.AddScoped<RefreshMenusHandler>();

// Persist Data Protection keys so ProtectedSessionStorage survives restarts
var keysPath = Environment.GetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH") ?? "/app/keys";
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("MeyersMenuCalendar");

// Blazor SSR services with interactivity
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
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
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();

    // Compress these MIME types
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "text/calendar",
        "application/javascript",
        "text/css"
    });
});

// Configure compression levels
builder.Services.Configure<BrotliCompressionProviderOptions>(options => { options.Level = CompressionLevel.Optimal; });

builder.Services.Configure<GzipCompressionProviderOptions>(options => { options.Level = CompressionLevel.Optimal; });

var app = builder.Build();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MenuDbContext>();
    context.Database.Migrate();
}

// Trust forwarded headers from reverse proxy (X-Forwarded-For, X-Forwarded-Proto)
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Configure static files
app.MapStaticAssets();
app.UseStaticFiles();

// Keep non-content endpoints (admin, API, calendar feeds) out of search indexes
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (path.StartsWithSegments("/admin") ||
        path.StartsWithSegments("/api") ||
        path.StartsWithSegments("/calendar"))
        context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";

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

// Map Blazor components with interactivity
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

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

// SEO endpoints: robots.txt and sitemap.xml are generated dynamically so the
// Sitemap directive and sitemap URLs can use the request's host
app.MapGet("/robots.txt", (HttpContext context) =>
{
    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    var content = $"""
                   User-agent: *
                   Disallow: /admin
                   Disallow: /api
                   Disallow: /calendar

                   # AI training crawlers
                   User-agent: GPTBot
                   Disallow: /

                   User-agent: ChatGPT-User
                   Disallow: /

                   User-agent: CCBot
                   Disallow: /

                   User-agent: Claude-Web
                   Disallow: /

                   User-agent: anthropic-ai
                   Disallow: /

                   User-agent: Google-Extended
                   Disallow: /

                   Sitemap: {baseUrl}/sitemap.xml
                   """;
    return Results.Text(content, "text/plain");
});

app.MapGet("/sitemap.xml", async (HttpContext context, IMenuRepository menuRepository) =>
{
    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    var lastUpdated = await menuRepository.GetLastUpdateTimeAsync();
    var lastMod = lastUpdated.HasValue
        ? $"\n        <lastmod>{lastUpdated.Value:yyyy-MM-dd}</lastmod>"
        : string.Empty;
    var content = $"""
                   <?xml version="1.0" encoding="UTF-8"?>
                   <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
                       <url>
                           <loc>{baseUrl}/</loc>{lastMod}
                           <changefreq>daily</changefreq>
                       </url>
                   </urlset>
                   """;
    return Results.Text(content, "application/xml");
});

// Hidden endpoint for manual menu refresh (for development/troubleshooting)
app.MapGet("/admin/refresh-menus", async (HttpContext context, RefreshMenusHandler handler) =>
    await handler.RefreshMenusAsync(context));

app.Run();

public partial class Program
{
}