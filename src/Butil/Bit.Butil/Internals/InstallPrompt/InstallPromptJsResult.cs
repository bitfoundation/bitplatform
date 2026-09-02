namespace Bit.Butil;

/// <summary>
/// The shape JS hands back from <c>BitButil.installPrompt.prompt</c>: the raw
/// <c>userChoice</c> members, before the outcome string is mapped onto
/// <see cref="InstallPromptOutcome"/>.
/// </summary>
internal class InstallPromptJsResult
{
    public string Outcome { get; set; } = "unavailable";

    public string Platform { get; set; } = string.Empty;
}
