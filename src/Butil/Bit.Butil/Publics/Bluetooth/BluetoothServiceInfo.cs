namespace Bit.Butil;

/// <summary>
/// A primary GATT service exposed by a connected device.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/BluetoothRemoteGATTService">BluetoothRemoteGATTService</see>
/// </summary>
public class BluetoothServiceInfo
{
    /// <summary>The service's full 128-bit UUID, which is what every later call should pass.</summary>
    public string Uuid { get; set; } = string.Empty;

    /// <summary>True for a primary service - the only kind <c>getPrimaryServices</c> returns.</summary>
    public bool IsPrimary { get; set; }
}
