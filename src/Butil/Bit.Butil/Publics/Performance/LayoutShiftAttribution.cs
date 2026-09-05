namespace Bit.Butil;

/// <summary>
/// One element that moved during a layout shift, with the rectangle it occupied before and after.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/LayoutShiftAttribution">https://developer.mozilla.org/en-US/docs/Web/API/LayoutShiftAttribution</see>
/// </summary>
/// <remarks>
/// The DOM node itself is deliberately not carried across the interop boundary - it is a live object
/// that cannot be serialized. The two rectangles are what identifies the shift in practice: the
/// element that moved is the one that was at <see cref="PreviousRect"/>.
/// </remarks>
public class LayoutShiftAttribution
{
    /// <summary>Where the element was before the shift.</summary>
    public LayoutShiftRect? PreviousRect { get; set; }

    /// <summary>Where it ended up.</summary>
    public LayoutShiftRect? CurrentRect { get; set; }
}
