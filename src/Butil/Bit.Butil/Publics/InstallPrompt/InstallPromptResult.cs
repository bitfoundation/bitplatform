namespace Bit.Butil;

/// <summary>
/// The answer to the install dialog shown by <see cref="InstallPrompt.Prompt"/>.
/// </summary>
public class InstallPromptResult
{
    /// <summary>What the user did - or <see cref="InstallPromptOutcome.Unavailable"/> if nothing was shown.</summary>
    public InstallPromptOutcome Outcome { get; set; }

    /// <summary>
    /// The platform the user installed on (<c>"web"</c>, or <c>"play"</c> when the manifest declares
    /// a related native app). Empty when the prompt was dismissed or unavailable.
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>Shorthand for <c>Outcome == InstallPromptOutcome.Accepted</c>.</summary>
    public bool Accepted => Outcome == InstallPromptOutcome.Accepted;
}
