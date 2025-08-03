using Microsoft.EntityFrameworkCore;
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

// Blazor services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();

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

// Configure routing
app.UseRouting();

// Map Blazor hub
app.MapBlazorHub();

// Map fallback to page for Blazor
app.MapFallbackToPage("/_Host");

// Map both endpoints to the calendar handler
app.MapGet("/calendar", async (CalendarEndpointHandler handler) => await handler.GetCalendarAsync());
app.MapGet("/calendar.ics", async (CalendarEndpointHandler handler) => await handler.GetCalendarAsync());

// Map Razor Pages
app.MapRazorPages();

app.Run();

public partial class Program { }
