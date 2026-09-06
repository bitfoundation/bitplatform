namespace Bit.Butil;

/// <summary>
/// <see cref="SerialOptions"/> in the shape <c>SerialPort.open()</c> wants: parity and flow control
/// are spelled-out strings there, and a .NET enum would cross the boundary as its numeric value.
/// </summary>
internal class SerialOpenJsOptions
{
    public int BaudRate { get; set; }

    public byte DataBits { get; set; }

    public byte StopBits { get; set; }

    public string Parity { get; set; } = "none";

    public int BufferSize { get; set; }

    public string FlowControl { get; set; } = "none";
}
