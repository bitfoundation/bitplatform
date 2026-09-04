namespace Bit.BlazorUI;

/// <summary>
/// The side of the anchor a callout is placed on when there is room for it there.
/// </summary>
/// <remarks>
/// The side is a preference, not a demand: a callout that does not fit on the side asked for is placed on
/// the opposite one, and when neither has room it falls back to the automatic placement, which weighs
/// every side the drop direction allows.
/// </remarks>
public enum BitCalloutSide
{
    /// <summary>
    /// Above the anchor.
    /// </summary>
    Top,

    /// <summary>
    /// Below the anchor.
    /// </summary>
    Bottom,

    /// <summary>
    /// Beside the anchor, on the side the content starts from - the left in a left-to-right layout.
    /// </summary>
    Start,

    /// <summary>
    /// Beside the anchor, on the side the content ends at - the right in a left-to-right layout.
    /// </summary>
    End
}
