namespace Bit.Butil;

/// <summary>
/// Snapshot of <see href="https://developer.mozilla.org/en-US/docs/Web/API/NetworkInformation">NetworkInformation</see>.
/// </summary>
public class NetworkConnectionStatus
{
    /// <summary>True when the user-agent considers itself online.</summary>
    public bool Online { get; set; }

    /// <summary>Effective connection type: <c>"slow-2g"</c>, <c>"2g"</c>, <c>"3g"</c>, <c>"4g"</c>, or null.</summary>
    public string? EffectiveType { get; set; }

    /// <summary>Underlying connection type: <c>"wifi"</c>, <c>"cellular"</c>, <c>"ethernet"</c>, etc., or null.</summary>
    public string? Type { get; set; }

    /// <summary>Estimated effective downlink in megabits per second.</summary>
    public double? Downlink { get; set; }

    /// <summary>Maximum advertised downlink in megabits per second.</summary>
    public double? DownlinkMax { get; set; }

    /// <summary>Round-trip time estimate in milliseconds.</summary>
    public int? Rtt { get; set; }

    /// <summary>True when the user has requested reduced data usage.</summary>
    public bool? SaveData { get; set; }
}
