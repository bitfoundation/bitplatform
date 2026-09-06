namespace Bit.Butil;

/// <summary>
/// How a serial port asks the other end to pause when its buffer fills.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SerialPort/open">SerialPort.open()</see>
/// </summary>
public enum SerialFlowControl
{
    /// <summary>No flow control - the sender is expected not to outrun the receiver.</summary>
    None,

    /// <summary>Hardware flow control over the RTS/CTS lines.</summary>
    Hardware
}
