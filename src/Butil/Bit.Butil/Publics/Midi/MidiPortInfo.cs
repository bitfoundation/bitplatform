namespace Bit.Butil;

/// <summary>
/// One MIDI input or output port.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MIDIPort">MIDIPort</see>
/// </summary>
public class MidiPortInfo
{
    /// <summary>
    /// The port's id, assigned by the browser and stable for the life of the access grant. This is
    /// what every send and subscribe takes.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The port's name, as the device reports it.</summary>
    public string? Name { get; set; }

    /// <summary>The manufacturer, when the device reports one.</summary>
    public string? Manufacturer { get; set; }

    /// <summary>The device's firmware or driver version, when it reports one.</summary>
    public string? Version { get; set; }

    /// <summary><c>"input"</c> or <c>"output"</c>.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <c>"connected"</c> or <c>"disconnected"</c>. A port that has been unplugged stays in the
    /// list as disconnected, so a subscription can survive the cable being re-seated.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary><c>"open"</c>, <c>"closed"</c> or <c>"pending"</c> - whether this page is holding the port.</summary>
    public string Connection { get; set; } = string.Empty;
}
