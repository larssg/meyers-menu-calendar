namespace Meyers.Core.Models;

public class DownloadSubscriberSummary
{
    public string IpHash { get; set; } = "";
    public string Client { get; set; } = "";
    public string Feeds { get; set; } = "";
    public int Count { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
}
