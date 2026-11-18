using System.Numerics;

namespace Bit.Bmage;

/// <summary>A pointer event on the editor canvas, expressed in image coordinates.</summary>
/// <param name="Point">Position in image pixels.</param>
/// <param name="ShiftKey">True when shift was held, which constrains shapes and angles.</param>
/// <param name="AltKey">True when alt was held.</param>
/// <param name="CtrlKey">True when control (or command) was held.</param>
/// <param name="PointerType">"mouse", "pen" or "touch".</param>
/// <param name="Pressure">Stylus pressure in [0,1]; 0.5 for devices that do not report it.</param>
/// <param name="Handle">The crop handle being dragged, when the drag started on one.</param>
public readonly record struct BmageCanvasPointerArgs(
    Vector2 Point,
    bool ShiftKey,
    bool AltKey,
    bool CtrlKey,
    string PointerType,
    float Pressure,
    BmageCropHandle? Handle)
{
    /// <summary>True when the input came from a stylus, which reports usable pressure.</summary>
    /// <summary>
    /// The pointer position in viewport (CSS) pixels. Unlike <see cref="Point"/> it does not move when the view pans or
    /// zooms, which makes it the right measure for gestures that change the view.
    /// </summary>
    public Vector2 Screen { get; init; }

    public bool IsPen => PointerType == "pen";

    /// <summary>True when the input came from a finger.</summary>
    public bool IsTouch => PointerType == "touch";
}
