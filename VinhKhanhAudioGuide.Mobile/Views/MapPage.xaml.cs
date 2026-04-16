using VinhKhanhAudioGuide.Mobile.ViewModels;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class MapPage : ContentPage, IQueryAttributable
{
    private readonly MapViewModel _viewModel;
    private bool _isHandlingExit;

    public MapPage(MapViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _viewModel.ApplyQueryAttributes(query);
    }

    protected override void OnDisappearing()
    {
        _viewModel.OnDisappearing();
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_isHandlingExit)
        {
            return true;
        }

        _ = HandleBackAsync();
        return true;
    }

    private async Task HandleBackAsync()
    {
        _isHandlingExit = true;
        try
        {
            var handled = await _viewModel.RequestExitTourAsync();
            if (!handled)
            {
                await Shell.Current.GoToAsync("..");
            }
        }
        finally
        {
            _isHandlingExit = false;
        }
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
        