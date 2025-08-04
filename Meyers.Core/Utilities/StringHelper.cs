using System.Net;
using System.Text.RegularExpressions;

namespace Meyers.Core.Utilities;

public static partial class StringHelper
{
    public static string CapitalizeFirst(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpper(input[0]) + input[1..].ToLower();
    }

    public static string ExtractMainDishFromFirstItem(string firstItem)
    {
        var colonIndex = firstItem.IndexOf(':');
        if (colonIndex <= 0 || colonIndex >= firstItem.Length - 1) return firstItem;

        var content = firstItem.Substring(colonIndex + 1).Trim();
        if (content.Length > 100) content = content.Substring(0, 100).Trim() + "...";

        return content;
    }

    public static string FormatDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
            return description;

        // Decode HTML entities first
        var formatted = WebUtility.HtmlDecode(description);

        // Add line breaks before section headers for better readability
        // Use actual newlines - the iCal library will handle proper encoding
        formatted = formatted.Replace(", Delikatesser:", "\n\nDelikatesser:")
            .Replace(", Dagens salater:", "\n\nDagens salater:")
            .Replace(", Brød:", "\n\nBrød:")
            .Replace(" | ", "\n");

        // Break up long lines by adding line breaks after sentences
        formatted = SentenceBreakRegex().Replace(formatted, "$1\n$2");

        // Clean up any multiple spaces and normalize whitespace
        formatted = MultipleSpacesRegex().Replace(formatted, " ");

        // Clean up any extra line breaks at the start
        formatted = formatted.TrimStart('\n', ' ');

        return formatted;
    }

    [GeneratedRegex(@"(\. )([A-ZÆØÅ])")]
    private static partial Regex SentenceBreakRegex();

    [GeneratedRegex(@"[ ]+")]
    private static partial Regex MultipleSpacesRegex();
}