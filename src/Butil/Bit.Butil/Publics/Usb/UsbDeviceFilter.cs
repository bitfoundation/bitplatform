namespace Bit.Butil;

/// <summary>
/// One entry of the filter list handed to <see cref="Usb.RequestDevice"/>. A device matches when it
/// satisfies every property that is set; the chooser shows a device matching any filter.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/USB/requestDevice">USB.requestDevice()</see>
/// </summary>
public class UsbDeviceFilter
{
    /// <summary>The USB-IF vendor id, e.g. <c>0x2341</c> for Arduino.</summary>
    public ushort? VendorId { get; set; }

    /// <summary>The product id. Only meaningful together with <see cref="VendorId"/>.</summary>
    public ushort? ProductId { get; set; }

    /// <summary>The USB class code - <c>0x03</c> for HID, <c>0x08</c> for mass storage, and so on.</summary>
    public byte? ClassCode { get; set; }

    /// <summary>The subclass code. Only meaningful together with <see cref="ClassCode"/>.</summary>
    public byte? SubclassCode { get; set; }

    /// <summary>The protocol code. Only meaningful together with <see cref="SubclassCode"/>.</summary>
    public byte? ProtocolCode { get; set; }

    /// <summary>The device's serial number, for singling out one unit of a model.</summary>
    public string? SerialNumber { get; set; }
}
