using System.Numerics;

namespace Bit.Bmage.Operations;

/// <summary>
/// A 4×5 colour matrix (rows R,G,B,A; columns r,g,b,a,offset) applied to straight-alpha RGBA in [0,1].
/// Matches the semantics of SVG/CSS <c>feColorMatrix</c>, so browser filters and Bmage produce the same result.
/// </summary>
public readonly struct ColorMatrix : IEquatable<ColorMatrix>
{
    /// <summary>Row for the output red channel (coefficients for r,g,b,a).</summary>
    public readonly Vector4 R;
    /// <summary>Row for the output green channel.</summary>
    public readonly Vector4 G;
    /// <summary>Row for the output blue channel.</summary>
    public readonly Vector4 B;
    /// <summary>Row for the output alpha channel.</summary>
    public readonly Vector4 A;
    /// <summary>Constant offsets added to each output channel.</summary>
    public readonly Vector4 Offset;

    public ColorMatrix(Vector4 r, Vector4 g, Vector4 b, Vector4 a, Vector4 offset)
    {
        R = r; G = g; B = b; A = a; Offset = offset;
    }

    /// <summary>Creates a matrix from 20 row-major values (r,g,b,a,offset for each of R,G,B,A).</summary>
    public ColorMatrix(ReadOnlySpan<float> m)
    {
        if (m.Length != 20) throw new ArgumentException("A colour matrix needs exactly 20 values.", nameof(m));
        R = new Vector4(m[0], m[1], m[2], m[3]);
        G = new Vector4(m[5], m[6], m[7], m[8]);
        B = new Vector4(m[10], m[11], m[12], m[13]);
        A = new Vector4(m[15], m[16], m[17], m[18]);
        Offset = new Vector4(m[4], m[9], m[14], m[19]);
    }

    public static ColorMatrix Identity { get; } = new(Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ, Vector4.UnitW, Vector4.Zero);

    /// <summary>True when the matrix leaves all pixels unchanged.</summary>
    public bool IsIdentity => Equals(Identity);

    /// <summary>True when the alpha channel is affected by this matrix.</summary>
    public bool AffectsAlpha => A != Vector4.UnitW || Offset.W != 0f;

    /// <summary>
    /// True when every output channel is guaranteed to stay within [0,1] for inputs within [0,1], so no clamping can
    /// occur. Matrices with this property can be concatenated with a following matrix without changing the result.
    /// </summary>
    public bool PreservesRange
    {
        get
        {
            return RowSafe(R, Offset.X) && RowSafe(G, Offset.Y) && RowSafe(B, Offset.Z) && RowSafe(A, Offset.W);

            static bool RowSafe(Vector4 row, float offset)
            {
                // Extremes over the unit hypercube: negative coefficients pull the minimum down, positive ones push the maximum up.
                var min = offset + MathF.Min(row.X, 0) + MathF.Min(row.Y, 0) + MathF.Min(row.Z, 0) + MathF.Min(row.W, 0);
                var max = offset + MathF.Max(row.X, 0) + MathF.Max(row.Y, 0) + MathF.Max(row.Z, 0) + MathF.Max(row.W, 0);
                return min >= -1e-6f && max <= 1f + 1e-6f;
            }
        }
    }

    /// <summary>Applies the matrix to one pixel.</summary>
    public Vector4 Transform(Vector4 p) => new(
        Vector4.Dot(R, p) + Offset.X,
        Vector4.Dot(G, p) + Offset.Y,
        Vector4.Dot(B, p) + Offset.Z,
        Vector4.Dot(A, p) + Offset.W);

    /// <summary>Returns a matrix equivalent to applying <paramref name="first"/> and then <paramref name="second"/>.</summary>
    public static ColorMatrix Concat(ColorMatrix first, ColorMatrix second)
    {
        // second * first in augmented 5x5 form.
        Vector4 Row(Vector4 s) => new(
            s.X * first.R.X + s.Y * first.G.X + s.Z * first.B.X + s.W * first.A.X,
            s.X * first.R.Y + s.Y * first.G.Y + s.Z * first.B.Y + s.W * first.A.Y,
            s.X * first.R.Z + s.Y * first.G.Z + s.Z * first.B.Z + s.W * first.A.Z,
            s.X * first.R.W + s.Y * first.G.W + s.Z * first.B.W + s.W * first.A.W);
        var offset = new Vector4(
            Vector4.Dot(second.R, first.Offset) + second.Offset.X,
            Vector4.Dot(second.G, first.Offset) + second.Offset.Y,
            Vector4.Dot(second.B, first.Offset) + second.Offset.Z,
            Vector4.Dot(second.A, first.Offset) + second.Offset.W);
        return new ColorMatrix(Row(second.R), Row(second.G), Row(second.B), Row(second.A), offset);
    }

    /// <summary>Scales each RGB channel and leaves alpha untouched.</summary>
    public static ColorMatrix Scale(float r, float g, float b, float a = 1f)
        => new(new Vector4(r, 0, 0, 0), new Vector4(0, g, 0, 0), new Vector4(0, 0, b, 0), new Vector4(0, 0, 0, a), Vector4.Zero);

    /// <summary>Adds a constant to each RGB channel.</summary>
    public static ColorMatrix Translate(float r, float g, float b, float a = 0f)
        => new(Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ, Vector4.UnitW, new Vector4(r, g, b, a));

    /// <summary>CSS <c>saturate(s)</c> matrix. 0 = grayscale, 1 = unchanged, &gt;1 = boosted.</summary>
    public static ColorMatrix Saturate(float s) => new(
        new Vector4(0.213f + 0.787f * s, 0.715f - 0.715f * s, 0.072f - 0.072f * s, 0),
        new Vector4(0.213f - 0.213f * s, 0.715f + 0.285f * s, 0.072f - 0.072f * s, 0),
        new Vector4(0.213f - 0.213f * s, 0.715f - 0.715f * s, 0.072f + 0.928f * s, 0),
        Vector4.UnitW, Vector4.Zero);

    /// <summary>CSS <c>hue-rotate(deg)</c> matrix.</summary>
    public static ColorMatrix HueRotate(float degrees)
    {
        var rad = degrees * MathF.PI / 180f;
        var c = MathF.Cos(rad);
        var s = MathF.Sin(rad);
        return new ColorMatrix(
            new Vector4(0.213f + c * 0.787f - s * 0.213f, 0.715f - c * 0.715f - s * 0.715f, 0.072f - c * 0.072f + s * 0.928f, 0),
            new Vector4(0.213f - c * 0.213f + s * 0.143f, 0.715f + c * 0.285f + s * 0.140f, 0.072f - c * 0.072f - s * 0.283f, 0),
            new Vector4(0.213f - c * 0.213f - s * 0.787f, 0.715f - c * 0.715f + s * 0.715f, 0.072f + c * 0.928f + s * 0.072f, 0),
            Vector4.UnitW, Vector4.Zero);
    }

    /// <summary>CSS <c>grayscale(amount)</c> matrix (amount 0..1).</summary>
    public static ColorMatrix Grayscale(float amount) => Saturate(1f - Math.Clamp(amount, 0f, 1f));

    /// <summary>CSS <c>sepia(amount)</c> matrix (amount 0..1).</summary>
    public static ColorMatrix Sepia(float amount)
    {
        var a = Math.Clamp(amount, 0f, 1f);
        return new ColorMatrix(
            new Vector4(0.393f + 0.607f * (1 - a), 0.769f - 0.769f * (1 - a), 0.189f - 0.189f * (1 - a), 0),
            new Vector4(0.349f - 0.349f * (1 - a), 0.686f + 0.314f * (1 - a), 0.168f - 0.168f * (1 - a), 0),
            new Vector4(0.272f - 0.272f * (1 - a), 0.534f - 0.534f * (1 - a), 0.131f + 0.869f * (1 - a), 0),
            Vector4.UnitW, Vector4.Zero);
    }

    /// <summary>CSS <c>invert(amount)</c> matrix (amount 0..1).</summary>
    public static ColorMatrix Invert(float amount)
    {
        var a = Math.Clamp(amount, 0f, 1f);
        var s = 1f - 2f * a;
        return new ColorMatrix(new Vector4(s, 0, 0, 0), new Vector4(0, s, 0, 0), new Vector4(0, 0, s, 0), Vector4.UnitW, new Vector4(a, a, a, 0));
    }

    /// <summary>CSS <c>contrast(c)</c> matrix: scales around mid grey.</summary>
    public static ColorMatrix Contrast(float c)
    {
        var o = 0.5f - 0.5f * c;
        return new ColorMatrix(new Vector4(c, 0, 0, 0), new Vector4(0, c, 0, 0), new Vector4(0, 0, c, 0), Vector4.UnitW, new Vector4(o, o, o, 0));
    }

    public bool Equals(ColorMatrix other) => R == other.R && G == other.G && B == other.B && A == other.A && Offset == other.Offset;
    public override bool Equals(object? obj) => obj is ColorMatrix m && Equals(m);
    public override int GetHashCode() => HashCode.Combine(R, G, B, A, Offset);
    public static bool operator ==(ColorMatrix left, ColorMatrix right) => left.Equals(right);
    public static bool operator !=(ColorMatrix left, ColorMatrix right) => !left.Equals(right);

    /// <summary>Returns the 20 matrix values in row-major order (r,g,b,a,offset per output row).</summary>
    public float[] ToArray() =>
    [
        R.X, R.Y, R.Z, R.W, Offset.X,
        G.X, G.Y, G.Z, G.W, Offset.Y,
        B.X, B.Y, B.Z, B.W, Offset.Z,
        A.X, A.Y, A.Z, A.W, Offset.W,
    ];
}

/// <summary>Applies a <see cref="ColorMatrix"/> to every pixel. Consecutive matrix operations fuse into one.</summary>
public class ColorMatrixOperation : PointOperation
{
    public ColorMatrixOperation(ColorMatrix matrix, string? name = null)
    {
        Matrix = matrix;
        _name = name ?? "Color matrix";
    }

    private readonly string _name;

    /// <summary>The matrix applied by this operation.</summary>
    public ColorMatrix Matrix { get; }

    public override string Name => _name;

    public override bool IsIdentity => Matrix.IsIdentity;

    public override void ProcessRow(Span<Vector4> pixels)
    {
        var m = Matrix;
        if (!m.AffectsAlpha)
        {
            var r = m.R; var g = m.G; var b = m.B; var o = m.Offset;
            for (var i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                pixels[i] = new Vector4(Vector4.Dot(r, p) + o.X, Vector4.Dot(g, p) + o.Y, Vector4.Dot(b, p) + o.Z, p.W);
            }
        }
        else
        {
            for (var i = 0; i < pixels.Length; i++) pixels[i] = m.Transform(pixels[i]);
        }
    }

    /// <summary>
    /// Concatenates with a following matrix. Only done when this matrix cannot produce out-of-range values, because a
    /// separate stage would clamp its result to [0,1] before the next one runs (matching CSS filter chain semantics).
    /// </summary>
    public override PointOperation? TryFuse(PointOperation next)
        => next is ColorMatrixOperation cm && Matrix.PreservesRange
            ? new ColorMatrixOperation(ColorMatrix.Concat(Matrix, cm.Matrix), Name == cm.Name ? Name : "Color matrix")
            : null;
}
