namespace Bit.Butil;

/// <summary>
/// One button on a controller, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/GamepadButton">GamepadButton</see>.
/// </summary>
public class GamepadButton
{
    /// <summary>True while the button is held down.</summary>
    public bool Pressed { get; set; }

    /// <summary>
    /// True while the button is touched but not necessarily pressed. Only some hardware reports
    /// this; elsewhere it tracks <see cref="Pressed"/>.
    /// </summary>
    public bool Touched { get; set; }

    /// <summary>
    /// How far the button is depressed, 0 to 1. Digital buttons only ever report 0 or 1; analogue
    /// triggers report the whole range.
    /// </summary>
    public double Value { get; set; }
}
