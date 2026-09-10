namespace Bit.BlazorUI;

/// <summary>
/// Result of <see cref="BitMap{TMapProvider}.Locate(BitMapGeolocationOptions?)"/>.
/// </summary>
public sealed class BitMapGeolocationResult
{
    /// <summary>The reported position.</summary>
    public required BitMapLatLng Position { get; init; }

    /// <summary>Radius of uncertainty in meters, as reported by the browser.</summary>
    public double AccuracyMeters { get; init; }
}

/// <summary>
/// Options forwarded to the browser's <c>navigator.geolocation.getCurrentPosition</c>.
/// </summary>
public sealed class BitMapGeolocationOptions
{
    /// <summary>Ask the device for its most accurate fix (slower, and costs more battery on mobile).</summary>
    public bool EnableHighAccuracy { get; set; }

    /// <summary>How long to wait for a fix before failing. Defaults to 10 seconds.</summary>
    public int TimeoutMilliseconds { get; set; } = 10_000;

    /// <summary>How old a cached fix may be before a fresh one is requested. Defaults to 0 (always fresh).</summary>
    public int MaximumAgeMilliseconds { get; set; }

    /// <summary>When true, the map pans to the located position once the fix arrives.</summary>
    public bool SetView { get; set; } = true;

    /// <summary>Zoom level to apply when <see cref="SetView"/> is true. Null keeps the current zoom.</summary>
    public double? Zoom { get; set; } = 15;
}
