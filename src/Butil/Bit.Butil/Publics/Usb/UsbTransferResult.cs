namespace Bit.Butil;

/// <summary>
/// The outcome of a USB transfer. IN and OUT results are the same type here, so one shape covers
/// every transfer: an IN fills <see cref="Data"/>, an OUT fills <see cref="BytesWritten"/>.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/USBInTransferResult">USBInTransferResult</see>
/// / <see href="https://developer.mozilla.org/en-US/docs/Web/API/USBOutTransferResult">USBOutTransferResult</see>
/// </summary>
public class UsbTransferResult
{
    /// <summary>
    /// <c>"ok"</c>, <c>"stall"</c> (the endpoint halted - clear it with
    /// <see cref="UsbDevice.ClearHalt"/>) or <c>"babble"</c> (the device sent more than was asked for).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Bytes accepted by the device on an OUT transfer; 0 for an IN transfer.</summary>
    public uint BytesWritten { get; set; }

    /// <summary>The bytes the device returned on an IN transfer; null for an OUT transfer.</summary>
    public byte[]? Data { get; set; }
}
