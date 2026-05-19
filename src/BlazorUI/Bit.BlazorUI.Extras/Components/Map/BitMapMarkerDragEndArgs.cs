namespace Bit.BlazorUI;

/// <summary>Payload for the <see cref="BitMap{TMapProvider}.OnMarkerDragEnd"/> callback.</summary>
public sealed class BitMapMarkerDragEndArgs
{
    /// <summary>Identifier of the dragged marker.</summary>
    public required string Id { get; init; }

    /// <summary>Final position of the marker after the drag.</summary>
    public required BitMapLatLng Position { get; init; }
}

/// <summary>Payload for the <see cref="BitMap{TMapProvider}.OnVectorClick"/> callback.</summary>
public sealed class BitMapVectorClickArgs
{
    /// <summary>Identifier of the vector layer that was clicked.</summary>
    public required string LayerId { get; init; }

    /// <summary>Kind of vector geometry, one of <c>polyline</c>, <c>polygon</c>, <c>circle</c>, <c>rectangle</c>.</summary>
    public required string Kind { get; init; }

    /// <summary>Geographic location of the click.</summary>
    public required BitMapLatLng Position { get; init; }
}

/// <summary>Payload for the <see cref="BitMap{TMapProvider}.OnGeoJsonFeatureClick"/> callback.</summary>
public sealed class BitMapGeoJsonFeatureClickArgs
{
    /// <summary>Identifier of the GeoJSON layer.</summary>
    public required string LayerId { get; init; }

    /// <summary>Properties of the clicked feature, serialized as JSON.</summary>
    public required System.Text.Json.JsonElement Properties { get; init; }
}
