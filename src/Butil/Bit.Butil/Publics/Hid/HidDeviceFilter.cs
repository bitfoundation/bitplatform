namespace Bit.Butil;

/// <summary>
/// One entry of the filter list handed to <see cref="Hid.RequestDevice"/>. A device matches when it
/// satisfies every property that is set; the chooser shows a device matching any filter.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HID/requestDevice">HID.requestDevice()</see>
/// </summary>
public class HidDeviceFilter
{
    /// <summary>The USB-IF vendor id.</summary>
    public ushort? VendorId { get; set; }

    /// <summary>The product id. Only meaningful together with <see cref="VendorId"/>.</summary>
    public ushort? ProductId { get; set; }

    /// <summary>
    /// The HID usage page a top-level collection must declare - <c>0x01</c> for generic desktop
    /// controls, <c>0x0c</c> for consumer controls, <c>0xff00</c> and up for vendor-defined ones.
    /// </summary>
    public ushort? UsagePage { get; set; }

    /// <summary>
    /// The usage inside <see cref="UsagePage"/> - on the generic desktop page, <c>0x05</c> is a
    /// gamepad and <c>0x06</c> a keyboard.
    /// </summary>
    public ushort? Usage { get; set; }
}
