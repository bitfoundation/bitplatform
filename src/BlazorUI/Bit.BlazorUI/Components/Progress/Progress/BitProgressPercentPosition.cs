namespace Bit.BlazorUI;

/// <summary>
/// Where the percentage readout of a linear BitProgress is placed. The readout of a circular one is
/// always in the middle of the ring, so this has no effect there.
/// </summary>
public enum BitProgressPercentPosition
{
    /// <summary>
    /// Under the bar, aligned to the end of it. This is the default.
    /// </summary>
    End,

    /// <summary>
    /// Under the bar, aligned to the start of it.
    /// </summary>
    Start,

    /// <summary>
    /// Under the bar, in the middle of it.
    /// </summary>
    Center,

    /// <summary>
    /// On the bar itself rather than under it, which keeps the whole indicator to one line. The text
    /// is painted in the contrast color of the bar, so it stays legible over the filled part.
    /// </summary>
    Inside,

    /// <summary>
    /// Above the bar, on the same row as the label and aligned to the end of it - the layout that reads
    /// "Uploading ... 42 %" over the bar it belongs to. Without a label it is a line of its own above the
    /// bar.
    /// </summary>
    Top
}
