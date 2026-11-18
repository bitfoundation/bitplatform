using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// The eight orientations of the dihedral group D4, numbered exactly as EXIF orientation values.
/// Each value describes how the stored pixels must be transformed to display the image upright.
/// </summary>
public enum BmageOrientation
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
