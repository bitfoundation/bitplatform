namespace Boilerplate.Workbench.Components.Pages.Showcases.Assistant;

public sealed class AssistantAiStrings(IStringLocalizer<WorkbenchStrings> localizer) : BitAiStrings
{
    public override string Send => localizer[nameof(WorkbenchStrings.AssistantSend)];

    public override string Stop => localizer[nameof(WorkbenchStrings.AssistantStop)];

    public override string Regenerate => localizer[nameof(WorkbenchStrings.AssistantRegenerate)];

    public override string Retry => localizer[nameof(WorkbenchStrings.AssistantRetry)];

    public override string Copy => localizer[nameof(WorkbenchStrings.AssistantCopy)];

    public override string Copied => localizer[nameof(WorkbenchStrings.AssistantCopied)];

    public override string Edit => localizer[nameof(WorkbenchStrings.AssistantEdit)];

    public override string Save => localizer[nameof(WorkbenchStrings.AssistantSave)];

    public override string Cancel => localizer[nameof(WorkbenchStrings.AssistantCancel)];

    public override string GoodResponse => localizer[nameof(WorkbenchStrings.AssistantHelpful)];

    public override string BadResponse => localizer[nameof(WorkbenchStrings.AssistantNotHelpful)];

    public override string ScrollToLatest => localizer[nameof(WorkbenchStrings.AssistantScrollToLatest)];

    public override string Thinking => localizer[nameof(WorkbenchStrings.AssistantThinking)];

    public override string Reasoning => localizer[nameof(WorkbenchStrings.AssistantReasoning)];

    public override string ReasonedFor(TimeSpan duration) => localizer[nameof(WorkbenchStrings.AssistantReasonedFor), FormatDuration(duration)];

    public override string Sources => localizer[nameof(WorkbenchStrings.AssistantSources)];

    public override string CitationLabel(int? number, string title) => number is null ? title : localizer[nameof(WorkbenchStrings.AssistantCitationLabel), number, title];

    public override string AttachFile => localizer[nameof(WorkbenchStrings.AssistantAttachFile)];

    public override string Remove => localizer[nameof(WorkbenchStrings.AssistantRemove)];

    public override string DropFiles => localizer[nameof(WorkbenchStrings.AssistantDropFiles)];

    public override string Dismiss => localizer[nameof(WorkbenchStrings.AssistantDismiss)];

    public override string AttachmentTooLarge(string fileName, long maxSize) => localizer[nameof(WorkbenchStrings.AssistantAttachmentTooLarge), fileName, FormatBytes(maxSize)];

    public override string AttachmentTypeNotAllowed(string fileName) => localizer[nameof(WorkbenchStrings.AssistantAttachmentTypeNotAllowed), fileName];

    public override string AttachmentReadFailed(string fileName) => localizer[nameof(WorkbenchStrings.AssistantAttachmentReadFailed), fileName];

    public override string AttachmentTooMany(string fileName, int maxCount) => localizer[nameof(WorkbenchStrings.AssistantAttachmentTooMany), fileName, maxCount];

    public override string Model => localizer[nameof(WorkbenchStrings.AssistantModel)];

    public override string CallingTool(string tool) => localizer[nameof(WorkbenchStrings.AssistantCallingTool), tool];

    public override string CalledTool(string tool) => localizer[nameof(WorkbenchStrings.AssistantCalledTool), tool];

    public override string ToolFailed(string tool) => localizer[nameof(WorkbenchStrings.AssistantToolFailed), tool];

    public override string ToolCancelled(string tool) => localizer[nameof(WorkbenchStrings.AssistantToolCancelled), tool];

    public override string ToolRejected(string tool) => localizer[nameof(WorkbenchStrings.AssistantToolRejected), tool];

    public override string ToolAwaitingApproval(string tool) => localizer[nameof(WorkbenchStrings.AssistantToolAwaitingApproval), tool];

    public override string ApproveTool(string tool) => localizer[nameof(WorkbenchStrings.AssistantApproveTool), tool];

    public override string Approve => localizer[nameof(WorkbenchStrings.AssistantApprove)];

    public override string ApproveAlways => localizer[nameof(WorkbenchStrings.AssistantApproveAlways)];

    public override string Reject => localizer[nameof(WorkbenchStrings.AssistantReject)];

    public override string ToolCallDeclined => localizer[nameof(WorkbenchStrings.AssistantToolDeclined)];

    public override string Arguments => localizer[nameof(WorkbenchStrings.AssistantArguments)];

    public override string Result => localizer[nameof(WorkbenchStrings.AssistantResult)];

    public override string JsonRoot => localizer[nameof(WorkbenchStrings.AssistantJsonRoot)];

    public override string Download => localizer[nameof(WorkbenchStrings.AssistantDownload)];

    public override string Expand => localizer[nameof(WorkbenchStrings.AssistantExpand)];

    public override string Collapse => localizer[nameof(WorkbenchStrings.AssistantCollapse)];

    public override string Version => localizer[nameof(WorkbenchStrings.AssistantVersion)];

    public override string NewConversation => localizer[nameof(WorkbenchStrings.AssistantNewChat)];

    public override string Conversations => localizer[nameof(WorkbenchStrings.AssistantConversations)];

    public override string Transcript => localizer[nameof(WorkbenchStrings.AssistantTranscript)];

    public override string Delete => localizer[nameof(WorkbenchStrings.AssistantDelete)];

    public override string Untitled => localizer[nameof(WorkbenchStrings.AssistantUntitled)];

    public override string Today => localizer[nameof(WorkbenchStrings.AssistantToday)];

    public override string Yesterday => localizer[nameof(WorkbenchStrings.AssistantYesterday)];

    public override string PreviousSevenDays => localizer[nameof(WorkbenchStrings.AssistantPreviousSevenDays)];

    public override string PreviousThirtyDays => localizer[nameof(WorkbenchStrings.AssistantPreviousThirtyDays)];

    public override string SomethingWentWrong => localizer[nameof(WorkbenchStrings.AssistantSomethingWentWrong)];

    public override string Tokens => localizer[nameof(WorkbenchStrings.AssistantTokens)];

    public override string TokensPerSecond(double rate) => localizer[nameof(WorkbenchStrings.AssistantTokensPerSecond), rate.ToString("0.#", CultureInfo.CurrentCulture)];

    public override string InputTokens => localizer[nameof(WorkbenchStrings.AssistantInputTokens)];

    public override string OutputTokens => localizer[nameof(WorkbenchStrings.AssistantOutputTokens)];

    public override string ReasoningTokens => localizer[nameof(WorkbenchStrings.AssistantReasoning)];

    public override string UsageLine(string label, long tokens) => localizer[nameof(WorkbenchStrings.AssistantUsageLine), label, FormatTokens(tokens)];

    public override string Context => localizer[nameof(WorkbenchStrings.AssistantContext)];

    public override string ContextUsage(long used, long window) => localizer[nameof(WorkbenchStrings.AssistantContextUsage), FormatTokens(used), FormatTokens(window)];

    public override string ContextWarning => localizer[nameof(WorkbenchStrings.AssistantContextWarning)];

    public override string ContextCritical => localizer[nameof(WorkbenchStrings.AssistantContextCritical)];

    public override string FormatDuration(TimeSpan duration) => localizer[nameof(WorkbenchStrings.AssistantSeconds), Math.Max(0.1, duration.TotalSeconds).ToString("0.#", CultureInfo.CurrentCulture)];

    public override string FormatBytes(long bytes) => bytes < 1024 * 1024
        ? localizer[nameof(WorkbenchStrings.AssistantKilobytes), Math.Max(1, bytes / 1024).ToString("N0", CultureInfo.CurrentCulture)]
        : localizer[nameof(WorkbenchStrings.AssistantMegabytes), (bytes / (1024.0 * 1024)).ToString("0.#", CultureInfo.CurrentCulture)];

    public override string FormatTokens(long tokens) => tokens.ToString("N0", CultureInfo.CurrentCulture);
}
