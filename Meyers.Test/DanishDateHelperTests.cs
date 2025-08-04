using Meyers.Core.Utilities;

namespace Meyers.Test;

public class DanishDateHelperTests
{
    [Theory]
    [InlineData("jan", 1)]
    [InlineData("feb", 2)]
    [InlineData("mar", 3)]
    [InlineData("apr", 4)]
    [InlineData("maj", 5)]
    [InlineData("jun", 6)]
    [InlineData("jul", 7)]
    [InlineData("aug", 8)]
    [InlineData("sep", 9)]
    [InlineData("okt", 10)]
    [InlineData("nov", 11)]
    [InlineData("dec", 12)]
    public void ParseDanishMonth_ValidMonths_ReturnsCorrectNumber(string monthName, int expected)
    {
        var result = DanishDateHelper.ParseDanishMonth(monthName);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("JAN", 1)]
    [InlineData("Feb", 2)]
    [InlineData("MAJ", 5)]
    public void ParseDanishMonth_CaseInsensitive_ReturnsCorrectNumber(string monthName, int expected)
    {
        var result = DanishDateHelper.ParseDanishMonth(monthName);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("january")]
    [InlineData("")]
    [InlineData("13")]
    public void ParseDanishMonth_InvalidMonth_ReturnsZero(string monthName)
    {
        var result = DanishDateHelper.ParseDanishMonth(monthName);
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData("Mandag", true)]
    [InlineData("Tirsdag", true)]
    [InlineData("Onsdag", true)]
    [InlineData("Torsdag", true)]
    [InlineData("Fredag", true)]
    public void IsWeekday_WeekdayNames_ReturnsTrue(string dayName, bool expected)
    {
        var result = DanishDateHelper.IsWeekday(dayName);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Lørdag", false)]
    [InlineData("Søndag", false)]
    [InlineData("Monday", false)]
    [InlineData("", false)]
    [InlineData("Invalid", false)]
    public void IsWeekday_NonWeekdayNames_ReturnsFalse(string dayName, bool expected)
    {
        var result = DanishDateHelper.IsWeekday(dayName);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("mandag", true)]
    [InlineData("TIRSDAG", true)]
    [InlineData("Onsdag", true)]
    public void IsWeekday_CaseInsensitive_ReturnsTrue(string dayName, bool expected)
    {
        var result = DanishDateHelper.IsWeekday(dayName);
        Assert.Equal(expected, result);
    }
}