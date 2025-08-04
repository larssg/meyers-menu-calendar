using System.Text.RegularExpressions;

namespace Meyers.Core.Utilities;

public static partial class SlugHelper
{
    public static string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Convert to lowercase and handle Danish characters
        var slug = name.ToLowerInvariant()
            .Replace("ø", "oe")
            .Replace("å", "aa")
            .Replace("æ", "ae")
            .Replace("é", "e")
            .Replace("ü", "u");

        // Replace spaces and special characters with hyphens
        slug = NonAlphanumericRegex().Replace(slug, "-");

        // Remove multiple consecutive hyphens
        slug = MultipleHyphensRegex().Replace(slug, "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        return slug;
    }

    [GeneratedRegex(@"[^a-z0-9\-]")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"-+")]
    private static partial Regex MultipleHyphensRegex();
}