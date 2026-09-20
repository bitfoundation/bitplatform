namespace Bit.Butil;

/// <summary>
/// A top-level HID collection - one logical device inside the physical one, which is how a keyboard
/// with media keys presents as two.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HIDDevice/collections">HIDDevice.collections</see>
/// </summary>
public class HidCollectionInfo
{
    /// <summary>The HID usage page this collection belongs to.</summary>
    public ushort UsagePage { get; set; }

    /// <summary>The usage inside <see cref="UsagePage"/> that names what the collection is.</summary>
    public ushort Usage { get; set; }

    /// <summary>Reports the device sends unprompted - button presses, axis movement.</summary>
    public HidReportInfo[] InputReports { get; set; } = [];

    /// <summary>Reports the page sends to the device - LEDs, rumble.</summary>
    public HidReportInfo[] OutputReports { get; set; } = [];

    /// <summary>Reports read and written on demand, used for configuration rather than for events.</summary>
    public HidReportInfo[] FeatureReports { get; set; } = [];
}
