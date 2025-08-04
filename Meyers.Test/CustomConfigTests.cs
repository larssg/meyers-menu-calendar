using System.Reflection;
using Meyers.Web.Handlers;

namespace Meyers.Test;

public class CustomConfigTests
{
    [Theory]
    [InlineData("M1T2W3R4F5", 5)] // All single digits
    [InlineData("M10T20W30R40F50", 5)] // All double digits
    [InlineData("M1T10W3R25F7", 5)] // Mixed single and multi-digit
    [InlineData("M15", 1)] // Single day with multi-digit ID
    [InlineData("F999", 1)] // Large ID number
    public void DecodeCustomConfig_ValidFormats_ReturnsCorrectDictionary(string config, int expectedCount)
    {
        var result = CallDecodeCustomConfig(config);

        Assert.NotNull(result);
        Assert.Equal(expectedCount, result.Count);
    }

    [Theory]
    [InlineData("M1T2W3R4F5", DayOfWeek.Monday, 1)]
    [InlineData("M1T2W3R4F5", DayOfWeek.Tuesday, 2)]
    [InlineData("M1T2W3R4F5", DayOfWeek.Wednesday, 3)]
    [InlineData("M1T2W3R4F5", DayOfWeek.Thursday, 4)]
    [InlineData("M1T2W3R4F5", DayOfWeek.Friday, 5)]
    public void DecodeCustomConfig_SingleDigitIds_MapsCorrectly(string config, DayOfWeek day, int expectedId)
    {
        var result = CallDecodeCustomConfig(config);

        Assert.NotNull(result);
        Assert.True(result.ContainsKey(day));
        Assert.Equal(expectedId, result[day]);
    }

    [Theory]
    [InlineData("M10T20W30R40F50", DayOfWeek.Monday, 10)]
    [InlineData("M10T20W30R40F50", DayOfWeek.Tuesday, 20)]
    [InlineData("M10T20W30R40F50", DayOfWeek.Wednesday, 30)]
    [InlineData("M10T20W30R40F50", DayOfWeek.Thursday, 40)]
    [InlineData("M10T20W30R40F50", DayOfWeek.Friday, 50)]
    public void DecodeCustomConfig_MultiDigitIds_MapsCorrectly(string config, DayOfWeek day, int expectedId)
    {
        var result = CallDecodeCustomConfig(config);

        Assert.NotNull(result);
        Assert.True(result.ContainsKey(day));
        Assert.Equal(expectedId, result[day]);
    }

    [Theory]
    [InlineData("M1T10W3R25F7", DayOfWeek.Monday, 1)]
    [InlineData("M1T10W3R25F7", DayOfWeek.Tuesday, 10)]
    [InlineData("M1T10W3R25F7", DayOfWeek.Wednesday, 3)]
    [InlineData("M1T10W3R25F7", DayOfWeek.Thursday, 25)]
    [InlineData("M1T10W3R25F7", DayOfWeek.Friday, 7)]
    public void DecodeCustomConfig_MixedDigitIds_MapsCorrectly(string config, DayOfWeek day, int expectedId)
    {
        var result = CallDecodeCustomConfig(config);

        Assert.NotNull(result);
        Assert.True(result.ContainsKey(day));
        Assert.Equal(expectedId, result[day]);
    }

    [Theory]
    [InlineData("M999", DayOfWeek.Monday, 999)]
    [InlineData("F123456", DayOfWeek.Friday, 123456)]
    public void DecodeCustomConfig_LargeIds_HandlesCorrectly(string config, DayOfWeek day, int expectedId)
    {
        var result = CallDecodeCustomConfig(config);

        Assert.NotNull(result);
        Assert.True(result.ContainsKey(day));
        Assert.Equal(expectedId, result[day]);
    }

    [Theory]
    [InlineData("MW3")] // Missing number after M
    [InlineData("M")] // Day letter only
    [InlineData("1M")] // Number before day letter
    [InlineData("Ma")] // Letter instead of number
    [InlineData("")] // Empty string
    [InlineData("X1")] // Invalid day letter
    public void DecodeCustomConfig_InvalidFormats_ReturnsNull(string config)
    {
        var result = CallDecodeCustomConfig(config);

        Assert.Null(result);
    }

    [Fact]
    public void EncodeCustomConfig_SingleDigitIds_CreatesCorrectFormat()
    {
        var config = new Dictionary<DayOfWeek, int>
        {
            { DayOfWeek.Monday, 1 },
            { DayOfWeek.Tuesday, 2 },
            { DayOfWeek.Wednesday, 3 },
            { DayOfWeek.Thursday, 4 },
            { DayOfWeek.Friday, 5 }
        };

        var result = CalendarEndpointHandler.EncodeCustomConfig(config);

        Assert.Equal("M1T2W3R4F5", result);
    }

    [Fact]
    public void EncodeCustomConfig_MultiDigitIds_CreatesCorrectFormat()
    {
        var config = new Dictionary<DayOfWeek, int>
        {
            { DayOfWeek.Monday, 10 },
            { DayOfWeek.Tuesday, 20 },
            { DayOfWeek.Wednesday, 30 },
            { DayOfWeek.Thursday, 40 },
            { DayOfWeek.Friday, 50 }
        };

        var result = CalendarEndpointHandler.EncodeCustomConfig(config);

        Assert.Equal("M10T20W30R40F50", result);
    }

    [Fact]
    public void EncodeCustomConfig_MixedDigitIds_CreatesCorrectFormat()
    {
        var config = new Dictionary<DayOfWeek, int>
        {
            { DayOfWeek.Monday, 1 },
            { DayOfWeek.Tuesday, 15 },
            { DayOfWeek.Wednesday, 3 },
            { DayOfWeek.Thursday, 128 },
            { DayOfWeek.Friday, 9 }
        };

        var result = CalendarEndpointHandler.EncodeCustomConfig(config);

        Assert.Equal("M1T15W3R128F9", result);
    }

    [Fact]
    public void EncodeDecodeRoundTrip_SingleDigit_PreservesData()
    {
        var original = new Dictionary<DayOfWeek, int>
        {
            { DayOfWeek.Monday, 1 },
            { DayOfWeek.Wednesday, 3 },
            { DayOfWeek.Friday, 5 }
        };

        var encoded = CalendarEndpointHandler.EncodeCustomConfig(original);
        var decoded = CallDecodeCustomConfig(encoded);

        Assert.NotNull(decoded);
        Assert.Equal(original.Count, decoded.Count);
        foreach (var kvp in original)
        {
            Assert.True(decoded.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, decoded[kvp.Key]);
        }
    }

    [Fact]
    public void EncodeDecodeRoundTrip_MultiDigit_PreservesData()
    {
        var original = new Dictionary<DayOfWeek, int>
        {
            { DayOfWeek.Monday, 15 },
            { DayOfWeek.Tuesday, 128 },
            { DayOfWeek.Thursday, 999 }
        };

        var encoded = CalendarEndpointHandler.EncodeCustomConfig(original);
        var decoded = CallDecodeCustomConfig(encoded);

        Assert.NotNull(decoded);
        Assert.Equal(original.Count, decoded.Count);
        foreach (var kvp in original)
        {
            Assert.True(decoded.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, decoded[kvp.Key]);
        }
    }

    // Helper method to access the private DecodeCustomConfig method
    private static Dictionary<DayOfWeek, int>? CallDecodeCustomConfig(string config)
    {
        var method = typeof(CalendarEndpointHandler).GetMethod("DecodeCustomConfig",
            BindingFlags.NonPublic | BindingFlags.Static);

        return method?.Invoke(null, [config]) as Dictionary<DayOfWeek, int>;
    }
}