namespace Bit.Butil;

/// <summary>
/// A Bluetooth device the user has granted this origin.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/BluetoothDevice">BluetoothDevice</see>
/// </summary>
public class BluetoothDeviceInfo
{
    /// <summary>
    /// The handle the JavaScript side files this device under. Passed back on every operation -
    /// the <c>BluetoothDevice</c> object itself is the permission grant and never leaves the browser.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The device's own id: an opaque string, stable for this origin across sessions.</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>The advertised name, or null for a device that advertises none.</summary>
    public string? Name { get; set; }

    /// <summary>True while the GATT server is connected.</summary>
    public bool Connected { get; set; }
}
