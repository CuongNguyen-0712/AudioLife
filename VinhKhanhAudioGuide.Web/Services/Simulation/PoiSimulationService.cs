using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Hubs;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Models.Simulation;

namespace VinhKhanhAudioGuide.Web.Services.Simulation;

/// <summary>
/// Core engine giả lập nhiều người dùng cùng nghe một POI.
///
/// Chiến lược:
/// - Dùng Parallel.ForEachAsync với MaxDegreeOfParallelism để kiểm soát concurrency.
/// - Mỗi Virtual User chạy một vòng lặp streaming tuần tự (sequential progress steps).
/// - Jitter ngẫu nhiên khi khởi động để dàn đều load thay vì spike đột ngột.
/// - Tổng hợp metrics mỗi giây và push qua SignalR về Admin dashboard.
/// - Structured logging với IsSimulation=true để dễ filter.
/// </summary>
public class PoiSimulationService : IPoiSimulationService
{
    private readonly IHubContext<SimulationHub> _hubContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PoiSimulationService> _logger;

    private SimulationBatch? _currentBatch;
    private CancellationTokenSource? _batchCts;

    // Thread-safe counters (không dùng lock, dùng Interlocked)
    private long _totalRequests;
    private long _failedRequests;
    private long _requestsLastSecond;

    public SimulationBatch? CurrentBatch => _currentBatch;

    public PoiSimulationService(
        IHubContext<SimulationHub> hubContext,
        IServiceProvider serviceProvider,
        ILogger<PoiSimulationService> logger)
    {
        _hubContext = hubContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(StartSimulationRequest request, CancellationToken externalCt = default)
    {
        // Không cho phép chạy đồng thời nhiều batch
        if (_currentBatch?.Status == SimulationStatus.Running)
        {
            await PushLog(new SimulationLogEntry
            {
                BatchId = _currentBatch.BatchId,
                Level = "WARN",
                Message = "Đã có một batch đang chạy. Vui lòng dừng trước khi bắt đầu mới."
            });
            return;
        }

        // Resolve location + audio info
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var location = await db.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == request.LocationId, externalCt);
        var audioGuide = await db.AudioGuides.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.AudioGuideId, externalCt);

        if (location == null || audioGuide == null)
        {
            await PushLog(new SimulationLogEntry
            {
                Level = "ERROR",
                Message = $"Không tìm thấy Location '{request.LocationId}' hoặc AudioGuide '{request.AudioGuideId}'."
            });
            return;
        }

        // Reset counters
        Interlocked.Exchange(ref _totalRequests, 0);
        Interlocked.Exchange(ref _failedRequests, 0);
        Interlocked.Exchange(ref _requestsLastSecond, 0);

        _batchCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
        var batchToken = _batchCts.Token;

        var config = new SimulationConfig
        {
            LocationId = request.LocationId,
            AudioGuideId = request.AudioGuideId,
            LocationName = location.Name,
            AudioGuideName = audioGuide.Title,
            AudioDurationMinutes = Math.Max(audioGuide.Duration, 1),
            VirtualUserCount = Math.Clamp(request.VirtualUserCount, 1, 2000),
            SpeedMultiplier = Math.Clamp(request.SpeedMultiplier, 1, 100),
            MaxConcurrency = Math.Clamp(request.MaxConcurrency, 1, 200),
            WriteToDatabase = request.WriteToDatabase
        };

        _currentBatch = new SimulationBatch
        {
            Config = config,
            Status = SimulationStatus.Running,
            StartedAtUtc = DateTime.UtcNow,
            TotalUsers = config.VirtualUserCount
        };

        var batch = _currentBatch;

        // Log khởi động
        _logger.LogInformation(
            "[SIM] {IsSimulation} Batch {BatchId} started. Location={LocationName}, AudioGuide={AudioGuide}, Users={Users}, Speed=x{Speed}, Concurrency={Concurrency}",
            true, batch.BatchId, config.LocationName, config.AudioGuideName,
            config.VirtualUserCount, config.SpeedMultiplier, config.MaxConcurrency);

        await PushLog(new SimulationLogEntry
        {
            BatchId = batch.BatchId,
            Level = "SIM",
            Message = $"🚀 Batch [{batch.BatchId}] bắt đầu | POI: {config.LocationName} | {config.VirtualUserCount} users | Tốc độ: x{config.SpeedMultiplier}"
        });

        // Chạy engine trong background (không block Hub call)
        _ = Task.Run(() => RunBatchAsync(batch, batchToken), batchToken);
    }

    public Task StopAsync()
    {
        if (_currentBatch == null || _currentBatch.Status != SimulationStatus.Running)
            return Task.CompletedTask;

        _batchCts?.Cancel();
        _logger.LogInformation("[SIM] {IsSimulation} Batch {BatchId} manually stopped.", true, _currentBatch.BatchId);
        return Task.CompletedTask;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CORE ENGINE
    // ──────────────────────────────────────────────────────────────────────────

    private async Task RunBatchAsync(SimulationBatch batch, CancellationToken ct)
    {
        var config = batch.Config;
        var sw = Stopwatch.StartNew();

        // Bắt đầu vòng lặp push metrics 1 giây/lần
        var metricsTask = PushMetricsLoopAsync(batch, sw, ct);

        // Tạo danh sách Virtual User IDs
        var userIds = Enumerable.Range(1, config.VirtualUserCount)
            .Select(i => $"VU-{batch.BatchId}-{i:D4}")
            .ToList();

        // Channel để feed user ID vào Parallel consumer — giới hạn concurrency
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = config.MaxConcurrency,
            CancellationToken = ct
        };

        try
        {
            await Parallel.ForEachAsync(userIds, parallelOptions, async (userId, userCt) =>
            {
                Interlocked.Increment(ref _dummy); // warmup
                batch.ActiveUsers = (int)Interlocked.Increment(ref _activeUsers);
                try
                {
                    await RunVirtualUserAsync(userId, batch, config, userCt);
                    batch.CompletedUsers = (int)Interlocked.Increment(ref _completedUsers);
                }
                catch (OperationCanceledException)
                {
                    // Batch bị dừng bởi Admin
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _failedRequests);
                    batch.FailedUsers = (int)Interlocked.Increment(ref _failedUsers);
                    _logger.LogWarning(ex, "[SIM] {IsSimulation} VirtualUser {UserId} failed.", true, userId);
                }
                finally
                {
                    batch.ActiveUsers = (int)Interlocked.Decrement(ref _activeUsers);
                }
            });

            batch.Status = ct.IsCancellationRequested ? SimulationStatus.Cancelled : SimulationStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            batch.Status = SimulationStatus.Cancelled;
        }
        catch (Exception ex)
        {
            batch.Status = SimulationStatus.Cancelled;
            _logger.LogError(ex, "[SIM] {IsSimulation} Batch {BatchId} crashed.", true, batch.BatchId);
        }
        finally
        {
            batch.CompletedAtUtc = DateTime.UtcNow;
            sw.Stop();

            // Đồng bộ số liệu cuối
            batch.TotalRequests = Interlocked.Read(ref _totalRequests);
            batch.FailedRequests = Interlocked.Read(ref _failedRequests);
        }

        // Chờ metrics loop kết thúc (nó sẽ tự dừng khi batch không còn Running)
        await metricsTask;

        var emoji = batch.Status == SimulationStatus.Completed ? "✅" : "🛑";
        var summary = $"{emoji} Batch [{batch.BatchId}] {batch.Status} | " +
                      $"Hoàn thành: {batch.CompletedUsers}/{batch.TotalUsers} | " +
                      $"Lỗi: {batch.FailedUsers} | " +
                      $"Tổng req: {batch.TotalRequests} | " +
                      $"Thời gian: {FormatElapsed(batch.Elapsed)}";

        _logger.LogInformation("[SIM] {IsSimulation} {Summary}", true, summary);

        await PushLog(new SimulationLogEntry
        {
            BatchId = batch.BatchId,
            Level = batch.Status == SimulationStatus.Completed ? "SUCCESS" : "WARN",
            Message = summary
        });

        // Push snapshot cuối
        await PushMetrics(batch, 0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // VIRTUAL USER LOOP — Sequential Streaming Progress
    // ──────────────────────────────────────────────────────────────────────────

    private async Task RunVirtualUserAsync(string userId, SimulationBatch batch, SimulationConfig config, CancellationToken ct)
    {
        // JITTER: trải đều lúc bắt đầu, tránh thundering herd
        var jitterMs = Random.Shared.Next(0, 3000);
        await Task.Delay(jitterMs, ct);

        var totalDurationSeconds = config.AudioDurationMinutes * 60;

        // Số bước cập nhật tiến độ (mỗi 15% hoặc mỗi N giây)
        const int stepCount = 8;
        var stepDurationReal = (double)totalDurationSeconds / config.SpeedMultiplier / stepCount * 1000; // ms thực tế
        stepDurationReal = Math.Max(stepDurationReal, 200); // Không nhỏ hơn 200ms

        var listenedSeconds = 0;
        var stepSeconds = totalDurationSeconds / stepCount;

        for (var step = 1; step <= stepCount; step++)
        {
            ct.ThrowIfCancellationRequested();

            // Mô phỏng thời gian nghe tích lũy
            listenedSeconds = Math.Min(step * stepSeconds, totalDurationSeconds);
            var progress = (double)listenedSeconds / totalDurationSeconds;
            var isCompleted = step == stepCount;

            // Simulate DB write (hoặc chỉ log tùy config)
            var success = await SimulateProgressUpdateAsync(userId, batch, config, progress, listenedSeconds, isCompleted, ct);

            Interlocked.Increment(ref _totalRequests);
            Interlocked.Increment(ref _requestsLastSecond);
            if (!success) Interlocked.Increment(ref _failedRequests);

            // Log mỗi bước nếu là bước đầu, giữa, hoặc cuối (tránh log quá nhiều)
            if (step == 1 || step == stepCount / 2 || step == stepCount)
            {
                await PushLog(new SimulationLogEntry
                {
                    BatchId = batch.BatchId,
                    Level = success ? "SIM" : "WARN",
                    VirtualUserId = userId,
                    Message = $"{userId} → {(isCompleted ? "Hoàn thành ✓" : $"Tiến độ {progress:P0}")}",
                    Progress = progress
                });
            }

            if (!isCompleted)
                await Task.Delay((int)stepDurationReal, ct);
        }
    }

    private async Task<bool> SimulateProgressUpdateAsync(
        string userId,
        SimulationBatch batch,
        SimulationConfig config,
        double progress,
        int listenedSeconds,
        bool isCompleted,
        CancellationToken ct)
    {
        if (!config.WriteToDatabase)
        {
            // Dry-run: giả lập xử lý logic mà không ghi DB
            await Task.Delay(Random.Shared.Next(5, 25), ct); // Simulate latency
            return true;
        }

        try
        {
            // Ghi thực vào DB — dùng scope riêng biệt để tránh EF concurrency
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var historyId = $"sim_{batch.BatchId}_{userId.GetHashCode():X8}_{config.AudioGuideId}";
            var existing = await db.ListeningHistories.FirstOrDefaultAsync(h => h.Id == historyId, ct);

            if (existing == null)
            {
                existing = new ListeningHistory
                {
                    Id = historyId,
                    UserId = null, // Virtual user — không liên kết AppUser thật
                    AudioGuideId = config.AudioGuideId,
                    LocationId = config.LocationId,
                    AudioTitle = $"[SIM] {config.AudioGuideName}",
                    LocationName = $"[SIM] {config.LocationName}",
                    LocationImageUrl = string.Empty,
                    AudioDuration = config.AudioDurationMinutes,
                };
                db.ListeningHistories.Add(existing);
            }

            existing.Progress = (decimal)progress;
            existing.ListenedSeconds = listenedSeconds;
            existing.IsCompleted = isCompleted;
            existing.LastListenedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SIM] {IsSimulation} DB write failed for {UserId}", true, userId);
            return false;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // METRICS PUSH LOOP (1 giây / lần)
    // ──────────────────────────────────────────────────────────────────────────

    private async Task PushMetricsLoopAsync(SimulationBatch batch, Stopwatch sw, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && batch.Status == SimulationStatus.Running)
        {
            var rps = Interlocked.Exchange(ref _requestsLastSecond, 0);

            batch.TotalRequests = Interlocked.Read(ref _totalRequests);
            batch.FailedRequests = Interlocked.Read(ref _failedRequests);

            await PushMetrics(batch, rps);

            try { await Task.Delay(1000, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task PushMetrics(SimulationBatch batch, long rps)
    {
        var snapshot = new SimulationMetricsSnapshot
        {
            BatchId = batch.BatchId,
            Status = batch.Status,
            TotalUsers = batch.TotalUsers,
            ActiveUsers = Interlocked.Read(ref _activeUsers) > 0 ? (int)Interlocked.Read(ref _activeUsers) : batch.ActiveUsers,
            CompletedUsers = batch.CompletedUsers,
            FailedUsers = batch.FailedUsers,
            TotalRequests = batch.TotalRequests,
            FailedRequests = batch.FailedRequests,
            OverallProgressPercent = batch.OverallProgressPercent,
            SuccessRate = batch.SuccessRate,
            RequestsPerSecond = rps,
            ElapsedDisplay = FormatElapsed(batch.Elapsed),
            Timestamp = DateTime.Now.ToString("HH:mm:ss")
        };

        await _hubContext.Clients.All.SendAsync("ReceiveSimulationMetrics", snapshot);
    }

    private async Task PushLog(SimulationLogEntry entry)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveSimulationLog", entry);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers — thread-safe counters
    // ──────────────────────────────────────────────────────────────────────────

    private long _activeUsers;
    private long _completedUsers;
    private long _failedUsers;
    private long _dummy;

    private static string FormatElapsed(TimeSpan elapsed)
        => elapsed.TotalHours >= 1
            ? elapsed.ToString(@"hh\:mm\:ss")
            : elapsed.ToString(@"mm\:ss");
}
