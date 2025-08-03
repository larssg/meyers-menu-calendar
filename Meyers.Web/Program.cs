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
        context.Response.Headers.Add("X-Robots-Tag", "noindex, nofollow, noarchive, nosnippet, noimageindex, notranslate, nocache");
    }
    
    await next();
});

// Configure routing
app.UseRouting();

// Add anti-forgery middleware for Blazor SSR
app.UseAntiforgery();

// Map Blazor components
app.MapRazorComponents<App>();

// Map both endpoints to the calendar handler
app.MapGet("/calendar", async (CalendarEndpointHandler handler) => await handler.GetCalendarAsync());
app.MapGet("/calendar.ics", async (CalendarEndpointHandler handler) => await handler.GetCalendarAsync());

app.Run();

public partial class Program { }
