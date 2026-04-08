using System.Collections.ObjectModel;
using System.ComponentModel;
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
    private string _playPauseGlyph = "play_white_icon.svg";

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

    [ObservableProperty]
    private int _currentAudioGuideIndex;

    [ObservableProperty]
    private bool _canPlayNext;

    [ObservableProperty]
    private bool _canPlayBack;

    [ObservableProperty]
    private string _currentPlayingAudioGuideId = string.Empty;

    public ObservableCollection<AudioGuide> AudioGuides { get; } = new();
    public ObservableCollection<ScriptSegmentViewModel> ScriptSegments { get; } = new();

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
                AudioPlaybackState.Loading => "loading.svg",
                AudioPlaybackState.Playing => "pause.svg",
                _ => "play_white_icon.svg"
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

                // Load all audio guides
                AudioGuides.Clear();
                foreach (var audio in location.AudioGuides)
                {
                    AudioGuides.Add(audio);
                }

                if (AudioGuides.Count > 0)
                {
                    // Find and select the appropriate audio guide
                    var selectedIndex = 0;
                    
                    if (!string.IsNullOrWhiteSpace(AudioGuideId))
                    {
                        selectedIndex = AudioGuides.ToList().FindIndex(g => g.Id == AudioGuideId);
                        if (selectedIndex == -1) selectedIndex = 0;
                    }
                    else if (!string.IsNullOrWhiteSpace(AudioUrl))
                    {
                        selectedIndex = AudioGuides.ToList().FindIndex(g =>
                            string.Equals(g.AudioUrl, AudioUrl, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(g.CloudinaryAudioUrl, AudioUrl, StringComparison.OrdinalIgnoreCase));
                        if (selectedIndex == -1) selectedIndex = 0;
                    }

                    CurrentAudioGuideIndex = selectedIndex;
                    UpdateCurrentAudioGuide();
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
                AudioGuides.Clear();
            }
        }
        finally
        {
            _isLoadingLocationData = false;
        }
    }

    private void UpdateCurrentAudioGuide()
    {
        if (CurrentAudioGuideIndex < 0 || CurrentAudioGuideIndex >= AudioGuides.Count)
        {
            return;
        }

        var guide = AudioGuides[CurrentAudioGuideIndex];
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
            : LocationName;

        AudioGuideId = guide.Id;
        CurrentPlayingAudioGuideId = guide.Id;
        AudioUrl = !string.IsNullOrWhiteSpace(guide.CloudinaryAudioUrl)
            ? guide.CloudinaryAudioUrl
            : guide.AudioUrl;

        UpdateScriptSegmentsForAudio();
        UpdateNextBackButtonStates();
        SyncPlaybackStateFromService();
    }

    private void UpdateNextBackButtonStates()
    {
        CanPlayNext = CurrentAudioGuideIndex < AudioGuides.Count - 1;
        CanPlayBack = CurrentAudioGuideIndex > 0;
    }

    private void UpdateScriptSegmentsForAudio()
    {
        if (CurrentAudioGuideIndex < 0 || CurrentAudioGuideIndex >= AudioGuides.Count)
        {
            return;
        }

        var guide = AudioGuides[CurrentAudioGuideIndex];
        LoadScriptSegments(guide);
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
            CheckAutoAdvanceAudio();
            return;
        }

        var currentSeconds = position.TotalSeconds;
        var activeSegment = _scriptSegments
            .LastOrDefault(segment => currentSeconds >= segment.StartTimeSeconds && currentSeconds < segment.EndTimeSeconds)
            ?? (currentSeconds >= _scriptSegments.Last().EndTimeSeconds ? _scriptSegments.Last() : _scriptSegments.First());

        if (activeSegment.Id == _activeScriptSegmentId)
        {
            CheckAutoAdvanceAudio();
            return;
        }

        _activeScriptSegmentId = activeSegment.Id;
        CurrentScriptText = activeSegment.ScriptText;
        CheckAutoAdvanceAudio();
    }

    private void SyncPlaybackStateFromService()
    {
        var currentAudioMatches = IsCurrentAudio(_audioService.CurrentAudioUrl);

        IsPlaying = currentAudioMatches && _audioService.IsPlaying;
        PlayPauseGlyph = IsPlaying ? "pause.svg" : "play_white_icon.svg";

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

    [RelayCommand]
    private void PlayNext()
    {
        if (CanPlayNext)
        {
            CurrentAudioGuideIndex++;
            UpdateCurrentAudioGuide();
        }
    }

    [RelayCommand]
    private void PlayBack()
    {
        if (CanPlayBack)
        {
            CurrentAudioGuideIndex--;
            UpdateCurrentAudioGuide();
        }
    }

    [RelayCommand]
    private async Task PlayAudioGuideAsync(AudioGuide audioGuide)
    {
        if (audioGuide == null)
        {
            return;
        }

        var index = AudioGuides.IndexOf(audioGuide);
        if (index >= 0)
        {
            CurrentAudioGuideIndex = index;
            UpdateCurrentAudioGuide();
            // Auto-play the selected audio guide
            await PlayPauseAsync();
        }
    }

    partial void OnCurrentAudioGuideIndexChanged(int value)
    {
        UpdateNextBackButtonStates();
        if (CurrentAudioGuideIndex >= 0 && CurrentAudioGuideIndex < AudioGuides.Count)
        {
            UpdateCurrentAudioGuide();
        }
    }

    // Check if a specific segment is the active one currently playing
    public bool IsSegmentActive(AudioScriptSegment segment)
    {
        return segment.Id == _activeScriptSegmentId;
    }

    // Check for auto-advance when audio finishes
    private void CheckAutoAdvanceAudio()
    {
        if (IsPlaying || CurrentPosition < Duration - 0.5)
        {
            return; // Still playing or not at the end yet
        }

        // Audio has finished, auto-advance to next if available
        if (CanPlayNext)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PlayNext();
            });
        }
    }

    private static string FormatTime(TimeSpan time)
    {
        return time.Hours > 0 
            ? $"{time.Hours}:{time.Minutes:D2}:{time.Seconds:D2}" 
            : $"{time.Minutes}:{time.Seconds:D2}";
    }
}

// Helper class for binding script segments with active state
public class ScriptSegmentViewModel : INotifyPropertyChanged
{
    private bool _isActive;
    public AudioScriptSegment Segment { get; set; } = new();

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value)
            {
                _isActive = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
