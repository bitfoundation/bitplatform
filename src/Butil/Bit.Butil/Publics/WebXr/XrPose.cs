namespace Bit.Butil;

/// <summary>
/// Where the viewer is and what each of their eyes sees, from
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/XRViewerPose">XRViewerPose</see>.
/// </summary>
/// <remarks>
/// A pose only exists inside an XR frame, so Butil keeps the most recent one from the session's own
/// frame loop: <see cref="XrSessionHandle.GetViewerPose"/> reads that snapshot, and
/// <see cref="XrSessionOptions.PoseIntervalMs"/> pushes it on a timer. Neither is a substitute for
/// rendering - drawing at headset frame rates belongs in WebGL, not across an interop boundary.
/// </remarks>
public class XrPose
{
    /// <summary>Where the viewer's head is, in the session's reference space.</summary>
    public XrRigidTransform Transform { get; set; } = new();

    /// <summary>
    /// True when the position is inferred rather than tracked - a three-degrees-of-freedom headset,
    /// or one that has lost tracking. Orientation is still real; the position is a guess.
    /// </summary>
    public bool EmulatedPosition { get; set; }

    /// <summary>One view per eye: two for a headset, one for a monoscopic session.</summary>
    public XrView[] Views { get; set; } = [];
}
