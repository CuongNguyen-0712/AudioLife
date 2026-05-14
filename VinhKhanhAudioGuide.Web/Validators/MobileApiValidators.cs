using FluentValidation;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Validators;

public class MobileHeartbeatRequestValidator : AbstractValidator<MobileHeartbeatRequest>
{
    public MobileHeartbeatRequestValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("DeviceId is required.");

        RuleFor(x => x.SessionToken)
            .NotEmpty().WithMessage("SessionToken is required.");
    }
}

public class MobilePaymentCompletionRequestValidator : AbstractValidator<MobilePaymentCompletionRequest>
{
    public MobilePaymentCompletionRequestValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("DeviceId is required.");

        RuleFor(x => x.PackageId)
            .NotEmpty().WithMessage("PackageId is required.");

        RuleFor(x => x.PaymentStatus)
            .NotEmpty().WithMessage("PaymentStatus is required.");
    }
}

public class MobileAddListeningHistoryRequestValidator : AbstractValidator<MobileAddListeningHistoryRequest>
{
    public MobileAddListeningHistoryRequestValidator()
    {
        RuleFor(x => x.AudioGuideId)
            .NotEmpty().WithMessage("AudioGuideId is required.");

        RuleFor(x => x.LocationId)
            .NotEmpty().WithMessage("LocationId is required.");

        RuleFor(x => x.Progress)
            .InclusiveBetween(0, 1).WithMessage("Progress must be between 0 and 1.");
    }
}

public class MobileSubmitReviewRequestValidator : AbstractValidator<MobileSubmitReviewRequest>
{
    public MobileSubmitReviewRequestValidator()
    {
        RuleFor(x => x.LocationId)
            .NotEmpty().WithMessage("LocationId is required.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("Comment cannot exceed 500 characters.");
    }
}

public class SessionRefreshRequestValidator : AbstractValidator<SessionRefreshRequest>
{
    public SessionRefreshRequestValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("DeviceId is required.");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("RefreshToken is required.");
    }
}

public class MobileDeviceRegistrationRequestValidator : AbstractValidator<MobileDeviceRegistrationRequest>
{
    public MobileDeviceRegistrationRequestValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("DeviceId is required.");

        RuleFor(x => x.DeviceToken)
            .NotEmpty().WithMessage("DeviceToken is required.");

        RuleFor(x => x.Platform)
            .NotEmpty().WithMessage("Platform is required.")
            .Must(p => p == "Android" || p == "iOS" || p == "Web")
            .WithMessage("Platform must be Android, iOS, or Web.");
    }
}
