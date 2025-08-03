using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace Meyers.Web.Services;

public class CalendarService
{
    public string GenerateCalendar(List<MenuDay> menuDays)
    {
        var calendar = new Calendar
        {
            ProductId = "Meyers Menu Calendar",
            Version = "2.0"
        };

        // If no menu days found, create a simple test event
        if (menuDays.Count == 0)
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
                    title = CleanupTitle(menuDay.MainDish);
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

    private static string CleanupTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
            return title;

        // Decode HTML entities first
        var cleanTitle = System.Net.WebUtility.HtmlDecode(title);

        // Split into sections and get the main dish section
        var sections = cleanTitle.Split([", Delikatesser:", ", Dagens salater:", ", Brød:"], StringSplitOptions.RemoveEmptyEntries);
        var mainSection = sections[0]; // Take only the first section (main dish)

        // Remove common boilerplate prefixes (case-insensitive)
        // Only remove if there's actual content after the prefix
        var prefixesToRemove = new[]
        {
            "Varm ret med tilbehør:",
            "Varm ret med tilbeh&#248;r:",  // HTML encoded version
            "Alm./Halal:",
            "Alm.:",
            "Halal:"
        };

        foreach (var prefix in prefixesToRemove)
        {
            if (!mainSection.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;

            // Only remove the prefix if there's content after it
            var remainingContent = mainSection.Substring(prefix.Length).Trim();
            if (!string.IsNullOrEmpty(remainingContent))
            {
                mainSection = remainingContent;
                break; // Only remove the first matching prefix
            }
        }

        // Clean up any remaining artifacts
        mainSection = mainSection.Trim();

        // Remove leading comma or other punctuation that might be left over
        while (mainSection.StartsWith(",") || mainSection.StartsWith(":"))
        {
            mainSection = mainSection.Substring(1).Trim();
        }

        // If the result is too long, take only the first sentence or reasonable portion
        if (mainSection.Length > 80)
        {
            var firstSentence = mainSection.Split('.')[0];
            if (firstSentence.Length > 20 && firstSentence.Length < mainSection.Length)
            {
                mainSection = firstSentence.Trim();
            }
            else if (mainSection.Length > 80)
            {
                // Find a good breaking point (space) around 60-80 characters
                var breakPoint = mainSection.LastIndexOf(' ', Math.Min(80, mainSection.Length - 1));
                if (breakPoint > 40)
                {
                    mainSection = mainSection.Substring(0, breakPoint).Trim() + "...";
                }
            }
        }

        return mainSection;
    }
}
