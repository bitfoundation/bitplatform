namespace Bit.Butil;

/// <summary>
/// One interface of a USB configuration. An interface has to be claimed before any transfer on its
/// endpoints is allowed.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/USBInterface">USBInterface</see>
/// </summary>
public class UsbInterfaceInfo
{
    /// <summary>The interface number, which is what <see cref="UsbDevice.ClaimInterface"/> takes.</summary>
    public byte InterfaceNumber { get; set; }

    /// <summary>True when this page has already claimed the interface.</summary>
    public bool Claimed { get; set; }

    /// <summary>The interface's alternate settings; the first is the default one.</summary>
    public UsbAlternateInterfaceInfo[] Alternates { get; set; } = [];
}
