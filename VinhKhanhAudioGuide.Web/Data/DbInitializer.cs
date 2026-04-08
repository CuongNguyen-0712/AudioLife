using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Data;

public static class DbInitializer
{
    public static void Seed(AppDbContext context)
    {
        var creator = context.Database.GetService<IRelationalDatabaseCreator>();
        if (!creator.Exists())
        {
            creator.Create();
        }
        if (!creator.HasTables())
        {
            creator.CreateTables();
        }

        EnsureApprovalTables(context);

        var hasVinhKhanhSeed = context.Locations
            .AsNoTracking()
            .Any(location => EF.Functions.Like(location.Address, "%Vĩnh Khánh%"));

        if (context.Categories.Any() && hasVinhKhanhSeed)
        {
            EnsurePoiAdminAssignments(context);
            return;
        }

        if (context.Categories.Any())
        {
            context.Feedbacks.RemoveRange(context.Feedbacks);
            context.AppUsers.RemoveRange(context.AppUsers);
            context.TourLocations.RemoveRange(context.TourLocations);
            context.AudioGuides.RemoveRange(context.AudioGuides);
            context.Tours.RemoveRange(context.Tours);
            context.Locations.RemoveRange(context.Locations);
            context.Categories.RemoveRange(context.Categories);
            context.SaveChanges();
        }

        var appUser = new AppUser { Id = "user_1", ScannedQrCode = "loc_001_qr", CreatedAt = DateTime.UtcNow, IsActive = true };
        context.AppUsers.Add(appUser);
        context.SaveChanges();

        var categories = SampleData.GetCategories();
        context.Categories.AddRange(categories);
        context.SaveChanges();

        var locations = SampleData.GetLocations();
        foreach (var loc in locations)
        {
            var audioGuides = loc.AudioGuides.ToList();
            loc.AudioGuides = new List<AudioGuide>();
            context.Locations.Add(loc);
            context.SaveChanges();

            foreach (var ag in audioGuides)
            {
                ag.LocationId = loc.Id;
                context.AudioGuides.Add(ag);
            }
            context.SaveChanges();
        }

        var tours = SampleData.GetTours();
        foreach (var tour in tours)
        {
            var locationIds = tour.LocationIds.ToList();
            context.Tours.Add(tour);
            context.SaveChanges();

            for (int i = 0; i < locationIds.Count; i++)
            {
                context.TourLocations.Add(new TourLocation
                {
                    TourId = tour.Id,
                    LocationId = locationIds[i],
                    SortOrder = i
                });
            }
            context.SaveChanges();
        }

        var feedback = new Feedback 
        { 
            UserId = "user_1", 
            LocationId = "loc_001", 
            Rating = 5, 
            Comment = "Rất tuyệt vời, trải nghiệm tốt.", 
            CreatedAt = DateTime.UtcNow 
        };
        context.Feedbacks.Add(feedback);
        context.SaveChanges();

        EnsurePoiAdminAssignments(context);
    }

    private static void EnsurePoiAdminAssignments(AppDbContext context)
    {
        var defaultAssignments = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["admin.poi.01"] = new[] { "loc_001", "loc_002", "loc_003", "loc_004", "loc_005" },
            ["admin.poi.02"] = new[] { "loc_006", "loc_007", "loc_008", "loc_009", "loc_010" }
        };

        context.PoiAdminLocationAssignments.RemoveRange(context.PoiAdminLocationAssignments);
        context.SaveChanges();

        foreach (var (username, locationIds) in defaultAssignments)
        {
            foreach (var locationId in locationIds.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                context.PoiAdminLocationAssignments.Add(new PoiAdminLocationAssignment
                {
                    Username = username,
                    LocationId = locationId
                });
            }
        }

        context.SaveChanges();
    }

    private static void EnsureApprovalTables(AppDbContext context)
    {
        context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'dbo.PoiChangeRequests', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PoiChangeRequests](
        [Id] uniqueidentifier NOT NULL,
        [SubmittedByUsername] nvarchar(100) NOT NULL,
        [SubmittedByName] nvarchar(150) NOT NULL,
        [LocationId] nvarchar(50) NOT NULL,
        [LocationName] nvarchar(200) NOT NULL,
        [Topic] nvarchar(100) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Details] nvarchar(2000) NOT NULL,
        [TargetType] int NOT NULL,
        [TargetEntityId] nvarchar(50) NOT NULL,
        [ChangeSetJson] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [SubmittedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [ReviewNote] nvarchar(500) NULL,
        CONSTRAINT [PK_PoiChangeRequests] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_PoiChangeRequests_Status] ON [dbo].[PoiChangeRequests]([Status]);
    CREATE INDEX [IX_PoiChangeRequests_SubmittedByUsername] ON [dbo].[PoiChangeRequests]([SubmittedByUsername]);
    CREATE INDEX [IX_PoiChangeRequests_LocationId] ON [dbo].[PoiChangeRequests]([LocationId]);
    CREATE INDEX [IX_PoiChangeRequests_SubmittedAtUtc] ON [dbo].[PoiChangeRequests]([SubmittedAtUtc]);
END
");

        context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'dbo.PoiAdminLocationAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PoiAdminLocationAssignments](
        [Id] int IDENTITY(1,1) NOT NULL,
        [Username] nvarchar(100) NOT NULL,
        [LocationId] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_PoiAdminLocationAssignments] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_PoiAdminLocationAssignments_Username] ON [dbo].[PoiAdminLocationAssignments]([Username]);
    CREATE INDEX [IX_PoiAdminLocationAssignments_LocationId] ON [dbo].[PoiAdminLocationAssignments]([LocationId]);
    CREATE UNIQUE INDEX [IX_PoiAdminLocationAssignments_Username_LocationId] ON [dbo].[PoiAdminLocationAssignments]([Username], [LocationId]);
END
;

IF OBJECT_ID(N'dbo.AuthUserAccounts', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AuthUserAccounts](
        [Id] int IDENTITY(1,1) NOT NULL,
        [Username] nvarchar(100) NOT NULL,
        [Password] nvarchar(200) NOT NULL,
        [DisplayName] nvarchar(150) NOT NULL,
        [Role] nvarchar(30) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AuthUserAccounts] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_AuthUserAccounts_Username] ON [dbo].[AuthUserAccounts]([Username]);
    CREATE INDEX [IX_AuthUserAccounts_Role_IsActive] ON [dbo].[AuthUserAccounts]([Role], [IsActive]);
END
");
    }
}
