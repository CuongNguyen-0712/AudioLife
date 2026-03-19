using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Data;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

[QueryProperty(nameof(LocationId), "LocationId")]
[QueryProperty(nameof(AudioUrl), "AudioUrl")]
public partial class AudioPlayerViewModel : ObservableObject
{
    private readonly IAudioService _audioService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _locationId = string.Empty;

    [ObservableProperty]
    private string _audioUrl = string.Empty;

    [ObservableProperty]
    private string _title = "Audio Guide";

    [ObservableProperty]
    private string _locationName = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _imageUrl = string.Empty;

    [ObservableProperty]
    private string _transcriptText = string.Empty;

    [ObservableProperty]
    private double _duration;

    [ObservableProperty]
    private double _currentPosition;

    [ObservableProperty]
    private string _currentPositionText = "0:00";

    [ObservableProperty]
    private string _durationText = "0:00";

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private string _playPauseIcon = "play_icon.png";

    public AudioPlayerViewModel(IAudioService audioService, INavigationService navigationService)
    {
        _audioService = audioService;
        _navigationService = navigationService;

        _audioService.PositionChanged += OnPositionChanged;
        _audioService.StateChanged += OnStateChanged;
    }

    private void OnPositionChanged(object? sender, AudioPositionChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentPosition = e.Position.TotalSeconds;
            CurrentPositionText = FormatTime(e.Position);
        });
    }

    private void OnStateChanged(object? sender, AudioStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsPlaying = e.State == AudioPlaybackState.Playing;
            PlayPauseIcon = IsPlaying ? "pause_icon.png" : "play_icon.png";
        });
    }

    partial void OnLocationIdChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            LoadLocationData(value);
        }
    }

    private void LoadLocationData(string locationId)
    {
        var location = SampleData.GetLocations()
            .FirstOrDefault(l => l.Id == locationId);

        if (location != null)
        {
            LocationName = location.Name;
            Description = location.Description;
            ImageUrl = location.ImageUrl;
            TranscriptText = $"Audio hu?ng d?n tham quan {location.Name}...";
            Title = location.Name;

            if (location.AudioGuides.Count > 0)
            {
                var guide = location.AudioGuides[0];
                Duration = guide.Duration * 60; // minutes to seconds
                DurationText = FormatTime(TimeSpan.FromSeconds(Duration));
                if (string.IsNullOrEmpty(AudioUrl))
                    AudioUrl = guide.AudioUrl;
            }
        }
        else
        {
            LocationName = "Ð?a di?m";
            Description = string.Empty;
            ImageUrl = string.Empty;
            Duration = 0;
            DurationText = "0:00";
            Title = LocationName;
        }
    }

    [RelayCommand]
    private async Task PlayPauseAsync()
    {
        if (IsPlaying)
        {
            await _audioService.PauseAsync();
        }
        else
        {
            // Resume if same audio, otherwise play new
            if (_audioService.CurrentAudioUrl == AudioUrl && _audioService.CurrentPosition > TimeSpan.Zero)
            {
                await _audioService.ResumeAsync();
            }
            else
            {
                await _audioService.PlayAsync(AudioUrl);
            }
        }
    }

    [RelayCommand]
    private async Task RewindAsync()
    {
        await _audioService.SeekAsync(TimeSpan.FromSeconds(Math.Max(0, CurrentPosition - 10)));
    }

    [RelayCommand]
    private async Task ForwardAsync()
    {
        await _audioService.SeekAsync(TimeSpan.FromSeconds(Math.Min(Duration, CurrentPosition + 10)));
    }

    private static string FormatTime(TimeSpan time)
    {
        return time.Hours > 0 
            ? $"{time.Hours}:{time.Minutes:D2}:{time.Seconds:D2}" 
            : $"{time.Minutes}:{time.Seconds:D2}";
    }
}
