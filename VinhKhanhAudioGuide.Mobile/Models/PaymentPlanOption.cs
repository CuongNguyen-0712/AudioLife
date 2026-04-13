namespace VinhKhanhAudioGuide.Mobile.Models;

public sealed record PaymentPlanOption(
    string Id,
    string Title,
    string PriceLabel,
    string Description,
    decimal Amount);
