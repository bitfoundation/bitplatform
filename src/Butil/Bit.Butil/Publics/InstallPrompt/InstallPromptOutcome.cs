namespace Bit.Butil;

/// <summary>
/// What the user did with the install dialog shown by <see cref="InstallPrompt.Prompt"/>.
/// </summary>
public enum InstallPromptOutcome
{
    /// <summary>
    /// Nothing was shown: no deferred <c>beforeinstallprompt</c> event was in hand, the call wasn't
    /// tied to a user gesture, or the event had already been used.
    /// </summary>
    Unavailable,

    /// <summary>The user accepted, and the browser is installing the app.</summary>
    Accepted,

    /// <summary>The user dismissed the dialog. The deferred event is spent either way.</summary>
    Dismissed,
}
