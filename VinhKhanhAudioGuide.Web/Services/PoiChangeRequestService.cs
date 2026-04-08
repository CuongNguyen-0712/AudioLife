using System.Text.Json;
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
    Task<PoiChangeRequest> SubmitAsync(PoiChangeRequest request);
    Task<bool> TryUpdateStatusAsync(Guid id, PoiChangeRequestStatus status, string updatedBy, string? reviewNote = null);
}

public class DbPoiChangeRequestService : IPoiChangeRequestService
{
    private const string ChangeActionField = "__action";
    private const string CreateAudioGuideAction = "create-audio-guide";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly AppDbContext _db;

    public DbPoiChangeRequestService(AppDbContext db)
    {
        _db = db;
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
        return await _db.PoiChangeRequests
            .AsNoTracking()
            .Where(item => item.SubmittedByUsername == username)
            .OrderByDescending(item => item.SubmittedAtUtc)
            .ToListAsync();
    }

    public async Task<PoiChangeRequest> SubmitAsync(PoiChangeRequest request)
    {
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

    private async Task<bool> TryApplyChangeSetAsync(PoiChangeRequest request)
    {
        PoiChangeSet? changeSet;
        try
        {
            changeSet = JsonSerializer.Deserialize<PoiChangeSet>(request.ChangeSetJson, JsonOptions);
        }
        catch
        {
            return false;
        }

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

    private async Task<bool> ApplyLocationChangesAsync(PoiChangeRequest request, PoiChangeSet changeSet)
    {
        var location = await _db.Locations.FirstOrDefaultAsync(item => item.Id == request.TargetEntityId);
        if (location is null)
        {
            return false;
        }

        if (!string.Equals(location.Id, request.LocationId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

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

        if (changeSet.Fields.TryGetValue(nameof(Location.Latitude), out var latText)
            && double.TryParse(latText, out var latitude))
        {
            location.Latitude = latitude;
        }

        if (changeSet.Fields.TryGetValue(nameof(Location.Longitude), out var lngText)
            && double.TryParse(lngText, out var longitude))
        {
            location.Longitude = longitude;
        }

        if (changeSet.Fields.TryGetValue(nameof(Location.Duration), out var durationText)
            && int.TryParse(durationText, out var duration))
        {
            location.Duration = Math.Max(0, duration);
        }

        return true;
    }

    private async Task<bool> ApplyAudioGuideChangesAsync(PoiChangeRequest request, PoiChangeSet changeSet)
    {
        changeSet.Fields.TryGetValue(ChangeActionField, out var actionValue);
        var isCreateAction = string.Equals(actionValue, CreateAudioGuideAction, StringComparison.OrdinalIgnoreCase);

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

            _db.AudioGuides.Add(audio);
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
        return true;
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
}
