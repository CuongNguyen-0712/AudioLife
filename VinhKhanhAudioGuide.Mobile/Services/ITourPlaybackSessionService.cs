namespace VinhKhanhAudioGuide.Mobile.Services;

public interface ITourPlaybackSessionService
{
    bool IsActive { get; }
    string TourId { get; }
    string CurrentLocationId { get; }

    void Initialize(string tourId, IReadOnlyList<string> orderedLocationIds, string? startLocationId = null);
    bool TryMoveNextLocation(out string nextLocationId);
    void Reset();
}

public sealed class TourPlaybackSessionService : ITourPlaybackSessionService
{
    private readonly object _sync = new();
    private IReadOnlyList<string> _orderedLocationIds = Array.Empty<string>();

    public bool IsActive { get; private set; }
    public string TourId { get; private set; } = string.Empty;
    public string CurrentLocationId { get; private set; } = string.Empty;

    public void Initialize(string tourId, IReadOnlyList<string> orderedLocationIds, string? startLocationId = null)
    {
        lock (_sync)
        {
            TourId = tourId ?? string.Empty;
            _orderedLocationIds = orderedLocationIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (_orderedLocationIds.Count == 0)
            {
                CurrentLocationId = string.Empty;
                IsActive = false;
                return;
            }

            var startIndex = 0;
            if (!string.IsNullOrWhiteSpace(startLocationId))
            {
                var matchedIndex = _orderedLocationIds.ToList().FindIndex(id =>
                    string.Equals(id, startLocationId, StringComparison.OrdinalIgnoreCase));
                if (matchedIndex >= 0)
                {
                    startIndex = matchedIndex;
                }
            }

            CurrentLocationId = _orderedLocationIds[startIndex];
            IsActive = true;
        }
    }

    public bool TryMoveNextLocation(out string nextLocationId)
    {
        lock (_sync)
        {
            nextLocationId = string.Empty;

            if (!IsActive || _orderedLocationIds.Count == 0 || string.IsNullOrWhiteSpace(CurrentLocationId))
            {
                return false;
            }

            var currentIndex = _orderedLocationIds.ToList().FindIndex(id =>
                string.Equals(id, CurrentLocationId, StringComparison.OrdinalIgnoreCase));
            if (currentIndex < 0)
            {
                return false;
            }

            var nextIndex = currentIndex + 1;
            if (nextIndex >= _orderedLocationIds.Count)
            {
                return false;
            }

            CurrentLocationId = _orderedLocationIds[nextIndex];
            nextLocationId = CurrentLocationId;
            return true;
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            IsActive = false;
            TourId = string.Empty;
            CurrentLocationId = string.Empty;
            _orderedLocationIds = Array.Empty<string>();
        }
    }
}