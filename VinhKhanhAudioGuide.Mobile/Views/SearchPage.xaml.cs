using VinhKhanhAudioGuide.Mobile.ViewModels;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class SearchPage : ContentPage, IQueryAttributable
{
    public SearchPage(SearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BindingContext is not SearchViewModel viewModel)
        {
            return;
        }

        query.TryGetValue("InitialQuery", out var initialQueryObj);
        query.TryGetValue("InitialCategoryId", out var initialCategoryIdObj);

        var initialQuery = initialQueryObj?.ToString();
        var initialCategoryId = initialCategoryIdObj?.ToString();

        _ = viewModel.ApplyNavigationContextAsync(initialQuery, initialCategoryId);
    }
}
