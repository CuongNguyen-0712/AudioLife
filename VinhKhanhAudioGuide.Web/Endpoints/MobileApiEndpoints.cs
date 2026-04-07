using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Endpoints;

public static class MobileApiEndpoints
{
    public static IEndpointRouteBuilder MapMobileApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mobile");

        group.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        group.MapGet("/categories", async (AppDbContext db) =>
        {
            var categories = await db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new MobileCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Icon = c.Icon,
                    Description = c.Description,
                    LocationCount = c.Locations.Count
                })
                .ToListAsync();

            return Results.Ok(categories);
        });

        group.MapGet("/locations", async (AppDbContext db) =>
        {
            var locations = await db.Locations
                .AsNoTracking()
                .Include(l => l.Category)
                .Include(l => l.AudioGuides)
                .OrderBy(l => l.Name)
                .ToListAsync();

            return Results.Ok(locations.Select(l => ToLocationDto(l, includeAudioGuides: true)));
        });

        group.MapGet("/locations/{id}", async (string id, AppDbContext db) =>
        {
            var location = await db.Locations
                .AsNoTracking()
                .Include(l => l.Category)
                .Include(l => l.AudioGuides)
                .FirstOrDefaultAsync(l => l.Id == id);

            return location is null
                ? Results.NotFound()
                : Results.Ok(ToLocationDto(location, includeAudioGuides: true));
        });

        group.MapGet("/locations/by-category/{categoryId}", async (string categoryId, AppDbContext db) =>
        {
            var locations = await db.Locations
                .AsNoTracking()
                .Include(l => l.Category)
                .Include(l => l.AudioGuides)
                .Where(l => l.CategoryId == categoryId)
                .OrderBy(l => l.Name)
                .ToListAsync();

            return Results.Ok(locations.Select(l => ToLocationDto(l, includeAudioGuides: true)));
        });

        group.MapGet("/locations/search", async (string query, AppDbContext db) =>
        {
            var normalized = query.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return Results.Ok(Array.Empty<MobileLocationDto>());
            }

            var locations = await db.Locations
                .AsNoTracking()
                .Include(l => l.Category)
                .Include(l => l.AudioGuides)
                .Where(l =>
                    EF.Functions.Like(l.Name, $"%{normalized}%") ||
                    EF.Functions.Like(l.Description, $"%{normalized}%") ||
                    EF.Functions.Like(l.Address, $"%{normalized}%"))
                .OrderBy(l => l.Name)
                .ToListAsync();

            return Results.Ok(locations.Select(l => ToLocationDto(l, includeAudioGuides: true)));
        });

        group.MapGet("/locations/nearby", async (double latitude, double longitude, double? radiusKm, AppDbContext db) =>
        {
            var radius = radiusKm.GetValueOrDefault(0.1);
            var locations = await db.Locations
                .AsNoTracking()
                .Include(l => l.Category)
                .Include(l => l.AudioGuides)
                .ToListAsync();

            var nearby = locations
                .Select(l => new
                {
                    Location = l,
                    DistanceKm = CalculateDistanceKm(latitude, longitude, l.Latitude, l.Longitude)
                })
                .Where(x => x.DistanceKm <= radius)
                .OrderBy(x => x.DistanceKm)
                .Select(x => ToLocationDto(x.Location, includeAudioGuides: true))
                .ToList();

            return Results.Ok(nearby);
        });

        group.MapGet("/tours", async (AppDbContext db) =>
        {
            var tours = await db.Tours
                .AsNoTracking()
                .Include(t => t.TourLocations)
                .OrderBy(t => t.Name)
                .ToListAsync();

            return Results.Ok(tours.Select(ToTourDto));
        });

        group.MapGet("/tours/{id}", async (string id, AppDbContext db) =>
        {
            var tour = await db.Tours
                .AsNoTracking()
                .Include(t => t.TourLocations)
                .FirstOrDefaultAsync(t => t.Id == id);

            return tour is null ? Results.NotFound() : Results.Ok(ToTourDto(tour));
        });

        group.MapGet("/tours/featured", async (AppDbContext db) =>
        {
            var tours = await db.Tours
                .AsNoTracking()
                .Include(t => t.TourLocations)
                .Where(t => t.IsFeatured)
                .OrderBy(t => t.Name)
                .ToListAsync();

            return Results.Ok(tours.Select(ToTourDto));
        });

        group.MapGet("/audio/by-location/{locationId}", async (string locationId, AppDbContext db) =>
        {
            var guides = await db.AudioGuides
                .AsNoTracking()
                .Include(a => a.ScriptSegments)
                .Where(a => a.LocationId == locationId)
                .OrderBy(a => a.Title)
                .ToListAsync();

            return Results.Ok(guides.Select(a => ToAudioGuideDto(a, includeSegments: true)));
        });

        group.MapGet("/audio/{id}", async (string id, AppDbContext db) =>
        {
            var guide = await db.AudioGuides
                .AsNoTracking()
                .Include(a => a.ScriptSegments)
                .FirstOrDefaultAsync(a => a.Id == id);

            return guide is null ? Results.NotFound() : Results.Ok(ToAudioGuideDto(guide, includeSegments: true));
        });

        return app;
    }

    private static MobileLocationDto ToLocationDto(Location location, bool includeAudioGuides)
    {
        return new MobileLocationDto
        {
            Id = location.Id,
            Name = location.Name,
            Description = location.Description,
            ImageUrl = location.ImageUrl,
            Address = location.Address,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Duration = location.Duration,
            CategoryId = location.CategoryId,
            CategoryName = location.Category?.Name ?? string.Empty,
            IsFavorite = false,
            AudioGuides = includeAudioGuides
                ? location.AudioGuides.Select(guide => ToAudioGuideDto(guide, includeSegments: false)).ToList()
                : new List<MobileAudioGuideDto>()
        };
    }

    private static MobileAudioGuideDto ToAudioGuideDto(AudioGuide guide, bool includeSegments)
    {
        return new MobileAudioGuideDto
        {
            Id = guide.Id,
            Title = guide.Title,
            Description = guide.Description,
            AudioUrl = guide.AudioUrl,
            CloudinaryAudioUrl = guide.CloudinaryAudioUrl,
            CloudinaryPublicId = guide.CloudinaryPublicId,
            TranscriptText = guide.TranscriptText,
            Duration = guide.Duration,
            LocationId = guide.LocationId,
            Language = guide.Language,
            ScriptSegments = includeSegments
                ? guide.ScriptSegments
                    .OrderBy(s => s.StartTimeSeconds)
                    .Select(s => new MobileAudioScriptSegmentDto
                    {
                        Id = s.Id,
                        AudioGuideId = s.AudioGuideId,
                        StartTimeSeconds = s.StartTimeSeconds,
                        EndTimeSeconds = s.EndTimeSeconds,
                        ScriptText = s.ScriptText
                    })
                    .ToList()
                : new List<MobileAudioScriptSegmentDto>()
        };
    }

    private static MobileTourDto ToTourDto(Tour tour)
    {
        return new MobileTourDto
        {
            Id = tour.Id,
            Name = tour.Name,
            Description = tour.Description,
            ImageUrl = tour.ImageUrl,
            Duration = tour.Duration,
            Price = tour.Price,
            IsFeatured = tour.IsFeatured,
            LocationIds = tour.TourLocations
                .OrderBy(tl => tl.SortOrder)
                .Select(tl => tl.LocationId)
                .ToList()
        };
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    private static double ToRadians(double angle) => angle * Math.PI / 180.0;

    private sealed class MobileCategoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int LocationCount { get; set; }
    }

    private sealed class MobileLocationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Duration { get; set; }
        public string CategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public List<MobileAudioGuideDto> AudioGuides { get; set; } = new();
    }

    private sealed class MobileTourDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int Duration { get; set; }
        public List<string> LocationIds { get; set; } = new();
        public decimal Price { get; set; }
        public bool IsFeatured { get; set; }
    }

    private sealed class MobileAudioGuideDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
        public string? CloudinaryAudioUrl { get; set; }
        public string? CloudinaryPublicId { get; set; }
        public string TranscriptText { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string LocationId { get; set; } = string.Empty;
        public string Language { get; set; } = "vi";
        public List<MobileAudioScriptSegmentDto> ScriptSegments { get; set; } = new();
    }

    private sealed class MobileAudioScriptSegmentDto
    {
        public int Id { get; set; }
        public string AudioGuideId { get; set; } = string.Empty;
        public int StartTimeSeconds { get; set; }
        public int EndTimeSeconds { get; set; }
        public string ScriptText { get; set; } = string.Empty;
    }
}
