namespace Bit.Butil;

/// <summary>
/// A USB device the user has granted this origin, with its descriptor tree.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/USBDevice">USBDevice</see>
/// </summary>
public class UsbDeviceInfo
{
    /// <summary>
    /// The handle the JavaScript side files this device under. Passed back on every operation -
    /// the <c>USBDevice</c> object itself is the permission grant and never leaves the browser.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The USB-IF vendor id.</summary>
    public ushort VendorId { get; set; }

    /// <summary>The product id.</summary>
    public ushort ProductId { get; set; }

    /// <summary>The device-level class code. Usually 0, with the real class declared per interface.</summary>
    public byte DeviceClass { get; set; }

    /// <summary>The device-level subclass code.</summary>
    public byte DeviceSubclass { get; set; }

    /// <summary>The device-level protocol code.</summary>
    public byte DeviceProtocol { get; set; }

    /// <summary>The manufacturer string, when the device reports one.</summary>
    public string? ManufacturerName { get; set; }

    /// <summary>The product string, when the device reports one.</summary>
    public string? ProductName { get; set; }

    /// <summary>The serial number, when the device reports one.</summary>
    public string? SerialNumber { get; set; }

    /// <summary>True while the page has the device open.</summary>
    public bool Opened { get; set; }

    /// <summary>The currently selected configuration, or null when none has been selected yet.</summary>
    public byte? ConfigurationValue { get; set; }

    /// <summary>Every configuration the device declares.</summary>
    public UsbConfigurationInfo[] Configurations { get; set; } = [];
}
