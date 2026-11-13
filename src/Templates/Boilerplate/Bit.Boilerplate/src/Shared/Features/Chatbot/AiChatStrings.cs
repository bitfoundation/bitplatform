using Bit.BlazorUI;

namespace Boilerplate.Shared.Features.Chatbot;

/// <summary>
/// The words of Bit.BlazorUI.AI the AI chat panel shows, read from AppStrings so they follow the app's language. What the
/// panel never shows - artifacts, agents, forms the model fills, the model picker - keeps the library's English.
/// <para>
/// Here rather than beside the panel: the client's router reads every type of the client's assemblies as the app starts,
/// before the chat's are loaded (See AppAiChatPanel.EnsureLoaded), and a type built on one of theirs would make them fail to
/// load for the rest of the visit. Nothing reads the types of this assembly that way (See AppProblemDetails' type lookup).
/// </para>
/// </summary>
public sealed class AiChatStrings(IStringLocalizer<AppStrings> localizer) : BitAiStrings
{
    public override string PromptPlaceholder => localizer[nameof(AppStrings.AiChatPanelPlaceholder)];

    public override string Send => localizer[nameof(AppStrings.Send)];

    public override string Stop => localizer[nameof(AppStrings.AiChatStop)];

    public override string Regenerate => localizer[nameof(AppStrings.AiChatRegenerate)];

    public override string Retry => localizer[nameof(AppStrings.AiChatRetry)];

    public override string Copy => localizer[nameof(AppStrings.Copy)];

    public override string Copied => localizer[nameof(AppStrings.Copied)];

    public override string Edit => localizer[nameof(AppStrings.Edit)];

    public override string Save => localizer[nameof(AppStrings.Send)];

    public override string Cancel => localizer[nameof(AppStrings.Cancel)];

    public override string Dismiss => localizer[nameof(AppStrings.Close)];

    public override string Remove => localizer[nameof(AppStrings.Remove)];

    public override string GoodResponse => localizer[nameof(AppStrings.AiChatGoodAnswer)];

    public override string BadResponse => localizer[nameof(AppStrings.AiChatBadAnswer)];

    public override string FollowUps => localizer[nameof(AppStrings.AiChatFollowUps)];

    public override string ScrollToLatest => localizer[nameof(AppStrings.AiChatScrollToLatest)];

    public override string Thinking => localizer[nameof(AppStrings.AiChatThinking)];

    public override string SomethingWentWrong => localizer[nameof(AppStrings.AiChatPanelVoiceCallAssistantFailed)];

    public override string GenerationStopped => localizer[nameof(AppStrings.Canceled)];

    public override string AttachFile => localizer[nameof(AppStrings.AiChatPanelAttachImage)];

    public override string DropFiles => localizer[nameof(AppStrings.AiChatDropImage)];

    public override string UploadFailed => localizer[nameof(AppStrings.AiChatPanelAttachmentFailed)];

    public override string AttachmentTooLarge(string fileName, long maxSize) => localizer[nameof(AppStrings.AiChatPanelImageTooLarge)];

    public override string AttachmentTypeNotAllowed(string fileName) => localizer[nameof(AppStrings.AiChatPanelOnlyImagesAllowed)];

    public override string AttachmentTooMany(string fileName, int maxCount) => localizer[nameof(AppStrings.AiChatTooManyImages), maxCount];

    public override string StartDictation => localizer[nameof(AppStrings.AiChatPanelDictate)];

    public override string StopDictation => localizer[nameof(AppStrings.AiChatPanelStopDictation)];

    public override string Listening => localizer[nameof(AppStrings.AiChatListening)];

    public override string Transcribing => localizer[nameof(AppStrings.AiChatPanelTranscribing)];

    public override string DictationFailed(string error) => localizer[nameof(AppStrings.AiChatPanelDictationStopped), error];

    public override string RecordingStarted(TimeSpan remaining) => localizer[nameof(AppStrings.AiChatRecordingStarted), RecordingDuration(remaining)];

    public override string RecordingTimeLeft(TimeSpan remaining) => localizer[nameof(AppStrings.AiChatRecordingTimeLeft), RecordingDuration(remaining)];

    // The library's own says it in English. Abbreviated units, so no language needs a plural of its own.
    public override string RecordingDuration(TimeSpan duration)
    {
        var seconds = (int)Math.Ceiling(Math.Max(0, duration.TotalSeconds));
        var (minutes, rest) = (seconds / 60, seconds % 60);

        if (minutes is 0) return localizer[nameof(AppStrings.AiChatDurationSeconds), rest];

        if (rest is 0) return localizer[nameof(AppStrings.AiChatDurationMinutes), minutes];

        return $"{localizer[nameof(AppStrings.AiChatDurationMinutes), minutes]} {localizer[nameof(AppStrings.AiChatDurationSeconds), rest]}";
    }

    public override string ReadAloud => localizer[nameof(AppStrings.AiChatPanelReadAloud)];

    public override string StopReadingAloud => localizer[nameof(AppStrings.AiChatPanelStopReading)];

    // A tool's heading is the title the panel gives it (See AppAiChatTools), the same while it runs and once it's done:
    // the spinner beside it is what says which
    public override string CallingTool(string tool) => tool;

    public override string CalledTool(string tool) => tool;

    public override string ToolFailed(string tool) => localizer[nameof(AppStrings.AiChatToolFailed), tool];

    public override string ToolRejected(string tool) => localizer[nameof(AppStrings.AiChatToolDeclined), tool];

    public override string ToolCancelled(string tool) => localizer[nameof(AppStrings.AiChatToolCancelled), tool];

    public override string ToolAwaitingApproval(string tool) => localizer[nameof(AppStrings.AiChatApprovalWaiting)];

    public override string Approve => localizer[nameof(AppStrings.AiChatApprovalApprove)];

    public override string Reject => localizer[nameof(AppStrings.AiChatApprovalDecline)];
}
