using VinhKhanhAudioGuide.Mobile.ViewModels;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _viewModel;

    public MapPage(MapViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadMapDataAsync();
    }

    private async void MapWebView_OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        const string poiPrefix = "app://poi/";
        if (string.IsNullOrWhiteSpace(e.Url) || !e.Url.StartsWith(poiPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        e.Cancel = true;

        if (BindingContext is not MapViewModel viewModel)
        {
            return;
        }

        var encodedId = e.Url[poiPrefix.Length..];
        var locationId = Uri.UnescapeDataString(encodedId);
        if (string.IsNullOrWhiteSpace(locationId))
        {
            return;
        }

        await viewModel.OpenPoiDetailByIdFromMapAsync(locationId);
    }
}
