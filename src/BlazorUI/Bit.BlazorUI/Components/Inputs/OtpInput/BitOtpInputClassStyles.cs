namespace Bit.BlazorUI;

public class BitOtpInputClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the otp input.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the label of the otp input.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the wrapper element of the inputs.
    /// </summary>
    public string? InputsWrapper { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each input in otp input.
    /// </summary>
    public string? Input { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for current focused input in otp input.
    /// </summary>
    public string? Focused { get; set; }
}
