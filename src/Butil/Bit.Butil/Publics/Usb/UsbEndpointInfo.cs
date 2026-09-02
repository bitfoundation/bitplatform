namespace Bit.Butil;

/// <summary>
/// One endpoint of a USB alternate interface - the address transfers are actually addressed to.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/USBEndpoint">USBEndpoint</see>
/// </summary>
public class UsbEndpointInfo
{
    /// <summary>The endpoint number, which is what <c>TransferIn</c>/<c>TransferOut</c> take.</summary>
    public byte EndpointNumber { get; set; }

    /// <summary><c>"in"</c> (device to host) or <c>"out"</c> (host to device).</summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary><c>"bulk"</c>, <c>"interrupt"</c> or <c>"isochronous"</c>.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>The largest packet the endpoint accepts, in bytes.</summary>
    public uint PacketSize { get; set; }
}
