namespace Bit.Butil;

/// <summary>
/// A snapshot of one controller, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Gamepad">Gamepad</see>.
/// </summary>
/// <remarks>
/// This is a value read at a point in time, not a live object: read it again (or subscribe with
/// <see cref="Gamepad.SubscribeChanges"/>) to see new input.
/// </remarks>
public class GamepadState
{
    /// <summary>The controller's port index, and the value <see cref="Gamepad.Vibrate"/> takes.</summary>
    public int Index { get; set; }

    /// <summary>A device string chosen by the browser, e.g. the vendor and product name.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>True while the controller is attached. A snapshot taken after it was unplugged reads false.</summary>
    public bool Connected { get; set; }

    /// <summary>
    /// <c>"standard"</c> when the browser mapped the device onto the standard button/axis layout,
    /// empty when it couldn't - in which case the indices below are device-specific.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Gamepad/mapping">Gamepad.mapping</see>
    /// </summary>
    public string Mapping { get; set; } = string.Empty;

    /// <summary>When this snapshot was taken, on the page's <c>performance.now()</c> timeline.</summary>
    public double Timestamp { get; set; }

    /// <summary>
    /// Stick and d-pad positions, each in the -1 to 1 range. Under the standard mapping, 0/1 are
    /// the left stick's X/Y and 2/3 the right stick's.
    /// </summary>
    public double[] Axes { get; set; } = [];

    /// <summary>
    /// Every button, including analogue triggers. Under the standard mapping, 0-3 are the face
    /// buttons and 6/7 the triggers.
    /// </summary>
    public GamepadButton[] Buttons { get; set; } = [];

    /// <summary>True when this pad exposes a vibration actuator, i.e. <see cref="Gamepad.Vibrate"/> can do something.</summary>
    public bool HasVibration { get; set; }
}
