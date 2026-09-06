namespace Bit.Butil;

/// <summary>
/// One input report pushed by a HID device.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HIDDevice/inputreport_event">HIDDevice inputreport event</see>
/// </summary>
public class HidInputReport
{
    /// <summary>The report id, or 0 on a device whose reports are unnumbered.</summary>
    public byte ReportId { get; set; }

    /// <summary>
    /// The report's payload, without the leading report id byte. What the bits mean is defined by
    /// the device's own report descriptor.
    /// </summary>
    public byte[] Data { get; set; } = [];
}
