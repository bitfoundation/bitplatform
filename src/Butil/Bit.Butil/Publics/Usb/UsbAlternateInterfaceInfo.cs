namespace Bit.Butil;

/// <summary>
/// One alternate setting of a USB interface. An interface always has setting 0; extra settings
/// trade bandwidth for features and are chosen with <see cref="UsbDevice.SelectAlternateInterface"/>.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/USBAlternateInterface">USBAlternateInterface</see>
/// </summary>
public class UsbAlternateInterfaceInfo
{
    /// <summary>The setting's number.</summary>
    public byte AlternateSetting { get; set; }

    /// <summary>The USB class code this setting implements.</summary>
    public byte InterfaceClass { get; set; }

    /// <summary>The subclass code.</summary>
    public byte InterfaceSubclass { get; set; }

    /// <summary>The protocol code.</summary>
    public byte InterfaceProtocol { get; set; }

    /// <summary>The device's own name for the setting, when it provides one.</summary>
    public string? InterfaceName { get; set; }

    /// <summary>The endpoints this setting exposes.</summary>
    public UsbEndpointInfo[] Endpoints { get; set; } = [];
}
