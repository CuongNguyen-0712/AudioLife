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
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AppUserActivityLog> AppUserActivityLogs => Set<AppUserActivityLog>();
    public DbSet<PaymentPackage> PaymentPackages => Set<PaymentPackage>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<UserAppSession> UserAppSessions => Set<UserAppSession>();
    public DbSet<PoiRegistrationRequest> PoiRegistrationRequests => Set<PoiRegistrationRequest>();

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

            entity.HasOne(item => item.User)
                .WithMany(user => user.ListeningHistories)
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.AudioGuide)
                .WithMany(guide => guide.ListeningHistories)
                .HasForeignKey(item => item.AudioGuideId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(item => item.Location)
                .WithMany(location => location.ListeningHistories)
                .HasForeignKey(item => item.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(item => item.UserId);
            entity.HasIndex(item => item.AudioGuideId);
            entity.HasIndex(item => item.LocationId);
            entity.HasIndex(item => item.LastListenedAtUtc);
            entity.HasIndex(item => new { item.LocationId, item.LastListenedAtUtc });
            entity.HasIndex(item => new { item.UserId, item.LastListenedAtUtc });
        });

        // Configuration for AppUser
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(item => item.QrCodeValue).IsUnique();
            entity.HasIndex(item => item.DeviceId).IsUnique();
            entity.HasIndex(item => item.Status);
            entity.HasIndex(item => item.CurrentActivityAtUtc);

            entity.Property(item => item.QrCodeValue).IsRequired().HasMaxLength(255);
            entity.Property(item => item.DeviceId).IsRequired().HasMaxLength(255);
            entity.Property(item => item.Status).IsRequired().HasMaxLength(30);
            entity.Property(item => item.CurrentActivity).HasMaxLength(200);

            entity.HasQueryFilter(item => !item.IsDeleted);
        });

        modelBuilder.Entity<AppUserActivityLog>(entity =>
        {
            entity.ToTable("AppUserActivityLog");
            entity.HasIndex(item => item.UserId);
            entity.HasIndex(item => item.DeviceId);
            entity.HasIndex(item => item.SessionToken);
            entity.HasIndex(item => item.LoggedAtUtc);
            entity.HasIndex(item => new { item.UserId, item.LoggedAtUtc });

            entity.Property(item => item.DeviceId).IsRequired().HasMaxLength(255);
            entity.Property(item => item.SessionToken).IsRequired().HasMaxLength(2000);
            entity.Property(item => item.ActivityName).IsRequired().HasMaxLength(200);
            entity.Property(item => item.ActivityContext).HasMaxLength(200);
            entity.Property(item => item.Route).HasMaxLength(200);

            entity.HasOne(item => item.User)
                .WithMany(user => user.ActivityLogs)
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuration for PaymentPackage
        modelBuilder.Entity<PaymentPackage>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.IsActive);
            entity.HasIndex(item => item.Price);

            entity.Property(item => item.Id).HasMaxLength(50);
            entity.Property(item => item.Name).IsRequired().HasMaxLength(150);
            entity.Property(item => item.Currency).IsRequired().HasMaxLength(10);
        });

        // Configuration for UserSubscription
        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.HasIndex(item => item.UserId);
            entity.HasIndex(item => item.AuthUserId);
            entity.HasIndex(item => item.PackageId);
            entity.HasIndex(item => item.Status);
            entity.HasIndex(item => item.ExpiresAtUtc);
            entity.HasIndex(item => new { item.UserId, item.Status });

            entity.Property(item => item.PackageId).IsRequired().HasMaxLength(50);
            entity.Property(item => item.Status).IsRequired().HasMaxLength(30);

            entity.HasOne(item => item.User)
                .WithMany(u => u.Subscriptions)
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.AuthUser)
                .WithMany() // AuthUserAccount doesn't have a Subscriptions collection yet
                .HasForeignKey(item => item.AuthUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(item => item.Package)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(item => item.PackageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuration for UserAppSession
        modelBuilder.Entity<UserAppSession>(entity =>
        {
            entity.HasIndex(item => item.UserId);
            entity.HasIndex(item => item.DeviceId);
            entity.HasIndex(item => item.TokenValue).IsUnique();
            entity.HasIndex(item => item.IsActive);
            entity.HasIndex(item => item.ExpiresAtUtc);
            entity.HasIndex(item => new { item.UserId, item.DeviceId });

            entity.Property(item => item.TokenValue).IsRequired().HasMaxLength(2000);
            entity.Property(item => item.DeviceId).IsRequired().HasMaxLength(255);

            entity.HasOne(item => item.User)
                .WithMany(u => u.AppSessions)
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuration for PoiRegistrationRequest
        modelBuilder.Entity<PoiRegistrationRequest>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Status);
            entity.HasIndex(item => item.ExpiresAtUtc);

            entity.Property(item => item.PackageId).IsRequired().HasMaxLength(50);
            entity.Property(item => item.Status).IsRequired().HasMaxLength(30);
            entity.Property(item => item.PaymentReference).HasMaxLength(200);
            entity.Property(item => item.CreatedUsername).HasMaxLength(100);

            entity.HasOne(item => item.Package)
                .WithMany()
                .HasForeignKey(item => item.PackageId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}