using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VinhKhanhAudioGuide.Mobile.Messages;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

[QueryProperty(nameof(LocationId), "LocationId")]
[QueryProperty(nameof(AudioUrl), "AudioUrl")]
[QueryProperty(nameof(AudioGuideId), "AudioGuideId")]
[QueryProperty(nameof(PlaybackSource), "PlaybackSource")]
[QueryProperty(nameof(ResumePositionSeconds), "ResumePositionSeconds")]
public partial class AudioPlayerViewModel : ObservableObject
{
    private const string PreferredAudioGuideKeyPrefix = "AutoNearestPreferredAudioGuide:";
    private const string PreferredAudioUrlKeyPrefix = "AutoNearestPreferredAudioUrl:";

    private readonly INavigationService _navigationService;
    private readonly IAudioService _audioService;
    private readonly IApiService _apiService;
    private readonly ITourPlaybackSessionService _tourPlaybackSessionService;
    private readonly ILocalizationService _localizationService;
    private readonly IAutoPlaybackService _autoPlaybackService;
    private readonly SemaphoreSlim _guideSelectionLock = new(1, 1);
    private readonly SemaphoreSlim _playerActionLock = new(1, 1);
    private readonly Dictionary<string, DateTime> _lastActionAt = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _actionSync = new();
    private readonly List<AudioScriptSegment> _scriptSegments = new();
    private static readonly TimeSpan ActionThrottleWindow = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan HistorySyncInterval = TimeSpan.FromSeconds(10);
    private const double HistorySyncProgressDelta = 0.03;
    private int _activeScriptSegmentId = -1;
    private bool _isSubscribedToAudioEvents;
    private bool _isLoadingLocationData;
    private bool _isApplyingGuideInternally;
    private bool _isAutoAdvancing;
    private DateTime _lastHistorySyncAtUtc = DateTime.MinValue;
    private string _lastHistoryAudioGuideId = string.Empty;
    private double _lastSyncedProgress;

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
    private bool _isSwitchingGuide;

    [ObservableProperty]
    private int _currentAudioGuideIndex;

    [ObservableProperty]
    private bool _canPlayNext;

    [ObservableProperty]
    private bool _canPlayBack;

    [ObservableProperty]
    private string _currentPlayingAudioGuideId = string.Empty;

    [ObservableProperty]
    private string _playbackSource = "Manual";

    [ObservableProperty]
    private bool _isAutoPlaybackSource;

    [ObservableProperty]
    private bool _isTourPlaybackSource;

    [ObservableProperty]
    private string _playbackSourceText = string.Empty;

    [ObservableProperty]
    private double _resumePositionSeconds;

    private bool _resumePositionApplied;

    public ObservableCollection<AudioGuideItemViewModel> AudioGuides { get; } = new();

    public AudioPlayerViewModel(
        INavigationService navigationService,
        IAudioService audioService,
        IApiService apiService,
        ITourPlaybackSessionService tourPlaybackSessionService,
        ILocalizationService localizationService,
        IAutoPlaybackService autoPlaybackService)
    {
        _navigationService = navigationService;
        _audioService = audioService;
        _apiService = apiService;
        _tourPlaybackSessionService = tourPlaybackSessionService;
        _localizationService = localizationService;
        _autoPlaybackService = autoPlaybackService;
        UpdatePlaybackSourceUi();
    }

    partial void OnPlaybackSourceChanged(string value)
    {
        UpdatePlaybackSourceUi();
    }

    partial void OnResumePositionSecondsChanged(double value)
    {
        _resumePositionApplied = false;
    }

    private void UpdatePlaybackSourceUi()
    {
        IsAutoPlaybackSource = string.Equals(PlaybackSource, "AutoNearest", StringComparison.OrdinalIgnoreCase);
        IsTourPlaybackSource = PlaybackSource.StartsWith("Tour", StringComparison.OrdinalIgnoreCase);
        PlaybackSourceText = IsTourPlaybackSource
            ? T("AudioPlayer_PlaybackSourceTour")
            : (IsAutoPlaybackSource ? T("AudioPlayer_PlaybackSourceAutoNearest") : T("AudioPlayer_PlaybackSourceManual"));
    }

    private void MarkManualPlaybackSource()
    {
        PlaybackSource = "Manual";
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

    public async Task OnAppearingAsync()
    {
        OnAppearing();

        if (!string.IsNullOrWhiteSpace(LocationId) && AudioGuides.Count == 0 && !_isLoadingLocationData)
        {
            await LoadLocationDataAsync(LocationId);
        }
    }

    public void OnDisappearing()
    {
        _ = SyncListeningHistoryAsync(force: true);

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

            CurrentPosition = Math.Max(0, e.Position.TotalSeconds);
            CurrentPositionText = FormatTime(TimeSpan.FromSeconds(CurrentPosition));

            if (e.Duration > TimeSpan.Zero)
            {
                Duration = e.Duration.TotalSeconds;
                DurationText = FormatTime(e.Duration);
            }

            UpdateCurrentScriptText(TimeSpan.FromSeconds(CurrentPosition));
            _ = SyncListeningHistoryAsync();
        });
    }

    // Nhận state từ AudioService để cập nhật icon, highlight và auto-advance khi hết bài.
    // Thuộc flow state management chính của AudioPlayer.
    private void OnStateChanged(object? sender, AudioStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var matchedIndex = FindGuideIndexByAudioUrl(e.AudioUrl);
            var isCurrentAudio = IsCurrentAudio(e.AudioUrl);
            if (matchedIndex < 0 && !isCurrentAudio)
            {
                return;
            }

            // Ignore stop signal from an old track while user is switching to another one.
            if (e.State == AudioPlaybackState.Stopped && matchedIndex >= 0 && matchedIndex != CurrentAudioGuideIndex)
            {
                return;
            }

            if (matchedIndex >= 0)
            {
                if (matchedIndex != CurrentAudioGuideIndex)
                {
                    _ = SetCurrentAudioGuideAsync(matchedIndex, autoPlay: false, forceRestart: false, persistAsUserSelection: false);
                }

                CurrentPlayingAudioGuideId = AudioGuides[matchedIndex].Id;
            }

            IsPlaying = e.State == AudioPlaybackState.Playing;
            IsSwitchingGuide = e.State == AudioPlaybackState.Loading;
            PlayPauseGlyph = e.State switch
            {
                AudioPlaybackState.Loading => "loading.svg",
                AudioPlaybackState.Playing => "pause.svg",
                _ => "play_white_icon.svg"
            };

            if (!IsPlaying && e.State == AudioPlaybackState.Stopped)
            {
                CurrentPlayingAudioGuideId = string.Empty;
                _ = SyncListeningHistoryAsync(force: true);

                if (!_isAutoAdvancing && ShouldAutoPlayNextWithinPoi() && matchedIndex == CurrentAudioGuideIndex && CanPlayNext)
                {
                    _ = AutoAdvanceToNextGuideAsync();
                    return;
                }

                if (!_isAutoAdvancing && IsTourPlaybackSource && matchedIndex == CurrentAudioGuideIndex)
                {
                    _ = AdvanceTourLocationAsync();
                }
            }

            ApplyAudioGuideHighlightState();
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
        if (!string.IsNullOrEmpty(LocationId) && !_isLoadingLocationData && !_isApplyingGuideInternally)
        {
            _ = LoadLocationDataAsync(LocationId);
        }
    }

    partial void OnAudioUrlChanged(string value)
    {
        if (!string.IsNullOrEmpty(LocationId) && !_isLoadingLocationData && !_isApplyingGuideInternally)
        {
            _ = LoadLocationDataAsync(LocationId);
        }
    }

    // Load toàn bộ dữ liệu POI + danh sách audio guide rồi bind lên màn hình player.
    // Thuộc flow mở AudioPlayer từ manual/auto/tour.
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

            if (location == null)
            {
                ResetScreen();
                return;
            }

            LocationName = location.Name;
            Description = location.Description;
            ImageUrl = location.ImageUrl;
            Title = location.Name;

            AudioGuides.Clear();
            foreach (var guide in location.AudioGuides)
            {
                AudioGuides.Add(new AudioGuideItemViewModel(guide));
            }

            if (AudioGuides.Count == 0)
            {
                if (IsTourPlaybackSource)
                {
                    await AdvanceTourLocationAsync();
                    return;
                }

                ResetPlaybackState();
                return;
            }

            var selectedIndex = ResolveInitialGuideIndex();
            await SetCurrentAudioGuideAsync(selectedIndex, autoPlay: IsTourPlaybackSource, forceRestart: false, persistAsUserSelection: false);

            if (!_resumePositionApplied && ResumePositionSeconds > 0 && !string.IsNullOrWhiteSpace(AudioUrl))
            {
                _resumePositionApplied = true;
                if (!string.Equals(_audioService.CurrentAudioUrl, AudioUrl, StringComparison.OrdinalIgnoreCase) || !_audioService.IsPlaying)
                {
                    await _audioService.PlayAsync(AudioUrl);
                }

                await _audioService.SeekAsync(TimeSpan.FromSeconds(ResumePositionSeconds));
            }
        }
        finally
        {
            _isLoadingLocationData = false;
        }
    }

    private int ResolveInitialGuideIndex()
    {
        if (!string.IsNullOrWhiteSpace(AudioGuideId))
        {
            var byId = AudioGuides.ToList().FindIndex(item => item.Id == AudioGuideId);
            if (byId >= 0)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(AudioUrl))
        {
            var byUrl = AudioGuides.ToList().FindIndex(item =>
                string.Equals(item.Guide.AudioUrl, AudioUrl, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.Guide.CloudinaryAudioUrl, AudioUrl, StringComparison.OrdinalIgnoreCase));
            if (byUrl >= 0)
            {
                return byUrl;
            }
        }

        return 0;
    }

    // Chuyển guide đang chọn, cập nhật UI/state và quyết định có auto-play ngay hay không.
    // Thuộc flow đổi track, next/back và manual selection.
    private async Task SetCurrentAudioGuideAsync(int index, bool autoPlay, bool forceRestart, bool persistAsUserSelection)
    {
        await _guideSelectionLock.WaitAsync();
        if (index < 0 || index >= AudioGuides.Count)
        {
            _guideSelectionLock.Release();
            return;
        }
        try
        {
            var currentGuideId = AudioGuideId;
            if (!string.IsNullOrWhiteSpace(currentGuideId)
                && currentGuideId != AudioGuides[index].Guide.Id)
            {
                await SyncListeningHistoryAsync(force: true);
            }

            IsSwitchingGuide = true;
            CurrentAudioGuideIndex = index;

            var item = AudioGuides[index];
            var guide = item.Guide;

            _isApplyingGuideInternally = true;
            try
            {
                AudioGuideId = guide.Id;
                CurrentPlayingAudioGuideId = guide.Id;
                AudioUrl = ResolveAudioSource(guide);
            }
            finally
            {
                _isApplyingGuideInternally = false;
            }

            if (persistAsUserSelection)
            {
                SaveUserPreferredAudioForLocation();
                BroadcastUserAudioSelection();
            }

            Title = string.IsNullOrWhiteSpace(guide.Title) ? LocationName : guide.Title;
            AudioTranslationSummary = string.IsNullOrWhiteSpace(guide.Description)
                ? string.Format(T("AudioPlayer_TranslationSummaryFormat"), LocationName)
                : guide.Description;
            AudioTranslationText = string.IsNullOrWhiteSpace(guide.TranscriptText)
                ? guide.Description
                : guide.TranscriptText;

            Duration = Math.Max(0, guide.Duration * 60);
            DurationText = FormatTime(TimeSpan.FromSeconds(Duration));
            CurrentPosition = 0;
            CurrentPositionText = "0:00";

            LoadScriptSegments(guide);
            UpdateCurrentScriptText(TimeSpan.Zero);
            UpdateNextBackButtonStates();
            ApplyAudioGuideHighlightState();

            if (autoPlay)
            {
                await StartSelectedAudioAsync(forceRestart);
            }
            else
            {
                SyncPlaybackStateFromService();
            }
        }
        finally
        {
            IsSwitchingGuide = false;
            _guideSelectionLock.Release();
        }
    }

    private void SaveUserPreferredAudioForLocation()
    {
        if (string.IsNullOrWhiteSpace(LocationId)
            || string.IsNullOrWhiteSpace(AudioGuideId)
            || string.IsNullOrWhiteSpace(AudioUrl))
        {
            return;
        }

        Preferences.Set(GetPreferredAudioGuideKey(LocationId), AudioGuideId);
        Preferences.Set(GetPreferredAudioUrlKey(LocationId), AudioUrl);
    }

    private void BroadcastUserAudioSelection()
    {
        if (string.IsNullOrWhiteSpace(LocationId)
            || string.IsNullOrWhiteSpace(AudioGuideId)
            || string.IsNullOrWhiteSpace(AudioUrl))
        {
            return;
        }

        var payload = new AutoAudioSelectionPayload(
            LocationId,
            LocationName,
            AudioGuideId,
            AudioUrl);

        WeakReferenceMessenger.Default.Send(new AutoAudioSelectionChangedMessage(payload));
    }

    // Khởi động phát guide hiện tại theo trạng thái player (play mới/resume/restart).
    // Đồng bộ với AutoPlaybackService để xử lý manual override đúng flow.
    private async Task StartSelectedAudioAsync(bool forceRestart)
    {
        if (string.IsNullOrWhiteSpace(AudioUrl))
        {
            return;
        }

        var isCurrent = string.Equals(_audioService.CurrentAudioUrl, AudioUrl, StringComparison.OrdinalIgnoreCase);
        if (!isCurrent)
        {
            await _autoPlaybackService.HandleManualPlaybackAsync(LocationId, AudioGuideId);
            return;
        }

        if (forceRestart)
        {
            await _audioService.StopAsync();
            await _audioService.PlayAsync(AudioUrl);
            return;
        }

        if (_audioService.IsPlaying)
        {
            return;
        }

        if (_audioService.CurrentPosition > TimeSpan.Zero)
        {
            await _audioService.ResumeAsync();
            return;
        }

        await _autoPlaybackService.HandleManualPlaybackAsync(LocationId, AudioGuideId);
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
                ScriptText = string.IsNullOrWhiteSpace(guide.TranscriptText) ? guide.Description : guide.TranscriptText
            });
            return;
        }

        _scriptSegments.AddRange(segments);
    }

    private void UpdateCurrentScriptText(TimeSpan position)
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
        PlayPauseGlyph = IsPlaying ? "pause.svg" : "play_white_icon.svg";

        if (!currentAudioMatches)
        {
            CurrentPosition = 0;
            CurrentPositionText = "0:00";
            UpdateCurrentScriptText(TimeSpan.Zero);
            ApplyAudioGuideHighlightState();
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

        UpdateCurrentScriptText(servicePosition);
        ApplyAudioGuideHighlightState();
    }

    private bool IsCurrentAudio(string? audioUrl)
    {
        if (string.IsNullOrWhiteSpace(AudioUrl) || string.IsNullOrWhiteSpace(audioUrl))
        {
            return false;
        }

        return string.Equals(AudioUrl, audioUrl, StringComparison.OrdinalIgnoreCase);
    }

    private int FindGuideIndexByAudioUrl(string? audioUrl)
    {
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            return -1;
        }

        for (var i = 0; i < AudioGuides.Count; i++)
        {
            var source = ResolveAudioSource(AudioGuides[i].Guide);
            if (string.Equals(source, audioUrl, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private void UpdateNextBackButtonStates()
    {
        CanPlayNext = CurrentAudioGuideIndex < AudioGuides.Count - 1;
        CanPlayBack = CurrentAudioGuideIndex > 0;
    }

    private void ApplyAudioGuideHighlightState()
    {
        for (var i = 0; i < AudioGuides.Count; i++)
        {
            var item = AudioGuides[i];
            item.IsSelected = i == CurrentAudioGuideIndex;
            item.IsCurrentPlaying = IsPlaying && item.Id == CurrentPlayingAudioGuideId;
        }
    }

    private void ResetPlaybackState()
    {
        Duration = 0;
        CurrentPosition = 0;
        DurationText = "0:00";
        CurrentPositionText = "0:00";
        CurrentScriptText = string.Empty;
        IsPlaying = false;
        CanPlayBack = false;
        CanPlayNext = false;
        CurrentPlayingAudioGuideId = string.Empty;
        PlayPauseGlyph = "play_white_icon.svg";
        IsSwitchingGuide = false;
    }

    private void ResetScreen()
    {
        LocationName = T("LocationDetail_DefaultLocationName");
        Description = string.Empty;
        ImageUrl = string.Empty;
        Title = LocationName;
        AudioGuides.Clear();
        _scriptSegments.Clear();
        _activeScriptSegmentId = -1;
        ResetPlaybackState();
    }

    [RelayCommand]
    private async Task PlayPauseAsync()
    {
        await ExecutePlayerActionAsync("play-pause", async () =>
        {
            if (string.IsNullOrWhiteSpace(AudioUrl))
            {
                return;
            }

            if (IsPlaying)
            {
                await _audioService.PauseAsync();
                await SyncListeningHistoryAsync(force: true);
                return;
            }

            await StartSelectedAudioAsync(forceRestart: false);
        });
    }

    [RelayCommand]
    private async Task RewindAsync()
    {
        await ExecutePlayerActionAsync("seek-back-10", async () =>
        {
            await SeekToAsync(CurrentPosition - 10);
        });
    }

    [RelayCommand]
    private async Task ForwardAsync()
    {
        await ExecutePlayerActionAsync("seek-next-10", async () =>
        {
            await SeekToAsync(CurrentPosition + 10);
        });
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
        await ExecutePlayerActionAsync("seek-complete", async () =>
        {
            await SeekToAsync(CurrentPosition);
        }, useThrottle: false);
    }

    [RelayCommand]
    private async Task PlayNextAsync()
    {
        await ExecutePlayerActionAsync("track-next", async () =>
        {
            if (!CanPlayNext || IsSwitchingGuide)
            {
                return;
            }

            MarkManualPlaybackSource();
            await SetCurrentAudioGuideAsync(CurrentAudioGuideIndex + 1, autoPlay: true, forceRestart: true, persistAsUserSelection: true);
        });
    }

    [RelayCommand]
    private async Task PlayBackAsync()
    {
        await ExecutePlayerActionAsync("track-prev", async () =>
        {
            if (!CanPlayBack || IsSwitchingGuide)
            {
                return;
            }

            MarkManualPlaybackSource();
            await SetCurrentAudioGuideAsync(CurrentAudioGuideIndex - 1, autoPlay: true, forceRestart: true, persistAsUserSelection: true);
        });
    }

    [RelayCommand]
    private async Task PlayAudioGuideAsync(AudioGuideItemViewModel? audioGuideItem)
    {
        await ExecutePlayerActionAsync("track-select", async () =>
        {
            if (audioGuideItem is null || IsSwitchingGuide)
            {
                return;
            }

            var index = AudioGuides.IndexOf(audioGuideItem);
            if (index < 0)
            {
                return;
            }

            MarkManualPlaybackSource();
            await SetCurrentAudioGuideAsync(index, autoPlay: true, forceRestart: true, persistAsUserSelection: true);
        });
    }

    private async Task SeekToAsync(double seconds)
    {
        if (string.IsNullOrWhiteSpace(AudioUrl)
            || string.IsNullOrWhiteSpace(_audioService.CurrentAudioUrl)
            || !IsCurrentAudio(_audioService.CurrentAudioUrl))
        {
            return;
        }

        var effectiveDuration = Duration > 0 ? Duration : _audioService.Duration.TotalSeconds;
        var clamped = Math.Max(0, seconds);
        if (effectiveDuration > 0)
        {
            clamped = Math.Min(effectiveDuration, clamped);
        }

        CurrentPosition = clamped;
        CurrentPositionText = FormatTime(TimeSpan.FromSeconds(clamped));
        UpdateCurrentScriptText(TimeSpan.FromSeconds(clamped));
        await _audioService.SeekAsync(TimeSpan.FromSeconds(clamped));
    }

    private bool TryThrottleAction(string actionKey)
    {
        var now = DateTime.UtcNow;
        lock (_actionSync)
        {
            if (_lastActionAt.TryGetValue(actionKey, out var lastTime)
                && now - lastTime < ActionThrottleWindow)
            {
                return false;
            }

            _lastActionAt[actionKey] = now;
            return true;
        }
    }

    // Bọc thao tác player bằng throttle + lock để tránh bấm nhanh gây race condition.
    // Thuộc flow ổn định điều khiển play/pause/seek/next/back.
    private async Task ExecutePlayerActionAsync(string actionKey, Func<Task> action, bool useThrottle = true)
    {
        if (useThrottle && !TryThrottleAction(actionKey))
        {
            return;
        }

        if (!await _playerActionLock.WaitAsync(0))
        {
            return;
        }

        try
        {
            await action();
        }
        catch
        {
            // Keep player UI stable if underlying playback fails unexpectedly.
            SyncPlaybackStateFromService();
        }
        finally
        {
            _playerActionLock.Release();
        }
    }

    private bool ShouldAutoPlayNextWithinPoi()
    {
        return Preferences.Get("AutoPlayNext", true)
               && !IsAutoPlaybackSource;
    }

    private async Task AutoAdvanceToNextGuideAsync()
    {
        if (!CanPlayNext || IsSwitchingGuide)
        {
            return;
        }

        try
        {
            _isAutoAdvancing = true;
            await SetCurrentAudioGuideAsync(
                CurrentAudioGuideIndex + 1,
                autoPlay: true,
                forceRestart: true,
                persistAsUserSelection: false);
        }
        finally
        {
            _isAutoAdvancing = false;
        }
    }

    private async Task AdvanceTourLocationAsync()
    {
        if (IsSwitchingGuide || string.IsNullOrWhiteSpace(LocationId))
        {
            return;
        }

        if (!_tourPlaybackSessionService.TryMoveNextLocation(out var nextLocationId))
        {
            await FinishTourAsync();
            return;
        }

        await _navigationService.NavigateToAsync("///AudioPlayerPage", new Dictionary<string, object>
        {
            { "LocationId", nextLocationId },
            { "PlaybackSource", "TourRoute" }
        });
    }

    private async Task FinishTourAsync()
    {
        await SyncListeningHistoryAsync(force: true);
        _tourPlaybackSessionService.Reset();

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Application.Current!.MainPage!.DisplayAlert(
                T("Map_TourCompleteTitle"),
                T("Map_TourCompleteMessage"),
                T("Map_TourCompleteAction"));
        });

        await _navigationService.NavigateToAsync("///MapPage");
    }

    private static string GetPreferredAudioGuideKey(string locationId) => $"{PreferredAudioGuideKeyPrefix}{locationId}";

    private static string GetPreferredAudioUrlKey(string locationId) => $"{PreferredAudioUrlKeyPrefix}{locationId}";

    private static string ResolveAudioSource(AudioGuide guide)
    {
        return !string.IsNullOrWhiteSpace(guide.CloudinaryAudioUrl)
            ? guide.CloudinaryAudioUrl
            : guide.AudioUrl;
    }

    private static string FormatTime(TimeSpan time)
    {
        return time.Hours > 0
            ? $"{time.Hours}:{time.Minutes:D2}:{time.Seconds:D2}"
            : $"{time.Minutes}:{time.Seconds:D2}";
    }

    // Đồng bộ tiến độ nghe lên API theo chu kỳ hoặc khi force.
    // Thuộc flow history/resume và analytics nghe audio.
    private async Task SyncListeningHistoryAsync(bool force = false)
    {
        if (string.IsNullOrWhiteSpace(LocationId) || string.IsNullOrWhiteSpace(AudioGuideId))
        {
            return;
        }

        var durationSeconds = Duration > 0 ? Duration : _audioService.Duration.TotalSeconds;
        if (durationSeconds <= 0)
        {
            return;
        }

        var progress = Math.Clamp(CurrentPosition / durationSeconds, 0d, 1d);
        if (!force && progress <= 0d)
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;
        if (!force
            && string.Equals(_lastHistoryAudioGuideId, AudioGuideId, StringComparison.Ordinal)
            && nowUtc - _lastHistorySyncAtUtc < HistorySyncInterval
            && progress - _lastSyncedProgress < HistorySyncProgressDelta)
        {
            return;
        }

        try
        {
            await _apiService.AddListeningHistoryAsync(AudioGuideId, LocationId, progress);
            _lastHistoryAudioGuideId = AudioGuideId;
            _lastHistorySyncAtUtc = nowUtc;
            _lastSyncedProgress = progress;
        }
        catch
        {
            // Best-effort sync only; never block playback.
        }
    }

    private string T(string key) => _localizationService.GetString(key);
}

public partial class AudioGuideItemViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isCurrentPlaying;

    public AudioGuide Guide { get; }
    public string Id => Guide.Id;
    public string Title => Guide.Title;
    public string Description => Guide.Description;
    public int Duration => Guide.Duration;

    public AudioGuideItemViewModel(AudioGuide guide)
    {
        Guide = guide;
    }
}
