using Meyers.Core.Utilities;

namespace Meyers.Test;

public class StringHelperTests
{
    [Fact]
    public void CapitalizeFirst_EmptyString_ReturnsEmpty()
    {
        var result = StringHelper.CapitalizeFirst("");
        Assert.Equal("", result);
    }

    [Fact]
    public void CapitalizeFirst_NullString_ReturnsNull()
    {
        var result = StringHelper.CapitalizeFirst(null!);
        Assert.Null(result);
    }

    [Fact]
    public void CapitalizeFirst_SingleChar_CapitalizesCorrectly()
    {
        var result = StringHelper.CapitalizeFirst("a");
        Assert.Equal("A", result);
    }

    [Fact]
    public void CapitalizeFirst_MultipleWords_CapitalizesFirstLetterOnly()
    {
        var result = StringHelper.CapitalizeFirst("mandag tirsdag");
        Assert.Equal("Mandag tirsdag", result);
    }

    [Fact]
    public void ExtractMainDishFromFirstItem_NoColon_ReturnsOriginal()
    {
        var result = StringHelper.ExtractMainDishFromFirstItem("No colon here");
        Assert.Equal("No colon here", result);
    }

    [Fact]
    public void ExtractMainDishFromFirstItem_WithColon_ExtractsAfterColon()
    {
        var result = StringHelper.ExtractMainDishFromFirstItem("Label: Main dish content");
        Assert.Equal("Main dish content", result);
    }

    [Fact]
    public void ExtractMainDishFromFirstItem_LongContent_TruncatesWithEllipsis()
    {
        var longContent = "Label: " + new string('a', 150);
        var result = StringHelper.ExtractMainDishFromFirstItem(longContent);
        
        Assert.EndsWith("...", result);
        Assert.True(result.Length <= 104); // 100 chars + "..."
    }

    [Fact]
    public void FormatDescription_EmptyString_ReturnsEmpty()
    {
        var result = StringHelper.FormatDescription("");
        Assert.Equal("", result);
    }

    [Fact]
    public void FormatDescription_WithSectionHeaders_AddsLineBreaks()
    {
        var input = "Soup, Delikatesser: Cheese, Dagens salater: Green salad";
        var result = StringHelper.FormatDescription(input);
        
        Assert.Contains("\n\nDelikatesser:", result);
        Assert.Contains("\n\nDagens salater:", result);
    }

    [Fact]
    public void FormatDescription_WithHtmlEntities_DecodesCorrectly()
    {
        var input = "Soup &amp; bread";
        var result = StringHelper.FormatDescription(input);
        
        Assert.Equal("Soup & bread", result);
    }
}