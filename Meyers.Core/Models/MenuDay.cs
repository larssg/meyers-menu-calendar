namespace Meyers.Core.Models;

public class MenuDay
{
    public string DayName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<string> MenuItems { get; set; } = [];
    public string MainDish { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string MenuType { get; set; } = string.Empty;
}