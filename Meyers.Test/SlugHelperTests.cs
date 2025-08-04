using Meyers.Core.Utilities;

namespace Meyers.Test;

public class SlugHelperTests
{
    [Fact]
    public void GenerateSlug_EmptyString_ReturnsEmpty()
    {
        var result = SlugHelper.GenerateSlug("");
        Assert.Equal("", result);
    }

    [Fact]
    public void GenerateSlug_NullString_ReturnsEmpty()
    {
        var result = SlugHelper.GenerateSlug(null!);
        Assert.Equal("", result);
    }

    [Fact]
    public void GenerateSlug_WhitespaceOnly_ReturnsEmpty()
    {
        var result = SlugHelper.GenerateSlug("   ");
        Assert.Equal("", result);
    }

    [Fact]
    public void GenerateSlug_SimpleText_ConvertsToLowercase()
    {
        var result = SlugHelper.GenerateSlug("Simple Text");
        Assert.Equal("simple-text", result);
    }

    [Theory]
    [InlineData("ø", "oe")]
    [InlineData("å", "aa")]
    [InlineData("æ", "ae")]
    [InlineData("é", "e")]
    [InlineData("ü", "u")]
    public void GenerateSlug_DanishCharacters_ConvertsCorrectly(string input, string expected)
    {
        var result = SlugHelper.GenerateSlug(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GenerateSlug_DanishMenuName_ConvertsCorrectly()
    {
        var result = SlugHelper.GenerateSlug("Grønne menü");
        Assert.Equal("groenne-menu", result);
    }

    [Fact]
    public void GenerateSlug_SpecialCharacters_ReplacesWithHyphens()
    {
        var result = SlugHelper.GenerateSlug("Menu & More!");
        Assert.Equal("menu-more", result);
    }

    [Fact]
    public void GenerateSlug_MultipleSpaces_SingleHyphen()
    {
        var result = SlugHelper.GenerateSlug("Multiple    Spaces");
        Assert.Equal("multiple-spaces", result);
    }

    [Fact]
    public void GenerateSlug_MultipleHyphens_SingleHyphen()
    {
        var result = SlugHelper.GenerateSlug("Multiple---Hyphens");
        Assert.Equal("multiple-hyphens", result);
    }

    [Fact]
    public void GenerateSlug_LeadingTrailingHyphens_TrimsCorrectly()
    {
        var result = SlugHelper.GenerateSlug("-Leading and Trailing-");
        Assert.Equal("leading-and-trailing", result);
    }

    [Fact]
    public void GenerateSlug_ComplexExample_HandlesAll()
    {
        var result = SlugHelper.GenerateSlug("Det grønne køkken & café!");
        Assert.Equal("det-groenne-koekken-cafe", result);
    }
}