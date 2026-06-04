namespace Bit.Butil;

/// <summary>
/// Snapshot of <see href="https://developer.mozilla.org/en-US/docs/Web/API/BatteryManager">BatteryManager</see>.
/// </summary>
public class BatteryStatus
{
    /// <summary>True if the device is currently charging.</summary>
    public bool Charging { get; set; }

    /// <summary>Seconds remaining until fully charged, or <see langword="null"/> when unknown.</summary>
    public double? ChargingTime { get; set; }

    /// <summary>Seconds remaining until discharged, or <see langword="null"/> when unknown.</summary>
    public double? DischargingTime { get; set; }

    /// <summary>Battery level, in [0, 1].</summary>
    public double Level { get; set; }
}
