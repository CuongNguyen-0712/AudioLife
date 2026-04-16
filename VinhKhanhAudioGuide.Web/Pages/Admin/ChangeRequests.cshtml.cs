using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Admin;

public class ChangeRequestsModel : PageModel
{
    private const string DeleteLocationAction = "delete-location";
    private const string DeleteAudioGuideAction = "delete-audio-guide";

    private readonly AppDbContext _db;
    private readonly IPoiChangeRequestService _changeRequestService;

    public ChangeRequestsModel(AppDbContext db, IPoiChangeRequestService changeRequestService)
    {
        _db = db;
        _changeRequestService = changeRequestService;
    }

    public List<PoiChangeRequest> Requests { get; set; } = new();
    public List<ChangeRequestReviewRow> ReviewRows { get; set; } = new();
    public int PendingCount { get; set; }
    public int InReviewCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? TargetFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? SelectedId { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(Guid id, PoiChangeRequestStatus status, string? reviewNote)
    {
        if (status == PoiChangeRequestStatus.Rejected && string.IsNullOrWhiteSpace(reviewNote))
        {
            TempData["Error"] = "Vui lòng nhập ghi chú lý do trước khi từ chối yêu cầu.";
            return RedirectToPage(new { StatusFilter, TargetFilter, Keyword, SelectedId = id });
        }

        var updatedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                       ?? User.Identity?.Name
                       ?? "SystemAdmin";

        var success = await _changeRequestService.TryUpdateStatusAsync(id, status, updatedBy, reviewNote?.Trim());
        TempData["Success"] = success
            ? "Đã cập nhật trạng thái yêu cầu."
            : "Không thể cập nhật yêu cầu. Vui lòng kiểm tra dữ liệu request.";

        return RedirectToPage(new { StatusFilter, TargetFilter, Keyword, SelectedId = id });
    }

    private async Task LoadAsync()
    {
        var allRequests = (await _changeRequestService.GetAllAsync()).ToList();

        PendingCount = allRequests.Count(item => item.Status == PoiChangeRequestStatus.Pending);
        InReviewCount = allRequests.Count(item => item.Status == PoiChangeRequestStatus.InReview);
        ApprovedCount = allRequests.Count(item => item.Status == PoiChangeRequestStatus.Approved);
        RejectedCount = allRequests.Count(item => item.Status == PoiChangeRequestStatus.Rejected);

        IEnumerable<PoiChangeRequest> query = allRequests;

        if (Enum.TryParse<PoiChangeRequestStatus>(StatusFilter, true, out var status))
        {
            query = query.Where(item => item.Status == status);
        }

        if (Enum.TryParse<PoiChangeTargetType>(TargetFilter, true, out var targetType))
        {
            query = query.Where(item => item.TargetType == targetType);
        }

        if (!string.IsNullOrWhiteSpace(Keyword))
        {
            var keyword = Keyword.Trim();
            query = query.Where(item =>
                Contains(item.LocationName, keyword)
                || Contains(item.Title, keyword)
                || Contains(item.Details, keyword)
                || Contains(item.SubmittedByName, keyword)
                || Contains(item.SubmittedByUsername, keyword));
        }

        Requests = query.ToList();
        ReviewRows = await BuildReviewRowsAsync(Requests);
    }

    private async Task<List<ChangeRequestReviewRow>> BuildReviewRowsAsync(IEnumerable<PoiChangeRequest> requests)
    {
        var requestList = requests.ToList();

        var locationIds = requestList
            .Where(item => item.TargetType == PoiChangeTargetType.Location)
            .Select(item => item.TargetEntityId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var audioIds = requestList
            .Where(item => item.TargetType == PoiChangeTargetType.AudioGuide)
            .Select(item => item.TargetEntityId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var locationLookup = await _db.Locations
            .AsNoTracking()
            .Where(item => locationIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, StringComparer.OrdinalIgnoreCase);

        var audioLookup = await _db.AudioGuides
            .AsNoTracking()
            .Where(item => audioIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, StringComparer.OrdinalIgnoreCase);

        var rows = new List<ChangeRequestReviewRow>(requestList.Count);

        foreach (var request in requestList)
        {
            var fields = ParseChangeFields(request.ChangeSetJson);
            var row = new ChangeRequestReviewRow
            {
                Request = request,
                CanReview = request.Status is PoiChangeRequestStatus.Pending or PoiChangeRequestStatus.InReview,
                StatusBadgeClass = GetStatusBadgeClass(request.Status)
            };

            if (fields.TryGetValue("__tts_on_approval", out var ttsOnApprovalValue)
                && bool.TryParse(ttsOnApprovalValue, out var ttsOnApproval))
            {
                row.IsTtsOnApproval = ttsOnApproval;
            }

            var hasCreateAudioAction = fields.TryGetValue("__action", out var action)
                && string.Equals(action, "create-audio-guide", StringComparison.OrdinalIgnoreCase);

            var hasCreateLocationAction = string.Equals(action, "create-location", StringComparison.OrdinalIgnoreCase);
            var hasDeleteLocationAction = string.Equals(action, DeleteLocationAction, StringComparison.OrdinalIgnoreCase);
            var hasDeleteAudioAction = string.Equals(action, DeleteAudioGuideAction, StringComparison.OrdinalIgnoreCase);
            row.IsDeleteRequest = hasDeleteLocationAction || hasDeleteAudioAction;

            foreach (var field in fields)
            {
                if (string.Equals(field.Key, "__action", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(field.Key, "__tts_on_approval", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var currentValue = ResolveCurrentValue(
                    request,
                    field.Key,
                    locationLookup,
                    audioLookup,
                    hasCreateLocationAction,
                    hasCreateAudioAction);
                row.Changes.Add(new ChangeFieldDiff
                {
                    FieldName = field.Key,
                    CurrentValue = FormatValue(currentValue),
                    ProposedValue = FormatValue(field.Value)
                });
            }

            if (hasDeleteLocationAction)
            {
                row.Changes.Add(new ChangeFieldDiff
                {
                    FieldName = "Hành động",
                    CurrentValue = locationLookup.TryGetValue(request.TargetEntityId, out var location)
                        ? $"{location.Name} ({location.Id})"
                        : "(POI không tồn tại trên DB)",
                    ProposedValue = "Xóa POI sau duyệt"
                });
            }
            else if (hasDeleteAudioAction)
            {
                row.Changes.Add(new ChangeFieldDiff
                {
                    FieldName = "Hành động",
                    CurrentValue = audioLookup.TryGetValue(request.TargetEntityId, out var audioGuide)
                        ? $"{audioGuide.Title} ({audioGuide.Id})"
                        : "(audio không tồn tại trên DB)",
                    ProposedValue = "Xóa audio sau duyệt"
                });
            }

            if (!row.Changes.Any())
            {
                row.Changes.Add(new ChangeFieldDiff
                {
                    FieldName = "(không có field hợp lệ)",
                    CurrentValue = "-",
                    ProposedValue = "-"
                });
            }

            rows.Add(row);
        }

        return rows;
    }

    private static Dictionary<string, string?> ParseChangeFields(string json)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
        {
            return result;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return result;
            }

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
                result[property.Name] = ToDisplayString(property.Value);
            }
        }
        catch
        {
            // Keep empty result when change-set JSON is malformed.
        }

        return result;
    }

    private static string? ToDisplayString(JsonElement value)
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

    private static string? ResolveCurrentValue(
        PoiChangeRequest request,
        string fieldName,
        IReadOnlyDictionary<string, Location> locationLookup,
        IReadOnlyDictionary<string, AudioGuide> audioLookup,
        bool hasCreateLocationAction,
        bool hasCreateAudioAction)
    {
        if (request.TargetType == PoiChangeTargetType.Location)
        {
            if (!locationLookup.TryGetValue(request.TargetEntityId, out var location))
            {
                return hasCreateLocationAction
                    ? "(chưa có dữ liệu - yêu cầu tạo mới)"
                    : "(POI không tồn tại trên DB)";
            }

            return fieldName switch
            {
                nameof(Location.Name) => location.Name,
                nameof(Location.Description) => location.Description,
                nameof(Location.Address) => location.Address,
                nameof(Location.ImageUrl) => location.ImageUrl,
                nameof(Location.CategoryId) => location.CategoryId,
                nameof(Location.Latitude) => location.Latitude.ToString(CultureInfo.InvariantCulture),
                nameof(Location.Longitude) => location.Longitude.ToString(CultureInfo.InvariantCulture),
                nameof(Location.Duration) => location.Duration.ToString(),
                _ => "(field chưa map)"
            };
        }

        if (!audioLookup.TryGetValue(request.TargetEntityId, out var audioGuide))
        {
            return hasCreateAudioAction
                ? "(chưa có dữ liệu - yêu cầu tạo mới)"
                : "(audio không tồn tại trên DB)";
        }

        return fieldName switch
        {
            nameof(AudioGuide.Title) => audioGuide.Title,
            nameof(AudioGuide.Description) => audioGuide.Description,
            nameof(AudioGuide.Duration) => audioGuide.Duration.ToString(),
            nameof(AudioGuide.Language) => audioGuide.Language,
            nameof(AudioGuide.TranscriptText) => audioGuide.TranscriptText,
            nameof(AudioGuide.AudioUrl) => audioGuide.AudioUrl,
            nameof(AudioGuide.CloudinaryAudioUrl) => audioGuide.CloudinaryAudioUrl,
            nameof(AudioGuide.CloudinaryPublicId) => audioGuide.CloudinaryPublicId,
            nameof(AudioGuide.GeneratedFromTts) => audioGuide.GeneratedFromTts.ToString(),
            nameof(AudioGuide.TtsSourceText) => audioGuide.TtsSourceText,
            _ => "(field chưa map)"
        };
    }

    private static bool Contains(string? source, string keyword)
    {
        return !string.IsNullOrWhiteSpace(source)
               && source.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "(trống)"
            : value;
    }

    public static string GetStatusBadgeClass(PoiChangeRequestStatus status)
    {
        return status switch
        {
            PoiChangeRequestStatus.Pending => "text-bg-secondary",
            PoiChangeRequestStatus.InReview => "text-bg-warning",
            PoiChangeRequestStatus.Approved => "text-bg-success",
            PoiChangeRequestStatus.Rejected => "text-bg-danger",
            _ => "text-bg-secondary"
        };
    }

    public class ChangeRequestReviewRow
    {
        public PoiChangeRequest Request { get; set; } = new();
        public List<ChangeFieldDiff> Changes { get; set; } = new();
        public bool CanReview { get; set; }
        public bool IsTtsOnApproval { get; set; }
        public bool IsDeleteRequest { get; set; }
        public string StatusBadgeClass { get; set; } = "text-bg-secondary";
    }

    public class ChangeFieldDiff
    {
        public string FieldName { get; set; } = string.Empty;
        public string CurrentValue { get; set; } = string.Empty;
        public string ProposedValue { get; set; } = string.Empty;
    }
}
