namespace Bit.BlazorUI;

/// <summary>
/// Bounding box defined by a south-west and a north-east corner.
/// </summary>
public readonly record struct BitMapLatLngBounds(BitMapLatLng SouthWest, BitMapLatLng NorthEast);
