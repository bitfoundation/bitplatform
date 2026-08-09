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
    /// Custom CSS classes/styles for the description (helper text) of the otp input.
    /// </summary>
    public string? Description { get; set; }

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

    /// <summary>
    /// Custom CSS classes/styles for each filled input in otp input.
    /// </summary>
    public string? Filled { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the separator rendered between the inputs of the otp input.
    /// </summary>
    public string? Separator { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the progress bar rendered under the inputs while the otp input is
    /// in the loading state.
    /// </summary>
    public string? Loader { get; set; }
}
