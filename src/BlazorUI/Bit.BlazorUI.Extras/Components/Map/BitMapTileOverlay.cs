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

    /// <summary>
    /// Validates this overlay's required values and throws when any of them are
    /// missing or malformed. Call this before sending the overlay to the JS layer
    /// so configuration mistakes surface early at the call site.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="Id"/> or <see cref="UrlTemplate"/> is null/empty/whitespace,
    /// or when <see cref="UrlTemplate"/> is missing one of the required <c>{z}</c>,
    /// <c>{x}</c>, or <c>{y}</c> placeholders.
    /// </exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new ArgumentException(
                $"{nameof(BitMapTileOverlay)}.{nameof(Id)} must be a non-empty, non-whitespace value.",
                nameof(Id));
        }

        if (string.IsNullOrWhiteSpace(UrlTemplate))
        {
            throw new ArgumentException(
                $"{nameof(BitMapTileOverlay)}.{nameof(UrlTemplate)} must be a non-empty, non-whitespace value.",
                nameof(UrlTemplate));
        }

        if (UrlTemplate.Contains("{z}", StringComparison.Ordinal) is false ||
            UrlTemplate.Contains("{x}", StringComparison.Ordinal) is false ||
            UrlTemplate.Contains("{y}", StringComparison.Ordinal) is false)
        {
            throw new ArgumentException(
                $"{nameof(BitMapTileOverlay)}.{nameof(UrlTemplate)} must contain the '{{z}}', '{{x}}', and '{{y}}' placeholders. The optional '{{s}}' placeholder is also supported.",
                nameof(UrlTemplate));
        }
    }
}
