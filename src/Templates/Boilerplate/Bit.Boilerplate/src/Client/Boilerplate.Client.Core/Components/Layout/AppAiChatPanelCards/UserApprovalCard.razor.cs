using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Client.Core.Components.Layout.AppAiChatPanelCards;

/// <summary>The user approves on screen what a tool is about to do (See AppChatbot.AwaitCard). Its text is fixed per action and never written by the model.</summary>
public partial class UserApprovalCard
{
    private readonly string headingId = $"approval-{Guid.NewGuid():N}";

    private string? Decision => Message.Data.GetValueOrDefault("Decision");

    /// <summary>Unanswered and still awaited; a restored or expired card isn't.</summary>
    private bool IsWaiting => Decision is null && Host.IsAwaited(Message);

    private bool IsApproved => Decision is AiChatCardDecision.Approved;

    private ApprovalText Text => Message.Data.GetValueOrDefault("Action") switch
    {
        "ClearAppFiles" => new(
            Title: Localizer[nameof(AppStrings.AiChatApprovalClearAppFilesTitle)],
            Description: Localizer[nameof(AppStrings.AiChatApprovalClearAppFilesDescription)],
            Consequences:
            [
                new(BitIconName.SignOut, Localizer[nameof(AppStrings.AiChatApprovalClearAppFilesSignOut)]),
                new(BitIconName.Delete, Localizer[nameof(AppStrings.AiChatApprovalClearAppFilesDeleteConversation)]),
                new(BitIconName.Refresh, Localizer[nameof(AppStrings.AiChatApprovalClearAppFilesRestart)]),
                .. (Host.IsInVoiceCall ? new ApprovalConsequence[] { new(BitIconName.DeclineCall, Localizer[nameof(AppStrings.AiChatApprovalClearAppFilesEndVoiceCall)]) } : [])
            ],
            Approve: Localizer[nameof(AppStrings.AiChatApprovalClearAppFilesApprove)],
            Decline: Localizer[nameof(AppStrings.AiChatApprovalClearAppFilesDecline)],
            Approved: Localizer[nameof(AppStrings.AiChatApprovalClearAppFilesApproved)],
            Declined: Localizer[nameof(AppStrings.AiChatApprovalClearAppFilesDeclined)],
            NoAnswer: Localizer[nameof(AppStrings.AiChatApprovalClearAppFilesNoAnswer)]),

        _ => new(
            Title: Localizer[nameof(AppStrings.AiChatApprovalTitle)],
            Description: Message.RawMarkdown ?? string.Empty,
            Consequences: [],
            Approve: Localizer[nameof(AppStrings.AiChatApprovalApprove)],
            Decline: Localizer[nameof(AppStrings.AiChatApprovalDecline)],
            Approved: Localizer[nameof(AppStrings.AiChatApprovalApproved)],
            Declined: Localizer[nameof(AppStrings.AiChatApprovalDeclined)],
            NoAnswer: Localizer[nameof(AppStrings.AiChatApprovalNoAnswer)])
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
