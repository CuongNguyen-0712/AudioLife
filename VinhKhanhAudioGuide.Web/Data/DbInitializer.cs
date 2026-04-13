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
        EnsureListeningHistoryTable(context);
        EnsureLegacyTablesRemoved(context);

        var hasVinhKhanhSeed = context.Locations
            .AsNoTracking()
            .Any(location => EF.Functions.Like(location.Address, "%Vĩnh Khánh%"));

        if (context.Categories.Any() && hasVinhKhanhSeed)
        {
            EnsureAuthUserAccounts(context);
            EnsurePoiAdminAssignments(context);
            EnsureListeningHistory(context);
            return;
        }

        if (context.Categories.Any())
        {
            context.ListeningHistories.RemoveRange(context.ListeningHistories);
            context.TourLocations.RemoveRange(context.TourLocations);
            context.AudioGuides.RemoveRange(context.AudioGuides);
            context.Tours.RemoveRange(context.Tours);
            context.Locations.RemoveRange(context.Locations);
            context.Categories.RemoveRange(context.Categories);
            context.SaveChanges();
        }

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

        EnsureAuthUserAccounts(context);
        EnsurePoiAdminAssignments(context);
        EnsureListeningHistory(context);
    }

    private static void EnsureAuthUserAccounts(AppDbContext context)
    {
        if (context.AuthUserAccounts.Any())
        {
            return;
        }

        var defaultAccounts = SampleData.GetAuthUserAccounts();
        if (defaultAccounts.Count == 0)
        {
            return;
        }

        context.AuthUserAccounts.AddRange(defaultAccounts);
        context.SaveChanges();
    }

    private static void EnsurePoiAdminAssignments(AppDbContext context)
    {
        if (context.PoiAdminLocationAssignments.Any())
        {
            return;
        }

        var defaultAssignments = SampleData.GetPoiAdminLocationAssignments();
        foreach (var assignment in defaultAssignments)
        {
            context.PoiAdminLocationAssignments.Add(assignment);
        }

        context.SaveChanges();
    }

    private static void EnsureListeningHistory(AppDbContext context)
    {
        if (context.ListeningHistories.Any())
        {
            return;
        }

        var seeds = SampleData.GetListeningHistorySeeds();
        foreach (var seed in seeds)
        {
            var audioExists = context.AudioGuides.Any(item => item.Id == seed.AudioGuideId);
            var locationExists = context.Locations.Any(item => item.Id == seed.LocationId);
            if (!audioExists || !locationExists)
            {
                continue;
            }

            context.ListeningHistories.Add(new ListeningHistory
            {
                Id = seed.Id,
                AudioGuideId = seed.AudioGuideId,
                LocationId = seed.LocationId,
                AudioTitle = seed.AudioTitle,
                LocationName = seed.LocationName,
                LocationImageUrl = seed.LocationImageUrl,
                AudioDuration = seed.AudioDuration,
                Progress = seed.Progress,
                ListenedSeconds = seed.ListenedSeconds,
                IsCompleted = seed.IsCompleted,
                LastListenedAtUtc = seed.LastListenedAtUtc
            });
        }

        context.SaveChanges();
    }

    private static void EnsureLegacyTablesRemoved(AppDbContext context)
    {
        context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'dbo.ListeningHistories', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[ListeningHistories];
END

IF OBJECT_ID(N'dbo.Feedbacks', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[Feedbacks];
END

IF OBJECT_ID(N'dbo.AppUsers', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[AppUsers];
END
");
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

    private static void EnsureListeningHistoryTable(AppDbContext context)
    {
        context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'dbo.ListeningHistory', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ListeningHistory](
        [Id] nvarchar(100) NOT NULL,
        [AudioGuideId] nvarchar(50) NOT NULL,
        [LocationId] nvarchar(50) NOT NULL,
        [AudioTitle] nvarchar(200) NOT NULL,
        [LocationName] nvarchar(200) NOT NULL,
        [LocationImageUrl] nvarchar(500) NOT NULL,
        [AudioDuration] int NOT NULL,
        [Progress] decimal(5,4) NOT NULL,
        [ListenedSeconds] int NOT NULL,
        [IsCompleted] bit NOT NULL,
        [LastListenedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ListeningHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ListeningHistory_AudioGuides_AudioGuideId]
            FOREIGN KEY ([AudioGuideId]) REFERENCES [dbo].[AudioGuides]([Id]),
        CONSTRAINT [FK_ListeningHistory_Locations_LocationId]
            FOREIGN KEY ([LocationId]) REFERENCES [dbo].[Locations]([Id])
    );

    CREATE INDEX [IX_ListeningHistory_AudioGuideId] ON [dbo].[ListeningHistory]([AudioGuideId]);
    CREATE INDEX [IX_ListeningHistory_LocationId] ON [dbo].[ListeningHistory]([LocationId]);
    CREATE INDEX [IX_ListeningHistory_LastListenedAtUtc] ON [dbo].[ListeningHistory]([LastListenedAtUtc]);
END
");
    }
}
