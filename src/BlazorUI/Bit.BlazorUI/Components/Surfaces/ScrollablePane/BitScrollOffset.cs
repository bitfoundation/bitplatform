namespace Bit.BlazorUI;

/// <summary>
/// Where a <see cref="BitScrollablePane"/> stands, as measured in the browser.
/// </summary>
/// <remarks>
/// Everything here is in CSS pixels and is read off the element itself, so the six values are the ones the
/// browser had at the moment it was measured rather than anything this component keeps a copy of. The
/// properties below them are derived from those six and cost nothing to read.
/// </remarks>
public class BitScrollOffset
{
    /// <summary>
    /// How far the content has been scrolled along the horizontal axis.
    /// </summary>
    /// <remarks>
    /// This is the raw <c>scrollLeft</c> of the element, so in a right-to-left pane it starts at 0 at the
    /// right edge and runs to a negative value at the left one. Use <see cref="Left"/> where the sign
    /// matters and <see cref="OffsetLeft"/> where the distance from the visual left edge is what is wanted.
    /// </remarks>
    public double Left { get; set; }

    /// <summary>
    /// How far the content has been scrolled along the vertical axis.
    /// </summary>
    public double Top { get; set; }

    /// <summary>
    /// The full width of the content, including the part of it that is scrolled out of sight.
    /// </summary>
    public double ScrollWidth { get; set; }

    /// <summary>
    /// The full height of the content, including the part of it that is scrolled out of sight.
    /// </summary>
    public double ScrollHeight { get; set; }

    /// <summary>
    /// The width of the visible area of the pane, without its scrollbar.
    /// </summary>
    public double ClientWidth { get; set; }

    /// <summary>
    /// The height of the visible area of the pane, without its scrollbar.
    /// </summary>
    public double ClientHeight { get; set; }

    /// <summary>
    /// Whether the pane was laid out right to left when it was measured.
    /// </summary>
    /// <remarks>
    /// It is what tells the two readings of a <see cref="Left"/> of 0 apart: the visual left edge of a
    /// left-to-right pane, and the visual right edge of a right-to-left one. It is only ever true for a
    /// pane that has something to scroll sideways, since an axis with nothing to scroll reads the same
    /// either way.
    /// </remarks>
    public bool Rtl { get; set; }



    /// <summary>
    /// The distance between the visual left edge of the content and the visual left edge of the pane,
    /// which is <see cref="Left"/> made positive and direction independent.
    /// </summary>
    public double OffsetLeft => (Rtl || Left < 0) ? MaxLeft + Left : Left;

    /// <summary>
    /// The largest horizontal offset the pane can reach, which is how much of the content is out of sight.
    /// </summary>
    public double MaxLeft => Math.Max(0, ScrollWidth - ClientWidth);

    /// <summary>
    /// The largest vertical offset the pane can reach, which is how much of the content is out of sight.
    /// </summary>
    public double MaxTop => Math.Max(0, ScrollHeight - ClientHeight);

    /// <summary>
    /// Whether the content is wider than the pane, so there is anything to scroll sideways at all.
    /// </summary>
    public bool ScrollableX => MaxLeft > 0;

    /// <summary>
    /// Whether the content is taller than the pane, so there is anything to scroll up and down at all.
    /// </summary>
    public bool ScrollableY => MaxTop > 0;

    /// <summary>
    /// Whether the pane is standing at the visual left edge of its content.
    /// </summary>
    public bool AtLeft => OffsetLeft <= 0;

    /// <summary>
    /// Whether the pane is standing at the visual right edge of its content.
    /// </summary>
    public bool AtRight => OffsetLeft >= MaxLeft;

    /// <summary>
    /// Whether the pane is standing at the top of its content.
    /// </summary>
    public bool AtTop => Top <= 0;

    /// <summary>
    /// Whether the pane is standing at the bottom of its content.
    /// </summary>
    public bool AtBottom => Top >= MaxTop;

    /// <summary>
    /// How far the pane has been scrolled sideways, from 0 at the visual left edge to 1 at the right one.
    /// A pane with nothing to scroll sideways reports 0.
    /// </summary>
    public double PercentX => MaxLeft > 0 ? Math.Clamp(OffsetLeft / MaxLeft, 0, 1) : 0;

    /// <summary>
    /// How far the pane has been scrolled down, from 0 at the top to 1 at the bottom. A pane with nothing
    /// to scroll up and down reports 0.
    /// </summary>
    public double PercentY => MaxTop > 0 ? Math.Clamp(Top / MaxTop, 0, 1) : 0;
}
