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

        group.MapGet("/payment/packages", async (AppDbContext db) =>
        {
            var packages = await db.PaymentPackages
                .AsNoTracking()
                .OrderBy(item => item.Price)
                .Select(item => new MobilePaymentPackageDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    Price = item.Price,
                    Currency = item.Currency,
                    DurationDays = item.DurationDays,
                    IsActive = item.IsActive,
                    CreatedAtUtc = item.CreatedAtUtc
                })
                .ToListAsync();

            return Results.Ok(packages);
        });

        group.MapPost("/payment/complete", async (MobilePaymentCompletionRequest request, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId) || string.IsNullOrWhiteSpace(request.PackageId))
            {
                return Results.BadRequest(new MobilePaymentCompletionResponse
                {
                    Success = false,
                    Message = "DeviceId và PackageId là bắt buộc.",
                    UserAppId = string.Empty,
                    SessionToken = request.SessionToken ?? string.Empty,
                    RefreshToken = request.RefreshToken,
                    PackageId = request.PackageId,
                    PaymentStatus = request.PaymentStatus,
                    PaymentReference = request.PaymentReference,
                    ExpiresAtUtc = DateTime.UtcNow,
                    LastValidatedAtUtc = DateTime.UtcNow
                });
            }

            var package = await db.PaymentPackages.FirstOrDefaultAsync(item => item.Id == request.PackageId && item.IsActive);
            if (package is null)
            {
                return Results.NotFound(new MobilePaymentCompletionResponse
                {
                    Success = false,
                    Message = "Gói thanh toán không tồn tại.",
                    UserAppId = string.Empty,
                    SessionToken = request.SessionToken ?? string.Empty,
                    RefreshToken = request.RefreshToken,
                    PackageId = request.PackageId,
                    PaymentStatus = request.PaymentStatus,
                    PaymentReference = request.PaymentReference,
                    ExpiresAtUtc = DateTime.UtcNow,
                    LastValidatedAtUtc = DateTime.UtcNow
                });
            }

            var nowUtc = DateTime.UtcNow;
            var qrToken = ResolveQrToken(request);
            var user = await db.AppUsers.FirstOrDefaultAsync(item => item.QrCodeValue == qrToken);
            if (user is null)
            {
                user = new AppUser
                {
                    QrCodeValue = qrToken,
                    Status = "Active",
                    CreatedAtUtc = nowUtc,
                    LastSeenAtUtc = nowUtc
                };

                db.AppUsers.Add(user);
                await db.SaveChangesAsync();
            }

            user.LastSeenAtUtc = nowUtc;

            var isPaid = IsSuccessfulPaymentStatus(request.PaymentStatus);
            var subscription = await db.UserSubscriptions
                .Where(item => item.UserId == user.Id && item.PackageId == package.Id)
                .OrderByDescending(item => item.PurchasedAtUtc)
                .FirstOrDefaultAsync();

            if (subscription is null)
            {
                subscription = new UserSubscription
                {
                    UserId = user.Id,
                    PackageId = package.Id
                };

                db.UserSubscriptions.Add(subscription);
            }

            subscription.Status = isPaid ? "Active" : request.PaymentStatus;
            subscription.PurchasedAtUtc = nowUtc;
            subscription.StartsAtUtc = isPaid ? nowUtc : null;
            subscription.ExpiresAtUtc = isPaid ? nowUtc.AddDays(Math.Max(package.DurationDays, 1)) : null;
            subscription.PaymentReference = request.PaymentReference;
            subscription.LastVerifiedAtUtc = nowUtc;

            var sessionToken = string.IsNullOrWhiteSpace(request.SessionToken)
                ? Guid.NewGuid().ToString("N")
                : request.SessionToken;

            var session = await db.UserAppSessions.FirstOrDefaultAsync(item => item.TokenValue == sessionToken || (item.UserId == user.Id && item.DeviceId == request.DeviceId));
            if (session is null)
            {
                session = new UserAppSession
                {
                    UserId = user.Id,
                    DeviceId = request.DeviceId,
                    TokenValue = sessionToken,
                    RefreshToken = request.RefreshToken,
                    IssuedAtUtc = nowUtc,
                    ExpiresAtUtc = isPaid ? nowUtc.AddDays(Math.Max(package.DurationDays, 1)) : nowUtc.AddMinutes(5),
                    LastValidatedAtUtc = nowUtc,
                    IsActive = isPaid
                };

                db.UserAppSessions.Add(session);
            }
            else
            {
                session.DeviceId = request.DeviceId;
                session.TokenValue = sessionToken;
                session.RefreshToken = request.RefreshToken;
                session.LastValidatedAtUtc = nowUtc;
                session.IsActive = isPaid;
                session.RevokedAtUtc = isPaid ? null : nowUtc;
                session.ExpiresAtUtc = isPaid ? nowUtc.AddDays(Math.Max(package.DurationDays, 1)) : nowUtc.AddMinutes(5);
            }

            await db.SaveChangesAsync();

            return Results.Ok(new MobilePaymentCompletionResponse
            {
                Success = isPaid,
                Message = isPaid ? "Thanh toán đã được xác nhận." : "Thanh toán đang chờ xử lý.",
                UserAppId = qrToken,
                SessionToken = sessionToken,
                RefreshToken = request.RefreshToken,
                PackageId = package.Id,
                PaymentStatus = subscription.Status,
                PaymentReference = request.PaymentReference,
                ExpiresAtUtc = session.ExpiresAtUtc,
                LastValidatedAtUtc = nowUtc
            });
        });

        group.MapGet("/session/validate", async (string sessionToken, string deviceId, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(sessionToken) || string.IsNullOrWhiteSpace(deviceId))
            {
                return Results.Ok(new SessionValidationResult
                {
                    IsValid = false,
                    Message = "Thiếu token phiên hoặc deviceId.",
                    UserAppId = string.Empty,
                    SessionToken = sessionToken ?? string.Empty,
                    RefreshToken = null,
                    PackageId = null,
                    PaymentStatus = null,
                    ExpiresAtUtc = DateTime.UtcNow,
                    LastValidatedAtUtc = DateTime.UtcNow
                });
            }

            var session = await db.UserAppSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.TokenValue == sessionToken && item.DeviceId == deviceId);

            if (session is null || !session.IsActive || session.RevokedAtUtc is not null || session.ExpiresAtUtc <= DateTime.UtcNow)
            {
                return Results.Ok(new SessionValidationResult
                {
                    IsValid = false,
                    Message = "Phiên không hợp lệ hoặc đã hết hạn.",
                    UserAppId = string.Empty,
                    SessionToken = sessionToken,
                    RefreshToken = null,
                    PackageId = null,
                    PaymentStatus = null,
                    ExpiresAtUtc = session?.ExpiresAtUtc ?? DateTime.UtcNow,
                    LastValidatedAtUtc = DateTime.UtcNow
                });
            }

            var activeSubscription = await db.UserSubscriptions
                .AsNoTracking()
                .Where(item => item.UserId == session.UserId && item.Status == "Active")
                .OrderByDescending(item => item.ExpiresAtUtc)
                .FirstOrDefaultAsync();

            return Results.Ok(new SessionValidationResult
            {
                IsValid = activeSubscription is not null,
                Message = activeSubscription is null ? "Không tìm thấy gói hoạt động." : "Phiên hợp lệ.",
                UserAppId = session.UserId.ToString(),
                SessionToken = session.TokenValue,
                RefreshToken = session.RefreshToken,
                PackageId = activeSubscription?.PackageId,
                PaymentStatus = activeSubscription?.Status,
                ExpiresAtUtc = session.ExpiresAtUtc,
                LastValidatedAtUtc = DateTime.UtcNow
            });
        });

        group.MapGet("/session/by-device", async (string deviceId, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Results.Ok(new DeviceSessionCheckResult
                {
                    HasSession = false,
                    Message = "Thiếu deviceId.",
                    UserAppId = string.Empty,
                    SessionToken = string.Empty,
                    RefreshToken = null,
                    PackageId = null,
                    PaymentStatus = null,
                    ExpiresAtUtc = DateTime.UtcNow,
                    LastValidatedAtUtc = DateTime.UtcNow
                });
            }

            var nowUtc = DateTime.UtcNow;
            var session = await db.UserAppSessions
                .AsNoTracking()
                .Where(item => item.DeviceId == deviceId && item.IsActive && item.RevokedAtUtc == null)
                .OrderByDescending(item => item.LastValidatedAtUtc ?? item.IssuedAtUtc)
                .FirstOrDefaultAsync();

            if (session is null || session.ExpiresAtUtc <= nowUtc)
            {
                return Results.Ok(new DeviceSessionCheckResult
                {
                    HasSession = false,
                    Message = "Không tìm thấy phiên hợp lệ cho thiết bị.",
                    UserAppId = string.Empty,
                    SessionToken = string.Empty,
                    RefreshToken = null,
                    PackageId = null,
                    PaymentStatus = null,
                    ExpiresAtUtc = session?.ExpiresAtUtc ?? nowUtc,
                    LastValidatedAtUtc = nowUtc
                });
            }

            var activeSubscription = await db.UserSubscriptions
                .AsNoTracking()
                .Where(item => item.UserId == session.UserId && item.Status == "Active")
                .OrderByDescending(item => item.ExpiresAtUtc)
                .FirstOrDefaultAsync();

            return Results.Ok(new DeviceSessionCheckResult
            {
                HasSession = true,
                Message = "Đã tìm thấy phiên thiết bị.",
                UserAppId = session.UserId.ToString(),
                SessionToken = session.TokenValue,
                RefreshToken = session.RefreshToken,
                PackageId = activeSubscription?.PackageId,
                PaymentStatus = activeSubscription?.Status,
                ExpiresAtUtc = session.ExpiresAtUtc,
                LastValidatedAtUtc = session.LastValidatedAtUtc ?? nowUtc
            });
        });

        group.MapPost("/heartbeat", async (MobileHeartbeatRequest request, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId) || string.IsNullOrWhiteSpace(request.SessionToken))
            {
                return Results.BadRequest(new MobileHeartbeatResponse
                {
                    Success = false,
                    SessionValid = false,
                    Message = "DeviceId và SessionToken là bắt buộc.",
                    UserAppId = string.Empty,
                    SessionToken = request.SessionToken ?? string.Empty,
                    CurrentActivity = request.ActivityName,
                    CurrentActivityAtUtc = DateTime.UtcNow,
                    LastSeenAtUtc = DateTime.UtcNow,
                    LastValidatedAtUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow
                });
            }

            var nowUtc = DateTime.UtcNow;
            var session = await db.UserAppSessions
                .Include(item => item.User)
                .FirstOrDefaultAsync(item => item.TokenValue == request.SessionToken && item.DeviceId == request.DeviceId);

            if (session is null || !session.IsActive || session.RevokedAtUtc is not null || session.ExpiresAtUtc <= nowUtc)
            {
                return Results.Ok(new MobileHeartbeatResponse
                {
                    Success = false,
                    SessionValid = false,
                    Message = "Phiên không hợp lệ hoặc đã hết hạn.",
                    UserAppId = session?.UserId.ToString() ?? string.Empty,
                    SessionToken = request.SessionToken,
                    CurrentActivity = request.ActivityName,
                    CurrentActivityAtUtc = nowUtc,
                    LastSeenAtUtc = session?.User?.LastSeenAtUtc ?? nowUtc,
                    LastValidatedAtUtc = nowUtc,
                    ExpiresAtUtc = session?.ExpiresAtUtc ?? nowUtc
                });
            }

            var user = session.User ?? await db.AppUsers.FirstOrDefaultAsync(item => item.Id == session.UserId);
            if (user is null || string.Equals(user.Status, "Blocked", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Ok(new MobileHeartbeatResponse
                {
                    Success = false,
                    SessionValid = false,
                    Message = "Người dùng bị chặn hoặc không tồn tại.",
                    UserAppId = session.UserId.ToString(),
                    SessionToken = request.SessionToken,
                    CurrentActivity = request.ActivityName,
                    CurrentActivityAtUtc = nowUtc,
                    LastSeenAtUtc = nowUtc,
                    LastValidatedAtUtc = nowUtc,
                    ExpiresAtUtc = session.ExpiresAtUtc
                });
            }

            var activeSubscription = await db.UserSubscriptions
                .AsNoTracking()
                .Where(item => item.UserId == user.Id && item.Status == "Active")
                .OrderByDescending(item => item.ExpiresAtUtc)
                .FirstOrDefaultAsync();

            if (activeSubscription is null)
            {
                return Results.Ok(new MobileHeartbeatResponse
                {
                    Success = false,
                    SessionValid = false,
                    Message = "Không tìm thấy gói active.",
                    UserAppId = user.Id.ToString(),
                    SessionToken = request.SessionToken,
                    CurrentActivity = request.ActivityName,
                    CurrentActivityAtUtc = nowUtc,
                    LastSeenAtUtc = user.LastSeenAtUtc,
                    LastValidatedAtUtc = nowUtc,
                    ExpiresAtUtc = session.ExpiresAtUtc
                });
            }

            var activityName = NormalizeHeartbeatActivityName(request.ActivityName, request.Route);
            var activityContext = NormalizeHeartbeatContext(request.ActivityContext, request.ScreenName);

            user.LastSeenAtUtc = nowUtc;
            user.CurrentActivity = activityName;
            user.CurrentActivityAtUtc = nowUtc;

            session.LastValidatedAtUtc = nowUtc;
            session.ExpiresAtUtc = nowUtc.AddMinutes(30);
            session.IsActive = true;
            session.RevokedAtUtc = null;

            db.AppUserActivityLogs.Add(new AppUserActivityLog
            {
                UserId = user.Id,
                DeviceId = request.DeviceId,
                SessionToken = request.SessionToken,
                ActivityName = activityName,
                ActivityContext = activityContext,
                Route = request.Route,
                IsForeground = request.IsForeground,
                LoggedAtUtc = nowUtc
            });

            await db.SaveChangesAsync();

            return Results.Ok(new MobileHeartbeatResponse
            {
                Success = true,
                SessionValid = true,
                Message = "Heartbeat đã được ghi nhận.",
                UserAppId = user.Id.ToString(),
                SessionToken = session.TokenValue,
                CurrentActivity = activityName,
                CurrentActivityAtUtc = nowUtc,
                LastSeenAtUtc = user.LastSeenAtUtc,
                LastValidatedAtUtc = nowUtc,
                ExpiresAtUtc = session.ExpiresAtUtc
            });
        });

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

        group.MapGet("/locations", async (string? language, AppDbContext db) =>
        {
            var locations = await db.Locations
                .AsNoTracking()
                .Include(l => l.Category)
                .Include(l => l.AudioGuides)
                .OrderBy(l => l.Name)
                .ToListAsync();

            return Results.Ok(locations.Select(l => ToLocationDto(l, includeAudioGuides: true, language)));
        });

        group.MapGet("/locations/{id}", async (string id, string? language, AppDbContext db) =>
        {
            var location = await db.Locations
                .AsNoTracking()
                .Include(l => l.Category)
                .Include(l => l.AudioGuides)
                .FirstOrDefaultAsync(l => l.Id == id);

            return location is null
                ? Results.NotFound()
                : Results.Ok(ToLocationDto(location, includeAudioGuides: true, language));
        });

        group.MapGet("/locations/by-category/{categoryId}", async (string categoryId, string? language, AppDbContext db) =>
        {
            var locations = await db.Locations
                .AsNoTracking()
                .Include(l => l.Category)
                .Include(l => l.AudioGuides)
                .Where(l => l.CategoryId == categoryId)
                .OrderBy(l => l.Name)
                .ToListAsync();

            return Results.Ok(locations.Select(l => ToLocationDto(l, includeAudioGuides: true, language)));
        });

        group.MapGet("/locations/search", async (string query, string? language, AppDbContext db) =>
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

            return Results.Ok(locations.Select(l => ToLocationDto(l, includeAudioGuides: true, language)));
        });

        group.MapGet("/locations/nearby", async (double latitude, double longitude, double? radiusKm, string? language, AppDbContext db) =>
        {
            var userScanRadiusKm = radiusKm.GetValueOrDefault(0.1);
            var locations = await db.Locations
                .AsNoTracking()
                .Include(l => l.Category)
                .Include(l => l.AudioGuides)
                .ToListAsync();

            var nearby = locations
                .Select(l => new
                {
                    Location = l,
                    DistanceKm = CalculateDistanceKm(latitude, longitude, l.Latitude, l.Longitude),
                    PoiRadiusKm = Math.Max(l.DetectionRadiusMeters, 0) / 1000d
                })
                .Where(x => x.DistanceKm <= userScanRadiusKm + x.PoiRadiusKm)
                .OrderBy(x => x.DistanceKm)
                .ThenByDescending(x => x.Location.Priority)
                .ThenBy(x => x.Location.Id)
                .Select(x => ToLocationDto(x.Location, includeAudioGuides: true, language))
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

        group.MapGet("/audio/by-location/{locationId}", async (string locationId, string? language, AppDbContext db) =>
        {
            var allGuides = await db.AudioGuides
                .AsNoTracking()
                .Include(a => a.ScriptSegments)
                .Where(a => a.LocationId == locationId)
                .OrderBy(a => a.Title)
                .ToListAsync();

            var languageResolution = ResolveLanguageSelection(allGuides, language);
            var guides = languageResolution.Guides;

            return Results.Ok(guides.Select(a => ToAudioGuideDto(a, includeSegments: true, languageResolution.ResolvedLanguage)));
        });

        group.MapGet("/audio/{id}", async (string id, AppDbContext db) =>
        {
            var guide = await db.AudioGuides
                .AsNoTracking()
                .Include(a => a.ScriptSegments)
                .FirstOrDefaultAsync(a => a.Id == id);

            return guide is null ? Results.NotFound() : Results.Ok(ToAudioGuideDto(guide, includeSegments: true));
        });

        group.MapGet("/history", async (int? take, AppDbContext db) =>
        {
            var maxItems = Math.Clamp(take.GetValueOrDefault(50), 1, 200);
            var history = await db.ListeningHistories
                .AsNoTracking()
                .OrderByDescending(item => item.LastListenedAtUtc)
                .Take(maxItems)
                .Select(item => new MobileListeningHistoryDto
                {
                    Id = item.Id,
                    AudioGuideId = item.AudioGuideId,
                    AudioTitle = item.AudioTitle,
                    LocationId = item.LocationId,
                    LocationName = item.LocationName,
                    LocationImageUrl = item.LocationImageUrl,
                    AudioDuration = item.AudioDuration,
                    Progress = (double)item.Progress,
                    ListenedSeconds = item.ListenedSeconds,
                    IsCompleted = item.IsCompleted,
                    LastListenedAt = item.LastListenedAtUtc,
                    ListenedAt = item.LastListenedAtUtc,
                    UserId = "anonymous"
                })
                .ToListAsync();

            return Results.Ok(history);
        });

        group.MapPost("/history", async (MobileAddListeningHistoryRequest request, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.AudioGuideId) || string.IsNullOrWhiteSpace(request.LocationId))
            {
                return Results.BadRequest(new { message = "audioGuideId and locationId are required." });
            }

            var audio = await db.AudioGuides
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == request.AudioGuideId);
            if (audio is null)
            {
                return Results.NotFound(new { message = "Audio guide not found." });
            }

            var location = await db.Locations
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == request.LocationId);
            if (location is null)
            {
                return Results.NotFound(new { message = "Location not found." });
            }

            var progress = Math.Clamp((decimal)request.Progress, 0M, 1M);
            var listenedSeconds = request.ListenedSeconds > 0
                ? request.ListenedSeconds
                : (int)Math.Round(audio.Duration * 60 * (double)progress);
            var nowUtc = DateTime.UtcNow;

            var existing = await db.ListeningHistories.FirstOrDefaultAsync(item => item.AudioGuideId == request.AudioGuideId);
            if (existing is null)
            {
                existing = new ListeningHistory
                {
                    Id = $"hist_{request.AudioGuideId}",
                    AudioGuideId = request.AudioGuideId,
                    LocationId = request.LocationId
                };

                db.ListeningHistories.Add(existing);
            }

            existing.AudioTitle = audio.Title;
            existing.LocationName = location.Name;
            existing.LocationImageUrl = location.ImageUrl;
            existing.AudioDuration = audio.Duration;
            existing.LocationId = request.LocationId;
            existing.Progress = progress;
            existing.ListenedSeconds = listenedSeconds;
            existing.IsCompleted = request.IsCompleted || progress >= 0.999M;
            existing.LastListenedAtUtc = nowUtc;

            await db.SaveChangesAsync();

            return Results.Ok(new MobileListeningHistoryDto
            {
                Id = existing.Id,
                AudioGuideId = existing.AudioGuideId,
                AudioTitle = existing.AudioTitle,
                LocationId = existing.LocationId,
                LocationName = existing.LocationName,
                LocationImageUrl = existing.LocationImageUrl,
                AudioDuration = existing.AudioDuration,
                Progress = (double)existing.Progress,
                ListenedSeconds = existing.ListenedSeconds,
                IsCompleted = existing.IsCompleted,
                LastListenedAt = existing.LastListenedAtUtc,
                ListenedAt = existing.LastListenedAtUtc,
                UserId = "anonymous"
            });
        });

        return app;
    }

    private static MobileLocationDto ToLocationDto(Location location, bool includeAudioGuides, string? language)
    {
        var languageResolution = ResolveLanguageSelection(location.AudioGuides, language);
        var filteredGuides = includeAudioGuides
            ? languageResolution.Guides
            : Array.Empty<AudioGuide>();

        return new MobileLocationDto
        {
            Id = location.Id,
            Name = location.Name,
            Description = location.Description,
            ImageUrl = location.ImageUrl,
            Address = location.Address,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Priority = location.Priority,
            DetectionRadiusMeters = location.DetectionRadiusMeters,
            Duration = location.Duration,
            CategoryId = location.CategoryId,
            CategoryName = location.Category?.Name ?? string.Empty,
            IsFavorite = false,
            ResolvedLanguage = languageResolution.ResolvedLanguage,
            AudioGuides = filteredGuides
                .Select(guide => ToAudioGuideDto(guide, includeSegments: false, languageResolution.ResolvedLanguage))
                .ToList()
        };
    }

    private static LanguageResolution ResolveLanguageSelection(IEnumerable<AudioGuide> guides, string? language)
    {
        var allGuides = guides
            .OrderBy(guide => guide.Title)
            .ThenBy(guide => guide.Id)
            .ToList();

        if (allGuides.Count == 0)
        {
            return new LanguageResolution(Array.Empty<AudioGuide>(), string.Empty);
        }

        var normalizedLanguage = NormalizeLanguage(language);
        if (string.IsNullOrWhiteSpace(normalizedLanguage))
        {
            return new LanguageResolution(allGuides, string.Empty);
        }

        var requestedLanguageGuides = allGuides
            .Where(guide => NormalizeLanguage(guide.Language) == normalizedLanguage)
            .ToList();

        if (requestedLanguageGuides.Count > 0)
        {
            return new LanguageResolution(requestedLanguageGuides, normalizedLanguage);
        }

        if (!string.Equals(normalizedLanguage, "vi", StringComparison.OrdinalIgnoreCase))
        {
            var vietnameseGuides = allGuides
                .Where(guide => NormalizeLanguage(guide.Language) == "vi")
                .ToList();

            if (vietnameseGuides.Count > 0)
            {
                return new LanguageResolution(vietnameseGuides, "vi");
            }
        }

        return new LanguageResolution(allGuides, normalizedLanguage);
    }

    private static string NormalizeLanguage(string? language)
    {
        var normalized = (language ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        var separatorIndex = normalized.IndexOfAny(['-', '_']);
        return separatorIndex > 0 ? normalized[..separatorIndex] : normalized;
    }

    private static MobileAudioGuideDto ToAudioGuideDto(AudioGuide guide, bool includeSegments, string? resolvedLanguage = null)
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
            ResolvedLanguage = string.IsNullOrWhiteSpace(resolvedLanguage)
                ? NormalizeLanguage(guide.Language)
                : resolvedLanguage,
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

    private static bool IsSuccessfulPaymentStatus(string? status)
    {
        return string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveQrToken(MobilePaymentCompletionRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.QrToken))
        {
            return request.QrToken;
        }

        if (!string.IsNullOrWhiteSpace(request.UserAppId))
        {
            return request.UserAppId;
        }

        return request.DeviceId;
    }

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
        public int Priority { get; set; }
        public double DetectionRadiusMeters { get; set; }
        public int Duration { get; set; }
        public string CategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public string ResolvedLanguage { get; set; } = string.Empty;
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
        public string ResolvedLanguage { get; set; } = string.Empty;
        public List<MobileAudioScriptSegmentDto> ScriptSegments { get; set; } = new();
    }

    private sealed record LanguageResolution(IReadOnlyList<AudioGuide> Guides, string ResolvedLanguage);

    private sealed class MobileAudioScriptSegmentDto
    {
        public int Id { get; set; }
        public string AudioGuideId { get; set; } = string.Empty;
        public int StartTimeSeconds { get; set; }
        public int EndTimeSeconds { get; set; }
        public string ScriptText { get; set; } = string.Empty;
    }

    private sealed class MobileListeningHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string AudioGuideId { get; set; } = string.Empty;
        public string AudioTitle { get; set; } = string.Empty;
        public string LocationId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string LocationImageUrl { get; set; } = string.Empty;
        public int AudioDuration { get; set; }
        public double Progress { get; set; }
        public DateTime ListenedAt { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int ListenedSeconds { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime LastListenedAt { get; set; }
    }

    private sealed class MobileHeartbeatRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public string? ActivityName { get; set; }
        public string? ActivityContext { get; set; }
        public string? ScreenName { get; set; }
        public string? Route { get; set; }
        public bool IsForeground { get; set; } = true;
    }

    private sealed class MobileHeartbeatResponse
    {
        public bool Success { get; set; }
        public bool SessionValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string UserAppId { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public string? CurrentActivity { get; set; }
        public DateTime CurrentActivityAtUtc { get; set; }
        public DateTime LastSeenAtUtc { get; set; }
        public DateTime LastValidatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }

    private static string NormalizeHeartbeatActivityName(string? activityName, string? route)
    {
        var name = (activityName ?? route ?? "Shell").Trim();
        return string.IsNullOrWhiteSpace(name) ? "Shell" : name;
    }

    private static string? NormalizeHeartbeatContext(string? activityContext, string? screenName)
    {
        var context = !string.IsNullOrWhiteSpace(activityContext) ? activityContext : screenName;
        if (string.IsNullOrWhiteSpace(context))
        {
            return null;
        }

        return context.Trim();
    }

    private sealed class MobilePaymentPackageDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public int DurationDays { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    private sealed class MobilePaymentCompletionRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public string? QrToken { get; set; }
        public string? UserAppId { get; set; }
        public string? LocationId { get; set; }
        public string? AudioGuideId { get; set; }
        public string? AudioUrl { get; set; }
        public string PackageId { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }
    }

    private sealed class MobilePaymentCompletionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string UserAppId { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public string PackageId { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime LastValidatedAtUtc { get; set; }
    }

    private sealed class SessionValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string UserAppId { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public string? PackageId { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime LastValidatedAtUtc { get; set; }
    }

    private sealed class DeviceSessionCheckResult
    {
        public bool HasSession { get; set; }
        public string Message { get; set; } = string.Empty;
        public string UserAppId { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public string? PackageId { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime LastValidatedAtUtc { get; set; }
    }

    private sealed class MobileAddListeningHistoryRequest
    {
        public string AudioGuideId { get; set; } = string.Empty;
        public string LocationId { get; set; } = string.Empty;
        public double Progress { get; set; }
        public int ListenedSeconds { get; set; }
        public bool IsCompleted { get; set; }
    }
}
