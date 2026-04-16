namespace VinhKhanhAudioGuide.Mobile.Models;

public sealed record PaymentPackage(
    string Id,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int DurationDays,
    bool IsActive,
    DateTime CreatedAtUtc);