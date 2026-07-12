// Affine-transform helpers for PDF coordinate spaces.

using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// A 2-D affine transform stored as the six PDF coefficients [a b c d e f],
/// mapping (x, y) to (a·x + c·y + e, b·x + d·y + f).
/// </summary>
public readonly struct BitPdfMatrix
{
    public double A { get; }
    public double B { get; }
    public double C { get; }
    public double D { get; }
    public double E { get; }
    public double F { get; }

    public BitPdfMatrix(double a, double b, double c, double d, double e, double f)
    {
        A = a; B = b; C = c; D = d; E = e; F = f;
    }

    /// <summary>The identity transform.</summary>
    public static readonly BitPdfMatrix Identity = new(1, 0, 0, 1, 0, 0);

    /// <summary>
    /// Concatenates two transforms so that the result applies <paramref name="inner"/>
    /// first and <paramref name="outer"/> second.
    /// </summary>
    public static BitPdfMatrix Concat(BitPdfMatrix outer, BitPdfMatrix inner) => new(
        outer.A * inner.A + outer.C * inner.B,
        outer.B * inner.A + outer.D * inner.B,
        outer.A * inner.C + outer.C * inner.D,
        outer.B * inner.C + outer.D * inner.D,
        outer.A * inner.E + outer.C * inner.F + outer.E,
        outer.B * inner.E + outer.D * inner.F + outer.F);

    /// <summary>Transforms the point (x, y).</summary>
    public (double X, double Y) Apply(double x, double y)
        => (A * x + C * y + E, B * x + D * y + F);

    /// <summary>Transforms the direction (dx, dy), ignoring translation.</summary>
    public (double X, double Y) ApplyDirection(double dx, double dy)
        => (A * dx + C * dy, B * dx + D * dy);

    /// <summary>
    /// Returns the inverse transform, or <c>null</c> when this matrix is
    /// singular (non-invertible).
    /// </summary>
    public BitPdfMatrix? Invert()
    {
        double det = A * D - B * C;
        if (Math.Abs(det) < 1e-12)
        {
            return null;
        }
        double inv = 1.0 / det;
        double a = D * inv;
        double b = -B * inv;
        double c = -C * inv;
        double d = A * inv;
        double e = -(a * E + c * F);
        double f = -(b * E + d * F);
        return new BitPdfMatrix(a, b, c, d, e, f);
    }

    /// <summary>An approximate uniform scale factor (used for line widths and font sizes).</summary>
    public double ScaleFactor
    {
        get
        {
            double sx = Math.Sqrt(A * A + B * B);
            double sy = Math.Sqrt(C * C + D * D);
            return (sx + sy) / 2.0;
        }
    }

    /// <summary>Renders the transform as an SVG <c>matrix(...)</c> string.</summary>
    public string ToSvg()
    {
        return string.Create(CultureInfo.InvariantCulture,
            $"matrix({A:0.####},{B:0.####},{C:0.####},{D:0.####},{E:0.####},{F:0.####})");
    }

    public override string ToString() => ToSvg();
}
