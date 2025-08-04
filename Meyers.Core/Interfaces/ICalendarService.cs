using Meyers.Core.Models;

namespace Meyers.Core.Interfaces;

public interface ICalendarService
{
    string GenerateCalendar(List<MenuDay> menuDays, string? menuTypeName = null);
    string CleanupTitle(string title);
}