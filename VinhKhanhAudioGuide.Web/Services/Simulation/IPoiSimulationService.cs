using VinhKhanhAudioGuide.Web.Models.Simulation;

namespace VinhKhanhAudioGuide.Web.Services.Simulation;

/// <summary>
/// Interface cho dịch vụ giả lập tải POI.
/// </summary>
public interface IPoiSimulationService
{
    /// <summary>Batch đang chạy hiện tại (null nếu chưa bắt đầu hoặc đã kết thúc).</summary>
    SimulationBatch? CurrentBatch { get; }

    /// <summary>Bắt đầu một đợt giả lập mới.</summary>
    Task StartAsync(StartSimulationRequest request, CancellationToken ct = default);

    /// <summary>Dừng đợt giả lập đang chạy.</summary>
    Task StopAsync();
}
