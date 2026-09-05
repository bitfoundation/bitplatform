namespace Bit.Butil;

/// <summary>
/// The parity bit a serial port appends to each frame.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SerialPort/open">SerialPort.open()</see>
/// </summary>
public enum SerialParity
{
    /// <summary>No parity bit. The overwhelmingly common choice.</summary>
    None,

    /// <summary>A parity bit making the number of set bits even.</summary>
    Even,

    /// <summary>A parity bit making the number of set bits odd.</summary>
    Odd
}
