using VinhKhanhAudioGuide.Mobile.ViewModels;
using System.Globalization;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class AudioPlayerPage : ContentPage
{
    private readonly AudioPlayerViewModel _viewModel;

    public AudioPlayerPage(AudioPlayerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        _viewModel.OnDisappearing();
        base.OnDisappearing();
    }
}

/// <summary>
/// Converter to highlight currently playing audio and show pause button
/// </summary>
public class IsCurrentAudioConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            if (targetType == typeof(Color))
                return Colors.Transparent;
            return "▶"; // Play icon
        }

        var isCurrentAudio = value.ToString() == parameter.ToString();

        if (targetType == typeof(Color))
        {
            // SecondaryContainer color for currently playing audio
            return isCurrentAudio ? Color.FromHex("#C5E6E8") : Colors.Transparent;
        }

        // Return pause icon for playing, play icon for others
        return isCurrentAudio ? "⏸" : "▶";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
