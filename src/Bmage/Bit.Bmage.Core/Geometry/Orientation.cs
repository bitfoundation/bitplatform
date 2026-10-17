using System.Drawing;
using System.Numerics;

namespace Bit.Bmage.Geometry;

/// <summary>
/// The eight orientations of the dihedral group D4, numbered exactly as EXIF orientation values.
/// Each value describes how the stored pixels must be transformed to display the image upright.
/// </summary>
public enum Orientation
{
    /// <summary>Unknown / not specified (treated as <see cref="Normal"/>).</summary>
    Unspecified = 0,
    /// <summary>No transformation.</summary>
    Normal = 1,
    /// <summary>Mirrored horizontally.</summary>
    FlipHorizontal = 2,
    /// <summary>Rotated 180°.</summary>
    Rotate180 = 3,
    /// <summary>Mirrored vertically.</summary>
    FlipVertical = 4,
    /// <summary>Mirrored horizontally then rotated 270° clockwise (transpose).</summary>
    Transpose = 5,
    /// <summary>Rotated 90° clockwise.</summary>
    Rotate90 = 6,
    /// <summary>Mirrored horizontally then rotated 90° clockwise (transverse).</summary>
    Transverse = 7,
    /// <summary>Rotated 270° clockwise (90° counter-clockwise).</summary>
    Rotate270 = 8,
}

/// <summary>Algebra for <see cref="Orientation"/> values.</summary>
public static class OrientationMath
{
    // Composition table: Compose[a][b] = apply a, then b.
    // Represent each orientation as (rotation quarter-turns clockwise, flipX applied first).
    private static (int Rot, bool Flip) Decompose(Orientation o) => o switch
    {
        Orientation.FlipHorizontal => (0, true),
        Orientation.Rotate180 => (2, false),
        Orientation.FlipVertical => (2, true),
        Orientation.Transpose => (3, true),
        Orientation.Rotate90 => (1, false),
        Orientation.Transverse => (1, true),
        Orientation.Rotate270 => (3, false),
        _ => (0, false),
    };

    private static Orientation Compose(int rot, bool flip)
    {
        rot = ((rot % 4) + 4) % 4;
        return (rot, flip) switch
        {
            (0, false) => Orientation.Normal,
            (0, true) => Orientation.FlipHorizontal,
            (1, false) => Orientation.Rotate90,
            (1, true) => Orientation.Transverse,
            (2, false) => Orientation.Rotate180,
            (2, true) => Orientation.FlipVertical,
            (3, false) => Orientation.Rotate270,
            _ => Orientation.Transpose,
        };
    }

    /// <summary>Returns the orientation equivalent to applying <paramref name="first"/> and then <paramref name="second"/>.</summary>
    public static Orientation Then(this Orientation first, Orientation second)
    {
        var (r1, f1) = Decompose(first);
        var (r2, f2) = Decompose(second);
        // An element is: flip (optional) then rotate. Composition: (F^f1 R^r1) then (F^f2 R^r2).
        // R^r1 F = F R^-r1, so F^f1 R^r1 F^f2 R^r2 = F^(f1+f2) R^(±r1 + r2) where the sign flips when f2 is set.
        var rot = (f2 ? -r1 : r1) + r2;
        return Compose(rot, f1 ^ f2);
    }

    /// <summary>Returns the orientation that undoes <paramref name="orientation"/>.</summary>
    public static Orientation Inverse(this Orientation orientation)
    {
        var (r, f) = Decompose(orientation);
        // (F^f R^r)^-1 = R^-r F^f = F^f R^(f ? r : -r)
        return Compose(f ? r : -r, f);
    }

    /// <summary>True when the orientation swaps width and height.</summary>
    public static bool SwapsDimensions(this Orientation orientation)
        => orientation is Orientation.Transpose or Orientation.Rotate90 or Orientation.Transverse or Orientation.Rotate270;

    /// <summary>Clockwise rotation in degrees expressed by the orientation (ignoring the mirror component).</summary>
    public static int RotationDegrees(this Orientation orientation) => Decompose(orientation).Rot * 90;

    /// <summary>True when the orientation includes a horizontal mirror.</summary>
    public static bool IsMirrored(this Orientation orientation) => Decompose(orientation).Flip;

    /// <summary>Creates an orientation from a clockwise rotation (multiple of 90°) and an optional preceding horizontal mirror.</summary>
    public static Orientation FromRotation(int clockwiseDegrees, bool mirrorFirst = false)
    {
        if (clockwiseDegrees % 90 != 0) throw new ArgumentOutOfRangeException(nameof(clockwiseDegrees), "Rotation must be a multiple of 90 degrees.");
        return Compose(clockwiseDegrees / 90, mirrorFirst);
    }

    /// <summary>Output size after applying the orientation to an image of the given size.</summary>
    public static Size Transform(this Orientation orientation, Size size)
        => orientation.SwapsDimensions() ? new Size(size.Height, size.Width) : size;

    /// <summary>
    /// A matrix that maps source pixel coordinates to destination pixel coordinates (in continuous units) for an image of
    /// the given size when the orientation is applied.
    /// </summary>
    public static Matrix3x2 ToMatrix(this Orientation orientation, Size sourceSize)
    {
        float w = sourceSize.Width, h = sourceSize.Height;
        var (rot, flip) = Decompose(orientation);
        var m = flip ? new Matrix3x2(-1, 0, 0, 1, w, 0) : Matrix3x2.Identity;
        // After a flip the image is still w x h.
        var r = rot switch
        {
            1 => new Matrix3x2(0, 1, -1, 0, h, 0),     // 90° cw: (x,y) -> (h - y, x)
            2 => new Matrix3x2(-1, 0, 0, -1, w, h),   // 180°
            3 => new Matrix3x2(0, -1, 1, 0, 0, w),     // 270° cw: (x,y) -> (y, w - x)
            _ => Matrix3x2.Identity,
        };
        return m * r;
    }

    /// <summary>Maps a point in source coordinates to destination coordinates.</summary>
    public static PointF Transform(this Orientation orientation, PointF point, Size sourceSize)
    {
        var v = Vector2.Transform(new Vector2(point.X, point.Y), orientation.ToMatrix(sourceSize));
        return new PointF(v.X, v.Y);
    }
}
