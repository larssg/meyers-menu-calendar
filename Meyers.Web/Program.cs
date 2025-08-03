using Microsoft.EntityFrameworkCore;
using Meyers.Web.Configuration;
using Meyers.Web.Data;
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

// Background service registration
builder.Services.AddHostedService<MenuCacheBackgroundService>();

var app = builder.Build();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MenuDbContext>();
    context.Database.Migrate();
}

app.MapGet("/", () => "Meyers Menu Calendar API");

// Define the calendar generation logic as a shared delegate
var calendarHandler = async (MenuScrapingService menuService, CalendarService calendarService, IMenuRepository menuRepository) =>
{
    try
    {
        // Get current menu data (this will refresh the cache if needed)
        var currentMenuDays = await menuService.ScrapeMenuAsync();

        // Also get historical data from the last month plus any future items
        var startDate = DateTime.Today.AddMonths(-1);
        var endDate = DateTime.Today.AddMonths(1); // Get up to one month in the future
        var allCachedEntries = await menuRepository.GetMenusForDateRangeAsync(startDate, endDate);

        // Convert cached entries to MenuDay objects
        var historicalMenuDays = allCachedEntries
            .Where(entry => currentMenuDays.All(current => current.Date.Date != entry.Date.Date))
            .Select(entry => new MenuDay
            {
                DayName = entry.DayName,
                Date = entry.Date,
                MenuItems = string.IsNullOrEmpty(entry.MenuItems)
                    ? []
                    : entry.MenuItems.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
                MainDish = entry.MainDish,
                Details = entry.Details
            })
            .ToList();

        // Combine current and historical data
        var allMenuDays = historicalMenuDays.Concat(currentMenuDays)
            .OrderBy(m => m.Date)
            .ToList();

        var icalContent = calendarService.GenerateCalendar(allMenuDays);

        return Results.Text(icalContent, "text/calendar; charset=utf-8");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error generating calendar: {ex.Message}");
    }
};

// Map both endpoints to the same handler
app.MapGet("/calendar", calendarHandler);
app.MapGet("/calendar.ics", calendarHandler);

app.Run();

public partial class Program { }
