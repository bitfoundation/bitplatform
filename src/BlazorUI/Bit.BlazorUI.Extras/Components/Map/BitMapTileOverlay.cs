namespace Bit.BlazorUI;

/// <summary>
/// Optional XYZ tile layer rendered above the base map (weather, labels, etc.).
/// </summary>
public sealed class BitMapTileOverlay
{
    /// <summary>Unique identifier of the overlay within the map.</summary>
    public required string Id { get; init; }

    /// <summary>Tile URL template with <c>{z}</c>, <c>{x}</c>, <c>{y}</c>, and optional <c>{s}</c> placeholders.</summary>
    public required string UrlTemplate { get; init; }

    /// <summary>Optional attribution string shown in the map's attribution control.</summary>
    public string? Attribution { get; init; }

    /// <summary>Layer opacity (0–1).</summary>
    public double Opacity
    {
        get => _opacity;
        init => _opacity = Math.Clamp(value, 0, 1);
    }
    private double _opacity = 1;

    /// <summary>Stack order index of the overlay.</summary>
    public int ZIndex { get; init; } = 100;

    /// <summary>Maximum zoom level the tiles are available at.</summary>
    public int MaxZoom { get; init; } = 19;
}
