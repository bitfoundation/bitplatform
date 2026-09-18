namespace Bit.Butil;

/// <summary>
/// One entry of the filter list handed to <see cref="Serial.RequestPort"/>. Only USB-attached
/// serial adapters can be filtered - a built-in RS-232 port has no vendor or product id to match on.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Serial/requestPort">Serial.requestPort()</see>
/// </summary>
public class SerialPortFilter
{
    /// <summary>The USB-IF vendor id of the adapter.</summary>
    public ushort? UsbVendorId { get; set; }

    /// <summary>The adapter's product id. Only meaningful together with <see cref="UsbVendorId"/>.</summary>
    public ushort? UsbProductId { get; set; }
}
