namespace Bit.BlazorUI;

public class BitAiVoiceInputClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiVoiceInput.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiVoiceInput while it listens - through the speech API of
    /// the browser or by recording for the app's transcriber alike.
    /// </summary>
    public string? Listening { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitToggleButton of the BitAiVoiceInput that starts and stops the dictation.
    /// </summary>
    public BitToggleButtonClassStyles? Button { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the time left of a recording for the app's transcriber, shown beside the button of
    /// the BitAiVoiceInput while it records.
    /// </summary>
    public string? Countdown { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the reason a dictation failed, shown beside the button of the BitAiVoiceInput for a
    /// few seconds after the failure.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the visually hidden live region of the BitAiVoiceInput that announces whether the
    /// microphone is on.
    /// </summary>
    public string? LiveRegion { get; set; }
}
