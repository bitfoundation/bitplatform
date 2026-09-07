namespace Bit.Butil;

/// <summary>
/// One report a HID collection declares.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HIDDevice/collections">HIDDevice.collections</see>
/// </summary>
/// <remarks>
/// The report's items - the bit-level field layout - are summarised as a count rather than carried
/// across: acting on them means parsing the report bytes anyway, which is where a device's own
/// protocol documentation comes in.
/// </remarks>
public class HidReportInfo
{
    /// <summary>The report id, which is what <c>SendReport</c> and friends take. 0 for a device with unnumbered reports.</summary>
    public byte ReportId { get; set; }

    /// <summary>How many items the report declares.</summary>
    public int ItemCount { get; set; }
}
