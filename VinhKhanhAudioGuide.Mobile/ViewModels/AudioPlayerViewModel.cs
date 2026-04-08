using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

[QueryProperty(nameof(LocationId), "LocationId")]
[QueryProperty(nameof(AudioUrl), "AudioUrl")]
[QueryProperty(nameof(AudioGuideId), "AudioGuideId")]
public partial class AudioPlayerViewModel : ObservableObject
{
    private readonly IAudioService _audioService;
    private readonly IApiService _apiService;
    private readonly List<AudioScriptSegment> _scriptSegments = new();
    private int _activeScriptSegmentId = -1;
    private bool _isSubscribedToAudioEvents;
    private bool _isLoadingLocationData;

    [ObservableProperty]
    private string _locationId = string.Empty;

    [ObservableProperty]
    private string _audioUrl = string.Empty;

    [ObservableProperty]
    private string _audioGuideId = string.Empty;

    [ObservableProperty]
    private string _title = "Audio Guide";

    [ObservableProperty]
    private string _locationName = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _imageUrl = string.Empty;

    [ObservableProperty]
    private string _audioTranslationText = string.Empty;

    [ObservableProperty]
    private string _audioTranslationSummary = string.Empty;

    [ObservableProperty]
    private string _currentScriptText = string.Empty;

    [ObservableProperty]
    private string _playPauseGlyph = "▶";

    [ObservableProperty]
    private string _rewindGlyph = "⟲10";

    [ObservableProperty]
    private string _forwardGlyph = "10⟳";

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
    private bool _isSliderDragging;

    public AudioPlayerViewModel(IAudioService audioService, IApiService apiService)
    {
        _audioService = audioService;
        _apiService = apiService;
    }

    public void OnAppearing()
    {
        if (!_isSubscribedToAudioEvents)
        {
            _audioService.PositionChanged += OnPositionChanged;
            _audioService.StateChanged += OnStateChanged;
            _isSubscribedToAudioEvents = true;
        }

        SyncPlaybackStateFromService();
    }

    public void OnDisappearing()
    {
        if (!_isSubscribedToAudioEvents)
        {
            return;
        }

        _audioService.PositionChanged -= OnPositionChanged;
        _audioService.StateChanged -= OnStateChanged;
        _isSubscribedToAudioEvents = false;
    }

    private void OnPositionChanged(object? sender, AudioPositionChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (IsSliderDragging)
            {
                return;
            }

            CurrentPosition = e.Position.TotalSeconds;
            CurrentPositionText = FormatTime(e.Position);

            if (e.Duration > TimeSpan.Zero)
            {
                Duration = e.Duration.TotalSeconds;
                DurationText = FormatTime(e.Duration);
            }

            UpdateCurrentTranslationText(e.Position);
        });
    }

    private void OnStateChanged(object? sender, AudioStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!IsCurrentAudio(e.AudioUrl))
            {
                return;
            }

            IsPlaying = e.State == AudioPlaybackState.Playing;
            PlayPauseGlyph = e.State switch
            {
                AudioPlaybackState.Loading => "⏳",
                AudioPlaybackState.Playing => "⏸",
                _ => "▶"
            };
        });
    }

    partial void OnLocationIdChanged(string value)
    {
        if (!string.IsNullOrEmpty(value) && !_isLoadingLocationData)
        {
            _ = LoadLocationDataAsync(value);
        }
    }

    partial void OnAudioGuideIdChanged(string value)
    {
        if (!string.IsNullOrEmpty(LocationId) && !_isLoadingLocationData)
        {
            _ = LoadLocationDataAsync(LocationId);
        }
    }

    partial void OnAudioUrlChanged(string value)
    {
        if (!string.IsNullOrEmpty(LocationId) && !_isLoadingLocationData)
        {
            _ = LoadLocationDataAsync(LocationId);
        }
    }

    private async Task LoadLocationDataAsync(string locationId)
    {
        _isLoadingLocationData = true;

        try
        {
            var location = await _apiService.GetLocationByIdAsync(locationId);

            if (location == null)
            {
                var allLocations = await _apiService.GetLocationsAsync();
                location = allLocations.ElementAtOrDefault(int.TryParse(locationId, out var idx) ? idx - 1 : -1);
            }

            if (location != null)
            {
                LocationName = location.Name;
                Description = location.Description;
                ImageUrl = location.ImageUrl;
                AudioTranslationSummary = $"Bản dịch audio của {location.Name}";
                AudioTranslationText = $"Đang chờ phát nội dung của {location.Name}...";
                CurrentScriptText = AudioTranslationText;
                Title = location.Name;

                if (location.AudioGuides.Count > 0)
                {
                    var guide = SelectGuide(location.AudioGuides);
                    Duration = guide.Duration * 60; // minutes to seconds
                    DurationText = FormatTime(TimeSpan.FromSeconds(Duration));

                    AudioTranslationText = !string.IsNullOrWhiteSpace(guide.TranscriptText)
                        ? guide.TranscriptText
                        : guide.Description;

                    AudioTranslationSummary = !string.IsNullOrWhiteSpace(guide.Description)
                        ? guide.Description
                        : AudioTranslationSummary;

                    Title = !string.IsNullOrWhiteSpace(guide.Title)
                        ? guide.Title
                        : location.Name;

                    AudioGuideId = guide.Id;
                    AudioUrl = !string.IsNullOrWhiteSpace(guide.CloudinaryAudioUrl)
                        ? guide.CloudinaryAudioUrl
                        : guide.AudioUrl;

                    LoadScriptSegments(guide);
                    SyncPlaybackStateFromService();
                }
            }
            else
            {
                LocationName = "Địa điểm";
                Description = string.Empty;
                ImageUrl = string.Empty;
                Duration = 0;
                DurationText = "0:00";
                Title = LocationName;
                AudioTranslationSummary = string.Empty;
                AudioTranslationText = string.Empty;
                CurrentScriptText = string.Empty;
                _scriptSegments.Clear();
                _activeScriptSegmentId = -1;
            }
        }
        finally
        {
            _isLoadingLocationData = false;
        }
    }

    private void LoadScriptSegments(AudioGuide guide)
    {
        _scriptSegments.Clear();
        _activeScriptSegmentId = -1;

        var segments = guide.ScriptSegments
            .OrderBy(s => s.StartTimeSeconds)
            .ToList();

        if (segments.Count == 0)
        {
            _scriptSegments.Add(new AudioScriptSegment
            {
                Id = 1,
                AudioGuideId = guide.Id,
                StartTimeSeconds = 0,
                EndTimeSeconds = Math.Max(1, (int)Duration),
                ScriptText = !string.IsNullOrWhiteSpace(guide.TranscriptText) ? guide.TranscriptText : guide.Description
            });
            return;
        }

        _scriptSegments.AddRange(segments);
    }

    private void UpdateCurrentTranslationText(TimeSpan position)
    {
        if (_scriptSegments.Count == 0)
        {
            CurrentScriptText = AudioTranslationText;
            return;
        }

        var currentSeconds = position.TotalSeconds;
        var activeSegment = _scriptSegments
            .LastOrDefault(segment => currentSeconds >= segment.StartTimeSeconds && currentSeconds < segment.EndTimeSeconds)
            ?? (currentSeconds >= _scriptSegments.Last().EndTimeSeconds ? _scriptSegments.Last() : _scriptSegments.First());

        if (activeSegment.Id == _activeScriptSegmentId)
        {
            return;
        }

        _activeScriptSegmentId = activeSegment.Id;
        CurrentScriptText = activeSegment.ScriptText;
    }

    private void SyncPlaybackStateFromService()
    {
        var currentAudioMatches = IsCurrentAudio(_audioService.CurrentAudioUrl);

        IsPlaying = currentAudioMatches && _audioService.IsPlaying;
        PlayPauseGlyph = IsPlaying ? "⏸" : "▶";

        if (!currentAudioMatches)
        {
            CurrentPosition = 0;
            CurrentPositionText = "0:00";
            UpdateCurrentTranslationText(TimeSpan.Zero);
            return;
        }

        var servicePosition = _audioService.CurrentPosition;
        CurrentPosition = Math.Max(0, servicePosition.TotalSeconds);
        CurrentPositionText = FormatTime(servicePosition);

        if (_audioService.Duration > TimeSpan.Zero)
        {
            Duration = _audioService.Duration.TotalSeconds;
            DurationText = FormatTime(_audioService.Duration);
        }

        UpdateCurrentTranslationText(servicePosition);
    }

    private bool IsCurrentAudio(string? audioUrl)
    {
        if (string.IsNullOrWhiteSpace(AudioUrl) || string.IsNullOrWhiteSpace(audioUrl))
        {
            return false;
        }

        return string.Equals(AudioUrl, audioUrl, StringComparison.OrdinalIgnoreCase);
    }

    private AudioGuide SelectGuide(IReadOnlyList<AudioGuide> guides)
    {
        if (!string.IsNullOrWhiteSpace(AudioGuideId))
        {
            var byId = guides.FirstOrDefault(g => g.Id == AudioGuideId);
            if (byId != null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(AudioUrl))
        {
            var byUrl = guides.FirstOrDefault(g =>
                string.Equals(g.AudioUrl, AudioUrl, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(g.CloudinaryAudioUrl, AudioUrl, StringComparison.OrdinalIgnoreCase));
            if (byUrl != null)
            {
                return byUrl;
            }
        }

        return guides[0];
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
        var nextPosition = Math.Min(Duration, CurrentPosition + 10);
        await _audioService.SeekAsync(TimeSpan.FromSeconds(nextPosition));
    }

    [RelayCommand]
    private void BeginSeek()
    {
        IsSliderDragging = true;
    }

    [RelayCommand]
    private async Task CompleteSeekAsync()
    {
        IsSliderDragging = false;
        await _audioService.SeekAsync(TimeSpan.FromSeconds(CurrentPosition));
        CurrentPositionText = FormatTime(TimeSpan.FromSeconds(CurrentPosition));
    }

    private static string FormatTime(TimeSpan time)
    {
        return time.Hours > 0 
            ? $"{time.Hours}:{time.Minutes:D2}:{time.Seconds:D2}" 
            : $"{time.Minutes}:{time.Seconds:D2}";
    }
}
