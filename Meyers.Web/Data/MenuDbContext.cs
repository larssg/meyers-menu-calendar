using Microsoft.EntityFrameworkCore;
using Meyers.Web.Models;

namespace Meyers.Web.Data;

public class MenuDbContext(DbContextOptions<MenuDbContext> options) : DbContext(options)
{
    public DbSet<MenuEntry> MenuEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MenuEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Date).IsUnique();
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.DayName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.MenuItems).IsRequired();
            entity.Property(e => e.MainDish).IsRequired();
            entity.Property(e => e.Details).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });
    }
}
