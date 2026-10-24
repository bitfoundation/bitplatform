using System.Drawing;
using System.Numerics;

namespace Bit.Bmage.Geometry;

/// <summary>
/// An affine transform of the image read as what it does to an object on it: how much it scales the object, how far it
/// turns it and whether it mirrors it. Used to keep overlays on the content they mark when the image is transformed.
/// </summary>
/// <param name="Scale">The average scale, the square root of the transform's area factor.</param>
/// <param name="Angle">The clockwise rotation in degrees, after a horizontal mirror when <paramref name="Mirrored"/>.</param>
/// <param name="Mirrored">True when the transform reflects the image.</param>
internal readonly record struct AffineParts(float Scale, float Angle, bool Mirrored)
{
    /// <summary>Below this a matrix entry counts as zero: the cosine of a quarter turn in single precision is about 4e-8.</summary>
    private const float Epsilon = 1e-5f;

    public static AffineParts Of(Matrix3x2 m)
    {
        var determinant = m.M11 * m.M22 - m.M12 * m.M21;
        var mirrored = determinant < 0;
        // A rotation maps the x axis to (cos, sin), the first row; a mirror applied before it negates that row.
        var angle = mirrored ? MathF.Atan2(-m.M12, -m.M11) : MathF.Atan2(m.M12, m.M11);
        return new(MathF.Sqrt(MathF.Abs(determinant)), angle * 180f / MathF.PI, mirrored);
    }

    /// <summary>
    /// The rotation an object turned by <paramref name="rotation"/> about its centre has after the transform. A mirror
    /// reverses the object's own rotation; its drawing is not mirrored, so of the two equivalent readings the one
    /// closer to upright is kept, which keeps text readable after a vertical flip as after a horizontal one.
    /// </summary>
    public float RotationOf(float rotation)
    {
        if (!Mirrored) return Normalize(rotation + Angle);
        var mirrored = Normalize(Angle - rotation);
        return Math.Abs(mirrored) > 90 ? Normalize(mirrored + 180) : mirrored;
    }

    /// <summary>An angle in degrees in (-180, 180], snapped to whole thousandths so quarter turns stay exact.</summary>
    public static float Normalize(float degrees)
    {
        var d = degrees % 360;
        if (d > 180) d -= 360;
        else if (d <= -180) d += 360;
        return MathF.Round(d, 3);
    }

    /// <summary>
    /// True when the transform maps rectangles to rectangles: no rotation other than quarter turns, no shear. Scaling
    /// may differ between the axes.
    /// </summary>
    public static bool PreservesAxes(Matrix3x2 m)
        => (MathF.Abs(m.M12) < Epsilon && MathF.Abs(m.M21) < Epsilon) || (MathF.Abs(m.M11) < Epsilon && MathF.Abs(m.M22) < Epsilon);

    /// <summary>True when the transform only scales by positive factors and moves: no turn, mirror or shear.</summary>
    public static bool IsScaleAndTranslation(Matrix3x2 m)
        => MathF.Abs(m.M12) < Epsilon && MathF.Abs(m.M21) < Epsilon && m.M11 > 0 && m.M22 > 0;

    /// <summary>The rectangle a transform that <see cref="PreservesAxes"/> maps <paramref name="rectangle"/> to.</summary>
    public static RectangleF MapRectangle(RectangleF rectangle, Matrix3x2 m)
    {
        var a = Vector2.Transform(new Vector2(rectangle.Left, rectangle.Top), m);
        var b = Vector2.Transform(new Vector2(rectangle.Right, rectangle.Bottom), m);
        return RectangleF.FromLTRB(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y), MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y));
    }
}
