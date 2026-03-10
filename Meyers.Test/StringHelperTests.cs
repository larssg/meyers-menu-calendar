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

    [Fact]
    public void FormatMenuItemsGrouped_GroupsRepeatedCategories()
    {
        var items = new List<string>
        {
            "Varm ret med tilbehør: Alm.: Quiche Lorraine",
            "Varm ret med tilbehør: Halal: Quiche med kylling",
            "Varm ret med tilbehør: Vegetarisk: Tærte med squash",
            "Delikatesser: Sennepsstegt kyllingebryst",
            "Delikatesser: Kalvesteg med bearnaisecreme",
            "Dagens salater: Stegte persillerødder",
            "Dagens salater: Kålsalat med broccoli",
            "Brød: Økologisk rugbrød",
            "Brød: Sødmælksfranskbrød"
        };

        var result = StringHelper.FormatMenuItemsGrouped(items);

        // Each category header should appear exactly once
        Assert.Equal(1, result.Split("Varm ret med tilbehør").Length - 1);
        Assert.Equal(1, result.Split("Delikatesser").Length - 1);
        Assert.Equal(1, result.Split("Dagens salater").Length - 1);
        Assert.Equal(1, result.Split("Brød").Length - 1);

        // Items should be present
        Assert.Contains("Alm.: Quiche Lorraine", result);
        Assert.Contains("Halal: Quiche med kylling", result);
        Assert.Contains("Sennepsstegt kyllingebryst", result);
        Assert.Contains("Kålsalat med broccoli", result);
    }

    [Fact]
    public void FormatMenuItemsGrouped_SingleItemCategory_StaysOnSameLine()
    {
        var items = new List<string>
        {
            "Delikatesser: Kyllingebryst",
            "Brød: Rugbrød"
        };

        var result = StringHelper.FormatMenuItemsGrouped(items);

        Assert.Equal("Delikatesser: Kyllingebryst\n\nBrød: Rugbrød", result);
    }

    [Fact]
    public void FormatMenuItemsGrouped_MultipleItemCategory_ListsUnderHeader()
    {
        var items = new List<string>
        {
            "Brød: Rugbrød",
            "Brød: Franskbrød"
        };

        var result = StringHelper.FormatMenuItemsGrouped(items);

        Assert.Equal("Brød:\nRugbrød\nFranskbrød", result);
    }

    [Fact]
    public void FormatMenuItemsGrouped_PreservesOriginalCategoryOrder()
    {
        var items = new List<string>
        {
            "Varm ret: Suppe",
            "Delikatesser: Ost",
            "Brød: Rugbrød"
        };

        var result = StringHelper.FormatMenuItemsGrouped(items);

        var warmIndex = result.IndexOf("Varm ret");
        var deliIndex = result.IndexOf("Delikatesser");
        var breadIndex = result.IndexOf("Brød");

        Assert.True(warmIndex < deliIndex);
        Assert.True(deliIndex < breadIndex);
    }

    [Fact]
    public void FormatMenuItemsGrouped_DecodesHtmlEntities()
    {
        var items = new List<string>
        {
            "Varm ret med tilbeh&#248;r: Suppe &amp; brød"
        };

        var result = StringHelper.FormatMenuItemsGrouped(items);

        Assert.Equal("Varm ret med tilbehør: Suppe & brød", result);
    }

    [Fact]
    public void FormatMenuItemsGrouped_EmptyList_ReturnsEmpty()
    {
        var result = StringHelper.FormatMenuItemsGrouped([]);

        Assert.Equal("", result);
    }
}