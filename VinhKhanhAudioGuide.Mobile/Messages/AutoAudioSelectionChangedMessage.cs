using CommunityToolkit.Mvvm.Messaging.Messages;

namespace VinhKhanhAudioGuide.Mobile.Messages;

public sealed record AutoAudioSelectionPayload(
    string LocationId,
    string LocationName,
    string AudioGuideId,
    string AudioUrl);

public sealed class AutoAudioSelectionChangedMessage : ValueChangedMessage<AutoAudioSelectionPayload>
{
    public AutoAudioSelectionChangedMessage(AutoAudioSelectionPayload value)
        : base(value)
    {
    }
}
