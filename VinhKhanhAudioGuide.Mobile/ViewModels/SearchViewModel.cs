using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private const string RecentSearchesPreferenceKey = "SearchRecentQueriesV2";
    private const int MaxRecentSearches = 10;
    private const int SearchDebounceMs = 300;

    private readonly INavigationService _navigationService;
    private readonly IApiService _apiService;
    private readonly ISearchService _searchService;
    private readonly ILocalizationService _localizationService;
    private readonly SemaphoreSlim _searchLock = new(1, 1);

    private CancellationTokenSource? _searchDebounceCts;
    private CancellationTokenSource? _suggestionCts;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _hasSearchQuery;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _showNoResults;

    [ObservableProperty]
    private bool _showRecentSearches = true;

    [ObservableProperty]
    private bool _showResults;

    [ObservableProperty]
    private bool _showSuggestions;

    [ObservableProperty]
    private bool _showResultContextTitle;

    [ObservableProperty]
    private string _resultContextTitle = string.Empty;

    [ObservableProperty]
    private bool _isFilterPanelOpen;

    [ObservableProperty]
    private int _selectedAudioCountOperatorIndex;

    [ObservableProperty]
    private string _audioCountValueText = string.Empty;

    [ObservableProperty]
    private int _selectedDurationRangeIndex;

    [ObservableProperty]
    private int _selectedDurationModeIndex;

    public ObservableCollection<CategoryFilter> Categories { get; } = new();
    public ObservableCollection<string> AudioCountOperatorOptions { get; } =
    [
        "",
        ">=",
        "<=",
        "="
    ];

    public ObservableCollection<string> DurationRangeOptions { get; } =
    [
        "",
        "",
        "",
        ""
    ];

    public ObservableCollection<string> DurationModeOptions { get; } =
    [
        "",
        ""
    ];

    public ObservableCollection<string> RecentSearches { get; } = new();
    public ObservableCollection<string> Suggestions { get; } = new();
    public ObservableCollection<SearchResultItem> SearchResults { get; } = new();

    public SearchViewModel(
        INavigationService navigationService,
        IApiService apiService,
        ISearchService searchService,
        ILocalizationService localizationService)
    {
        _navigationService = navigationService;
        _apiService = apiService;
        _searchService = searchService;
        _localizationService = localizationService;

        AudioCountOperatorOptions[0] = T("Common_FilterAny");
        DurationRangeOptions[0] = T("Common_FilterAny");
        DurationRangeOptions[1] = T("Search_FilterDurationUnder5");
        DurationRangeOptions[2] = T("Search_FilterDuration5To10");
        DurationRangeOptions[3] = T("Search_FilterDurationOver10");
        DurationModeOptions[0] = T("Search_FilterModeTotalDuration");
        DurationModeOptions[1] = T("Search_FilterModeSingleAudio");

        _ = LoadCategoriesAsync();
        LoadRecentSearches();
    }

    public async Task ApplyNavigationContextAsync(string? initialQuery, string? initialCategoryId)
    {
        var query = initialQuery?.Trim() ?? string.Empty;
        var categoryId = initialCategoryId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(categoryId))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            if (Categories.Count == 0)
            {
                await LoadCategoriesAsync();
            }

            foreach (var category in Categories)
            {
                category.IsSelected = string.Equals(category.Id, categoryId, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            SearchQuery = query;
        }

        await ExecuteSearchAsync(CancellationToken.None);
    }

    partial void OnSearchQueryChanged(string value)
    {
        HasSearchQuery = !string.IsNullOrWhiteSpace(value?.Trim());
        _ = UpdateSuggestionsAsync(value);
        _ = TriggerSearchDebouncedAsync(SearchDebounceMs);
    }

    partial void OnAudioCountValueTextChanged(string value)
    {
        _ = TriggerSearchDebouncedAsync(SearchDebounceMs);
    }

    partial void OnSelectedAudioCountOperatorIndexChanged(int value)
    {
        _ = TriggerSearchDebouncedAsync(SearchDebounceMs);
    }

    partial void OnSelectedDurationRangeIndexChanged(int value)
    {
        _ = TriggerSearchDebouncedAsync(SearchDebounceMs);
    }

    partial void OnSelectedDurationModeIndexChanged(int value)
    {
        _ = TriggerSearchDebouncedAsync(SearchDebounceMs);
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await _apiService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var cat in categories)
            {
                Categories.Add(new CategoryFilter
                {
                    Id = cat.Id,
                    Name = cat.Name,
                    Icon = cat.Icon,
                    Description = cat.Description,
                    IsSelected = false
                });
            }
        }
        catch
        {
            Categories.Clear();
        }
    }

    private void LoadRecentSearches()
    {
        RecentSearches.Clear();

        try
        {
            var raw = Preferences.Get(RecentSearchesPreferenceKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            var loaded = JsonSerializer.Deserialize<List<string>>(raw) ?? new List<string>();
            foreach (var item in loaded
                         .Where(x => !string.IsNullOrWhiteSpace(x))
                         .Select(x => x.Trim())
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .Take(MaxRecentSearches))
            {
                RecentSearches.Add(item);
            }
        }
        catch
        {
            RecentSearches.Clear();
        }
    }

    private void SaveRecentSearches()
    {
        try
        {
            var data = RecentSearches.Take(MaxRecentSearches).ToList();
            var raw = JsonSerializer.Serialize(data);
            Preferences.Set(RecentSearchesPreferenceKey, raw);
        }
        catch
        {
            // Ignore serialization failures to keep search flow safe.
        }
    }

    [RelayCommand]
    private void ToggleFilterPanel()
    {
        IsFilterPanelOpen = !IsFilterPanelOpen;
    }

    [RelayCommand]
    private void SelectCategory(CategoryFilter? category)
    {
        if (category is null)
        {
            return;
        }

        category.IsSelected = !category.IsSelected;
        _ = TriggerSearchDebouncedAsync(SearchDebounceMs);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await ExecuteSearchAsync(CancellationToken.None);
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        Suggestions.Clear();
        ShowSuggestions = false;
        SearchResults.Clear();
        ShowResults = false;
        ShowNoResults = false;
        ShowRecentSearches = RecentSearches.Count > 0;
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SelectedAudioCountOperatorIndex = 0;
        AudioCountValueText = string.Empty;
        SelectedDurationRangeIndex = 0;
        SelectedDurationModeIndex = 0;

        foreach (var category in Categories)
        {
            category.IsSelected = false;
        }

        _ = TriggerSearchDebouncedAsync(SearchDebounceMs);
    }

    [RelayCommand]
    private async Task UseRecentSearchAsync(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        SearchQuery = query.Trim();
        await ExecuteSearchAsync(CancellationToken.None);
    }

    [RelayCommand]
    private void RemoveRecentSearch(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        RecentSearches.Remove(query);
        SaveRecentSearches();
        ShowRecentSearches = RecentSearches.Count > 0 && !HasSearchQuery && !HasActiveFilters();
    }

    [RelayCommand]
    private async Task UseSuggestionAsync(string? suggestion)
    {
        if (string.IsNullOrWhiteSpace(suggestion))
        {
            return;
        }

        SearchQuery = suggestion.Trim();
        Suggestions.Clear();
        ShowSuggestions = false;
        await ExecuteSearchAsync(CancellationToken.None);
    }

    [RelayCommand]
    private async Task LocationSelectedAsync(SearchResultItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (item.ResultType == SearchResultType.Tour)
        {
            await _navigationService.NavigateToAsync(nameof(Views.TourDetailPage),
                new Dictionary<string, object> { { "TourId", item.Id } });
            return;
        }

        await _navigationService.NavigateToAsync(nameof(Views.LocationDetailPage),
            new Dictionary<string, object> { { "LocationId", item.Id } });
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await _navigationService.GoBackAsync();
    }

    private async Task UpdateSuggestionsAsync(string? input)
    {
        _suggestionCts?.Cancel();
        _suggestionCts?.Dispose();
        _suggestionCts = new CancellationTokenSource();

        var token = _suggestionCts.Token;
        try
        {
            var trimmed = input?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length < 2)
            {
                Suggestions.Clear();
                ShowSuggestions = false;
                return;
            }

            var suggestions = await _searchService.GetSuggestionsAsync(trimmed, 6, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            Suggestions.Clear();
            foreach (var suggestion in suggestions)
            {
                Suggestions.Add(suggestion);
            }

            ShowSuggestions = Suggestions.Count > 0 && HasSearchQuery;
        }
        catch
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            Suggestions.Clear();
            ShowSuggestions = false;
        }
    }

    private async Task TriggerSearchDebouncedAsync(int delayMs)
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = new CancellationTokenSource();
        var token = _searchDebounceCts.Token;

        try
        {
            await Task.Delay(delayMs, token);
            await ExecuteSearchAsync(token);
        }
        catch (OperationCanceledException)
        {
            // Debounced request superseded by newer input.
        }
    }

    private async Task ExecuteSearchAsync(CancellationToken cancellationToken)
    {
        var normalizedQuery = (SearchQuery ?? string.Empty).Trim();
        var hasFilters = HasActiveFilters();

        if (string.IsNullOrWhiteSpace(normalizedQuery) && !hasFilters)
        {
            IsLoading = false;
            SearchResults.Clear();
            ShowResults = false;
            ShowNoResults = false;
            ShowResultContextTitle = false;
            ResultContextTitle = string.Empty;
            ShowRecentSearches = RecentSearches.Count > 0;
            return;
        }

        var lockAcquired = false;
        try
        {
            await _searchLock.WaitAsync(cancellationToken);
            lockAcquired = true;

            IsLoading = true;
            ShowRecentSearches = false;
            ShowSuggestions = false;

            var selectedCategoryIds = Categories
                .Where(c => c.IsSelected)
                .Select(c => c.Id)
                .ToList();

            ResultContextTitle = BuildResultContextTitle(normalizedQuery, selectedCategoryIds, hasFilters);
            ShowResultContextTitle = !string.IsNullOrWhiteSpace(ResultContextTitle);

            var options = new SearchQueryOptions
            {
                Query = normalizedQuery,
                CategoryIds = selectedCategoryIds,
                AudioCountComparison = ParseAudioComparison(SelectedAudioCountOperatorIndex),
                AudioCountValue = ParseAudioCountValue(AudioCountValueText),
                DurationRange = ParseDurationRange(SelectedDurationRangeIndex),
                DurationMode = ParseDurationMode(SelectedDurationModeIndex),
                MaxResults = 150
            };

            var results = await _searchService.SearchAsync(options, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var isTourKeywordQuery = IsTourKeywordQuery(normalizedQuery);
            var mergedResults = new List<SearchResultItem>(results.Count + 20);
            if (!isTourKeywordQuery)
            {
                foreach (var result in results)
                {
                    mergedResults.Add(new SearchResultItem
                    {
                        Id = result.Location.Id,
                        Name = result.Location.Name,
                        Description = result.Location.Description,
                        ImageUrl = result.Location.ImageUrl,
                        CategoryName = result.CategoryName,
                        AudioCount = result.AudioCount,
                        TotalDurationMinutes = result.TotalDurationMinutes,
                        DurationDisplay = string.Format(T("Search_TotalDurationFormat"), result.TotalDurationMinutes),
                        CountDisplay = string.Format(T("Search_AudioCountFormat"), result.AudioCount),
                        ResultType = SearchResultType.Location
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(normalizedQuery))
            {
                var tours = await BuildTourResultsAsync(normalizedQuery, cancellationToken, isTourKeywordQuery);
                mergedResults.AddRange(tours);
            }

            SearchResults.Clear();
            foreach (var item in mergedResults)
            {
                SearchResults.Add(item);
            }

            IsLoading = false;
            ShowResults = SearchResults.Count > 0;
            ShowNoResults = SearchResults.Count == 0;

            if (!string.IsNullOrWhiteSpace(normalizedQuery))
            {
                UpsertRecentSearch(normalizedQuery);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancelled searches.
        }
        catch
        {
            IsLoading = false;
            ShowResults = false;
            ShowNoResults = true;
        }
        finally
        {
            if (lockAcquired)
            {
                _searchLock.Release();
            }
        }
    }

    private bool HasActiveFilters()
    {
        var hasCategory = Categories.Any(c => c.IsSelected);
        var hasAudioCountFilter = SelectedAudioCountOperatorIndex > 0 && ParseAudioCountValue(AudioCountValueText).HasValue;
        var hasDurationFilter = SelectedDurationRangeIndex > 0;
        return hasCategory || hasAudioCountFilter || hasDurationFilter;
    }

    private string BuildResultContextTitle(string query, IReadOnlyCollection<string> selectedCategoryIds, bool hasFilters)
    {
        if (!string.IsNullOrWhiteSpace(query))
        {
            return string.Format(T("Search_ResultForQueryFormat"), query);
        }

        if (selectedCategoryIds.Count == 1)
        {
            var category = Categories.FirstOrDefault(c => c.IsSelected);
            if (category is not null && !string.IsNullOrWhiteSpace(category.Name))
            {
                return string.Format(T("Search_ResultForCategoryFormat"), category.Name);
            }
        }

        if (selectedCategoryIds.Count > 1)
        {
            return string.Format(T("Search_ResultForCategoriesFormat"), selectedCategoryIds.Count);
        }

        if (hasFilters)
        {
            return T("Search_ResultByFilters");
        }

        return string.Empty;
    }

    private void UpsertRecentSearch(string query)
    {
        var existing = RecentSearches.FirstOrDefault(item => string.Equals(item, query, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(existing))
        {
            RecentSearches.Remove(existing);
        }

        RecentSearches.Insert(0, query);
        while (RecentSearches.Count > MaxRecentSearches)
        {
            RecentSearches.RemoveAt(RecentSearches.Count - 1);
        }

        SaveRecentSearches();
    }

    private static AudioCountComparison ParseAudioComparison(int index)
    {
        return index switch
        {
            1 => AudioCountComparison.GreaterOrEqual,
            2 => AudioCountComparison.LessOrEqual,
            3 => AudioCountComparison.Equal,
            _ => AudioCountComparison.Any
        };
    }

    private static int? ParseAudioCountValue(string? input)
    {
        if (!int.TryParse(input?.Trim(), out var value))
        {
            return null;
        }

        return Math.Max(0, value);
    }

    private static DurationFilterRange ParseDurationRange(int index)
    {
        return index switch
        {
            1 => DurationFilterRange.LessThan5Minutes,
            2 => DurationFilterRange.Between5And10Minutes,
            3 => DurationFilterRange.MoreThan10Minutes,
            _ => DurationFilterRange.Any
        };
    }

    private static DurationFilterMode ParseDurationMode(int index)
    {
        return index switch
        {
            1 => DurationFilterMode.AnySingleAudio,
            _ => DurationFilterMode.TotalDuration
        };
    }

    private async Task<List<SearchResultItem>> BuildTourResultsAsync(string query, CancellationToken cancellationToken, bool includeAllTours)
    {
        var normalizedQuery = SearchService.NormalizeSearchText(query);
        if (string.IsNullOrWhiteSpace(normalizedQuery) && !includeAllTours)
        {
            return new List<SearchResultItem>();
        }

        var tourSpecificQuery = normalizedQuery;
        if (tourSpecificQuery.StartsWith("tour ", StringComparison.Ordinal))
        {
            tourSpecificQuery = tourSpecificQuery[5..].Trim();
        }

        var tours = await _apiService.GetToursAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var filteredTours = tours
            .Select(tour => new
            {
                Tour = tour,
                NormalizedName = SearchService.NormalizeSearchText(tour.Name),
                NormalizedDescription = SearchService.NormalizeSearchText(tour.Description)
            })
            .Where(x =>
                includeAllTours
                || string.IsNullOrWhiteSpace(tourSpecificQuery)
                || x.NormalizedName.Contains(tourSpecificQuery, StringComparison.Ordinal)
                || x.NormalizedDescription.Contains(tourSpecificQuery, StringComparison.Ordinal))
            .OrderBy(x =>
                string.IsNullOrWhiteSpace(tourSpecificQuery)
                    ? 0
                    : x.NormalizedName.StartsWith(tourSpecificQuery, StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(x => x.Tour.Name, StringComparer.OrdinalIgnoreCase)
            .Take(30)
            .Select(x => new SearchResultItem
            {
                Id = x.Tour.Id,
                Name = x.Tour.Name,
                Description = x.Tour.Description,
                ImageUrl = x.Tour.ImageUrl,
                CategoryName = T("Tours_PageTitle"),
                AudioCount = x.Tour.LocationIds.Count,
                TotalDurationMinutes = x.Tour.Duration,
                DurationDisplay = string.Format(T("Search_TourDurationFormat"), x.Tour.Duration),
                CountDisplay = string.Format(T("Search_LocationCountFormat"), x.Tour.LocationIds.Count),
                ResultType = SearchResultType.Tour
            })
            .ToList();

        return filteredTours;
    }

    private static bool IsTourKeywordQuery(string query)
    {
        var normalized = SearchService.NormalizeSearchText(query).Trim();
        return string.Equals(normalized, "tour", StringComparison.Ordinal);
    }

    private string T(string key) => _localizationService.GetString(key);

    private static FormattedString BuildHighlightedText(string text, string query)
    {
        var formatted = new FormattedString();
        if (string.IsNullOrWhiteSpace(text))
        {
            formatted.Spans.Add(new Span { Text = string.Empty });
            return formatted;
        }

        var tokens = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeToken)
            .Where(token => token.Length >= 2)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (tokens.Length == 0)
        {
            formatted.Spans.Add(new Span { Text = text });
            return formatted;
        }

        var ranges = new List<(int Start, int End)>();
        BuildNormalizedTextMap(text, out var normalizedText, out var normalizedToOriginalIndex);

        foreach (var token in tokens)
        {
            var startIndex = 0;
            while (startIndex < normalizedText.Length)
            {
                var idx = normalizedText.IndexOf(token, startIndex, StringComparison.Ordinal);
                if (idx < 0)
                {
                    break;
                }

                var originalStart = normalizedToOriginalIndex[idx];
                var originalEnd = normalizedToOriginalIndex[Math.Min(idx + token.Length - 1, normalizedToOriginalIndex.Count - 1)] + 1;
                ranges.Add((originalStart, originalEnd));
                startIndex = idx + token.Length;
            }
        }

        if (ranges.Count == 0)
        {
            formatted.Spans.Add(new Span { Text = text });
            return formatted;
        }

        var merged = MergeRanges(ranges);
        var cursor = 0;
        foreach (var range in merged)
        {
            if (range.Start > cursor)
            {
                formatted.Spans.Add(new Span { Text = text[cursor..range.Start] });
            }

            formatted.Spans.Add(new Span
            {
                Text = text[range.Start..range.End],
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#E74C3C")
            });

            cursor = range.End;
        }

        if (cursor < text.Length)
        {
            formatted.Spans.Add(new Span { Text = text[cursor..] });
        }

        return formatted;
    }

    private static List<(int Start, int End)> MergeRanges(List<(int Start, int End)> ranges)
    {
        var ordered = ranges.OrderBy(x => x.Start).ThenBy(x => x.End).ToList();
        var merged = new List<(int Start, int End)>();

        foreach (var range in ordered)
        {
            if (merged.Count == 0)
            {
                merged.Add(range);
                continue;
            }

            var last = merged[^1];
            if (range.Start <= last.End)
            {
                merged[^1] = (last.Start, Math.Max(last.End, range.End));
            }
            else
            {
                merged.Add(range);
            }
        }

        return merged;
    }

    private static string NormalizeToken(string input)
    {
        return SearchService.NormalizeSearchText(input);
    }

    private static void BuildNormalizedTextMap(string source, out string normalizedText, out List<int> normalizedToOriginalIndex)
    {
        var builder = new StringBuilder(source.Length);
        normalizedToOriginalIndex = new List<int>(source.Length);

        for (var i = 0; i < source.Length; i++)
        {
            var folded = FoldForSearch(source[i]);
            foreach (var ch in folded)
            {
                builder.Append(ch);
                normalizedToOriginalIndex.Add(i);
            }
        }

        normalizedText = builder.ToString();
    }

    private static string FoldForSearch(char input)
    {
        var normalized = input.ToString().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark ||
                category == UnicodeCategory.SpacingCombiningMark ||
                category == UnicodeCategory.EnclosingMark)
            {
                continue;
            }

            builder.Append(ch);
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D')
            .ToLowerInvariant();
    }
}

public class CategoryFilter : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public class SearchResultItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int AudioCount { get; set; }
    public int TotalDurationMinutes { get; set; }
    public string DurationDisplay { get; set; } = string.Empty;
    public string CountDisplay { get; set; } = string.Empty;
    public SearchResultType ResultType { get; set; } = SearchResultType.Location;
}

public enum SearchResultType
{
    Location = 0,
    Tour = 1
}
