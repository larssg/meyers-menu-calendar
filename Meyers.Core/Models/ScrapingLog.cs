namespace Meyers.Core.Models;

public class ScrapingLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public bool RequestSuccessful { get; set; }
    public bool ParsingSuccessful { get; set; }
    public int NewMenuItemsCount { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
    public string Source { get; set; } = string.Empty; // "Background", "Manual", "API"
}