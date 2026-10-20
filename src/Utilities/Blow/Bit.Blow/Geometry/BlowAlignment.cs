namespace Bit.Blow;

/// <summary>
/// Works out where a dragged node wants to line up with the nodes around it - the "smart guides"
/// every professional diagram editor has, where a node being dragged clicks into alignment with
/// its neighbours and a thin line appears to show why.
/// </summary>
public static class BlowAlignment
{
    /// <summary>The result of testing a dragged rectangle against its neighbours.</summary>
    /// <param name="OffsetX">How far to nudge the rectangle on X to sit on the guide, in flow units.</param>
    /// <param name="OffsetY">How far to nudge it on Y.</param>
    /// <param name="Guides">The lines to draw, already spanning both rectangles.</param>
    public readonly record struct Result(double OffsetX, double OffsetY, IReadOnlyList<BlowAlignmentGuide> Guides);

    /// <summary>Nothing lined up: no nudge, no lines.</summary>
    public static readonly Result None = new(0, 0, []);

    /// <summary>
    /// Finds the closest alignment between <paramref name="moving"/> and any of
    /// <paramref name="candidates"/>, within <paramref name="tolerance"/> flow units.
    /// </summary>
    /// <remarks>
    /// Both axes are considered independently, so a node can snap horizontally to one neighbour
    /// while snapping vertically to another - which is what makes the feature feel like it is
    /// reading your intent rather than fighting you. Each axis offers three lines (leading edge,
    /// centre, trailing edge), and the nearest match within tolerance wins; ties go to the
    /// earlier candidate, so the result does not flicker between equally good neighbours.
    /// </remarks>
    public static Result Snap(BlowRect moving, IEnumerable<BlowRect> candidates, double tolerance)
    {
        if (tolerance <= 0) return None;

        var bestX = double.MaxValue;
        var bestY = double.MaxValue;
        double offsetX = 0, offsetY = 0;
        BlowRect? partnerX = null, partnerY = null;
        double lineX = 0, lineY = 0;

        Span<double> movingX = [moving.Left, moving.Left + moving.Width / 2, moving.Right];
        Span<double> movingY = [moving.Top, moving.Top + moving.Height / 2, moving.Bottom];

        foreach (var other in candidates)
        {
            Span<double> otherX = [other.Left, other.Left + other.Width / 2, other.Right];
            Span<double> otherY = [other.Top, other.Top + other.Height / 2, other.Bottom];

            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    var dx = otherX[j] - movingX[i];
                    if (Math.Abs(dx) <= tolerance && Math.Abs(dx) < bestX)
                    {
                        bestX = Math.Abs(dx);
                        offsetX = dx;
                        lineX = otherX[j];
                        partnerX = other;
                    }

                    var dy = otherY[j] - movingY[i];
                    if (Math.Abs(dy) <= tolerance && Math.Abs(dy) < bestY)
                    {
                        bestY = Math.Abs(dy);
                        offsetY = dy;
                        lineY = otherY[j];
                        partnerY = other;
                    }
                }
            }
        }

        if (partnerX is null && partnerY is null) return None;

        // Draw each line across the span of both rectangles once the nudge has been applied, so
        // it visibly connects the node being dragged to the one it lined up with.
        var snapped = new BlowRect(moving.X + offsetX, moving.Y + offsetY, moving.Width, moving.Height);

        var guides = new List<BlowAlignmentGuide>(2);
        if (partnerX is { } px)
        {
            guides.Add(new BlowAlignmentGuide(
                BlowGuideOrientation.Vertical, lineX,
                Math.Min(snapped.Top, px.Top), Math.Max(snapped.Bottom, px.Bottom)));
        }
        if (partnerY is { } py)
        {
            guides.Add(new BlowAlignmentGuide(
                BlowGuideOrientation.Horizontal, lineY,
                Math.Min(snapped.Left, py.Left), Math.Max(snapped.Right, py.Right)));
        }

        return new Result(offsetX, offsetY, guides);
    }
}
