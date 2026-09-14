namespace Bit.Butil;

/// <summary>
/// An action the user took with a controller, a hand, their gaze or a screen tap.
/// </summary>
/// <param name="Type">Which action, and which end of it.</param>
/// <param name="Handedness">Which hand it came from: <c>"left"</c>, <c>"right"</c> or <c>"none"</c>.</param>
/// <param name="TargetRayMode">How the source is aimed: <c>"gaze"</c>, <c>"tracked-pointer"</c> or <c>"screen"</c>.</param>
public record XrInputEvent(XrInputEventType Type, string Handedness, string TargetRayMode);
