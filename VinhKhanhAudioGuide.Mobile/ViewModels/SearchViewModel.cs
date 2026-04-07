using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

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
    private bool _allSelected = true;

    public ObservableCollection<CategoryFilter> Categories { get; } = new();
    public ObservableCollection<string> RecentSearches { get; } = new();
    public ObservableCollection<SearchResultItem> SearchResults { get; } = new();

    public SearchViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        LoadCategories();
        LoadRecentSearches();
    }

    private void LoadCategories()
    {
        var categories = Data.SampleData.GetCategories();
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

    private void LoadRecentSearches()
    {
        // Load from preferences
        RecentSearches.Add("Bánh xèo miền Tây");
        RecentSearches.Add("Trà sữa trân châu");
        RecentSearches.Add("Ốc xào bơ tỏi");
        RecentSearches.Add("Tôm nướng muối ớt");
    }

    partial void OnSearchQueryChanged(string value)
    {
        HasSearchQuery = !string.IsNullOrWhiteSpace(value?.Trim());
        ShowRecentSearches = !HasSearchQuery;

        if (!HasSearchQuery)
        {
            ShowResults = false;
            ShowNoResults = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var normalizedQuery = NormalizeSearchText(SearchQuery);
        if (string.IsNullOrWhiteSpace(normalizedQuery))
            return;

        IsLoading = true;
        ShowRecentSearches = false;
        ShowResults = false;
        ShowNoResults = false;

        await Task.Delay(500); // Simulate search

        var allLocations = Data.SampleData.GetLocations();
        var categories = Data.SampleData.GetCategories();

        SearchResults.Clear();

        var selectedCategoryIds = Categories.Where(c => c.IsSelected).Select(c => c.Id).ToList();

        foreach (var location in allLocations)
        {
            var normalizedName = NormalizeSearchText(location.Name);
            var normalizedDescription = NormalizeSearchText(location.Description);

            if (normalizedName.Contains(normalizedQuery, StringComparison.Ordinal) ||
                normalizedDescription.Contains(normalizedQuery, StringComparison.Ordinal))
            {
                if (!AllSelected && selectedCategoryIds.Any() && !selectedCategoryIds.Contains(location.CategoryId))
                    continue;

                var category = categories.FirstOrDefault(c => c.Id == location.CategoryId);
                SearchResults.Add(new SearchResultItem
                {
                    Id = location.Id,
                    Name = location.Name,
                    Description = location.Description,
                    ImageUrl = location.ImageUrl,
                    CategoryName = category?.Name ?? ""
                });
            }
        }

        // Add to recent searches
        var cleanedQuery = SearchQuery.Trim().Normalize(NormalizationForm.FormC);
        if (!RecentSearches.Any(existing => NormalizeSearchText(existing) == normalizedQuery))
        {
            RecentSearches.Insert(0, cleanedQuery);
            if (RecentSearches.Count > 10)
                RecentSearches.RemoveAt(RecentSearches.Count - 1);
        }

        IsLoading = false;
        ShowResults = SearchResults.Count > 0;
        ShowNoResults = SearchResults.Count == 0;
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        SearchResults.Clear();
        ShowResults = false;
        ShowNoResults = false;
        ShowRecentSearches = true;
    }

    [RelayCommand]
    private void SelectAll()
    {
        AllSelected = true;
        foreach (var cat in Categories)
        {
            cat.IsSelected = false;
        }
    }

    [RelayCommand]
    private void SelectCategory(CategoryFilter? category)
    {
        if (category is null) return;

        AllSelected = false;
        category.IsSelected = !category.IsSelected;

        if (!Categories.Any(c => c.IsSelected))
        {
            AllSelected = true;
        }
    }

    [RelayCommand]
    private async Task UseRecentSearchAsync(string? query)
    {
        if (string.IsNullOrEmpty(query)) return;

        SearchQuery = query.Trim().Normalize(NormalizationForm.FormC);
        await SearchAsync();
    }

    [RelayCommand]
    private void RemoveRecentSearch(string? query)
    {
        if (string.IsNullOrEmpty(query)) return;
        RecentSearches.Remove(query);
    }

    [RelayCommand]
    private async Task LocationSelectedAsync(SearchResultItem? item)
    {
        if (item is null) return;

        await _navigationService.NavigateToAsync(nameof(Views.LocationDetailPage),
            new Dictionary<string, object> { { "LocationId", item.Id } });
    }

    private static string NormalizeSearchText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

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
}
