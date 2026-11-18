using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// A vector path made of straight and curved segments. Curves are flattened to line segments at construction time using
/// an adaptive tolerance, so the rasteriser only ever sees polygons.
/// </summary>
public sealed class BmageVectorPath
{
    private readonly List<Vector2[]> _contours;

    internal BmageVectorPath(List<Vector2[]> contours, bool[] closed)
    {
        _contours = contours;
        Closed = closed;
    }

    /// <summary>The flattened contours; each is a polyline of at least two points.</summary>
    public IReadOnlyList<Vector2[]> Contours => _contours;

    /// <summary>Whether each contour is closed.</summary>
    public IReadOnlyList<bool> Closed { get; }

    /// <summary>True when the path has nothing to draw.</summary>
    public bool IsEmpty => _contours.Count == 0;

    /// <summary>The axis-aligned bounding box of all points, or an empty rectangle for an empty path.</summary>
    public System.Drawing.RectangleF Bounds
    {
        get
        {
            if (_contours.Count == 0) return System.Drawing.RectangleF.Empty;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var c in _contours)
                foreach (var p in c)
                {
                    if (p.X < minX) minX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y > maxY) maxY = p.Y;
                }
            return System.Drawing.RectangleF.FromLTRB(minX, minY, maxX, maxY);
        }
    }

    /// <summary>Returns a copy of the path with every point transformed.</summary>
    public BmageVectorPath Transform(Matrix3x2 matrix)
    {
        var contours = new List<Vector2[]>(_contours.Count);
        foreach (var c in _contours)
        {
            var t = new Vector2[c.Length];
            for (var i = 0; i < c.Length; i++) t[i] = Vector2.Transform(c[i], matrix);
            contours.Add(t);
        }
        return new BmageVectorPath(contours, (bool[])((bool[])Closed).Clone());
    }
}
