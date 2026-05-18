namespace Bit.BlazorUI;

/// <summary>
/// Azure Maps Web SDK v3 provider for <see cref="BitMap{TMapProvider}"/>.
/// Requires an Azure Maps subscription key.
/// </summary>
public sealed class BitAzureMapsMapProvider : BitMapProviderBase
{
    /// <summary>Azure Maps subscription key (required).</summary>
    public string SubscriptionKey { get; set; } = "";

    /// <summary>Map style. Common values: <c>road</c>, <c>satellite</c>, <c>satellite_road_labels</c>, <c>night</c>, <c>grayscale_dark</c>.</summary>
    public string Style { get; set; } = "road";

    /// <summary>Show a scale bar control.</summary>
    public bool ShowScaleControl { get; set; }

    /// <inheritdoc />
    public override string Key => "azuremaps";

    /// <inheritdoc />
    public override string JsObjectName => "BitMapAzureMaps";

    /// <inheritdoc />
    public override IReadOnlyList<string> Scripts => ["https://atlas.microsoft.com/sdk/javascript/mapcontrol/3/atlas.min.js"];

    /// <inheritdoc />
    public override IReadOnlyList<string> Stylesheets => ["https://atlas.microsoft.com/sdk/javascript/mapcontrol/3/atlas.min.css"];

    /// <inheritdoc />
    public override object BuildOptionsPayload()
    {
        if (string.IsNullOrWhiteSpace(SubscriptionKey))
        {
            throw new InvalidOperationException(
                "BitAzureMapsMapProvider: A SubscriptionKey is required. " +
                "Obtain one from the Azure portal (Azure Maps resource → Authentication).");
        }

        var common = GetCommonOptions();
        common["subscriptionKey"] = SubscriptionKey;
        common["style"] = Style;
        common["showScaleControl"] = ShowScaleControl;
        return common;
    }
}
