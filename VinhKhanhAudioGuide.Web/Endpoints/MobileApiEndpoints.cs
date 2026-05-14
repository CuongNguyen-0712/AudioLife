using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;
using VinhKhanhAudioGuide.Web.Filters;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using VinhKhanhAudioGuide.Web.Extensions;


namespace VinhKhanhAudioGuide.Web.Endpoints;

public static class MobileApiEndpoints
{
    // Fix: Static types cannot be used as type arguments.
    // We use this private class as a marker for ILogger category.
    public class MobileApiLogger { }

    // Đăng ký toàn bộ endpoint cho mobile app (health, payment, session, heartbeat, catalog).
    // Đây là entry point chính của flow API mobile phía server.
    public static IEndpointRouteBuilder MapMobileApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mobile")
            .RequireAuthorization("MobileApi")
            .RequireRateLimiting("fixed-mobile");

        group.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

        group.MapGet("/payment/packages", async (AppDbContext db, IMemoryCache cache) =>
        {
            var cacheKey = "MobileApi_PaymentPackages";
            if (!cache.TryGetValue(cacheKey, out List<MobilePaymentPackageDto>? packages))
            {
                packages = await db.PaymentPackages
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

                cache.Set(cacheKey, packages, TimeSpan.FromHours(1));
            }

            return Results.Ok(packages);
        });

        // Xử lý callback thanh toán: tạo/cập nhật user, subscription và session cho thiết bị.
        group.MapPost("/payment/complete", async (MobilePaymentCompletionRequest request, AppDbContext db, IJwtTokenService jwtService) =>
        {
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
            
            // Identify machine by DeviceId
            var user = await db.AppUsers.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.DeviceId == request.DeviceId);
            if (user is null)
            {
                // Generate a random token as the machine's primary identity token (stored in QrCodeValue)
                var randomToken = Guid.NewGuid().ToString("N");
                user = new AppUser
                {
                    DeviceId = request.DeviceId,
                    QrCodeValue = randomToken,
                    Status = "Active",
                    CreatedAtUtc = nowUtc,
                    LastSeenAtUtc = nowUtc
                };

                db.AppUsers.Add(user);
                await db.SaveChangesAsync();
            }
            else if (user.IsDeleted)
            {
                user.IsDeleted = false;
                user.Status = "Active";
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

            // Handle session (Ensure only one active session per device)
            // 1. Deactivate any existing active sessions for this device that aren't the one we are about to use
            var existingSessions = await db.UserAppSessions
                .Where(item => item.DeviceId == request.DeviceId && item.IsActive)
                .ToListAsync();

            UserAppSession? session = existingSessions.FirstOrDefault(s => s.TokenValue == sessionToken || s.UserId == user.Id);

            foreach (var s in existingSessions)
            {
                if (session == null || s.Id != session.Id)
                {
                    s.IsActive = false;
                    s.RevokedAtUtc = nowUtc;
                }
            }

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
                // Reuse and update existing session for this device/user
                session.UserId = user.Id;
                session.TokenValue = sessionToken;
                session.RefreshToken = request.RefreshToken;
                session.LastValidatedAtUtc = nowUtc;
                session.IsActive = isPaid;
                session.RevokedAtUtc = isPaid ? null : nowUtc;
                session.ExpiresAtUtc = isPaid ? nowUtc.AddDays(Math.Max(package.DurationDays, 1)) : nowUtc.AddMinutes(5);
            }

            await db.SaveChangesAsync();

            var accessToken = isPaid 
                ? jwtService.GenerateToken(user.Id.ToString(), user.DeviceId, "MobileUser") 
                : null;

            return Results.Ok(new MobilePaymentCompletionResponse
            {
                Success = isPaid,
                Message = isPaid ? "Thanh toán đã được xác nhận." : "Thanh toán đang chờ xử lý.",
                SessionToken = sessionToken,
                AccessToken = accessToken,
                RefreshToken = request.RefreshToken,
                PackageId = package.Id,
                PaymentStatus = subscription.Status,
                PaymentReference = request.PaymentReference,
                ExpiresAtUtc = session.ExpiresAtUtc,
                LastValidatedAtUtc = nowUtc
            });
        }).AllowAnonymous()
          .AddEndpointFilter<ValidationFilter<MobilePaymentCompletionRequest>>();


        // Validate session token theo device để quyết định user có được vào app hay không.
        group.MapGet("/session/validate", async (string sessionToken, string deviceId, AppDbContext db, IJwtTokenService jwtService) =>
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

            var accessToken = activeSubscription is not null
                ? jwtService.GenerateToken(session.UserId.ToString(), session.DeviceId, "MobileUser")
                : null;

            return Results.Ok(new SessionValidationResult
            {
                IsValid = activeSubscription is not null,
                Message = activeSubscription is null ? "Không tìm thấy gói hoạt động." : "Phiên hợp lệ.",
                UserAppId = session.UserId.ToString(),
                SessionToken = session.TokenValue,
                AccessToken = accessToken,
                RefreshToken = session.RefreshToken,
                PackageId = activeSubscription?.PackageId,
                PaymentStatus = activeSubscription?.Status,
                ExpiresAtUtc = session.ExpiresAtUtc,
                LastValidatedAtUtc = DateTime.UtcNow
            });
        }).AllowAnonymous();

        // Kiểm tra nhanh session hiện tại của thiết bị, dùng ở startup/QR onboarding.
        group.MapGet("/session/by-device", async (string deviceId, AppDbContext db, IJwtTokenService jwtService) =>
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

            // Cleanup is now handled by SessionCleanupBackgroundService


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

            var accessToken = activeSubscription is not null
                ? jwtService.GenerateToken(session.UserId.ToString(), session.DeviceId, "MobileUser")
                : null;

            return Results.Ok(new DeviceSessionCheckResult
            {
                HasSession = true,
                Message = "Đã tìm thấy phiên thiết bị.",
                UserAppId = session.UserId.ToString(),
                SessionToken = session.TokenValue,
                AccessToken = accessToken,
                RefreshToken = session.RefreshToken,
                PackageId = activeSubscription?.PackageId,
                PaymentStatus = activeSubscription?.Status,
                ExpiresAtUtc = session.ExpiresAtUtc,
                LastValidatedAtUtc = session.LastValidatedAtUtc ?? nowUtc
            });
        }).AllowAnonymous();

        // Refresh JWT token dùng RefreshToken — không cần thanh toán lại.
        group.MapPost("/session/refresh", async (SessionRefreshRequest? request, AppDbContext db, IJwtTokenService jwtService, ILogger<MobileApiLogger> logger) =>
        {
            try 
            {
                if (request == null)
                {
                    return Results.BadRequest(new SessionValidationResult
                    {
                        IsValid = false,
                        Message = "Dữ liệu yêu cầu không hợp lệ."
                    });
                }
                logger.LogInformation("Refreshing session for DeviceId: {DeviceId}", request.DeviceId);

                var session = await db.UserAppSessions
                    .FirstOrDefaultAsync(s => s.DeviceId == request.DeviceId
                                           && s.RefreshToken == request.RefreshToken
                                           && s.IsActive
                                           && s.RevokedAtUtc == null);

                if (session is null)
                {
                    logger.LogWarning("Session not found or inactive for RefreshToken in DeviceId: {DeviceId}", request.DeviceId);
                    return Results.Ok(new SessionValidationResult
                    {
                        IsValid = false,
                        Message = "Phiên không tồn tại hoặc đã bị thu hồi.",
                        LastValidatedAtUtc = DateTime.UtcNow
                    });
                }

                if (session.ExpiresAtUtc <= DateTime.UtcNow)
                {
                    logger.LogWarning("Expired RefreshToken for DeviceId: {DeviceId}", request.DeviceId);
                    return Results.Ok(new SessionValidationResult
                    {
                        IsValid = false,
                        Message = "Phiên đã hết hạn.",
                        UserAppId = session.UserId.ToString(),
                        ExpiresAtUtc = session.ExpiresAtUtc,
                        LastValidatedAtUtc = DateTime.UtcNow
                    });
                }

                var activeSubscription = await db.UserSubscriptions
                    .AsNoTracking()
                    .Where(s => s.UserId == session.UserId && s.Status == "Active")
                    .OrderByDescending(s => s.ExpiresAtUtc)
                    .FirstOrDefaultAsync();

                if (activeSubscription is null)
                {
                    logger.LogWarning("No active subscription for UserId: {UserId} during refresh", session.UserId);
                    return Results.Ok(new SessionValidationResult
                    {
                        IsValid = false,
                        Message = "Gói dịch vụ đã hết hạn hoặc không hợp lệ.",
                        UserAppId = session.UserId.ToString(),
                        SessionToken = session.TokenValue,
                        ExpiresAtUtc = session.ExpiresAtUtc,
                        LastValidatedAtUtc = DateTime.UtcNow
                    });
                }

                // Issue a fresh JWT — rotate RefreshToken for security
                var newAccessToken = jwtService.GenerateToken(session.UserId.ToString(), session.DeviceId, "MobileUser");
                var newRefreshToken = Guid.NewGuid().ToString("N");

                session.RefreshToken = newRefreshToken;
                session.LastValidatedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync();

                logger.LogInformation("Successfully refreshed session for UserId: {UserId}", session.UserId);

                return Results.Ok(new SessionValidationResult
                {
                    IsValid = true,
                    Message = "Token đã được làm mới.",
                    UserAppId = session.UserId.ToString(),
                    SessionToken = session.TokenValue,
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    PackageId = activeSubscription.PackageId,
                    PaymentStatus = activeSubscription.Status,
                    ExpiresAtUtc = session.ExpiresAtUtc,
                    LastValidatedAtUtc = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                // Safety check for logger to avoid secondary exception
                if (logger != null)
                {
                    logger.LogError(ex, "Error refreshing session for DeviceId: {DeviceId}", request?.DeviceId ?? "Unknown");
                }
                return Results.Problem("Có lỗi xảy ra trong quá trình làm mới phiên.");
            }
        }).AllowAnonymous()
          .AddEndpointFilter<ValidationFilter<SessionRefreshRequest>>();

        // Đăng ký token thiết bị cho thông báo đẩy (Push Notifications)
        group.MapPost("/devices/register", async (MobileDeviceRegistrationRequest request, INotificationService notificationService, ClaimsPrincipal user) =>
        {
            var userId = user.GetUserId();
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var success = await notificationService.RegisterDeviceTokenAsync(userId.Value, request.DeviceId, request.DeviceToken, request.Platform);
            
            return success ? Results.Ok(new { success = true }) : Results.Problem("Lỗi đăng ký token thiết bị.");
        }).AddEndpointFilter<ValidationFilter<MobileDeviceRegistrationRequest>>();

        // Nhận heartbeat định kỳ để keep-alive session và lưu activity user.
        group.MapPost("/heartbeat", async (MobileHeartbeatRequest request, AppDbContext db) =>
        {
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
            // session.ExpiresAtUtc = nowUtc.AddMinutes(30); // BUG: Don't shorten session if it's a long-lived paid session
            if (session.ExpiresAtUtc < nowUtc.AddMinutes(30))
            {
                session.ExpiresAtUtc = nowUtc.AddMinutes(30);
            }
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
        }).AddEndpointFilter<ValidationFilter<MobileHeartbeatRequest>>();


        group.MapGet("/categories", async (AppDbContext db, IMemoryCache cache) =>
        {
            var cacheKey = "MobileApi_Categories";
            if (!cache.TryGetValue(cacheKey, out List<MobileCategoryDto>? categories))
            {
                categories = await db.Categories
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

                cache.Set(cacheKey, categories, TimeSpan.FromMinutes(10));
            }

            return Results.Ok(categories);
        });

        group.MapGet("/locations", async (string? language, AppDbContext db, IMemoryCache cache) =>
        {
            var cacheKey = $"MobileApi_Locations_{language ?? "default"}";
            if (!cache.TryGetValue(cacheKey, out List<MobileLocationDto>? result))
            {
                var locations = await db.Locations
                    .AsNoTracking()
                    .Include(l => l.Category)
                    .Include(l => l.AudioGuides)
                    .Include(l => l.Reviews)
                    .OrderBy(l => l.Name)
                    .ToListAsync();

                result = locations.Select(l => ToLocationDto(l, includeAudioGuides: true, language)).ToList();
                cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            }

            return Results.Ok(result);
        });

        group.MapGet("/locations/{id}", async (string id, string? language, AppDbContext db, IMemoryCache cache) =>
        {
            var cacheKey = $"MobileApi_Location_{id}_{language ?? "default"}";
            if (!cache.TryGetValue(cacheKey, out MobileLocationDto? result))
            {
                var location = await db.Locations
                    .AsNoTracking()
                    .Include(l => l.Category)
                    .Include(l => l.AudioGuides)
                    .Include(l => l.Reviews)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (location is null) return Results.NotFound();

                result = ToLocationDto(location, includeAudioGuides: true, language);
                cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            }

            return Results.Ok(result);
        });

        group.MapGet("/locations/{id}/reviews", async (string id, AppDbContext db) =>
        {
            var reviews = await db.LocationReviews
                .AsNoTracking()
                .Where(r => r.LocationId == id && r.Status == ReviewStatus.Approved)
                .OrderByDescending(r => r.CreatedAtUtc)
                .Select(r => new MobileLocationReviewDto
                {
                    Id = r.Id,
                    LocationId = r.LocationId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAtUtc
                })
                .ToListAsync();

            return Results.Ok(reviews);
        }).AllowAnonymous();

        group.MapPost("/locations/{id}/reviews", async (string id, MobileSubmitReviewRequest request, ClaimsPrincipal user, AppDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            // Check if location exists
            var locationExists = await db.Locations.AnyAsync(l => l.Id == id);
            if (!locationExists) return Results.NotFound();

            var review = new LocationReview
            {
                Id = Guid.NewGuid(),
                LocationId = id,
                UserId = userId,
                
                Rating = request.Rating,
                Comment = request.Comment,
                Status = ReviewStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.LocationReviews.Add(review);
            await db.SaveChangesAsync();

            return Results.Ok(new { success = true, message = "Đánh giá của bạn đã được gửi và đang chờ duyệt." });
        }).AddEndpointFilter<ValidationFilter<MobileSubmitReviewRequest>>();

        group.MapGet("/locations/by-category/{categoryId}", async (string categoryId, string? language, AppDbContext db, IMemoryCache cache) =>
        {
            var cacheKey = $"MobileApi_LocationsByCategory_{categoryId}_{language ?? "default"}";
            if (!cache.TryGetValue(cacheKey, out List<MobileLocationDto>? result))
            {
                var locations = await db.Locations
                    .AsNoTracking()
                    .Include(l => l.Category)
                    .Include(l => l.AudioGuides)
                    .Include(l => l.Reviews)
                    .Where(l => l.CategoryId == categoryId)
                    .OrderBy(l => l.Name)
                    .ToListAsync();

                result = locations.Select(l => ToLocationDto(l, includeAudioGuides: true, language)).ToList();
                cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            }

            return Results.Ok(result);
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
                .Include(l => l.Reviews)
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
                .Include(l => l.Reviews)
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

        group.MapGet("/tours", async (AppDbContext db, IMemoryCache cache) =>
        {
            var cacheKey = "MobileApi_Tours";
            if (!cache.TryGetValue(cacheKey, out List<MobileTourDto>? result))
            {
                var tours = await db.Tours
                    .AsNoTracking()
                    .Include(t => t.TourLocations)
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                result = tours.Select(ToTourDto).ToList();
                cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
            }

            return Results.Ok(result);
        });

        group.MapGet("/tours/{id}", async (string id, AppDbContext db, IMemoryCache cache) =>
        {
            var cacheKey = $"MobileApi_Tour_{id}";
            if (!cache.TryGetValue(cacheKey, out MobileTourDto? result))
            {
                var tour = await db.Tours
                    .AsNoTracking()
                    .Include(t => t.TourLocations)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tour is null) return Results.NotFound();

                result = ToTourDto(tour);
                cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
            }

            return Results.Ok(result);
        });

        group.MapGet("/tours/featured", async (AppDbContext db, IMemoryCache cache) =>
        {
            var cacheKey = "MobileApi_FeaturedTours";
            if (!cache.TryGetValue(cacheKey, out List<MobileTourDto>? result))
            {
                var tours = await db.Tours
                    .AsNoTracking()
                    .Include(t => t.TourLocations)
                    .Where(t => t.IsFeatured)
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                result = tours.Select(ToTourDto).ToList();
                cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
            }

            return Results.Ok(result);
        });

        group.MapGet("/audio/by-location/{locationId}", async (string locationId, string? language, AppDbContext db, IMemoryCache cache) =>
        {
            var cacheKey = $"MobileApi_AudioByLocation_{locationId}_{language ?? "default"}";
            if (!cache.TryGetValue(cacheKey, out List<MobileAudioGuideDto>? result))
            {
                var allGuides = await db.AudioGuides
                    .AsNoTracking()
                    .Include(a => a.ScriptSegments)
                    .Where(a => a.LocationId == locationId)
                    .OrderBy(a => a.Title)
                    .ToListAsync();

                var languageResolution = ResolveLanguageSelection(allGuides, language);
                var guides = languageResolution.Guides;

                result = guides.Select(a => ToAudioGuideDto(a, includeSegments: true, languageResolution.ResolvedLanguage)).ToList();
                cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            }

            return Results.Ok(result);
        });

        group.MapGet("/audio/{id}", async (string id, AppDbContext db, IMemoryCache cache) =>
        {
            var cacheKey = $"MobileApi_Audio_{id}";
            if (!cache.TryGetValue(cacheKey, out MobileAudioGuideDto? result))
            {
                var guide = await db.AudioGuides
                    .AsNoTracking()
                    .Include(a => a.ScriptSegments)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (guide is null) return Results.NotFound();

                result = ToAudioGuideDto(guide, includeSegments: true);
                cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
            }

            return Results.Ok(result);
        });


        group.MapPost("/reviews", async (MobileSubmitReviewRequest request, AppDbContext db, ClaimsPrincipal user) =>
        {
            var userId = user.GetUserId();

            var review = new LocationReview
            {
                Id = Guid.NewGuid(),
                LocationId = request.LocationId,
                UserId = userId,
                
                Rating = Math.Clamp(request.Rating, 1, 5),
                Comment = request.Comment,
                Status = ReviewStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.LocationReviews.Add(review);
            await db.SaveChangesAsync();

            return Results.Ok(new { success = true, message = "Cảm ơn bạn đã gửi đánh giá. Đánh giá của bạn đang chờ kiểm duyệt." });
        }).AddEndpointFilter<ValidationFilter<MobileSubmitReviewRequest>>();

        group.MapGet("/history", async (int? take, AppDbContext db, ClaimsPrincipal user) =>
        {
            var userId = user.GetUserId();
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            var maxItems = Math.Clamp(take.GetValueOrDefault(50), 1, 200);
            var history = await db.ListeningHistories
                .AsNoTracking()
                .Where(item => item.UserId == userId.Value)
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
                    UserId = userId.Value.ToString()
                })
                .ToListAsync();

            return Results.Ok(history);
        });

        group.MapPost("/history", async (MobileAddListeningHistoryRequest request, AppDbContext db, ClaimsPrincipal user) =>
        {
            var userId = user.GetUserId();
            if (userId == null)
            {
                return Results.Unauthorized();
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

            var existing = await db.ListeningHistories
                .FirstOrDefaultAsync(item => item.AudioGuideId == request.AudioGuideId && item.UserId == userId.Value);
            
            if (existing is null)
            {
                existing = new ListeningHistory
                {
                    Id = $"hist_{userId.Value}_{request.AudioGuideId}",
                    UserId = userId.Value,
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
                UserId = userId.Value.ToString()
            });
        }).AddEndpointFilter<ValidationFilter<MobileAddListeningHistoryRequest>>();


        return app;
    }

    private static MobileLocationDto ToLocationDto(Location location, bool includeAudioGuides, string? language)
    {
        var languageResolution = ResolveLanguageSelection(location.AudioGuides, language);
        var filteredGuides = includeAudioGuides
            ? languageResolution.Guides
            : Array.Empty<AudioGuide>();

        var approvedReviews = location.Reviews.Where(r => r.Status == ReviewStatus.Approved).ToList();
        var avgRating = approvedReviews.Any() ? approvedReviews.Average(r => r.Rating) : 0;

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
            AverageRating = Math.Round(avgRating, 1),
            ReviewCount = approvedReviews.Count,
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


    private static string NormalizeHeartbeatActivityName(string? activityName, string? route)
    {
        if (!string.IsNullOrWhiteSpace(activityName)) return activityName;
        if (!string.IsNullOrWhiteSpace(route)) return $"Route: {route}";
        return "Idle";
    }

    private static string NormalizeHeartbeatContext(string? context, string? screenName)
    {
        if (!string.IsNullOrWhiteSpace(context)) return context;
        if (!string.IsNullOrWhiteSpace(screenName)) return screenName;
        return "N/A";
    }
}

internal record LanguageResolution(IEnumerable<AudioGuide> Guides, string ResolvedLanguage);
