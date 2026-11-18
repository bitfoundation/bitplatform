using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>A 3×3 projective transform.</summary>
public readonly struct BmageHomography
{
    private readonly float _a, _b, _c, _d, _e, _f, _g, _h;

    private BmageHomography(float a, float b, float c, float d, float e, float f, float g, float h)
    {
        _a = a; _b = b; _c = c; _d = d; _e = e; _f = f; _g = g; _h = h;
    }

    /// <summary>Transforms a point.</summary>
    public Vector2 Transform(Vector2 p)
    {
        var w = _g * p.X + _h * p.Y + 1f;
        if (MathF.Abs(w) < 1e-9f) w = 1e-9f;
        return new Vector2((_a * p.X + _b * p.Y + _c) / w, (_d * p.X + _e * p.Y + _f) / w);
    }

    /// <summary>Computes the homography mapping four source points onto four destination points.</summary>
    public static BmageHomography Compute(IReadOnlyList<PointF> from, IReadOnlyList<PointF> to)
    {
        // Solve the 8x8 linear system using Gaussian elimination.
        var m = new double[8, 9];
        for (var i = 0; i < 4; i++)
        {
            double x = from[i].X, y = from[i].Y, u = to[i].X, v = to[i].Y;
            m[i * 2, 0] = x; m[i * 2, 1] = y; m[i * 2, 2] = 1; m[i * 2, 6] = -u * x; m[i * 2, 7] = -u * y; m[i * 2, 8] = u;
            m[i * 2 + 1, 3] = x; m[i * 2 + 1, 4] = y; m[i * 2 + 1, 5] = 1; m[i * 2 + 1, 6] = -v * x; m[i * 2 + 1, 7] = -v * y; m[i * 2 + 1, 8] = v;
        }
        for (var col = 0; col < 8; col++)
        {
            var pivot = col;
            for (var r = col + 1; r < 8; r++) if (Math.Abs(m[r, col]) > Math.Abs(m[pivot, col])) pivot = r;
            if (Math.Abs(m[pivot, col]) < 1e-12) throw new ArgumentException("The corner points are degenerate (collinear).");
            if (pivot != col) for (var k = 0; k < 9; k++) (m[col, k], m[pivot, k]) = (m[pivot, k], m[col, k]);
            for (var r = 0; r < 8; r++)
            {
                if (r == col) continue;
                var f = m[r, col] / m[col, col];
                if (f == 0) continue;
                for (var k = col; k < 9; k++) m[r, k] -= f * m[col, k];
            }
        }
        var s = new float[8];
        for (var i = 0; i < 8; i++) s[i] = (float)(m[i, 8] / m[i, i]);
        return new BmageHomography(s[0], s[1], s[2], s[3], s[4], s[5], s[6], s[7]);
    }
}
