namespace Bit.BlazorUI;

/// <summary>
/// Snapshot of the map's current viewport after a pan or zoom.
/// </summary>
public sealed class BitMapViewState
{
    /// <summary>Current center of the map.</summary>
    public required BitMapLatLng Center { get; init; }

    /// <summary>Current zoom level.</summary>
    public double Zoom { get; init; }

    /// <summary>Current visible bounds.</summary>
    public required BitMapLatLngBounds Bounds { get; init; }
}
