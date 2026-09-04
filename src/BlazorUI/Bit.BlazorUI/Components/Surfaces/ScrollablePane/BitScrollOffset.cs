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
    /// How far the pane has moved sideways since the position before this one was reported, measured on
    /// the screen rather than in reading order: a positive value is a move to the right whichever way the
    /// pane reads.
    /// </summary>
    /// <remarks>
    /// It is the change in <see cref="OffsetLeft"/>, which is what a page reacting to the DIRECTION of a
    /// scroll - a toolbar that folds away on the way down and comes back on the way up - reads instead of
    /// keeping the previous position of its own. It is only carried by the reports
    /// <see cref="BitScrollablePane.OnScroll"/> makes: a position asked for at a moment of the page's own
    /// choosing, and the ones the start and the end of a scroll carry, have nothing to have moved from
    /// and report 0.
    /// </remarks>
    public double DeltaLeft { get; set; }

    /// <summary>
    /// How far the pane has moved up or down since the position before this one was reported, positive
    /// downwards. See <see cref="DeltaLeft"/> for when it is carried.
    /// </summary>
    public double DeltaTop { get; set; }



    /// <summary>
    /// The distance between the visual left edge of the content and the visual left edge of the pane,
    /// which is <see cref="Left"/> made positive and direction independent.
    /// </summary>
    /// <remarks>
    /// Only <see cref="Rtl"/> folds the sign away. A negative <see cref="Left"/> on a pane that reads
    /// left-to-right is not a right-to-left reading: it is the elastic overscroll of a pane being bounced
    /// past its left edge, and folding that one over would report it as standing at the far end instead.
    /// </remarks>
    public double OffsetLeft => Rtl ? MaxLeft + Left : Left;

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
    /// <remarks>
    /// A pane within a pixel of the edge counts as standing at it: a scroll offset is fractional at a
    /// fractional zoom level and on a scaled display, so an exact comparison would leave a pane that is
    /// visibly at its edge reporting that it is not.
    /// </remarks>
    public bool AtLeft => OffsetLeft <= Tolerance;

    /// <summary>
    /// Whether the pane is standing at the visual right edge of its content.
    /// </summary>
    /// <remarks>
    /// A pane within a pixel of the edge counts as standing at it - see <see cref="AtLeft"/> for why.
    /// </remarks>
    public bool AtRight => OffsetLeft >= MaxLeft - Tolerance;

    /// <summary>
    /// Whether the pane is standing at the top of its content.
    /// </summary>
    /// <remarks>
    /// A pane within a pixel of the edge counts as standing at it - see <see cref="AtLeft"/> for why.
    /// </remarks>
    public bool AtTop => Top <= Tolerance;

    /// <summary>
    /// Whether the pane is standing at the bottom of its content.
    /// </summary>
    /// <remarks>
    /// A pane within a pixel of the edge counts as standing at it - see <see cref="AtLeft"/> for why.
    /// </remarks>
    public bool AtBottom => Top >= MaxTop - Tolerance;

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

    /// <summary>
    /// Whether the move this report carries was downwards, which is what a header that gets out of the
    /// way on the way down and comes back on the way up reads. It is derived from <see cref="DeltaTop"/>,
    /// so it is only ever true on a report that carries one.
    /// </summary>
    public bool ScrollingDown => DeltaTop > 0;

    /// <summary>
    /// Whether the move this report carries was upwards. See <see cref="ScrollingDown"/>.
    /// </summary>
    public bool ScrollingUp => DeltaTop < 0;

    /// <summary>
    /// Whether the move this report carries was to the right on the screen, whichever way the pane reads.
    /// See <see cref="ScrollingDown"/>.
    /// </summary>
    public bool ScrollingRight => DeltaLeft > 0;

    /// <summary>
    /// Whether the move this report carries was to the left on the screen, whichever way the pane reads.
    /// See <see cref="ScrollingDown"/>.
    /// </summary>
    public bool ScrollingLeft => DeltaLeft < 0;



    // How near an edge still counts as standing at it. A scroll offset is fractional at a fractional zoom
    // level, so an exact comparison would leave a pane that is visibly at its edge reporting that it is
    // not. It is the same pixel of slack the browser side gives an auto scrolling pane.
    private const double Tolerance = 1;
}
