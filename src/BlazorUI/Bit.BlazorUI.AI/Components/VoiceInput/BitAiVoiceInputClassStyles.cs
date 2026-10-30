namespace Bit.BlazorUI;

public class BitAiVoiceInputClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiVoiceInput.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitToggleButton of the BitAiVoiceInput that starts and stops the dictation.
    /// </summary>
    public BitToggleButtonClassStyles? Button { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the visually hidden live region of the BitAiVoiceInput that announces whether the
    /// microphone is on.
    /// </summary>
    public string? LiveRegion { get; set; }
}
