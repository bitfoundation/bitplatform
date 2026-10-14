namespace Bit.Belemetry;

/// <summary>
/// The OpenTelemetry resource: the attributes that describe the entity producing telemetry
/// (<c>service.name</c>, <c>service.version</c>, <c>telemetry.sdk.*</c>, host/browser details, ...).
/// </summary>
public sealed class BelemetryResource
{
    /// <summary>Creates a resource with the given attributes.</summary>
    public BelemetryResource(BelemetryAttributes? attributes = null)
    {
        Attributes = attributes ?? new BelemetryAttributes();
    }

    /// <summary>The resource attributes.</summary>
    public BelemetryAttributes Attributes { get; }

    /// <summary>The <c>service.name</c> attribute.</summary>
    public string ServiceName => Attributes.GetString(BelemetrySemanticConventions.ServiceName) ?? "unknown_service";

    /// <summary>Creates a new resource holding this resource's attributes overlaid with <paramref name="other"/>'s.</summary>
    public BelemetryResource Merge(BelemetryAttributes? other)
    {
        var merged = Attributes.Clone();
        if (other is not null)
        {
            foreach (var (key, value) in other) merged.Set(key, value);
        }
        return new BelemetryResource(merged);
    }
}
