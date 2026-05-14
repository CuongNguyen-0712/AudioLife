using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using VinhKhanhAudioGuide.Web.Models.Simulation;
using VinhKhanhAudioGuide.Web.Services.Simulation;

namespace VinhKhanhAudioGuide.Web.Hubs;

/// <summary>
/// SignalR Hub cho tính năng giả lập POI.
/// Chỉ dành cho SystemAdmin.
/// Client kết nối vào hub này để nhận metrics và log theo thời gian thực.
/// </summary>
[Authorize(Policy = "SystemAdminOnly")]
public class SimulationHub : Hub
{
    private readonly IPoiSimulationService _simulationService;
    private readonly ILogger<SimulationHub> _logger;

    public SimulationHub(IPoiSimulationService simulationService, ILogger<SimulationHub> logger)
    {
        _simulationService = simulationService;
        _logger = logger;
    }

    /// <summary>
    /// Client kết nối → gửi ngay trạng thái batch hiện tại (nếu đang chạy).
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var batch = _simulationService.CurrentBatch;
        if (batch != null)
        {
            await Clients.Caller.SendAsync("ReceiveCurrentBatch", new SimulationMetricsSnapshot
            {
                BatchId = batch.BatchId,
                Status = batch.Status,
                TotalUsers = batch.TotalUsers,
                ActiveUsers = batch.ActiveUsers,
                CompletedUsers = batch.CompletedUsers,
                FailedUsers = batch.FailedUsers,
                TotalRequests = batch.TotalRequests,
                FailedRequests = batch.FailedRequests,
                OverallProgressPercent = batch.OverallProgressPercent,
                SuccessRate = batch.SuccessRate,
                ElapsedDisplay = FormatElapsed(batch.Elapsed),
                RequestsPerSecond = 0,
                Timestamp = DateTime.Now.ToString("HH:mm:ss")
            });
        }

        _logger.LogInformation("SimulationHub: Admin connected. ConnectionId={ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SimulationHub: Admin disconnected. ConnectionId={ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>Client gọi để bắt đầu batch giả lập mới.</summary>
    public async Task StartSimulation(StartSimulationRequest request)
    {
        _logger.LogInformation(
            "SimulationHub: StartSimulation requested by {User}. LocationId={LocationId}, Users={Count}",
            Context.User?.Identity?.Name, request.LocationId, request.VirtualUserCount);

        await _simulationService.StartAsync(request, Context.ConnectionAborted);
    }

    /// <summary>Client gọi để dừng batch đang chạy.</summary>
    public async Task StopSimulation()
    {
        _logger.LogInformation("SimulationHub: StopSimulation requested by {User}", Context.User?.Identity?.Name);
        await _simulationService.StopAsync();
    }

    /// <summary>Client yêu cầu lấy snapshot hiện tại.</summary>
    public async Task RequestStatus()
    {
        var batch = _simulationService.CurrentBatch;
        if (batch == null)
        {
            await Clients.Caller.SendAsync("ReceiveStatus", null);
            return;
        }

        await Clients.Caller.SendAsync("ReceiveStatus", new SimulationMetricsSnapshot
        {
            BatchId = batch.BatchId,
            Status = batch.Status,
            TotalUsers = batch.TotalUsers,
            ActiveUsers = batch.ActiveUsers,
            CompletedUsers = batch.CompletedUsers,
            FailedUsers = batch.FailedUsers,
            TotalRequests = batch.TotalRequests,
            FailedRequests = batch.FailedRequests,
            OverallProgressPercent = batch.OverallProgressPercent,
            SuccessRate = batch.SuccessRate,
            ElapsedDisplay = FormatElapsed(batch.Elapsed),
            Timestamp = DateTime.Now.ToString("HH:mm:ss")
        });
    }

    private static string FormatElapsed(TimeSpan elapsed)
        => elapsed.TotalHours >= 1
            ? elapsed.ToString(@"hh\:mm\:ss")
            : elapsed.ToString(@"mm\:ss");
}
