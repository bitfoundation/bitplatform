namespace Bit.Butil;

/// <summary>
/// One MIDI message received from an input port.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MIDIMessageEvent">MIDIMessageEvent</see>
/// </summary>
public class MidiMessage
{
    /// <summary>The id of the port the message arrived on.</summary>
    public string PortId { get; set; } = string.Empty;

    /// <summary>
    /// The raw message bytes: a status byte and its data bytes. A note-on is
    /// <c>0x90 | channel</c>, note number, velocity.
    /// </summary>
    public byte[] Data { get; set; } = [];

    /// <summary>
    /// When the message arrived, on the same clock as <c>performance.now()</c>, in milliseconds.
    /// Timestamps are what make a MIDI stream sequenceable - they are the browser's, not the wall
    /// clock's.
    /// </summary>
    public double TimeStamp { get; set; }
}
