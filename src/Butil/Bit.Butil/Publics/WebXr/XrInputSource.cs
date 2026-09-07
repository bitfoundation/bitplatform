namespace Bit.Butil;

/// <summary>
/// One thing the user is pointing or acting with, from
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/XRInputSource">XRInputSource</see> -
/// a controller, a tracked hand, or the user's gaze.
/// </summary>
public class XrInputSource
{
    /// <summary>Which hand this belongs to: <c>"left"</c>, <c>"right"</c>, or <c>"none"</c> for gaze and screen input.</summary>
    public string Handedness { get; set; } = string.Empty;

    /// <summary>
    /// How the target ray is aimed: <c>"gaze"</c> (where the user looks), <c>"tracked-pointer"</c> (a
    /// controller or hand) or <c>"screen"</c> (a tap on a phone's AR view).
    /// </summary>
    public string TargetRayMode { get; set; } = string.Empty;

    /// <summary>
    /// The device profile names, most specific first - <c>"oculus-touch-v3"</c>,
    /// <c>"generic-trigger-squeeze"</c>. What a renderer looks up to draw the right controller model.
    /// </summary>
    public string[] Profiles { get; set; } = [];

    /// <summary>True when the source has buttons and axes beyond select and squeeze.</summary>
    public bool HasGamepad { get; set; }

    /// <summary>True when the source reports where it is held, not only where it points.</summary>
    public bool HasGripSpace { get; set; }
}
