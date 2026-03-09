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

        if (context.Categories.Any()) return;

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
    }
}
