namespace Bit.BlazorUI;

/// <summary>
/// Determines the accessible relationship between a tooltip and the anchor it belongs to.
/// </summary>
public enum BitTooltipRelationship
{
    /// <summary>
    /// The tooltip adds information to an anchor that already has a name of its own, and is pointed at
    /// with aria-describedby. This is what a tooltip usually is.
    /// </summary>
    Description,

    /// <summary>
    /// The tooltip is the name of an anchor that has none of its own - an icon-only button, above all -
    /// and is pointed at with aria-labelledby.
    /// </summary>
    Label,

    /// <summary>
    /// The tooltip is left out of the accessibility tree altogether, for the case where the anchor
    /// already carries the same text by another route and a second announcement of it is noise.
    /// </summary>
    None
}
