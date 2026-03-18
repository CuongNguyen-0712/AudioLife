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

        var hasVinhKhanhSeed = context.Locations
            .AsNoTracking()
            .Any(location => EF.Functions.Like(location.Address, "%Vĩnh Khánh%"));

        if (context.Categories.Any() && hasVinhKhanhSeed)
        {
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
    }
}
