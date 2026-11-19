namespace Bit.Blow;

/// <summary>
/// The result of computing an edge path: the SVG path data plus the
/// label anchor point (path midpoint).
/// </summary>
public readonly record struct BlowPathResult(string Path, double LabelX, double LabelY);
