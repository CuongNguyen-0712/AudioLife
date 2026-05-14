namespace VinhKhanhAudioGuide.Web.Models.Simulation;

/// <summary>
/// Trạng thái của toàn bộ batch giả lập.
/// </summary>
public enum SimulationStatus
{
    Idle,
    Running,
    Paused,
    Completed,
    Cancelled
}

/// <summary>
/// Trạng thái của từng Virtual User.
/// </summary>
public enum VirtualUserStatus
{
    Queued,
    Running,
    Completed,
    Failed
}

/// <summary>
/// Cấu hình cho một đợt giả lập do Admin thiết lập.
/// </summary>
public class SimulationConfig
{
    /// <summary>ID của Location (POI) cần giả lập.</summary>
    public string LocationId { get; set; } = string.Empty;

    /// <summary>ID của AudioGuide cần phát.</summary>
    public string AudioGuideId { get; set; } = string.Empty;

    /// <summary>Tên hiển thị của Location.</summary>
    public string LocationName { get; set; } = string.Empty;

    /// <summary>Tên hiển thị của AudioGuide.</summary>
    public string AudioGuideName { get; set; } = string.Empty;

    /// <summary>Tổng thời lượng audio (phút).</summary>
    public int AudioDurationMinutes { get; set; } = 5;

    /// <summary>Số lượng Virtual Users cần giả lập.</summary>
    public int VirtualUserCount { get; set; } = 50;

    /// <summary>
    /// Hệ số tăng tốc thời gian: 1 = thời gian thật, 10 = nhanh gấp 10 lần.
    /// VD: AudioDuration=5 phút, SpeedMultiplier=10 → hoàn thành trong 30 giây thực.
    /// </summary>
    public int SpeedMultiplier { get; set; } = 10;

    /// <summary>
    /// Giới hạn concurrency: số Virtual User chạy cùng lúc tối đa.
    /// Giúp tránh spike cơ sở dữ liệu.
    /// </summary>
    public int MaxConcurrency { get; set; } = 20;

    /// <summary>Ghi dữ liệu giả lập vào Database hay chỉ giả lập in-memory.</summary>
    public bool WriteToDatabase { get; set; } = false;
}

/// <summary>
/// Thông tin tổng thể về một đợt chạy giả lập (Batch).
/// </summary>
public class SimulationBatch
{
    public string BatchId { get; init; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
    public SimulationConfig Config { get; set; } = new();
    public SimulationStatus Status { get; set; } = SimulationStatus.Idle;

    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int CompletedUsers { get; set; }
    public int FailedUsers { get; set; }
    public long TotalRequests { get; set; }
    public long FailedRequests { get; set; }

    public double OverallProgressPercent => TotalUsers == 0 ? 0
        : Math.Round((double)(CompletedUsers + FailedUsers) / TotalUsers * 100, 1);

    public double SuccessRate => (TotalRequests == 0) ? 100
        : Math.Round((double)(TotalRequests - FailedRequests) / TotalRequests * 100, 1);

    public TimeSpan Elapsed => Status == SimulationStatus.Running
        ? DateTime.UtcNow - StartedAtUtc
        : (CompletedAtUtc ?? StartedAtUtc) - StartedAtUtc;
}

/// <summary>
/// Snapshot metrics được push xuống client qua SignalR mỗi giây.
/// </summary>
public class SimulationMetricsSnapshot
{
    public string BatchId { get; set; } = string.Empty;
    public SimulationStatus Status { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int CompletedUsers { get; set; }
    public int FailedUsers { get; set; }
    public long TotalRequests { get; set; }
    public long FailedRequests { get; set; }
    public double OverallProgressPercent { get; set; }
    public double SuccessRate { get; set; }
    public double RequestsPerSecond { get; set; }
    public string ElapsedDisplay { get; set; } = "00:00";
    public string Timestamp { get; set; } = DateTime.Now.ToString("HH:mm:ss");
}

/// <summary>
/// Một dòng log được stream về client theo thời gian thực.
/// </summary>
public class SimulationLogEntry
{
    public string BatchId { get; set; } = string.Empty;
    public string Timestamp { get; set; } = DateTime.Now.ToString("HH:mm:ss.fff");
    public string Level { get; set; } = "INFO"; // INFO | WARN | ERROR | SUCCESS | SIM
    public string VirtualUserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public double? Progress { get; set; }
}

/// <summary>
/// Request từ Admin để bắt đầu, dừng một batch giả lập.
/// </summary>
public class StartSimulationRequest
{
    public string LocationId { get; set; } = string.Empty;
    public string AudioGuideId { get; set; } = string.Empty;
    public int VirtualUserCount { get; set; } = 50;
    public int SpeedMultiplier { get; set; } = 10;
    public int MaxConcurrency { get; set; } = 20;
    public bool WriteToDatabase { get; set; } = false;
}
