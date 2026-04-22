using VinhKhanhAudioGuide.Mobile.Models;

namespace VinhKhanhAudioGuide.Mobile.Services;

public sealed class AppHeartbeatService : IAppHeartbeatService, IDisposable
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(5);

    private readonly IApiService _apiService;
    private readonly IAppSessionStore _sessionStore;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _cts;
    private Func<Task>? _onSessionInvalidated;
    private bool _isRunning;

    public AppHeartbeatService(IApiService apiService, IAppSessionStore sessionStore)
    {
        _apiService = apiService;
        _sessionStore = sessionStore;
    }

    public bool IsRunning => _isRunning;

    public async Task<bool> StartAsync(Func<Task>? onSessionInvalidated = null)
    {
        await _gate.WaitAsync();
        try
        {
            if (_isRunning)
            {
                _onSessionInvalidated = onSessionInvalidated ?? _onSessionInvalidated;
                return true;
            }

            var snapshot = await _sessionStore.GetSnapshotAsync();
            if (snapshot is null || snapshot.IsExpired || string.IsNullOrWhiteSpace(snapshot.SessionToken))
            {
                return false;
            }

            _onSessionInvalidated = onSessionInvalidated;
            _cts = new CancellationTokenSource();
            _isRunning = true;

            _ = Task.Run(() => RunAsync(_cts.Token));
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync()
    {
        await _gate.WaitAsync();
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _isRunning = false;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(HeartbeatInterval);

        while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                var snapshot = await _sessionStore.GetSnapshotAsync();
                if (snapshot is null || snapshot.IsExpired || string.IsNullOrWhiteSpace(snapshot.SessionToken))
                {
                    await HandleInvalidSessionAsync();
                    return;
                }

                var request = new HeartbeatRequest(
                    snapshot.DeviceId,
                    snapshot.SessionToken,
                    ResolveActivityName(),
                    ResolveActivityContext(),
                    ResolveScreenName(),
                    ResolveRoute(),
                    true);

                var response = await _apiService.SendHeartbeatAsync(request);
                if (response is null || !response.Success || !response.SessionValid)
                {
                    await HandleInvalidSessionAsync();
                    return;
                }

                var refreshedSnapshot = snapshot with
                {
                    SessionToken = response.SessionToken,
                    ExpiresAtUtc = response.ExpiresAtUtc,
                    LastValidatedAtUtc = response.LastValidatedAtUtc,
                    UserAppId = string.IsNullOrWhiteSpace(response.UserAppId) ? snapshot.UserAppId : response.UserAppId
                };

                await _sessionStore.SaveSnapshotAsync(refreshedSnapshot);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                await HandleInvalidSessionAsync();
                return;
            }
        }
    }

    private static string ResolveActivityName()
    {
        return Shell.Current?.CurrentPage?.GetType().Name
            ?? Shell.Current?.CurrentState?.Location.OriginalString
            ?? "Shell";
    }

    private static string ResolveActivityContext()
    {
        var route = Shell.Current?.CurrentState?.Location.OriginalString;
        return string.IsNullOrWhiteSpace(route) ? ResolveActivityName() : route;
    }

    private static string ResolveScreenName()
    {
        return Shell.Current?.CurrentPage?.Title
            ?? Shell.Current?.CurrentPage?.GetType().Name
            ?? string.Empty;
    }

    private static string ResolveRoute()
    {
        return Shell.Current?.CurrentState?.Location.OriginalString ?? string.Empty;
    }

    private async Task HandleInvalidSessionAsync()
    {
        await StopAsync();
        await _sessionStore.ClearSnapshotAsync();

        var callback = _onSessionInvalidated;
        if (callback is not null)
        {
            await callback();
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _gate.Dispose();
    }
}