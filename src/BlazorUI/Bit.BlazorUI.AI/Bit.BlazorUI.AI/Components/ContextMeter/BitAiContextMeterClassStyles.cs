namespace Bit.BlazorUI;

public class BitAiContextMeterClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiContextMeter.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the label of the BitAiContextMeter.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitProgress meter of the BitAiContextMeter.
    /// </summary>
    public BitProgressClassStyles? Meter { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the percentage readout of the BitAiContextMeter.
    /// </summary>
    public string? Percent { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the visually hidden live region of the BitAiContextMeter.
    /// </summary>
    public string? Status { get; set; }
}
