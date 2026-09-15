namespace Bit.BlazorUI;

/// <summary>
/// A position in screen space - CSS pixels measured from the top-left corner of the map's
/// container - as returned by <see cref="BitMap{TMapProvider}.Project"/>.
/// <para>
/// Deliberately not a geographic type: a pixel position is only meaningful for the viewport it
/// was read at, and stops being true the moment the map moves.
/// </para>
/// </summary>
/// <param name="X">Distance from the container's left edge, in CSS pixels.</param>
/// <param name="Y">Distance from the container's top edge, in CSS pixels.</param>
public readonly record struct BitMapPoint(double X, double Y);
