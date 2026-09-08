namespace Bit.Butil;

/// <summary>
/// A serial port the user has granted this origin.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SerialPort/getInfo">SerialPort.getInfo()</see>
/// </summary>
public class SerialPortInfo
{
    /// <summary>
    /// The handle the JavaScript side files this port under. Passed back on every operation -
    /// the <c>SerialPort</c> object itself is the permission grant and never leaves the browser.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The USB vendor id of the adapter behind the port, or null for a built-in port.</summary>
    public ushort? UsbVendorId { get; set; }

    /// <summary>The USB product id of the adapter behind the port, or null for a built-in port.</summary>
    public ushort? UsbProductId { get; set; }

    /// <summary>True while the port is open.</summary>
    public bool Open { get; set; }
}
