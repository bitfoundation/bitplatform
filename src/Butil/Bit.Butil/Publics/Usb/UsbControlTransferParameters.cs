namespace Bit.Butil;

/// <summary>
/// The setup packet of a USB control transfer - the request itself, before any data stage.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/USBControlTransferParameters">USBControlTransferParameters</see>
/// </summary>
public class UsbControlTransferParameters
{
    /// <summary><c>"standard"</c>, <c>"class"</c> or <c>"vendor"</c>.</summary>
    public string RequestType { get; set; } = "vendor";

    /// <summary><c>"device"</c>, <c>"interface"</c>, <c>"endpoint"</c> or <c>"other"</c>.</summary>
    public string Recipient { get; set; } = "device";

    /// <summary>The vendor- or class-defined request code.</summary>
    public byte Request { get; set; }

    /// <summary>The request's <c>wValue</c> field; its meaning is defined by the request.</summary>
    public ushort Value { get; set; }

    /// <summary>
    /// The request's <c>wIndex</c> field. For an interface or endpoint recipient this is the
    /// interface or endpoint number the request is aimed at.
    /// </summary>
    public ushort Index { get; set; }
}
