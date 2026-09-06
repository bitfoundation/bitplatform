namespace Bit.Butil;

/// <summary>
/// The MIDI ports this page has been granted.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MIDIAccess">MIDIAccess</see>
/// </summary>
public class MidiAccessInfo
{
    /// <summary>
    /// True when the grant covers system-exclusive messages. Sysex is a separate, stricter
    /// permission because it can reprogram a device's firmware.
    /// </summary>
    public bool SysexEnabled { get; set; }

    /// <summary>The input ports - keyboards, controllers, anything that sends.</summary>
    public MidiPortInfo[] Inputs { get; set; } = [];

    /// <summary>The output ports - synths, drum machines, anything that receives.</summary>
    public MidiPortInfo[] Outputs { get; set; } = [];
}
