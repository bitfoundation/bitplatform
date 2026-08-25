namespace Bit.BlazorUI;

/// <summary>
/// A single mark drawn on the track of a <see cref="BitSlider"/>, optionally carrying a label under it.
/// </summary>
public class BitSliderMark
{
    /// <summary>
    /// Creates a mark with no label.
    /// </summary>
    public BitSliderMark() { }

    /// <summary>
    /// Creates a mark with no label at the given value.
    /// </summary>
    public BitSliderMark(double value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a labelled mark at the given value.
    /// </summary>
    public BitSliderMark(double value, string? label)
    {
        Value = value;
        Label = label;
    }

    /// <summary>
    /// The value the mark sits at. A mark outside the Min..Max range of the slider is not rendered.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// The text rendered under the mark. A mark without a label is drawn as a plain tick.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Custom CSS classes for this mark, added to both its tick and its label.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// Custom CSS styles for this mark, added to both its tick and its label.
    /// </summary>
    public string? Style { get; set; }
}
