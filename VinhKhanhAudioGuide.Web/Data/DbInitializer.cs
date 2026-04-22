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
        EnsureLocationSpatialColumns(context);
        EnsureAppUserLegacyColumnsRemoved(context);

        var hasVinhKhanhSeed = context.Locations
            .AsNoTracking()
            .Any(location => EF.Functions.Like(location.Address, "%Vĩnh Khánh%"));

        if (context.Categories.Any() && hasVinhKhanhSeed)
        {
            EnsureAuthUserAccounts(context);
            EnsurePoiAdminAssignments(context);
            EnsureListeningHistory(context);
            EnsurePaymentPackages(context);
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
        EnsurePaymentPackages(context);
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

            Guid? parsedUserId = null;
            if (!string.IsNullOrWhiteSpace(seed.UserId) && Guid.TryParse(seed.UserId, out var userIdValue))
            {
                var userExists = context.AppUsers.Any(item => item.Id == userIdValue);
                if (userExists)
                {
                    parsedUserId = userIdValue;
                }
            }

            context.ListeningHistories.Add(new ListeningHistory
            {
                Id = seed.Id,
                UserId = parsedUserId,
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

    private static void EnsurePaymentPackages(AppDbContext context)
    {
        var packages = SampleData.GetPaymentPackages();

        foreach (var seed in packages)
        {
            var existing = context.PaymentPackages.FirstOrDefault(item => item.Id == seed.Id);
            if (existing is null)
            {
                context.PaymentPackages.Add(seed);
                continue;
            }

            existing.Name = seed.Name;
            existing.Description = seed.Description;
            existing.Price = seed.Price;
            existing.Currency = seed.Currency;
            existing.DurationDays = seed.DurationDays;
            existing.IsActive = seed.IsActive;
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
        [UserId] uniqueidentifier NULL,
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
        CONSTRAINT [FK_ListeningHistory_AppUsers_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[AppUsers]([Id]),
        CONSTRAINT [FK_ListeningHistory_AudioGuides_AudioGuideId]
            FOREIGN KEY ([AudioGuideId]) REFERENCES [dbo].[AudioGuides]([Id]),
        CONSTRAINT [FK_ListeningHistory_Locations_LocationId]
            FOREIGN KEY ([LocationId]) REFERENCES [dbo].[Locations]([Id])
    );

    CREATE INDEX [IX_ListeningHistory_UserId] ON [dbo].[ListeningHistory]([UserId]);
    CREATE INDEX [IX_ListeningHistory_AudioGuideId] ON [dbo].[ListeningHistory]([AudioGuideId]);
    CREATE INDEX [IX_ListeningHistory_LocationId] ON [dbo].[ListeningHistory]([LocationId]);
    CREATE INDEX [IX_ListeningHistory_LastListenedAtUtc] ON [dbo].[ListeningHistory]([LastListenedAtUtc]);
    CREATE INDEX [IX_ListeningHistory_LocationId_LastListenedAtUtc] ON [dbo].[ListeningHistory]([LocationId], [LastListenedAtUtc]);
    CREATE INDEX [IX_ListeningHistory_UserId_LastListenedAtUtc] ON [dbo].[ListeningHistory]([UserId], [LastListenedAtUtc]);
END

IF OBJECT_ID(N'dbo.ListeningHistory', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ListeningHistory', 'UserId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[ListeningHistory] ADD [UserId] uniqueidentifier NULL;
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_ListeningHistory_AppUsers_UserId'
    )
    BEGIN
        ALTER TABLE [dbo].[ListeningHistory]
        ADD CONSTRAINT [FK_ListeningHistory_AppUsers_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[AppUsers]([Id]);
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_ListeningHistory_UserId'
          AND object_id = OBJECT_ID(N'dbo.ListeningHistory')
    )
    BEGIN
        CREATE INDEX [IX_ListeningHistory_UserId] ON [dbo].[ListeningHistory]([UserId]);
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_ListeningHistory_LocationId_LastListenedAtUtc'
          AND object_id = OBJECT_ID(N'dbo.ListeningHistory')
    )
    BEGIN
        CREATE INDEX [IX_ListeningHistory_LocationId_LastListenedAtUtc] ON [dbo].[ListeningHistory]([LocationId], [LastListenedAtUtc]);
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_ListeningHistory_UserId_LastListenedAtUtc'
          AND object_id = OBJECT_ID(N'dbo.ListeningHistory')
    )
    BEGIN
        CREATE INDEX [IX_ListeningHistory_UserId_LastListenedAtUtc] ON [dbo].[ListeningHistory]([UserId], [LastListenedAtUtc]);
    END
END
");
    }

    private static void EnsureLocationSpatialColumns(AppDbContext context)
    {
        context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'dbo.Locations', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Locations', 'Priority') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Locations] ADD [Priority] int NOT NULL CONSTRAINT [DF_Locations_Priority] DEFAULT (100);
    END

    IF COL_LENGTH('dbo.Locations', 'DetectionRadiusMeters') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Locations] ADD [DetectionRadiusMeters] float NOT NULL CONSTRAINT [DF_Locations_DetectionRadiusMeters] DEFAULT (80);
    END
END
");
    }

    private static void EnsureAppUserLegacyColumnsRemoved(AppDbContext context)
    {
        context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'dbo.AppUsers', N'U') IS NOT NULL
BEGIN
    IF EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'DisplayName' AND Object_ID = Object_ID(N'dbo.AppUsers'))
    BEGIN
        ALTER TABLE [dbo].[AppUsers] DROP COLUMN [DisplayName];
    END

    IF EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'PhoneNumber' AND Object_ID = Object_ID(N'dbo.AppUsers'))
    BEGIN
        IF EXISTS(SELECT 1 FROM sys.indexes WHERE name = N'IX_AppUsers_PhoneNumber' AND object_id = Object_ID(N'dbo.AppUsers'))
        BEGIN
            DROP INDEX [IX_AppUsers_PhoneNumber] ON [dbo].[AppUsers];
        END
        ALTER TABLE [dbo].[AppUsers] DROP COLUMN [PhoneNumber];
    END

    IF EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'Email' AND Object_ID = Object_ID(N'dbo.AppUsers'))
    BEGIN
        IF EXISTS(SELECT 1 FROM sys.indexes WHERE name = N'IX_AppUsers_Email' AND object_id = Object_ID(N'dbo.AppUsers'))
        BEGIN
            DROP INDEX [IX_AppUsers_Email] ON [dbo].[AppUsers];
        END
        ALTER TABLE [dbo].[AppUsers] DROP COLUMN [Email];
    END
END
");
    }
}
