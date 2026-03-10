namespace Meyers.Core.Models;

public class CalendarDownloadLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string FeedPath { get; set; } = string.Empty; // e.g. "almanak" or "custom/M1T1W1R2F1"
    public string ClientName { get; set; } = string.Empty; // User-Agent or parsed client name
    public string IpHash { get; set; } = string.Empty; // SHA256 hash of IP (first 16 hex chars)
    public bool NotModified { get; set; } // True if 304 was returned (ETag match)
}
