namespace Bit.Bmage;

/// <summary>One point of a glyph contour. Off-curve points are quadratic Bézier control points.</summary>
public readonly record struct BmageGlyphPoint(float X, float Y, bool OnCurve);
