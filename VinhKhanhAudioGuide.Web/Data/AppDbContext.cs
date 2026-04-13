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
    public DbSet<AudioScriptSegment> AudioScriptSegments => Set<AudioScriptSegment>();
    public DbSet<AuthUserAccount> AuthUserAccounts => Set<AuthUserAccount>();
    public DbSet<PoiChangeRequest> PoiChangeRequests => Set<PoiChangeRequest>();
    public DbSet<PoiAdminLocationAssignment> PoiAdminLocationAssignments => Set<PoiAdminLocationAssignment>();
    public DbSet<ListeningHistory> ListeningHistories => Set<ListeningHistory>();

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

        modelBuilder.Entity<AudioScriptSegment>(entity =>
        {
            entity.HasOne(s => s.AudioGuide)
                .WithMany(ag => ag.ScriptSegments)
                .HasForeignKey(s => s.AudioGuideId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PoiChangeRequest>(entity =>
        {
            entity.HasIndex(item => item.Status);
            entity.HasIndex(item => item.SubmittedByUsername);
            entity.HasIndex(item => item.LocationId);
            entity.HasIndex(item => item.SubmittedAtUtc);
        });

        modelBuilder.Entity<PoiAdminLocationAssignment>(entity =>
        {
            entity.HasIndex(item => item.Username);
            entity.HasIndex(item => item.LocationId);
            entity.HasIndex(item => new { item.Username, item.LocationId }).IsUnique();
        });

        modelBuilder.Entity<AuthUserAccount>(entity =>
        {
            entity.HasIndex(item => item.Username).IsUnique();
            entity.HasIndex(item => new { item.Role, item.IsActive });
        });

        modelBuilder.Entity<ListeningHistory>(entity =>
        {
            entity.ToTable("ListeningHistory");
            entity.Property(item => item.Progress).HasPrecision(5, 4);

            entity.HasOne(item => item.AudioGuide)
                .WithMany(guide => guide.ListeningHistories)
                .HasForeignKey(item => item.AudioGuideId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(item => item.Location)
                .WithMany(location => location.ListeningHistories)
                .HasForeignKey(item => item.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(item => item.AudioGuideId);
            entity.HasIndex(item => item.LocationId);
            entity.HasIndex(item => item.LastListenedAtUtc);
        });
    }
}

