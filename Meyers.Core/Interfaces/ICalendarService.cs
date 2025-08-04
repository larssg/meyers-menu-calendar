using Meyers.Core.Models;

namespace Meyers.Core.Interfaces;

public interface ICalendarService
{
    string GenerateCalendar(List<MenuDay> menuDays, string? menuTypeName = null, bool includeAlarms = false);
    string CleanupTitle(string title);
}