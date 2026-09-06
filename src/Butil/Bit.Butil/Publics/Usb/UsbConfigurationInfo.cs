namespace Bit.Butil;

/// <summary>
/// One configuration of a USB device. A device exposes one configuration at a time, selected with
/// <see cref="UsbDevice.SelectConfiguration"/>.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/USBConfiguration">USBConfiguration</see>
/// </summary>
public class UsbConfigurationInfo
{
    /// <summary>The configuration's value, which is what <see cref="UsbDevice.SelectConfiguration"/> takes.</summary>
    public byte ConfigurationValue { get; set; }

    /// <summary>The device's own name for the configuration, when it provides one.</summary>
    public string? ConfigurationName { get; set; }

    /// <summary>The interfaces this configuration exposes.</summary>
    public UsbInterfaceInfo[] Interfaces { get; set; } = [];
}
