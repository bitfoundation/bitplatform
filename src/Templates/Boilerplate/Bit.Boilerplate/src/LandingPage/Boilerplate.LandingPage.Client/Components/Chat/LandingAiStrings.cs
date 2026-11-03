namespace Boilerplate.LandingPage.Client.Components.Chat;

internal sealed class LandingAiStrings(IStringLocalizer<LandingStrings> localizer) : BitAiStrings
{
    public override string PromptPlaceholder => localizer[nameof(LandingStrings.ChatPlaceholder)];

    public override string Send => localizer[nameof(LandingStrings.ChatSend)];

    public override string Stop => localizer[nameof(LandingStrings.ChatStop)];

    public override string Retry => localizer[nameof(LandingStrings.ChatRetry)];

    public override string Copy => localizer[nameof(LandingStrings.ChatCopy)];

    public override string ScrollToLatest => localizer[nameof(LandingStrings.ChatScrollToLatest)];

    public override string Thinking => localizer[nameof(LandingStrings.ChatThinking)];

    public override string SomethingWentWrong => localizer[nameof(LandingStrings.ChatFailed)];

    public override string GenerationStopped => localizer[nameof(LandingStrings.ChatStopped)];

    public override string Dismiss => localizer[nameof(LandingStrings.Dismiss)];
}
