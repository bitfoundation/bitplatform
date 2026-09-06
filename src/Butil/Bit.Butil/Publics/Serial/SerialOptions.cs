namespace Bit.Butil;

/// <summary>
/// The line settings a serial port is opened with. They have to match what the device on the other
/// end expects - a mismatched baud rate produces bytes, just not the right ones.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SerialPort/open">SerialPort.open()</see>
/// </summary>
public class SerialOptions
{
    /// <summary>Bits per second. 9600 and 115200 cover most devices.</summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>Data bits per frame: 7 or 8.</summary>
    public byte DataBits { get; set; } = 8;

    /// <summary>Stop bits per frame: 1 or 2.</summary>
    public byte StopBits { get; set; } = 1;

    /// <summary>The parity bit scheme.</summary>
    public SerialParity Parity { get; set; } = SerialParity.None;

    /// <summary>
    /// The read and write buffer size, in bytes. Larger buffers absorb bursts from a chatty device
    /// at the cost of latency.
    /// </summary>
    public int BufferSize { get; set; } = 255;

    /// <summary>The flow-control scheme.</summary>
    public SerialFlowControl FlowControl { get; set; } = SerialFlowControl.None;
}
