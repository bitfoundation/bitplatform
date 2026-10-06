namespace Bit.BlazorUI;

/// <summary>
/// Defines where a label sits relative to what it labels.
/// </summary>
public enum BitLabelPosition
{
    /// <summary>
    /// The label sits above what it labels.
    /// </summary>
    Top,

    /// <summary>
    /// The label sits after what it labels, on the same line: to its right in left-to-right, to its left in right-to-left.
    /// </summary>
    End,

    /// <summary>
    /// The label sits below what it labels.
    /// </summary>
    Bottom,

    /// <summary>
    /// The label sits before what it labels, on the same line: to its left in left-to-right, to its right in right-to-left.
    /// </summary>
    Start
}
