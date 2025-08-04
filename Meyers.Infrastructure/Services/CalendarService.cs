using System.Net;
using System.Text.RegularExpressions;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Meyers.Core.Interfaces;
using Meyers.Core.Models;

namespace Meyers.Infrastructure.Services;

public class CalendarService : ICalendarService
{
    public string GenerateCalendar(List<MenuDay> menuDays, string? menuTypeName = null, bool includeAlarms = false)
    {
        var calendarName = string.IsNullOrEmpty(menuTypeName)
            ? "Meyers Menu Calendar"
            : $"Meyers Menu Calendar - {menuTypeName}";
        var calendar = new Calendar
        {
            ProductId = calendarName,
            Version = "2.0"
        };

        // Add timezone information for Copenhagen
        var copenhagenTz = new VTimeZone("Europe/Copenhagen");
        calendar.TimeZones.Add(copenhagenTz);

        // If no menu days found, create a simple test event
        if (menuDays.Count == 0)
        {
            var testEvent = new CalendarEvent
            {
                Uid = "test-event",
                Summary = "No menu found - Test Event",
                Description = "Unable to scrape menu from Meyers website",
                Start = new CalDateTime(DateTime.SpecifyKind(DateTime.Today.AddHours(12), DateTimeKind.Unspecified),
                    "Europe/Copenhagen"),
                End = new CalDateTime(DateTime.SpecifyKind(DateTime.Today.AddHours(13), DateTimeKind.Unspecified),
                    "Europe/Copenhagen")
            };

            // Add 5-minute alarm if requested
            if (includeAlarms)
            {
                var alarm = new Alarm
                {
                    Action = AlarmAction.Display,
                    Description = testEvent.Summary,
                    Trigger = new Trigger(Duration.FromMinutes(-5))
                };
                testEvent.Alarms.Add(alarm);
            }

            calendar.Events.Add(testEvent);
        }
        else
        {
            foreach (var menuDay in menuDays)
            {
                var date = DateTime.SpecifyKind(menuDay.Date, DateTimeKind.Unspecified);

                // Create dates with Copenhagen timezone
                var startTime = new CalDateTime(date.AddHours(12), "Europe/Copenhagen");
                var endTime = new CalDateTime(date.AddHours(13), "Europe/Copenhagen");

                // Use new MainDish and Details if available, otherwise fall back to MenuItems
                string title, description;

                if (!string.IsNullOrEmpty(menuDay.MainDish))
                {
                    title = CleanupTitle(menuDay.MainDish);
                    description = !string.IsNullOrEmpty(menuDay.Details)
                        ? FormatDescription(menuDay.Details)
                        : FormatDescription(string.Join(", ", menuDay.MenuItems));
                }
                else
                {
                    // Fallback to old format
                    title = $"Meyers Menu - {menuDay.DayName}";
                    description = FormatDescription(string.Join(", ", menuDay.MenuItems));
                }

                // Include menu type in UID to avoid conflicts when multiple menu types exist
                var uid = string.IsNullOrEmpty(menuDay.MenuType)
                    ? $"meyers-menu-{date:yyyy-MM-dd}"
                    : $"meyers-menu-{date:yyyy-MM-dd}-{menuDay.MenuType.Replace(" ", "-").Replace("/", "-").ToLowerInvariant()}";

                var calendarEvent = new CalendarEvent
                {
                    Uid = uid,
                    Summary = title,
                    Description = description,
                    Start = startTime,
                    End = endTime
                };

                // Add 5-minute alarm if requested
                if (includeAlarms)
                {
                    var alarm = new Alarm
                    {
                        Action = AlarmAction.Display,
                        Description = title,
                        Trigger = new Trigger(Duration.FromMinutes(-5))
                    };
                    calendarEvent.Alarms.Add(alarm);
                }

                calendar.Events.Add(calendarEvent);
            }
        }

        var serializer = new CalendarSerializer();
        return serializer.SerializeToString(calendar) ?? string.Empty;
    }

    public string CleanupTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
            return title;

        // Decode HTML entities first
        var cleanTitle = WebUtility.HtmlDecode(title);

        // Split into sections and get the main dish section
        var sections = cleanTitle.Split([", Delikatesser:", ", Dagens salater:", ", Brød:"],
            StringSplitOptions.RemoveEmptyEntries);
        var mainSection = sections[0]; // Take only the first section (main dish)

        // Remove common boilerplate prefixes (case-insensitive)
        // Only remove if there's actual content after the prefix
        var prefixesToRemove = new[]
        {
            "Varm ret med tilbehør:",
            "Varm ret med tilbeh&#248;r:", // HTML encoded version
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
            mainSection = mainSection.Substring(1).Trim();

        // If the result is too long, take only the first sentence or reasonable portion
        const int maxTitleLength = 80;
        if (mainSection.Length > maxTitleLength)
        {
            var firstSentence = mainSection.Split('.')[0];
            if (firstSentence.Length > 20 && firstSentence.Length < mainSection.Length)
            {
                // Only add "..." if we're actually cutting off content after the first sentence
                var remainingAfterSentence = mainSection.Substring(firstSentence.Length).Trim();
                if (remainingAfterSentence.Length > 1) // More than just the period
                    mainSection = firstSentence.Trim() + "...";
                else
                    mainSection = firstSentence.Trim();
            }
            else
            {
                // Find a good breaking point (space) around 60-80 characters
                var breakPoint = mainSection.LastIndexOf(' ', Math.Min(maxTitleLength, mainSection.Length - 1));
                if (breakPoint > 40)
                    mainSection = mainSection.Substring(0, breakPoint).Trim() + "...";
                else
                    // Fallback: hard truncate at maxTitleLength
                    mainSection = mainSection.Substring(0, maxTitleLength).Trim() + "...";
            }
        }

        return mainSection;
    }

    private static string FormatDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
            return description;

        // Decode HTML entities first
        var formatted = WebUtility.HtmlDecode(description);

        // Add line breaks before section headers for better readability
        // Use actual newlines - the iCal library will handle proper encoding
        formatted = formatted.Replace(", Delikatesser:", "\n\nDelikatesser:")
            .Replace(", Dagens salater:", "\n\nDagens salater:")
            .Replace(", Brød:", "\n\nBrød:")
            .Replace(" | ", "\n");

        // Break up long lines by adding line breaks after sentences
        formatted = Regex.Replace(formatted, @"(\. )([A-ZÆØÅ])", "$1\n$2");

        // Clean up any multiple spaces and normalize whitespace
        formatted = Regex.Replace(formatted, @"[ ]+", " ");

        // Clean up any extra line breaks at the start
        formatted = formatted.TrimStart('\n', ' ');

        return formatted;
    }
}