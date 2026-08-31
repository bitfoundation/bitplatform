namespace Bit.BlazorUI;

/// <summary>
/// The looping animations an icon can play.
/// </summary>
/// <remarks>
/// Every one of these loops for as long as the icon is on the page, so each is a claim that something
/// is still happening. All of them slow down rather than stop when the reader has asked for reduced
/// motion, and <see cref="BitComponentBase.ForceAnimation"/> restores their full speed.
/// </remarks>
public enum BitIconAnimation
{
    /// <summary>
    /// Turns continuously clockwise - the loading spinner.
    /// </summary>
    Spin,

    /// <summary>
    /// Turns continuously counter-clockwise.
    /// </summary>
    SpinReverse,

    /// <summary>
    /// Turns clockwise in eight discrete steps, the way a segmented spinner ticks around.
    /// </summary>
    Pulse,

    /// <summary>
    /// Scales up and back down, to draw the eye to something that just changed.
    /// </summary>
    Beat,

    /// <summary>
    /// Fades out and back in.
    /// </summary>
    Fade,

    /// <summary>
    /// Rocks back and forth, for something that needs attention now.
    /// </summary>
    Shake
}
