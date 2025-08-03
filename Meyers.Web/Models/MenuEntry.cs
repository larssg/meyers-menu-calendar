using System.ComponentModel.DataAnnotations;

namespace Meyers.Web.Models;

public class MenuEntry
{
    public int Id { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string DayName { get; set; } = string.Empty;
    
    [Required]
    public string MenuItems { get; set; } = string.Empty;
    
    public string MainDish { get; set; } = string.Empty;
    
    public string Details { get; set; } = string.Empty;
    
    [Required]
    public int MenuTypeId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public MenuType MenuType { get; set; } = null!;
}