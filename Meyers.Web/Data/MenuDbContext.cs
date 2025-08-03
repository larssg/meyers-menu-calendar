using Microsoft.EntityFrameworkCore;
using Meyers.Web.Models;

namespace Meyers.Web.Data;

public class MenuDbContext(DbContextOptions<MenuDbContext> options) : DbContext(options)
{
    public DbSet<MenuEntry> MenuEntries { get; set; }
    public DbSet<MenuType> MenuTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MenuType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<MenuEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Date, e.MenuTypeId }).IsUnique();
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.DayName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.MenuItems).IsRequired();
            entity.Property(e => e.MainDish).IsRequired();
            entity.Property(e => e.Details).IsRequired();
            entity.Property(e => e.MenuTypeId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Foreign key relationship
            entity.HasOne(e => e.MenuType)
                  .WithMany(mt => mt.MenuEntries)
                  .HasForeignKey(e => e.MenuTypeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
