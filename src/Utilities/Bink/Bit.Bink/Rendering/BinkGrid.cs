namespace Bit.Bink;

/// <summary>How <see cref="BinkGrid"/> is drawn.</summary>
public enum BinkGridStyle : byte
{
    /// <summary>Lines, with every few drawn stronger: graph paper.</summary>
    Lines = 0,
    /// <summary>A dot at every intersection: a whiteboard's dotted grid.</summary>
    Dots = 1,
}

/// <summary>
/// A grid drawn behind the drawing on <see cref="BinkCanvas"/>, as a guide. It is part of the view, not of the
/// document: it is never exported, saved or sent to collaborators, and it follows pan and zoom.
/// </summary>
/// <remarks>
/// When cells get too small on screen to be useful, multiples of the spacing are drawn instead, so zooming out never
/// turns the grid into a grey wash. To snap to the grid as well, set the same spacing as
/// <see cref="BinkSnapOptions.GridSize"/>.
/// </remarks>
public sealed record BinkGrid
{
    /// <summary>Light lines every 20 units, every fifth one stronger.</summary>
    public static readonly BinkGrid Default = new();

    /// <summary>Light dots every 20 units.</summary>
    public static readonly BinkGrid DottedDefault = new() { Style = BinkGridStyle.Dots, Color = new BinkColor(0, 0, 0, 64) };

    /// <summary>Lines or dots.</summary>
    public BinkGridStyle Style { get; init; } = BinkGridStyle.Lines;

    /// <summary>The distance between lines, in document units.</summary>
    public float Spacing { get; init; } = 20f;

    /// <summary>Every how many lines one is drawn in <see cref="MajorColor"/>; 0 for none. Lines only.</summary>
    public int MajorEvery { get; init; } = 5;

    /// <summary>The colour of the lines or dots. Translucent by default, so it suits any background.</summary>
    public BinkColor Color { get; init; } = new(0, 0, 0, 20);

    /// <summary>The colour of every <see cref="MajorEvery"/>-th line.</summary>
    public BinkColor MajorColor { get; init; } = new(0, 0, 0, 40);

    /// <summary>The smallest distance between lines on screen, in CSS pixels, before coarser multiples are drawn.</summary>
    public float MinScreenSpacing { get; init; } = 8f;
}

/// <summary>
/// Draws a <see cref="BinkGrid"/> and keeps the paths it built while they still cover the view, so panning within the
/// same stretch of grid redraws with the paths the browser already holds instead of sending new ones.
/// </summary>
internal sealed class BinkGridPainter
{
    // Built for this area and spacing; reused while the view stays inside the area.
    private (BinkRect Area, float Spacing, BinkGridStyle Style, int Major)? _key;
    private BinkPathGeometry _minor = BinkPathGeometry.Empty;
    private BinkPathGeometry _major = BinkPathGeometry.Empty;

    /// <summary>Draws the grid over <paramref name="visible"/>, limited to <paramref name="page"/> when the page has a size.</summary>
    public void Draw(IBinkRenderContext context, BinkGrid grid, BinkRect visible, BinkRect page, float zoom)
    {
        if (!(grid.Spacing > 0) || !float.IsFinite(grid.Spacing) || !(zoom > 0) || visible.IsEmpty) return;
        BinkRect area = page.IsEmpty ? visible : visible.Intersect(page);
        if (area.IsEmpty) return;

        // Coarser multiples while the cells are too small on screen: by the major interval, or by two without one.
        float spacing = grid.Spacing;
        int factor = grid.MajorEvery > 1 ? grid.MajorEvery : 2;
        float minimum = MathF.Max(grid.MinScreenSpacing, grid.Style == BinkGridStyle.Dots ? 12f : 4f);
        for (int i = 0; i < 12 && spacing * zoom < minimum; i++) spacing *= factor;

        // Built for whole tiles of eight cells around the view, so a small pan stays within what was built.
        float tile = spacing * 8f;
        var covered = BinkRect.FromLTRB(
            MathF.Floor(area.Left / tile) * tile, MathF.Floor(area.Top / tile) * tile,
            MathF.Ceiling(area.Right / tile) * tile, MathF.Ceiling(area.Bottom / tile) * tile);
        if (!page.IsEmpty) covered = covered.Intersect(page);
        if (covered.IsEmpty) return;

        var key = (covered, spacing, grid.Style, grid.MajorEvery);
        if (_key != key)
        {
            _key = key;
            Build(covered, spacing, grid);
        }

        float hairline = 1f / zoom;
        if (grid.Style == BinkGridStyle.Dots)
        {
            // Each row is one dashed line of zero-length dashes with round caps: a dot at every intersection for the
            // cost of one segment a row.
            float diameter = 2f / zoom;
            context.StrokePath(_minor, new BinkStrokePaint(grid.Color, diameter, 1f, BinkBlendMode.Normal, BinkLineCap.Round, BinkLineJoin.Round, [0f, spacing]));
            return;
        }
        if (!_minor.IsEmpty) context.StrokePath(_minor, new BinkStrokePaint(grid.Color, hairline, 1f, BinkBlendMode.Normal, BinkLineCap.Butt, BinkLineJoin.Miter));
        if (!_major.IsEmpty) context.StrokePath(_major, new BinkStrokePaint(grid.MajorColor, hairline, 1f, BinkBlendMode.Normal, BinkLineCap.Butt, BinkLineJoin.Miter));
    }

    private void Build(BinkRect area, float spacing, BinkGrid grid)
    {
        int columns = (int)MathF.Round(area.Width / spacing);
        int rows = (int)MathF.Round(area.Height / spacing);
        var minor = new BinkPathBuilder((columns + rows + 2) * 2);
        var major = new BinkPathBuilder(16);
        if (grid.Style == BinkGridStyle.Dots)
        {
            for (int r = 0; r <= rows; r++)
            {
                float y = area.Top + r * spacing;
                minor.MoveTo(area.Left, y).LineTo(area.Right, y);
            }
        }
        else
        {
            long firstColumn = (long)MathF.Round(area.Left / spacing);
            long firstRow = (long)MathF.Round(area.Top / spacing);
            for (int c = 0; c <= columns; c++)
            {
                float x = area.Left + c * spacing;
                BinkPathBuilder target = IsMajor(firstColumn + c, grid) ? major : minor;
                target.MoveTo(x, area.Top).LineTo(x, area.Bottom);
            }
            for (int r = 0; r <= rows; r++)
            {
                float y = area.Top + r * spacing;
                BinkPathBuilder target = IsMajor(firstRow + r, grid) ? major : minor;
                target.MoveTo(area.Left, y).LineTo(area.Right, y);
            }
        }
        _minor = minor.ToGeometry();
        _major = major.ToGeometry();
    }

    // Counted from the origin, so the same lines stay major wherever the view is; zoomed out to coarser multiples,
    // every fifth of those is stronger again, as graph paper is at every scale.
    private static bool IsMajor(long index, BinkGrid grid) => grid.MajorEvery > 1 && index % grid.MajorEvery == 0;
}
