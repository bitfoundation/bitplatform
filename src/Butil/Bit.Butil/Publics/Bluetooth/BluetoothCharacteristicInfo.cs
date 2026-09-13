namespace Bit.Butil;

/// <summary>
/// A characteristic of a GATT service, with the operations the device says it supports.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/BluetoothRemoteGATTCharacteristic">BluetoothRemoteGATTCharacteristic</see>
/// </summary>
/// <remarks>
/// The flags are the device's own declaration. Calling an operation a characteristic does not
/// declare fails with <c>NotSupportedError</c>, so they are worth checking before a read or a write.
/// </remarks>
public class BluetoothCharacteristicInfo
{
    /// <summary>The characteristic's full 128-bit UUID.</summary>
    public string Uuid { get; set; } = string.Empty;

    /// <summary>The UUID of the service this characteristic belongs to.</summary>
    public string ServiceUuid { get; set; } = string.Empty;

    /// <summary>The value may be broadcast in the device's advertising data.</summary>
    public bool Broadcast { get; set; }

    /// <summary>The value can be read.</summary>
    public bool Read { get; set; }

    /// <summary>The value can be written without waiting for the device to acknowledge it.</summary>
    public bool WriteWithoutResponse { get; set; }

    /// <summary>The value can be written and acknowledged.</summary>
    public bool Write { get; set; }

    /// <summary>The device can push value changes - what <c>SubscribeValueChanged</c> needs.</summary>
    public bool Notify { get; set; }

    /// <summary>Like <see cref="Notify"/>, but each push is acknowledged by the browser.</summary>
    public bool Indicate { get; set; }
}
