namespace Meyers.Core.Utilities;

public static class TimeFormatHelper
{
    public static string FormatTimeAgo(DateTime pastTime)
    {
        var timeAgo = DateTime.Now - pastTime;

        if (timeAgo.TotalSeconds < 60) return $"{(int)timeAgo.TotalSeconds:00} seconds ago";

        if (timeAgo.TotalMinutes < 60)
        {
            var minutes = (int)timeAgo.TotalMinutes;
            var seconds = (int)(timeAgo.TotalSeconds % 60);
            return $"{minutes:00}m {seconds:00}s ago";
        }

        if (timeAgo.TotalHours < 24)
        {
            var hours = (int)timeAgo.TotalHours;
            var minutes = (int)(timeAgo.TotalMinutes % 60);
            var seconds = (int)(timeAgo.TotalSeconds % 60);
            return $"{hours:00}h {minutes:00}m {seconds:00}s ago";
        }

        if (timeAgo.TotalDays < 7)
        {
            var days = (int)timeAgo.TotalDays;
            var hours = (int)(timeAgo.TotalHours % 24);
            var minutes = (int)(timeAgo.TotalMinutes % 60);
            var seconds = (int)(timeAgo.TotalSeconds % 60);
            return $"{days}d {hours:00}h {minutes:00}m {seconds:00}s ago";
        }

        return pastTime.ToString("MMM d, yyyy 'at' h:mm:ss tt");
    }
}