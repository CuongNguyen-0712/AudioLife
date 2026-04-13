using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VinhKhanhAudioGuide.Web.Configuration;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Services;

public interface IPoiAdminAssignmentService
{
    Task<PoiAdminAssignmentUpdateResult> UpdateAssignmentsAsync(
        string username,
        IEnumerable<string>? requestedLocationIds,
        CancellationToken cancellationToken = default);
}

public sealed class PoiAdminAssignmentUpdateResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> AssignedLocationIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TransferredLocationIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> InvalidLocationIds { get; init; } = Array.Empty<string>();
}

public class PoiAdminAssignmentService : IPoiAdminAssignmentService
{
    private readonly AppDbContext _db;
    private readonly AuthOptions _authOptions;

    public PoiAdminAssignmentService(AppDbContext db, IOptions<AuthOptions> authOptions)
    {
        _db = db;
        _authOptions = authOptions.Value;
    }

    public async Task<PoiAdminAssignmentUpdateResult> UpdateAssignmentsAsync(
        string username,
        IEnumerable<string>? requestedLocationIds,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return new PoiAdminAssignmentUpdateResult
            {
                Success = false,
                ErrorMessage = "Không xác định được tài khoản cần cập nhật."
            };
        }

        var normalizedUsername = username.Trim();
        var poiAdminExistsInConfig = _authOptions.Users.Any(user =>
            string.Equals(user.Username, normalizedUsername, StringComparison.OrdinalIgnoreCase)
            && string.Equals(user.Role, RoleNames.PoiAdmin, StringComparison.OrdinalIgnoreCase));

        var poiAdminExistsInDb = await _db.AuthUserAccounts
            .AsNoTracking()
            .AnyAsync(user => user.IsActive
                           && user.Role == RoleNames.PoiAdmin
                           && user.Username == normalizedUsername,
                      cancellationToken);

        if (!poiAdminExistsInConfig && !poiAdminExistsInDb)
        {
            return new PoiAdminAssignmentUpdateResult
            {
                Success = false,
                ErrorMessage = "Tài khoản không thuộc vai trò Admin POI."
            };
        }

        var requested = (requestedLocationIds ?? Enumerable.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var allowedLocationIds = await _db.Locations
            .AsNoTracking()
            .Select(location => location.Id)
            .ToListAsync(cancellationToken);

        var allowedSet = allowedLocationIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var invalidLocationIds = requested
            .Where(id => !allowedSet.Contains(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var candidates = requested
            .Where(id => allowedSet.Contains(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var transferredLocationIds = await _db.PoiAdminLocationAssignments
            .AsNoTracking()
            .Where(item => item.Username != normalizedUsername && candidates.Contains(item.LocationId))
            .Select(item => item.LocationId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var finalAssignments = candidates;

        var existing = await _db.PoiAdminLocationAssignments
            .Where(item => item.Username == normalizedUsername)
            .ToListAsync(cancellationToken);

        var existingFromOthers = await _db.PoiAdminLocationAssignments
            .Where(item => item.Username != normalizedUsername && finalAssignments.Contains(item.LocationId))
            .ToListAsync(cancellationToken);

        _db.PoiAdminLocationAssignments.RemoveRange(existing);
        _db.PoiAdminLocationAssignments.RemoveRange(existingFromOthers);

        foreach (var locationId in finalAssignments)
        {
            _db.PoiAdminLocationAssignments.Add(new PoiAdminLocationAssignment
            {
                Username = normalizedUsername,
                LocationId = locationId
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new PoiAdminAssignmentUpdateResult
        {
            Success = true,
            AssignedLocationIds = finalAssignments,
            TransferredLocationIds = transferredLocationIds,
            InvalidLocationIds = invalidLocationIds
        };
    }
}
