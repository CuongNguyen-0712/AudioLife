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
    public DbSet<ListeningHistory> ListeningHistories => Set<ListeningHistory>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AuthUserAccount> AuthUserAccounts => Set<AuthUserAccount>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<PoiChangeRequest> PoiChangeRequests => Set<PoiChangeRequest>();
    public DbSet<PoiAdminLocationAssignment> PoiAdminLocationAssignments => Set<PoiAdminLocationAssignment>();

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

        modelBuilder.Entity<ListeningHistory>(entity =>
        {
            entity.HasOne(lh => lh.AudioGuide)
                .WithMany(ag => ag.ListeningHistories)
                .HasForeignKey(lh => lh.AudioGuideId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(lh => lh.User)
                .WithMany(u => u.ListeningHistories)
                .HasForeignKey(lh => lh.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasOne(f => f.User)
                .WithMany(u => u.Feedbacks)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.Location)
                .WithMany(l => l.Feedbacks)
                .HasForeignKey(f => f.LocationId)
                .OnDelete(DeleteBehavior.SetNull);
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
    }
}

