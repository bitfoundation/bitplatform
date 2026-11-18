namespace Bit.Bink;

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
