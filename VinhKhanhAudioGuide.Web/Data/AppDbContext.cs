using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<AudioGuide> AudioGuides => Set<AudioGuide>();
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<TourLocation> TourLocations => Set<TourLocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TourLocation>(entity =>
        {
            entity.HasKey(tl => new { tl.TourId, tl.LocationId });

            entity.HasOne(tl => tl.Tour)
                .WithMany(t => t.TourLocations)
                .HasForeignKey(tl => tl.TourId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tl => tl.Location)
                .WithMany(l => l.TourLocations)
                .HasForeignKey(tl => tl.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasOne(l => l.Category)
                .WithMany(c => c.Locations)
                .HasForeignKey(l => l.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AudioGuide>(entity =>
        {
            entity.HasOne(ag => ag.Location)
                .WithMany(l => l.AudioGuides)
                .HasForeignKey(ag => ag.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
