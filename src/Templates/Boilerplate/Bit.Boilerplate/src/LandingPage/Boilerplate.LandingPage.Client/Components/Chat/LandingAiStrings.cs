namespace Boilerplate.LandingPage.Client.Components.Chat;

/// <summary>The words of Bit.BlazorUI.AI the landing page's chat shows, read from LandingStrings so they follow the page's language.</summary>
internal sealed class LandingAiStrings(IStringLocalizer<LandingStrings> localizer) : BitAiStrings
{
    public override string PromptPlaceholder => localizer[nameof(LandingStrings.ChatPlaceholder)];

    public override string Send => localizer[nameof(LandingStrings.ChatSend)];

    public override string Stop => localizer[nameof(LandingStrings.ChatStop)];

    public override string Retry => localizer[nameof(LandingStrings.ChatRetry)];

    public override string Regenerate => localizer[nameof(LandingStrings.ChatRegenerate)];

    public override string Copy => localizer[nameof(LandingStrings.ChatCopy)];

    public override string Copied => localizer[nameof(LandingStrings.ChatCopied)];

    public override string Edit => localizer[nameof(LandingStrings.ChatEdit)];

    public override string Save => localizer[nameof(LandingStrings.ChatSave)];

    public override string Cancel => localizer[nameof(LandingStrings.ChatCancel)];

    public override string FollowUps => localizer[nameof(LandingStrings.ChatFollowUps)];

    public override string StartDictation => localizer[nameof(LandingStrings.ChatStartDictation)];

    public override string StopDictation => localizer[nameof(LandingStrings.ChatStopDictation)];

    public override string Listening => localizer[nameof(LandingStrings.ChatListening)];

    public override string DictationStopped => localizer[nameof(LandingStrings.ChatDictationStopped)];

    // The error is the browser's own code (not-allowed, no-speech, ...), which says nothing to a visitor
    public override string DictationFailed(string error) => localizer[nameof(LandingStrings.ChatDictationFailed)];

    public override string ScrollToLatest => localizer[nameof(LandingStrings.ChatScrollToLatest)];

    public override string Thinking => localizer[nameof(LandingStrings.ChatThinking)];

    public override string Reasoning => localizer[nameof(LandingStrings.ChatReasoning)];

    public override string ReasonedFor(TimeSpan duration) => localizer[nameof(LandingStrings.ChatReasonedFor), FormatDuration(duration)];

    // The library's own says it in English. Abbreviated units, so no language needs a plural of its own.
    public override string FormatDuration(TimeSpan duration)
    {
        var seconds = (int)Math.Ceiling(Math.Max(0, duration.TotalSeconds));
        var (minutes, rest) = (seconds / 60, seconds % 60);

        if (minutes is 0) return localizer[nameof(LandingStrings.ChatDurationSeconds), rest];

        if (rest is 0) return localizer[nameof(LandingStrings.ChatDurationMinutes), minutes];

        return $"{localizer[nameof(LandingStrings.ChatDurationMinutes), minutes]} {localizer[nameof(LandingStrings.ChatDurationSeconds), rest]}";
    }

    public override string SomethingWentWrong => localizer[nameof(LandingStrings.ChatFailed)];

    public override string GenerationStopped => localizer[nameof(LandingStrings.ChatStopped)];

    public override string Dismiss => localizer[nameof(LandingStrings.Dismiss)];
}
