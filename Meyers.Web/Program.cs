using Microsoft.EntityFrameworkCore;
using Meyers.Web.Data;
using Meyers.Web.Repositories;
using Meyers.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Database configuration
builder.Services.AddDbContext<MenuDbContext>(options =>
    options.UseSqlite("Data Source=menus.db"));

// Repository registration
builder.Services.AddScoped<IMenuRepository, MenuRepository>();

// Service registration
builder.Services.AddHttpClient<MenuScrapingService>();
builder.Services.AddScoped<CalendarService>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MenuDbContext>();
    context.Database.EnsureCreated();
}

app.MapGet("/", () => "Meyers Menu Calendar API");

app.MapGet("/calendar", async (MenuScrapingService menuService, CalendarService calendarService) =>
{
    try
    {
        var menuDays = await menuService.ScrapeMenuAsync();
        var icalContent = calendarService.GenerateCalendar(menuDays);
        
        return Results.Text(icalContent, "text/calendar; charset=utf-8");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error generating calendar: {ex.Message}");
    }
});

app.Run();

public partial class Program { }
