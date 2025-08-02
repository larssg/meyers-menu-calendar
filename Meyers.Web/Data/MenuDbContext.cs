using Microsoft.EntityFrameworkCore;
using Meyers.Web.Models;

namespace Meyers.Web.Data;

public class MenuDbContext : DbContext
{
    public MenuDbContext(DbContextOptions<MenuDbContext> options) : base(options)
    {
    }
    
    public DbSet<MenuEntry> MenuEntries { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MenuEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Date);
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.DayName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.MenuItems).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });
    }
}