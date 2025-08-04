using Meyers.Infrastructure.Services;

namespace Meyers.Test;

public class CalendarServiceTests
{
    private readonly CalendarService _calendarService = new();

    [Theory]
    [InlineData("Grillet kyllingebryst med ratatouille", "Grillet kyllingebryst med ratatouille")]
    [InlineData("Grillet kyllingebryst med ratatouille.", "Grillet kyllingebryst med ratatouille.")]
    [InlineData("Pasta med kødsovs", "Pasta med kødsovs")]
    [InlineData("Pasta med kødsovs.", "Pasta med kødsovs.")]
    public void CleanupTitle_ShortTitles_DoesNotAddEllipsis(string input, string expected)
    {
        var result = _calendarService.CleanupTitle(input);

        Assert.Equal(expected, result);
        Assert.DoesNotContain("...", result);
    }

    [Fact]
    public void CleanupTitle_LongTitleWithFirstSentenceAndAdditionalContent_AddsEllipsis()
    {
        // A long title that exceeds 80 characters and has a good first sentence
        var input =
            "Grillet kyllingebryst med ratatouille. Serveres med kartofler og grøntsager og andre ting som gør teksten lang nok til at udløse algoritmen.";

        var result = _calendarService.CleanupTitle(input);

        Assert.Equal("Grillet kyllingebryst med ratatouille...", result);
    }

    [Fact]
    public void CleanupTitle_LongTitleWithoutGoodFirstSentence_TruncatesAtWordBoundary()
    {
        // A long title that exceeds 80 characters but doesn't have a good first sentence
        var input =
            "Dette er en meget lang titel der går langt over de ottenta tegn som er maksimum længde for en titel og indeholder ikke punktum";

        var result = _calendarService.CleanupTitle(input);

        Assert.EndsWith("...", result);
        Assert.True(result.Length <= 83); // 80 + "..."
        Assert.False(result.EndsWith(" ..."), "Should not have space before ellipsis");
    }

    [Fact]
    public void CleanupTitle_LongTitleWithShortFirstSentence_ActualBehavior()
    {
        // Long title where the first sentence is short (under 20 chars)
        // Testing actual behavior: the algorithm seems to use word boundary truncation when first sentence is too short
        var input =
            "Kort. Dette er en meget lang anden sætning der går langt over de ottenta tegn som er maksimum længde for en titel og derfor skal afkortes ved ord grænse";

        var result = _calendarService.CleanupTitle(input);

        // Based on actual testing, when first sentence is short, it uses word boundary truncation
        Assert.EndsWith("...", result);
        Assert.True(result.Length <= 83); // 80 + "..."

        // The algorithm actually does word boundary truncation starting from the full text
        // not just the short first sentence, so the result includes content beyond "Kort."
        Assert.True(result.Contains("Kort. Dette er"), "Should include content beyond the short first sentence");
    }

    [Theory]
    [InlineData("Varm ret med tilbehør: Grillet kylling", "Grillet kylling")]
    [InlineData("Varm ret med tilbeh&#248;r: Pasta bolognese", "Pasta bolognese")]
    [InlineData("Alm./Halal: Fiskefilet med ris", "Fiskefilet med ris")]
    [InlineData("Alm.: Vegetarisk lasagne", "Vegetarisk lasagne")]
    [InlineData("Halal: Kylling tikka masala", "Kylling tikka masala")]
    public void CleanupTitle_RemovesPrefixes_WhenContentFollows(string input, string expected)
    {
        var result = _calendarService.CleanupTitle(input);

        Assert.Equal(expected, result);
        Assert.DoesNotContain("Varm ret med tilbehør:", result);
        Assert.DoesNotContain("Alm./Halal:", result);
        Assert.DoesNotContain("Alm.:", result);
        Assert.DoesNotContain("Halal:", result);
    }

    [Theory]
    [InlineData("Varm ret med tilbehør:", "Varm ret med tilbehør:")]
    [InlineData("Alm./Halal:", "Alm./Halal:")]
    [InlineData("Alm.:", "Alm.:")]
    public void CleanupTitle_KeepsPrefixes_WhenNoContentFollows(string input, string expected)
    {
        var result = _calendarService.CleanupTitle(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Grillet kylling, Delikatesser: Salat, Dagens salater: Coleslaw, Brød: Rugbrød", "Grillet kylling")]
    [InlineData("Pasta bolognese, Delikatesser: Parmesanost", "Pasta bolognese")]
    [InlineData("Fiskefilet, Dagens salater: Kartoffelsalat, Brød: Franskbrød", "Fiskefilet")]
    public void CleanupTitle_ExtractsMainDish_FromSections(string input, string expected)
    {
        var result = _calendarService.CleanupTitle(input);

        Assert.Equal(expected, result);
        Assert.DoesNotContain("Delikatesser:", result);
        Assert.DoesNotContain("Dagens salater:", result);
        Assert.DoesNotContain("Brød:", result);
    }

    [Theory]
    [InlineData("&amp;", "&")]
    [InlineData("&lt;", "<")]
    [InlineData("&gt;", ">")]
    [InlineData("&quot;", "\"")]
    [InlineData("&#248;", "ø")]
    [InlineData("Gr&#248;n salat &amp; dressing", "Grøn salat & dressing")]
    public void CleanupTitle_DecodesHtmlEntities(string input, string expected)
    {
        var result = _calendarService.CleanupTitle(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(", Grillet kylling", "Grillet kylling")]
    [InlineData(": Pasta bolognese", "Pasta bolognese")]
    [InlineData(",,, Fiskefilet", "Fiskefilet")]
    [InlineData(":::: Vegetarret", "Vegetarret")]
    public void CleanupTitle_RemovesLeadingPunctuation(string input, string expected)
    {
        var result = _calendarService.CleanupTitle(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CleanupTitle_HandlesNull()
    {
        var result = _calendarService.CleanupTitle(null);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void CleanupTitle_HandlesEmpty(string input, string expected)
    {
        var result = _calendarService.CleanupTitle(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CleanupTitle_DoesNotAddEllipsisWhenFirstSentenceIsCompleteContent()
    {
        // This tests the fix: if first sentence has no meaningful content after it, don't add "..."
        var input = "Dette er en meget lang første sætning der i sig selv er over ottenta tegn men slutter her.";

        var result = _calendarService.CleanupTitle(input);

        // Since this is over 80 chars and ends with just a period, it should not add "..."
        Assert.DoesNotContain("...", result);
        Assert.Equal("Dette er en meget lang første sætning der i sig selv er over ottenta tegn men slutter her",
            result);
    }

    [Fact]
    public void CleanupTitle_AddsEllipsisWhenActuallyTruncatingContent()
    {
        // This should add "..." because there's substantial content after the first sentence
        var input =
            "Dette er en kort første sætning. Men så kommer der meget mere indhold herefter som gør at vi faktisk afkorter noget meningsfuldt indhold der er vigtigt.";

        var result = _calendarService.CleanupTitle(input);

        Assert.Equal("Dette er en kort første sætning...", result);
    }
}