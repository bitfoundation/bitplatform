using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// An anti-aliased scanline polygon rasterizer. Each pixel row is sampled on several sub-rows; within a sub-row
/// span ends contribute exact fractional horizontal coverage, which gives clean edges on near-horizontal lines
/// without the cost of full supersampling.
/// </summary>
/// <remarks>
/// The rasterizer produces a coverage value per pixel (0..1) and hands each covered row to a callback, so the
/// caller decides how to composite. Both the non-zero and even-odd fill rules are computed exactly.
/// </remarks>
public sealed class BinkRasterizer
{
    /// <summary>Sub-rows sampled per pixel row. 16 is visually indistinguishable from exact area coverage for ink.</summary>
    public const int SubSamples = 16;

    private readonly List<Edge> _edges = new(256);
    private readonly List<int> _active = new(64);
    private readonly List<Crossing> _crossings = new(64);
    private float[] _coverage = new float[1024];

    private readonly struct Edge(float x0, float y0, float x1, float y1)
    {
        /// <summary>Top y of the edge.</summary>
        public readonly float YMin = MathF.Min(y0, y1);
        /// <summary>Bottom y of the edge.</summary>
        public readonly float YMax = MathF.Max(y0, y1);
        /// <summary>X at <see cref="YMin"/>.</summary>
        public readonly float XAtYMin = y0 <= y1 ? x0 : x1;
        /// <summary>Change in x per unit y.</summary>
        public readonly float Slope = MathF.Abs(y1 - y0) < 1e-9f ? 0f : (x1 - x0) / (y1 - y0);
        /// <summary>+1 when the edge runs downwards, -1 upwards. Used for the non-zero winding rule.</summary>
        public readonly int Direction = y1 > y0 ? 1 : -1;

        public float XAt(float y) => XAtYMin + (y - YMin) * Slope;
    }

    private readonly record struct Crossing(float X, int Direction);

    /// <summary>Adds a closed polygon. Points are in device pixels.</summary>
    public void AddPolygon(ReadOnlySpan<Vector2> points)
    {
        int n = points.Length;
        if (n < 3) return;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 a = points[j];
            Vector2 b = points[i];
            if (MathF.Abs(a.Y - b.Y) < 1e-9f) continue; // horizontal edges never cross a scanline
            if (!float.IsFinite(a.X) || !float.IsFinite(a.Y) || !float.IsFinite(b.X) || !float.IsFinite(b.Y)) continue;
            _edges.Add(new Edge(a.X, a.Y, b.X, b.Y));
        }
    }

    /// <summary>Adds several closed polygons.</summary>
    public void AddPolygons(List<Vector2[]> polygons)
    {
        ArgumentNullException.ThrowIfNull(polygons);
        foreach (Vector2[] polygon in polygons) AddPolygon(polygon);
    }

    /// <summary>Adds a flattened path as closed polygons.</summary>
    public void AddPath(BinkPathGeometry path, Matrix3x2 transform, float tolerance)
    {
        ArgumentNullException.ThrowIfNull(path);
        BinkPathGeometry transformed = path.Transform(transform);
        transformed.Flatten(tolerance, (memory, _) => AddPolygon(memory.Span));
    }

    /// <summary>Removes all geometry so the instance can be reused.</summary>
    public void Reset()
    {
        _edges.Clear();
        _active.Clear();
    }

    /// <summary>True when no geometry has been added.</summary>
    public bool IsEmpty => _edges.Count == 0;

    /// <summary>Bounding box of the added geometry in device pixels.</summary>
    public BinkRect Bounds
    {
        get
        {
            if (_edges.Count == 0) return BinkRect.Empty;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (Edge e in _edges)
            {
                float x0 = e.XAtYMin;
                float x1 = e.XAt(e.YMax);
                minX = MathF.Min(minX, MathF.Min(x0, x1));
                maxX = MathF.Max(maxX, MathF.Max(x0, x1));
                minY = MathF.Min(minY, e.YMin);
                maxY = MathF.Max(maxY, e.YMax);
            }
            return BinkRect.FromLTRB(minX, minY, maxX, maxY);
        }
    }

    /// <summary>
    /// Rasterizes the accumulated geometry, calling <paramref name="onRow"/> once per pixel row that has coverage.
    /// </summary>
    /// <param name="clip">Device-pixel clip rectangle; nothing outside it is produced.</param>
    /// <param name="rule">Fill rule.</param>
    /// <param name="onRow">Receives the row index, the first covered column and the coverage values for that span.</param>
    public void Rasterize(BinkRect clip, BinkFillRule rule, RowCallback onRow)
    {
        ArgumentNullException.ThrowIfNull(onRow);
        if (_edges.Count == 0) return;

        BinkRect bounds = Bounds.Intersect(clip);
        if (bounds.IsEmpty) return;

        int yStart = Math.Max((int)MathF.Floor(bounds.Top), (int)MathF.Floor(clip.Top));
        int yEnd = Math.Min((int)MathF.Ceiling(bounds.Bottom), (int)MathF.Ceiling(clip.Bottom));
        int xStart = Math.Max((int)MathF.Floor(bounds.Left), (int)MathF.Floor(clip.Left));
        int xEnd = Math.Min((int)MathF.Ceiling(bounds.Right), (int)MathF.Ceiling(clip.Right));
        if (yEnd <= yStart || xEnd <= xStart) return;

        int rowWidth = xEnd - xStart;
        if (_coverage.Length < rowWidth) _coverage = new float[Math.Max(rowWidth, _coverage.Length * 2)];
        Span<float> coverage = _coverage.AsSpan(0, rowWidth);

        // Edges sorted by top y let the active list be maintained with a single forward scan.
        _edges.Sort(static (a, b) => a.YMin.CompareTo(b.YMin));
        _active.Clear();
        int nextEdge = 0;
        const float subHeight = 1f / SubSamples;
        float weight = 1f / SubSamples;

        for (int y = yStart; y < yEnd; y++)
        {
            coverage.Clear();
            float rowTop = y;
            float rowBottom = y + 1f;

            while (nextEdge < _edges.Count && _edges[nextEdge].YMin < rowBottom) _active.Add(nextEdge++);
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_edges[_active[i]].YMax <= rowTop) _active.RemoveAt(i);
            }
            if (_active.Count == 0)
            {
                if (nextEdge >= _edges.Count) break;
                continue;
            }

            bool touched = false;
            for (int s = 0; s < SubSamples; s++)
            {
                float sampleY = rowTop + (s + 0.5f) * subHeight;
                _crossings.Clear();
                foreach (int index in _active)
                {
                    Edge edge = _edges[index];
                    if (sampleY < edge.YMin || sampleY >= edge.YMax) continue;
                    _crossings.Add(new Crossing(edge.XAt(sampleY), edge.Direction));
                }
                if (_crossings.Count < 2) continue;
                _crossings.Sort(static (a, b) => a.X.CompareTo(b.X));

                int winding = 0;
                for (int i = 0; i < _crossings.Count - 1; i++)
                {
                    winding += _crossings[i].Direction;
                    bool inside = rule == BinkFillRule.EvenOdd ? ((i + 1) & 1) == 1 : winding != 0;
                    if (!inside) continue;
                    float spanStart = _crossings[i].X;
                    float spanEnd = _crossings[i + 1].X;
                    if (spanEnd <= xStart || spanStart >= xEnd) continue;
                    AccumulateSpan(coverage, xStart, xEnd, spanStart, spanEnd, weight);
                    touched = true;
                }
            }

            if (!touched) continue;

            // Trim the row to the actually covered range so callers do no work on empty pixels.
            int first = 0;
            while (first < rowWidth && coverage[first] <= 0.0005f) first++;
            if (first >= rowWidth) continue;
            int last = rowWidth - 1;
            while (last > first && coverage[last] <= 0.0005f) last--;
            onRow(y, xStart + first, coverage[first..(last + 1)]);
        }
    }

    private static void AccumulateSpan(Span<float> coverage, int xStart, int xEnd, float spanStart, float spanEnd, float weight)
    {
        spanStart = MathF.Max(spanStart, xStart);
        spanEnd = MathF.Min(spanEnd, xEnd);
        if (spanEnd <= spanStart) return;

        int first = (int)MathF.Floor(spanStart);
        int last = (int)MathF.Ceiling(spanEnd) - 1;

        if (first == last)
        {
            coverage[first - xStart] += (spanEnd - spanStart) * weight;
            return;
        }
        // Partial pixel at both ends, full pixels in between.
        coverage[first - xStart] += (first + 1 - spanStart) * weight;
        for (int x = first + 1; x < last; x++) coverage[x - xStart] += weight;
        coverage[last - xStart] += (spanEnd - last) * weight;
    }

    /// <summary>Receives one rasterized row: its y, the x of the first value and the coverage values.</summary>
    /// <param name="y">Row index in device pixels.</param>
    /// <param name="x">Column of the first coverage value.</param>
    /// <param name="coverage">Coverage values 0..1, one per pixel.</param>
    public delegate void RowCallback(int y, int x, ReadOnlySpan<float> coverage);
}
