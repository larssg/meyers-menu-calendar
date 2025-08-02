using Meyers.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<MenuScrapingService>();
builder.Services.AddScoped<CalendarService>();

var app = builder.Build();

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
