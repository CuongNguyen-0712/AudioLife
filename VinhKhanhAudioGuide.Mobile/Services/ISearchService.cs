using VinhKhanhAudioGuide.Mobile.Models;
using LocationModel = VinhKhanhAudioGuide.Mobile.Models.Location;

namespace VinhKhanhAudioGuide.Mobile.Services;

public interface ISearchService
{
    Task<IReadOnlyList<SearchLocationResult>> SearchAsync(SearchQueryOptions options, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetSuggestionsAsync(string query, int maxItems = 5, CancellationToken cancellationToken = default);
}

public sealed class SearchQueryOptions
{
    public string Query { get; init; } = string.Empty;
    public IReadOnlyCollection<string> CategoryIds { get; init; } = Array.Empty<string>();
    public AudioCountComparison AudioCountComparison { get; init; } = AudioCountComparison.Any;
    public int? AudioCountValue { get; init; }
    public DurationFilterRange DurationRange { get; init; } = DurationFilterRange.Any;
    public DurationFilterMode DurationMode { get; init; } = DurationFilterMode.TotalDuration;
    public int MaxResults { get; init; } = 100;
}

public sealed class SearchLocationResult
{
    public required LocationModel Location { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int AudioCount { get; init; }
    public int TotalDurationMinutes { get; init; }
    public double RelevanceScore { get; init; }
}

public enum AudioCountComparison
{
    Any = 0,
    GreaterOrEqual = 1,
    LessOrEqual = 2,
    Equal = 3
}

public enum DurationFilterRange
{
    Any = 0,
    LessThan5Minutes = 1,
    Between5And10Minutes = 2,
    MoreThan10Minutes = 3
}

public enum DurationFilterMode
{
    TotalDuration = 0,
    AnySingleAudio = 1
}
