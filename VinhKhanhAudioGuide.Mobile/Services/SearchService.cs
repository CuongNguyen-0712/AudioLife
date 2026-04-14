using System.Globalization;
using System.Text;
using VinhKhanhAudioGuide.Mobile.Models;
using LocationModel = VinhKhanhAudioGuide.Mobile.Models.Location;

namespace VinhKhanhAudioGuide.Mobile.Services;

public sealed class SearchService : ISearchService
{
    private readonly IApiService _apiService;
    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private readonly Dictionary<string, List<SearchLocationResult>> _searchCache = new(StringComparer.Ordinal);

    private List<SearchDocument>? _index;
    private DateTime _indexBuiltAtUtc = DateTime.MinValue;

    public SearchService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IReadOnlyList<SearchLocationResult>> SearchAsync(SearchQueryOptions options, CancellationToken cancellationToken = default)
    {
        options ??= new SearchQueryOptions();
        var docs = await EnsureIndexAsync(cancellationToken);

        var normalizedQuery = NormalizeSearchText(options.Query);
        var normalizedCategories = options.CategoryIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var cacheKey = BuildCacheKey(options, normalizedQuery, normalizedCategories);
        if (_searchCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var results = new List<SearchLocationResult>();

        foreach (var doc in docs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!PassCategoryFilter(doc, normalizedCategories))
            {
                continue;
            }

            if (!PassAudioCountFilter(doc, options.AudioCountComparison, options.AudioCountValue))
            {
                continue;
            }

            if (!PassDurationFilter(doc, options.DurationRange, options.DurationMode))
            {
                continue;
            }

            if (!PassQueryFilter(doc, normalizedQuery, out var relevanceScore))
            {
                continue;
            }

            results.Add(new SearchLocationResult
            {
                Location = doc.Location,
                CategoryName = doc.CategoryName,
                AudioCount = doc.AudioCount,
                TotalDurationMinutes = doc.TotalDurationMinutes,
                RelevanceScore = relevanceScore
            });
        }

        var ordered = results
            .OrderByDescending(r => r.RelevanceScore)
            .ThenBy(r => r.Location.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, options.MaxResults))
            .ToList();

        _searchCache[cacheKey] = ordered;
        if (_searchCache.Count > 120)
        {
            var firstKey = _searchCache.Keys.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstKey))
            {
                _searchCache.Remove(firstKey);
            }
        }

        return ordered;
    }

    public async Task<IReadOnlyList<string>> GetSuggestionsAsync(string query, int maxItems = 5, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSearchText(query);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<string>();
        }

        var docs = await EnsureIndexAsync(cancellationToken);
        maxItems = Math.Clamp(maxItems, 1, 20);

        var suggestions = docs
            .Select(doc => new
            {
                Name = doc.Location.Name,
                NormalizedName = doc.NormalizedName,
                Score = doc.NormalizedName.StartsWith(normalized, StringComparison.Ordinal)
                    ? 100
                    : doc.NormalizedName.Contains(normalized, StringComparison.Ordinal)
                        ? 70
                        : CalculateBestTokenSimilarity(normalized, doc.NormalizedName)
            })
            .Where(x => x.Score >= 60)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxItems)
            .ToList();

        return suggestions;
    }

    private async Task<List<SearchDocument>> EnsureIndexAsync(CancellationToken cancellationToken)
    {
        if (_index is not null && DateTime.UtcNow - _indexBuiltAtUtc < TimeSpan.FromMinutes(5))
        {
            return _index;
        }

        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            if (_index is not null && DateTime.UtcNow - _indexBuiltAtUtc < TimeSpan.FromMinutes(5))
            {
                return _index;
            }

            var locationsTask = _apiService.GetLocationsAsync();
            var categoriesTask = _apiService.GetCategoriesAsync();
            await Task.WhenAll(locationsTask, categoriesTask);

            var locations = locationsTask.Result;
            var categories = categoriesTask.Result;
            var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name, StringComparer.OrdinalIgnoreCase);

            _index = locations.Select(location =>
            {
                var normalizedName = NormalizeSearchText(location.Name);
                var normalizedDescription = NormalizeSearchText(location.Description);
                var categoryName = categoryLookup.TryGetValue(location.CategoryId, out var cName) ? cName : location.CategoryName;
                var normalizedCategory = NormalizeSearchText(categoryName);

                var audioDurations = location.AudioGuides.Select(a => Math.Max(0, a.Duration)).ToList();
                var totalDuration = audioDurations.Sum();

                return new SearchDocument
                {
                    Location = location,
                    CategoryName = categoryName,
                    NormalizedName = normalizedName,
                    NormalizedDescription = normalizedDescription,
                    NormalizedCategory = normalizedCategory,
                    AudioCount = location.AudioGuides.Count,
                    TotalDurationMinutes = totalDuration,
                    AudioDurations = audioDurations
                };
            }).ToList();

            _indexBuiltAtUtc = DateTime.UtcNow;
            _searchCache.Clear();
            return _index;
        }
        finally
        {
            _indexLock.Release();
        }
    }

    private static bool PassCategoryFilter(SearchDocument doc, IReadOnlyCollection<string> selectedCategoryIds)
    {
        if (selectedCategoryIds.Count == 0)
        {
            return true;
        }

        return selectedCategoryIds.Contains(doc.Location.CategoryId, StringComparer.OrdinalIgnoreCase);
    }

    private static bool PassAudioCountFilter(SearchDocument doc, AudioCountComparison comparison, int? expectedValue)
    {
        if (comparison == AudioCountComparison.Any || !expectedValue.HasValue)
        {
            return true;
        }

        var value = Math.Max(0, expectedValue.Value);
        return comparison switch
        {
            AudioCountComparison.GreaterOrEqual => doc.AudioCount >= value,
            AudioCountComparison.LessOrEqual => doc.AudioCount <= value,
            AudioCountComparison.Equal => doc.AudioCount == value,
            _ => true
        };
    }

    private static bool PassDurationFilter(SearchDocument doc, DurationFilterRange range, DurationFilterMode mode)
    {
        if (range == DurationFilterRange.Any)
        {
            return true;
        }

        if (mode == DurationFilterMode.TotalDuration)
        {
            return MatchDurationRange(doc.TotalDurationMinutes, range);
        }

        return doc.AudioDurations.Any(duration => MatchDurationRange(duration, range));
    }

    private static bool MatchDurationRange(int minutes, DurationFilterRange range)
    {
        return range switch
        {
            DurationFilterRange.LessThan5Minutes => minutes < 5,
            DurationFilterRange.Between5And10Minutes => minutes >= 5 && minutes <= 10,
            DurationFilterRange.MoreThan10Minutes => minutes > 10,
            _ => true
        };
    }

    private static bool PassQueryFilter(SearchDocument doc, string normalizedQuery, out double relevance)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            relevance = 10;
            return true;
        }

        var indexInName = doc.NormalizedName.IndexOf(normalizedQuery, StringComparison.Ordinal);
        if (indexInName >= 0)
        {
            relevance = 120 - Math.Min(indexInName, 50);
            return true;
        }

        var indexInCategory = doc.NormalizedCategory.IndexOf(normalizedQuery, StringComparison.Ordinal);
        if (indexInCategory >= 0)
        {
            relevance = 95 - Math.Min(indexInCategory, 30);
            return true;
        }

        var indexInDescription = doc.NormalizedDescription.IndexOf(normalizedQuery, StringComparison.Ordinal);
        if (indexInDescription >= 0)
        {
            relevance = 85 - Math.Min(indexInDescription, 40);
            return true;
        }

        var queryTokens = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (queryTokens.Length > 1)
        {
            var allMatched = queryTokens.All(token =>
                doc.NormalizedName.Contains(token, StringComparison.Ordinal)
                || doc.NormalizedDescription.Contains(token, StringComparison.Ordinal)
                || doc.NormalizedCategory.Contains(token, StringComparison.Ordinal));

            if (allMatched)
            {
                relevance = 80 + queryTokens.Length;
                return true;
            }
        }

        var fuzzySimilarity = Math.Max(
            CalculateBestTokenSimilarity(normalizedQuery, doc.NormalizedName),
            Math.Max(
                CalculateBestTokenSimilarity(normalizedQuery, doc.NormalizedDescription),
                CalculateBestTokenSimilarity(normalizedQuery, doc.NormalizedCategory)));

        if (fuzzySimilarity >= 0.72)
        {
            relevance = 60 + fuzzySimilarity * 20;
            return true;
        }

        relevance = 0;
        return false;
    }

    private static double CalculateBestTokenSimilarity(string query, string source)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(source))
        {
            return 0;
        }

        var tokens = source.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var best = 0d;

        foreach (var token in tokens)
        {
            var similarity = ComputeSimilarity(query, token);
            if (similarity > best)
            {
                best = similarity;
            }
        }

        return Math.Max(best, ComputeSimilarity(query, source));
    }

    private static double ComputeSimilarity(string a, string b)
    {
        if (string.Equals(a, b, StringComparison.Ordinal))
        {
            return 1.0;
        }

        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            return 0;
        }

        var distance = ComputeLevenshteinDistance(a, b);
        var maxLength = Math.Max(a.Length, b.Length);
        if (maxLength == 0)
        {
            return 1.0;
        }

        return 1.0 - (double)distance / maxLength;
    }

    private static int ComputeLevenshteinDistance(string source, string target)
    {
        var n = source.Length;
        var m = target.Length;
        if (n == 0) return m;
        if (m == 0) return n;

        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    private static string BuildCacheKey(SearchQueryOptions options, string normalizedQuery, IReadOnlyList<string> normalizedCategories)
    {
        var categories = string.Join(',', normalizedCategories);
        return string.Join('|',
            normalizedQuery,
            categories,
            (int)options.AudioCountComparison,
            options.AudioCountValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            (int)options.DurationRange,
            (int)options.DurationMode,
            options.MaxResults.ToString(CultureInfo.InvariantCulture));
    }

    internal static string NormalizeSearchText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var formC = input.Trim().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        var noDiacritics = RemoveDiacritics(formC);
        return string.Concat(noDiacritics.Where(ch => !char.IsControl(ch)));
    }

    private static string RemoveDiacritics(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark &&
                category != UnicodeCategory.SpacingCombiningMark &&
                category != UnicodeCategory.EnclosingMark)
            {
                builder.Append(ch);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D');
    }

    private sealed class SearchDocument
    {
        public required LocationModel Location { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public string NormalizedName { get; init; } = string.Empty;
        public string NormalizedDescription { get; init; } = string.Empty;
        public string NormalizedCategory { get; init; } = string.Empty;
        public int AudioCount { get; init; }
        public int TotalDurationMinutes { get; init; }
        public List<int> AudioDurations { get; init; } = new();
    }
}
