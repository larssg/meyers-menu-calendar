using Meyers.Core.Utilities;

namespace Meyers.Test;

public class TimeFormatHelperTests
{
    [Fact]
    public void FormatTimeAgo_LessThan60Seconds_ShowsSecondsWithPadding()
    {
        var pastTime = DateTime.Now.AddSeconds(-9);
        var result = TimeFormatHelper.FormatTimeAgo(pastTime);

        Assert.Matches(@"^\d{2} seconds ago$", result);
    }

    [Fact]
    public void FormatTimeAgo_LessThan60Minutes_ShowsMinutesAndSecondsWithPadding()
    {
        var pastTime = DateTime.Now.AddMinutes(-5).AddSeconds(-9);
        var result = TimeFormatHelper.FormatTimeAgo(pastTime);

        Assert.Matches(@"^\d{2}m \d{2}s ago$", result);
    }

    [Fact]
    public void FormatTimeAgo_LessThan24Hours_ShowsHoursMinutesSecondsWithPadding()
    {
        var pastTime = DateTime.Now.AddHours(-2).AddMinutes(-5).AddSeconds(-9);
        var result = TimeFormatHelper.FormatTimeAgo(pastTime);

        Assert.Matches(@"^\d{2}h \d{2}m \d{2}s ago$", result);
    }

    [Fact]
    public void FormatTimeAgo_LessThan7Days_ShowsDaysHoursMinutesSecondsWithPadding()
    {
        var pastTime = DateTime.Now.AddDays(-3).AddHours(-2).AddMinutes(-5).AddSeconds(-9);
        var result = TimeFormatHelper.FormatTimeAgo(pastTime);

        Assert.Matches(@"^\d+d \d{2}h \d{2}m \d{2}s ago$", result);
    }

    [Fact]
    public void FormatTimeAgo_MoreThan7Days_ShowsFullDate()
    {
        var pastTime = new DateTime(2024, 1, 15, 14, 30, 45);
        var result = TimeFormatHelper.FormatTimeAgo(pastTime);

        // Check that it contains the expected date parts (locale-independent)
        Assert.Contains("Jan 15, 2024", result);
        Assert.Contains("at", result);
        Assert.Contains("2", result); // hour
        Assert.Contains("30", result); // minute
        Assert.Contains("45", result); // second
        Assert.Contains("PM", result);
    }
}