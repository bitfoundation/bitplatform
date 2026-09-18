namespace Bit.Butil;

/// <summary>
/// A HID device the user has granted this origin.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/HIDDevice">HIDDevice</see>
/// </summary>
public class HidDeviceInfo
{
    /// <summary>
    /// The handle the JavaScript side files this device under. Passed back on every operation -
    /// the <c>HIDDevice</c> object itself is the permission grant and never leaves the browser.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The USB-IF vendor id.</summary>
    public ushort VendorId { get; set; }

    /// <summary>The product id.</summary>
    public ushort ProductId { get; set; }

    /// <summary>The product string, when the device reports one.</summary>
    public string? ProductName { get; set; }

    /// <summary>True while the page has the device open.</summary>
    public bool Opened { get; set; }

    /// <summary>The device's top-level collections and the reports each of them declares.</summary>
    public HidCollectionInfo[] Collections { get; set; } = [];
}
