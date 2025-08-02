using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace Meyers.Web.Services;

public class CalendarService
{
    public string GenerateCalendar(List<MenuDay> menuDays)
    {
        var calendar = new Calendar();
        calendar.ProductId = "Meyers Menu Calendar";
        calendar.Version = "2.0";
        
        // If no menu days found, create a simple test event
        if (!menuDays.Any())
        {
            var testEvent = new CalendarEvent
            {
                Uid = "test-event",
                Summary = "No menu found - Test Event",
                Description = "Unable to scrape menu from Meyers website",
                Start = new CalDateTime(DateTime.SpecifyKind(DateTime.Today.AddHours(12), DateTimeKind.Unspecified)),
                End = new CalDateTime(DateTime.SpecifyKind(DateTime.Today.AddHours(13), DateTimeKind.Unspecified))
            };
            calendar.Events.Add(testEvent);
        }
        else
        {
            foreach (var menuDay in menuDays)
            {
                var date = DateTime.SpecifyKind(menuDay.Date, DateTimeKind.Unspecified);
                
                // Use new MainDish and Details if available, otherwise fall back to MenuItems
                string title, description;
                
                if (!string.IsNullOrEmpty(menuDay.MainDish))
                {
                    title = $"Meyers Menu - {menuDay.DayName}: {menuDay.MainDish}";
                    description = !string.IsNullOrEmpty(menuDay.Details) ? menuDay.Details : string.Join(", ", menuDay.MenuItems);
                }
                else
                {
                    // Fallback to old format
                    title = $"Meyers Menu - {menuDay.DayName}";
                    description = string.Join(", ", menuDay.MenuItems);
                }
                
                var calendarEvent = new CalendarEvent
                {
                    Uid = $"meyers-menu-{date:yyyy-MM-dd}",
                    Summary = title,
                    Description = description,
                    Start = new CalDateTime(date.AddHours(12)),
                    End = new CalDateTime(date.AddHours(13))
                };
                
                calendar.Events.Add(calendarEvent);
            }
        }
        
        var serializer = new CalendarSerializer();
        return serializer.SerializeToString(calendar) ?? string.Empty;
    }
}