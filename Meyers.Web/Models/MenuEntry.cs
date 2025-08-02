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
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}