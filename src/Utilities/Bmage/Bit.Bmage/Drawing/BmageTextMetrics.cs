namespace Bit.Bmage;

/// <summary>Measured text extents in pixels.</summary>
public readonly record struct BmageTextMetrics(float Width, float Height, float Ascent, float Descent, int LineCount);
