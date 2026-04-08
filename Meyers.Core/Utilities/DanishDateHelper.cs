namespace Meyers.Core.Utilities;

public static class DanishDateHelper
{
    private static readonly string[] Weekdays = ["Mandag", "Tirsdag", "Onsdag", "Torsdag", "Fredag"];

    public static int ParseDanishMonth(string monthName)
    {
        return monthName.ToLowerInvariant() switch
        {
            "jan" => 1,
            "feb" => 2,
            "mar" => 3,
            "apr" => 4,
            "maj" => 5,
            "jun" => 6,
            "jul" => 7,
            "aug" => 8,
            "sep" => 9,
            "okt" => 10,
            "nov" => 11,
            "dec" => 12,
            _ => 0
        };
    }

    public static bool IsWeekday(string dayName)
    {
        return Weekdays.Contains(dayName, StringComparer.OrdinalIgnoreCase);
    }

    public static string GetDanishWeekday(DateTime date)
    {
        return date.DayOfWeek switch
        {
            DayOfWeek.Monday => "Mandag",
            DayOfWeek.Tuesday => "Tirsdag",
            DayOfWeek.Wednesday => "Onsdag",
            DayOfWeek.Thursday => "Torsdag",
            DayOfWeek.Friday => "Fredag",
            DayOfWeek.Saturday => "Lørdag",
            DayOfWeek.Sunday => "Søndag",
            _ => ""
        };
    }
}