namespace Bit.BlazorUI;

/// <summary>
/// Represents a shortcut that fills the BitDateRangePicker with a predefined range (e.g. "Last 7 days").
/// </summary>
public class BitDateRangePickerPreset
{
    /// <summary>
    /// The text of the preset's button.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// The range applied when the preset is selected.
    /// <see cref="ValueProvider"/> takes precedence over this when both are set.
    /// </summary>
    public BitDateRangePickerValue? Value { get; set; }

    /// <summary>
    /// Custom function providing the range applied when the preset is selected.
    /// Unlike <see cref="Value"/> it is evaluated on each selection, which keeps relative
    /// ranges (e.g. "Last 7 days") correct no matter how long the page has been open.
    /// </summary>
    public Func<BitDateRangePickerValue?>? ValueProvider { get; set; }

    /// <summary>
    /// Whether the preset's button is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// The title of the preset's button (tooltip).
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Custom CSS class for the preset's button.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// Custom CSS style for the preset's button.
    /// </summary>
    public string? Style { get; set; }
}
