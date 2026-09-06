using System;

namespace Bit.Butil;

/// <summary>
/// One entry of <see cref="BluetoothRequestOptions.Filters"/>. A device matches the filter when it
/// satisfies every property that is set, and the chooser shows a device that matches any filter.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Bluetooth/requestDevice#filters">Bluetooth.requestDevice() filters</see>
/// </summary>
public class BluetoothFilter
{
    /// <summary>
    /// GATT services the device must advertise. Each one may be a full UUID
    /// (<c>"0000180d-0000-1000-8000-00805f9b34fb"</c>), a 16-bit alias (<c>"0x180d"</c>) or a
    /// registered name (<c>"heart_rate"</c>).
    /// </summary>
    public string[]? Services { get; set; }

    /// <summary>The device's exact advertised name.</summary>
    public string? Name { get; set; }

    /// <summary>A prefix of the device's advertised name - what "show me every Polar H10" looks like.</summary>
    public string? NamePrefix { get; set; }
}
