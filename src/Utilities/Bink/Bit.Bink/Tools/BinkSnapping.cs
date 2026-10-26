using System.Numerics;

namespace Bit.Bink;

/// <summary>How moving, resizing and drawing shapes snap to a grid and to other elements.</summary>
public sealed record BinkSnapOptions
{
    /// <summary>No snapping.</summary>
    public static readonly BinkSnapOptions None = new() { ToObjects = false, ToPage = false };

    /// <summary>Grid spacing in document units, or 0 for no grid.</summary>
    public float GridSize { get; init; }

    /// <summary>Whether edges and centres line up with those of other elements, showing a guide when they do.</summary>
    public bool ToObjects { get; init; } = true;

    /// <summary>Whether the page's edges and centre count as an element to line up with.</summary>
    public bool ToPage { get; init; } = true;

    /// <summary>How close, in view (CSS) pixels, an edge has to come to another before it snaps.</summary>
    public float Threshold { get; init; } = 6f;

    /// <summary>True when anything snaps.</summary>
    public bool IsEnabled => GridSize > 0 || ToObjects || ToPage;
}

/// <summary>An alignment guide: a line two edges or centres share while snapped, in document coordinates.</summary>
/// <param name="Vertical">True for a vertical line at <paramref name="Position"/> on the x axis.</param>
/// <param name="Position">Where the line is: an x for a vertical guide, a y for a horizontal one.</param>
/// <param name="From">Where the line starts along its own direction.</param>
/// <param name="To">Where the line ends along its own direction.</param>
public readonly record struct BinkSnapGuide(bool Vertical, float Position, float From, float To);

/// <summary>
/// Snaps positions and rectangles to a grid and to the edges and centres of other elements. Built once when a
/// gesture starts, from the elements it does not move, and asked once per frame.
/// </summary>
/// <remarks>
/// Candidate lines are sorted, so each question is a binary search however many elements there are. Lining up with
/// another element wins over the grid when both are in reach, because it is the more deliberate intent.
/// </remarks>
public sealed class BinkSnapper
{
    private readonly Line[] _xs;
    private readonly Line[] _ys;
    private readonly float _grid;

    private readonly record struct Line(float Value, float From, float To);

    private BinkSnapper(Line[] xs, Line[] ys, float grid)
    {
        _xs = xs;
        _ys = ys;
        _grid = grid;
    }

    /// <summary>A snapper that changes nothing.</summary>
    public static BinkSnapper Disabled { get; } = new([], [], 0f);

    /// <summary>
    /// Builds a snapper from a document's visible elements.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="options">What to snap to.</param>
    /// <param name="exclude">Elements that are moving and must not snap to themselves, or null.</param>
    /// <param name="area">Only elements intersecting this rectangle are candidates (the visible area); empty for all.</param>
    public static BinkSnapper Create(BinkDocument document, BinkSnapOptions options, IReadOnlySet<Guid>? exclude = null, BinkRect area = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(options);
        float grid = options.GridSize > 0 && float.IsFinite(options.GridSize) ? options.GridSize : 0f;
        if (!options.ToObjects && !options.ToPage) return grid > 0 ? new BinkSnapper([], [], grid) : Disabled;

        var xs = new List<Line>();
        var ys = new List<Line>();
        void Add(BinkRect r)
        {
            if (r.IsEmpty || !float.IsFinite(r.X) || !float.IsFinite(r.Y)) return;
            xs.Add(new Line(r.Left, r.Top, r.Bottom));
            xs.Add(new Line(r.Center.X, r.Top, r.Bottom));
            xs.Add(new Line(r.Right, r.Top, r.Bottom));
            ys.Add(new Line(r.Top, r.Left, r.Right));
            ys.Add(new Line(r.Center.Y, r.Left, r.Right));
            ys.Add(new Line(r.Bottom, r.Left, r.Right));
        }

        if (options.ToPage && document.Settings.HasFixedSize) Add(document.Settings.PageRect);
        if (options.ToObjects)
        {
            bool cull = !area.IsEmpty;
            foreach (BinkLayer layer in document.Layers)
            {
                if (!layer.IsVisible) continue;
                foreach (BinkElement element in layer.Elements)
                {
                    if (!element.IsSelectable || (exclude is not null && exclude.Contains(element.Id))) continue;
                    BinkRect bounds = element.Bounds;
                    if (cull && !bounds.Intersects(area)) continue;
                    Add(bounds);
                }
            }
        }
        xs.Sort(static (a, b) => a.Value.CompareTo(b.Value));
        ys.Sort(static (a, b) => a.Value.CompareTo(b.Value));
        return new BinkSnapper([.. xs], [.. ys], grid);
    }

    /// <summary>True when the snapper can change anything.</summary>
    public bool IsEnabled => _grid > 0 || _xs.Length > 0 || _ys.Length > 0;

    /// <summary>
    /// Snaps a rectangle that is being moved: whichever of its left edge, centre and right edge is closest to a
    /// candidate line, and the same vertically, within <paramref name="tolerance"/>. Falls back to putting its
    /// top-left corner on the grid.
    /// </summary>
    /// <param name="rect">Where the rectangle would be without snapping.</param>
    /// <param name="tolerance">Snap distance in document units (the view threshold divided by the zoom).</param>
    /// <param name="guides">Receives the guides to show, or null.</param>
    /// <returns>The offset to add to the rectangle's position.</returns>
    public Vector2 SnapRect(BinkRect rect, float tolerance, List<BinkSnapGuide>? guides = null)
    {
        if (!IsEnabled || rect.IsUnset) return Vector2.Zero;
        float dx = SnapAxis(_xs, rect.Left, rect.Center.X, rect.Right, tolerance, out Line? lineX);
        float dy = SnapAxis(_ys, rect.Top, rect.Center.Y, rect.Bottom, tolerance, out Line? lineY);
        if (lineX is null && _grid > 0) dx = MathF.Round(rect.Left / _grid) * _grid - rect.Left;
        if (lineY is null && _grid > 0) dy = MathF.Round(rect.Top / _grid) * _grid - rect.Top;
        if (guides is not null)
        {
            BinkRect moved = rect.Offset(new Vector2(dx, dy));
            if (lineX is { } gx) guides.Add(new BinkSnapGuide(true, gx.Value, MathF.Min(gx.From, moved.Top), MathF.Max(gx.To, moved.Bottom)));
            if (lineY is { } gy) guides.Add(new BinkSnapGuide(false, gy.Value, MathF.Min(gy.From, moved.Left), MathF.Max(gy.To, moved.Right)));
        }
        return new Vector2(dx, dy);
    }

    /// <summary>
    /// Snaps a point, such as a dragged corner or the end of a line, on the axes asked for.
    /// </summary>
    /// <param name="point">The point without snapping.</param>
    /// <param name="tolerance">Snap distance in document units.</param>
    /// <param name="snapX">Whether its x may change.</param>
    /// <param name="snapY">Whether its y may change.</param>
    /// <param name="guides">Receives the guides to show, or null.</param>
    public Vector2 SnapPoint(Vector2 point, float tolerance, bool snapX = true, bool snapY = true, List<BinkSnapGuide>? guides = null)
    {
        if (!IsEnabled) return point;
        Vector2 result = point;
        if (snapX)
        {
            float dx = SnapAxis(_xs, point.X, point.X, point.X, tolerance, out Line? line);
            if (line is null && _grid > 0) dx = MathF.Round(point.X / _grid) * _grid - point.X;
            result.X += dx;
            if (line is { } l) guides?.Add(new BinkSnapGuide(true, l.Value, MathF.Min(l.From, point.Y), MathF.Max(l.To, point.Y)));
        }
        if (snapY)
        {
            float dy = SnapAxis(_ys, point.Y, point.Y, point.Y, tolerance, out Line? line);
            if (line is null && _grid > 0) dy = MathF.Round(point.Y / _grid) * _grid - point.Y;
            result.Y += dy;
            if (line is { } l) guides?.Add(new BinkSnapGuide(false, l.Value, MathF.Min(l.From, point.X), MathF.Max(l.To, point.X)));
        }
        return result;
    }

    private static float SnapAxis(Line[] lines, float a, float b, float c, float tolerance, out Line? matched)
    {
        matched = null;
        if (lines.Length == 0 || !(tolerance > 0)) return 0f;
        float best = tolerance;
        float adjustment = 0f;
        foreach (float value in (ReadOnlySpan<float>)[a, b, c])
        {
            if (Nearest(lines, value) is not { } line) continue;
            float distance = MathF.Abs(line.Value - value);
            if (distance > best || (matched is not null && distance == best)) continue;
            best = distance;
            adjustment = line.Value - value;
            matched = line;
        }
        return adjustment;
    }

    private static Line? Nearest(Line[] lines, float value)
    {
        int lo = 0, hi = lines.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) >>> 1;
            if (lines[mid].Value < value) lo = mid + 1;
            else hi = mid;
        }
        Line candidate = lines[lo];
        if (lo > 0 && MathF.Abs(lines[lo - 1].Value - value) < MathF.Abs(candidate.Value - value)) candidate = lines[lo - 1];
        return candidate;
    }

    /// <summary>Draws guides as thin lines a constant screen width at any zoom.</summary>
    public static void DrawGuides(IBinkRenderContext render, IReadOnlyList<BinkSnapGuide> guides, float zoom)
    {
        ArgumentNullException.ThrowIfNull(render);
        ArgumentNullException.ThrowIfNull(guides);
        if (guides.Count == 0) return;
        var builder = new BinkPathBuilder(guides.Count * 2);
        foreach (BinkSnapGuide g in guides)
        {
            if (g.Vertical) builder.MoveTo(g.Position, g.From).LineTo(g.Position, g.To);
            else builder.MoveTo(g.From, g.Position).LineTo(g.To, g.Position);
        }
        render.StrokePath(builder.ToGeometry(), new BinkStrokePaint(new BinkColor(236, 72, 153), 1f / MathF.Max(zoom, 0.001f), 1f, BinkBlendMode.Normal, BinkLineCap.Butt, BinkLineJoin.Miter));
    }
}
