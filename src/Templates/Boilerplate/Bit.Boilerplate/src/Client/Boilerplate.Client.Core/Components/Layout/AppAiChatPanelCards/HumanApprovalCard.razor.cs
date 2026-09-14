using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Client.Core.Components.Layout.AppAiChatPanelCards;

/// <summary>Human in the loop (See AppChatbot.AwaitCard). Its text is fixed per action and never written by the model.</summary>
public partial class HumanApprovalCard
{
    private readonly string headingId = $"approval-{Guid.NewGuid():N}";

    private string? Decision => Message.Data.GetValueOrDefault("Decision");

    /// <summary>Unanswered and still awaited; a restored or expired card isn't.</summary>
    private bool IsWaiting => Decision is null && Host.IsAwaited(Message);

    private bool IsApproved => Decision is AiChatCardDecision.Approved;

    private ApprovalText Text => Message.Data.GetValueOrDefault("Action") switch
    {
        "ClearAppFiles" => new(
            Title: Localizer["Clear the app's files on this device?"],
            Description: Localizer["Starting fresh fixes most problems caused by broken local data."],
            Consequences:
            [
                new(BitIconName.SignOut, Localizer["You'll be signed out"]),
                new(BitIconName.Delete, Localizer["This conversation will be deleted"]),
                new(BitIconName.Refresh, Localizer["The app will restart"]),
                .. (Host.IsInVoiceCall ? new ApprovalConsequence[] { new(BitIconName.DeclineCall, Localizer["The voice call will end"]) } : [])
            ],
            Approve: Localizer["Clear files"],
            Decline: Localizer["Not now"],
            Approved: Localizer["Clearing the app's files"],
            Declined: Localizer["You kept the app's files."],
            NoAnswer: Localizer["Nothing was cleared: the request went unanswered."]),

        _ => new(
            Title: Localizer["Approve this?"],
            Description: Message.RawMarkdown ?? string.Empty,
            Consequences: [],
            Approve: Localizer["Approve"],
            Decline: Localizer["Decline"],
            Approved: Localizer["Approved"],
            Declined: Localizer["Declined."],
            NoAnswer: Localizer["The request went unanswered."])
    };

    private string OutcomeText(ApprovalText text) => Decision switch
    {
        AiChatCardDecision.Approved => text.Approved,
        AiChatCardDecision.Declined => text.Declined,
        _ => text.NoAnswer
    };

    private string OutcomeIcon => Decision is AiChatCardDecision.Declined ? BitIconName.Cancel : BitIconName.Clock;

    private string OutcomeClass => Decision switch
    {
        AiChatCardDecision.Approved => "approved",
        AiChatCardDecision.Declined => "declined",
        _ => "unanswered"
    };

    private Task Approve() => Host.Resolve(Message, AiChatCardDecision.Approved);

    private Task Decline() => Host.Resolve(Message, AiChatCardDecision.Declined);

    private sealed record ApprovalText(string Title,
        string Description,
        ApprovalConsequence[] Consequences,
        string Approve,
        string Decline,
        string Approved,
        string Declined,
        string NoAnswer);

    private sealed record ApprovalConsequence(string IconName, string Text);
}
