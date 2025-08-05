using System.ComponentModel.DataAnnotations;
using Meyers.Core.Utilities;

namespace Meyers.Core.Models;

public class MenuType
{
    public int Id { get; set; }

    [Required] [MaxLength(100)] public string Name { get; set; } = string.Empty;

    [Required] [MaxLength(100)] public string Slug { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<MenuEntry> MenuEntries { get; set; } = new List<MenuEntry>();

    /// <summary>
    ///     Generates a URL-friendly slug from the menu type name.
    ///     Handles Danish characters: ø→oe, å→aa, æ→ae
    /// </summary>
    public static string GenerateSlug(string name)
    {
        return SlugHelper.GenerateSlug(name);
    }
}