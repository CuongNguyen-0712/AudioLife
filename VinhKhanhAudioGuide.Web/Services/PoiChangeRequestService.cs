using System.Text.Json;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Services;

public class PoiChangeSet
{
    public Dictionary<string, string?> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public interface IPoiChangeRequestService
{
    Task<IReadOnlyList<PoiChangeRequest>> GetAllAsync();
    Task<IReadOnlyList<PoiChangeRequest>> GetBySubmitterAsync(string username);
    Task<IReadOnlyList<PoiChangeRequest>> GetBySubmitterAliasesAsync(IEnumerable<string?> aliases);
    Task<PoiChangeRequest> SubmitAsync(PoiChangeRequest request);
    Task<bool> TryUpdateStatusAsync(Guid id, PoiChangeRequestStatus status, string updatedBy, string? reviewNote = null);
}

public class DbPoiChangeRequestService : IPoiChangeRequestService
{
    private const string ChangeActionField = "__action";
    private const string TtsOnApprovalField = "__tts_on_approval";
    private const string CreateLocationAction = "create-location";
    private const string CreateAudioGuideAction = "create-audio-guide";
    private const string DeleteLocationAction = "delete-location";
    private const string DeleteAudioGuideAction = "delete-audio-guide";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly AppDbContext _db;
    private readonly IAudioStorageService _audioStorageService;
    private readonly ITextToSpeechService _ttsService;

    public DbPoiChangeRequestService(
        AppDbContext db,
        IAudioStorageService audioStorageService,
        ITextToSpeechService ttsService)
    {
        _db = db;
        _audioStorageService = audioStorageService;
        _ttsService = ttsService;
    }

    public async Task<IReadOnlyList<PoiChangeRequest>> GetAllAsync()
    {
        return await _db.PoiChangeRequests
            .AsNoTracking()
            .OrderByDescending(item => item.SubmittedAtUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PoiChangeRequest>> GetBySubmitterAsync(string username)
    {
        var normalized = NormalizeIdentity(username);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<PoiChangeRequest>();
        }

        return await _db.PoiChangeRequests
            .AsNoTracking()
            .Where(item => item.SubmittedByUsername == normalized || item.SubmittedByName == normalized)
            .OrderByDescending(item => item.SubmittedAtUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PoiChangeRequest>> GetBySubmitterAliasesAsync(IEnumerable<string?> aliases)
    {
        var normalizedAliases = aliases
            .Select(NormalizeIdentity)
            .Where(alias => !string.IsNullOrWhiteSpace(alias))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedAliases.Count == 0)
        {
            return Array.Empty<PoiChangeRequest>();
        }

        return await _db.PoiChangeRequests
            .AsNoTracking()
            .Where(item => normalizedAliases.Contains(item.SubmittedByUsername) || normalizedAliases.Contains(item.SubmittedByName))
            .OrderByDescending(item => item.SubmittedAtUtc)
            .ToListAsync();
    }

    public async Task<PoiChangeRequest> SubmitAsync(PoiChangeRequest request)
    {
        request.SubmittedByUsername = NormalizeIdentity(request.SubmittedByUsername);
        request.SubmittedByName = NormalizeIdentity(request.SubmittedByName);

        if (string.IsNullOrWhiteSpace(request.SubmittedByUsername) && !string.IsNullOrWhiteSpace(request.SubmittedByName))
        {
            request.SubmittedByUsername = request.SubmittedByName;
        }

        if (string.IsNullOrWhiteSpace(request.SubmittedByName) && !string.IsNullOrWhiteSpace(request.SubmittedByUsername))
        {
            request.SubmittedByName = request.SubmittedByUsername;
        }

        request.Id = Guid.NewGuid();
        request.SubmittedAtUtc = DateTime.UtcNow;
        request.Status = PoiChangeRequestStatus.Pending;

        _db.PoiChangeRequests.Add(request);
        await _db.SaveChangesAsync();
        return request;
    }

    public async Task<bool> TryUpdateStatusAsync(Guid id, PoiChangeRequestStatus status, string updatedBy, string? reviewNote = null)
    {
        var item = await _db.PoiChangeRequests.FirstOrDefaultAsync(request => request.Id == id);
        if (item is null)
        {
            return false;
        }

        if (!CanTransition(item.Status, status))
        {
            return false;
        }

        if (status == PoiChangeRequestStatus.Approved && item.Status != PoiChangeRequestStatus.Approved)
        {
            var applied = await TryApplyChangeSetAsync(item);
            if (!applied)
            {
                return false;
            }
        }

        item.Status = status;
        item.UpdatedAtUtc = DateTime.UtcNow;
        item.UpdatedBy = updatedBy;
        item.ReviewNote = reviewNote;

        await _db.SaveChangesAsync();
        return true;
    }

    private static bool CanTransition(PoiChangeRequestStatus current, PoiChangeRequestStatus next)
    {
        if (current == next)
        {
            return false;
        }

        return current switch
        {
            PoiChangeRequestStatus.Pending => next is PoiChangeRequestStatus.InReview or PoiChangeRequestStatus.Approved or PoiChangeRequestStatus.Rejected,
            PoiChangeRequestStatus.InReview => next is PoiChangeRequestStatus.Approved or PoiChangeRequestStatus.Rejected,
            _ => false
        };
    }

    private async Task<bool> TryApplyChangeSetAsync(PoiChangeRequest request)
    {
        var changeSet = ParseChangeSet(request.ChangeSetJson);

        if (changeSet is null || changeSet.Fields.Count == 0)
        {
            return false;
        }

        switch (request.TargetType)
        {
            case PoiChangeTargetType.Location:
                return await ApplyLocationChangesAsync(request, changeSet);
            case PoiChangeTargetType.AudioGuide:
                return await ApplyAudioGuideChangesAsync(request, changeSet);
            default:
                return false;
        }
    }

    private static PoiChangeSet? ParseChangeSet(string? changeSetJson)
    {
        if (string.IsNullOrWhiteSpace(changeSetJson))
        {
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<PoiChangeSet>(changeSetJson, JsonOptions);
            if (parsed is not null && parsed.Fields.Count > 0)
            {
                return parsed;
            }
        }
        catch
        {
            // Fallback to legacy shape.
        }

        try
        {
            using var document = JsonDocument.Parse(changeSetJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            var source = document.RootElement;
            if (document.RootElement.TryGetProperty("fields", out var fieldsNode) && fieldsNode.ValueKind == JsonValueKind.Object)
            {
                source = fieldsNode;
            }
            else if (document.RootElement.TryGetProperty("Fields", out var fieldsNodePascal) && fieldsNodePascal.ValueKind == JsonValueKind.Object)
            {
                source = fieldsNodePascal;
            }

            foreach (var property in source.EnumerateObject())
            {
                fields[property.Name] = ToStringValue(property.Value);
            }

            return fields.Count == 0
                ? null
                : new PoiChangeSet { Fields = fields };
        }
        catch
        {
            return null;
        }
    }

    private static string? ToStringValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => value.GetRawText()
        };
    }

    private async Task<bool> ApplyLocationChangesAsync(PoiChangeRequest request, PoiChangeSet changeSet)
    {
        changeSet.Fields.TryGetValue(ChangeActionField, out var actionValue);
        var isCreateAction = string.Equals(actionValue, CreateLocationAction, StringComparison.OrdinalIgnoreCase);
        var isDeleteAction = string.Equals(actionValue, DeleteLocationAction, StringComparison.OrdinalIgnoreCase);

        var location = await _db.Locations.FirstOrDefaultAsync(item => item.Id == request.TargetEntityId);

        if (isCreateAction)
        {
            if (location is not null)
            {
                return false;
            }

            location = new Location
            {
                Id = request.TargetEntityId
            };

            ApplyLocationFieldChanges(location, changeSet);

            if (string.IsNullOrWhiteSpace(location.Name))
            {
                return false;
            }

            if (!await EnsureCategoryExistsAsync(location.CategoryId))
            {
                return false;
            }

            _db.Locations.Add(location);

            var assignmentAdded = await EnsurePoiAdminOwnsLocationAsync(request, location.Id);
            if (!assignmentAdded)
            {
                return false;
            }

            return true;
        }

        if (isDeleteAction)
        {
            if (location is null)
            {
                return false;
            }

            var audioIds = await _db.AudioGuides
                .AsNoTracking()
                .Where(item => item.LocationId == location.Id)
                .Select(item => item.Id)
                .ToListAsync();

            var hasListeningHistory = await _db.ListeningHistories
                .AsNoTracking()
                .AnyAsync(item => item.LocationId == location.Id || audioIds.Contains(item.AudioGuideId));

            if (hasListeningHistory)
            {
                return false;
            }

            var assignments = await _db.PoiAdminLocationAssignments
                .Where(item => item.LocationId == location.Id)
                .ToListAsync();

            if (assignments.Count > 0)
            {
                _db.PoiAdminLocationAssignments.RemoveRange(assignments);
            }

            _db.Locations.Remove(location);
            return true;
        }

        if (location is null)
        {
            return false;
        }

        if (!string.Equals(location.Id, request.LocationId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        ApplyLocationFieldChanges(location, changeSet);

        if (!await EnsureCategoryExistsAsync(location.CategoryId))
        {
            return false;
        }

        return true;
    }

    private static void ApplyLocationFieldChanges(Location location, PoiChangeSet changeSet)
    {
        if (changeSet.Fields.TryGetValue(nameof(Location.Name), out var name) && !string.IsNullOrWhiteSpace(name))
        {
            location.Name = name.Trim();
        }

        if (changeSet.Fields.TryGetValue(nameof(Location.Description), out var description) && description is not null)
        {
            location.Description = description.Trim();
        }

        if (changeSet.Fields.TryGetValue(nameof(Location.Address), out var address) && address is not null)
        {
            location.Address = address.Trim();
        }

        if (changeSet.Fields.TryGetValue(nameof(Location.ImageUrl), out var imageUrl) && imageUrl is not null)
        {
            location.ImageUrl = imageUrl.Trim();
        }

        if (changeSet.Fields.TryGetValue(nameof(Location.CategoryId), out var categoryId) && !string.IsNullOrWhiteSpace(categoryId))
        {
            location.CategoryId = categoryId.Trim();
        }

        if (changeSet.Fields.TryGetValue(nameof(Location.Latitude), out var latText)
            && TryParseDouble(latText, out var latitude))
        {
            location.Latitude = latitude;
        }

        if (changeSet.Fields.TryGetValue(nameof(Location.Longitude), out var lngText)
            && TryParseDouble(lngText, out var longitude))
        {
            location.Longitude = longitude;
        }

        if (changeSet.Fields.TryGetValue(nameof(Location.Duration), out var durationText)
            && int.TryParse(durationText, out var duration))
        {
            location.Duration = Math.Max(0, duration);
        }
    }

    private async Task<bool> EnsureCategoryExistsAsync(string? categoryId)
    {
        var normalized = categoryId?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return await _db.Categories
            .AsNoTracking()
            .AnyAsync(item => item.Id == normalized);
    }

    private async Task<bool> EnsurePoiAdminOwnsLocationAsync(PoiChangeRequest request, string locationId)
    {
        var submitter = await ResolvePoiAdminUsernameAsync(request);
        if (string.IsNullOrWhiteSpace(submitter))
        {
            return false;
        }

        var exists = await _db.PoiAdminLocationAssignments
            .AsNoTracking()
            .AnyAsync(item => item.Username == submitter && item.LocationId == locationId);

        if (!exists)
        {
            _db.PoiAdminLocationAssignments.Add(new PoiAdminLocationAssignment
            {
                Username = submitter,
                LocationId = locationId
            });
        }

        return true;
    }

    private async Task<string?> ResolvePoiAdminUsernameAsync(PoiChangeRequest request)
    {
        var candidates = new[]
        {
            NormalizeIdentity(request.SubmittedByUsername),
            NormalizeIdentity(request.SubmittedByName)
        }
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        var byUsername = await _db.AuthUserAccounts
            .AsNoTracking()
            .Where(user => user.IsActive
                           && user.Role == RoleNames.PoiAdmin
                           && candidates.Contains(user.Username))
            .Select(user => user.Username)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(byUsername))
        {
            return byUsername.Trim();
        }

        var byDisplayName = await _db.AuthUserAccounts
            .AsNoTracking()
            .Where(user => user.IsActive
                           && user.Role == RoleNames.PoiAdmin
                           && candidates.Contains(user.DisplayName))
            .Select(user => user.Username)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(byDisplayName))
        {
            return byDisplayName.Trim();
        }

        return candidates[0];
    }

    private async Task<bool> ApplyAudioGuideChangesAsync(PoiChangeRequest request, PoiChangeSet changeSet)
    {
        changeSet.Fields.TryGetValue(ChangeActionField, out var actionValue);
        changeSet.Fields.TryGetValue(TtsOnApprovalField, out var ttsOnApprovalValue);
        var isCreateAction = string.Equals(actionValue, CreateAudioGuideAction, StringComparison.OrdinalIgnoreCase);
        var isDeleteAction = string.Equals(actionValue, DeleteAudioGuideAction, StringComparison.OrdinalIgnoreCase);
        var shouldGenerateTtsOnApproval = bool.TryParse(ttsOnApprovalValue, out var parseResult) && parseResult;

        var audio = await _db.AudioGuides.FirstOrDefaultAsync(item => item.Id == request.TargetEntityId);

        if (isCreateAction)
        {
            if (audio is not null)
            {
                return false;
            }

            var locationExists = await _db.Locations
                .AsNoTracking()
                .AnyAsync(item => item.Id == request.LocationId);

            if (!locationExists)
            {
                return false;
            }

            audio = new AudioGuide
            {
                Id = request.TargetEntityId,
                LocationId = request.LocationId,
                Language = "vi"
            };

            ApplyAudioGuideFieldChanges(audio, changeSet);

            if (string.IsNullOrWhiteSpace(audio.Title))
            {
                return false;
            }

            if (shouldGenerateTtsOnApproval)
            {
                var generated = await TryGenerateTtsAudioAsync(audio, changeSet);
                if (!generated)
                {
                    return false;
                }
            }

            _db.AudioGuides.Add(audio);
            return true;
        }

        if (isDeleteAction)
        {
            if (audio is null)
            {
                return false;
            }

            var hasListeningHistory = await _db.ListeningHistories
                .AsNoTracking()
                .AnyAsync(item => item.AudioGuideId == audio.Id);

            if (hasListeningHistory)
            {
                return false;
            }

            _db.AudioGuides.Remove(audio);
            return true;
        }

        if (audio is null)
        {
            return false;
        }

        if (!string.Equals(audio.LocationId, request.LocationId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        ApplyAudioGuideFieldChanges(audio, changeSet);

        if (shouldGenerateTtsOnApproval)
        {
            var generated = await TryGenerateTtsAudioAsync(audio, changeSet);
            if (!generated)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> TryGenerateTtsAudioAsync(AudioGuide audio, PoiChangeSet changeSet)
    {
        var transcript = GetFieldValue(changeSet, nameof(AudioGuide.TranscriptText));
        if (string.IsNullOrWhiteSpace(transcript))
        {
            transcript = audio.TranscriptText;
        }

        if (string.IsNullOrWhiteSpace(transcript))
        {
            return false;
        }

        var language = GetFieldValue(changeSet, nameof(AudioGuide.Language));
        if (string.IsNullOrWhiteSpace(language))
        {
            language = audio.Language;
        }

        if (string.IsNullOrWhiteSpace(language))
        {
            language = "vi";
        }

        transcript = transcript.Trim();
        language = language.Trim();

        try
        {
            var audioBytes = await _ttsService.SynthesizeAsync(transcript, language);
            await using var stream = new MemoryStream(audioBytes);

            var uploadResult = await _audioStorageService.UploadAudioAsync(
                stream,
                $"tts_{language}_{Guid.NewGuid():N}.mp3",
                audio.Id);

            audio.AudioUrl = uploadResult.AudioUrl;
            audio.CloudinaryAudioUrl = uploadResult.CloudinaryAudioUrl;
            audio.CloudinaryPublicId = uploadResult.CloudinaryPublicId;
            audio.GeneratedFromTts = true;
            audio.TtsSourceText = transcript;
            audio.TranscriptText = transcript;
            audio.Language = language;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? GetFieldValue(PoiChangeSet changeSet, string key)
    {
        return changeSet.Fields.TryGetValue(key, out var value)
            ? value
            : null;
    }

    private static void ApplyAudioGuideFieldChanges(AudioGuide audio, PoiChangeSet changeSet)
    {
        if (changeSet.Fields.TryGetValue(nameof(AudioGuide.Title), out var title) && !string.IsNullOrWhiteSpace(title))
        {
            audio.Title = title.Trim();
        }

        if (changeSet.Fields.TryGetValue(nameof(AudioGuide.Description), out var description) && description is not null)
        {
            audio.Description = description.Trim();
        }

        if (changeSet.Fields.TryGetValue(nameof(AudioGuide.Duration), out var durationText)
            && int.TryParse(durationText, out var duration))
        {
            audio.Duration = Math.Max(0, duration);
        }

        if (changeSet.Fields.TryGetValue(nameof(AudioGuide.Language), out var language) && !string.IsNullOrWhiteSpace(language))
        {
            audio.Language = language.Trim();
        }

        if (changeSet.Fields.TryGetValue(nameof(AudioGuide.TranscriptText), out var transcript) && transcript is not null)
        {
            audio.TranscriptText = transcript;
        }

        if (changeSet.Fields.TryGetValue(nameof(AudioGuide.AudioUrl), out var audioUrl) && audioUrl is not null)
        {
            audio.AudioUrl = audioUrl.Trim();
        }

        if (changeSet.Fields.TryGetValue(nameof(AudioGuide.CloudinaryAudioUrl), out var cloudinaryAudioUrl))
        {
            audio.CloudinaryAudioUrl = string.IsNullOrWhiteSpace(cloudinaryAudioUrl)
                ? null
                : cloudinaryAudioUrl.Trim();
        }

        if (changeSet.Fields.TryGetValue(nameof(AudioGuide.CloudinaryPublicId), out var cloudinaryPublicId))
        {
            audio.CloudinaryPublicId = string.IsNullOrWhiteSpace(cloudinaryPublicId)
                ? null
                : cloudinaryPublicId.Trim();
        }

        if (changeSet.Fields.TryGetValue(nameof(AudioGuide.GeneratedFromTts), out var generatedFromTtsText)
            && bool.TryParse(generatedFromTtsText, out var generatedFromTts))
        {
            audio.GeneratedFromTts = generatedFromTts;
        }

        if (changeSet.Fields.TryGetValue(nameof(AudioGuide.TtsSourceText), out var ttsSourceText))
        {
            audio.TtsSourceText = string.IsNullOrWhiteSpace(ttsSourceText)
                ? null
                : ttsSourceText;
        }
    }

    private static bool TryParseDouble(string? value, out double result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result)
            || double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out result);
    }

    private static string NormalizeIdentity(string? raw)
    {
        return (raw ?? string.Empty).Trim();
    }
}
