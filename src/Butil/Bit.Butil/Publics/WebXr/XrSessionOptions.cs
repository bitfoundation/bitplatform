namespace Bit.Butil;

/// <summary>
/// What a session needs and what it would like, mirroring the options of
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/XRSystem/requestSession">XRSystem.requestSession()</see>,
/// plus how often Butil should push poses back to .NET.
/// </summary>
public class XrSessionOptions
{
    /// <summary>
    /// Features the session cannot do without, e.g. <c>"local-floor"</c>, <c>"hit-test"</c>,
    /// <c>"hand-tracking"</c>. A runtime that cannot provide one of these refuses the session
    /// outright - which is the point of listing it here rather than under
    /// <see cref="OptionalFeatures"/>.
    /// </summary>
    public string[]? RequiredFeatures { get; set; }

    /// <summary>Features to enable where they exist. A missing one costs nothing.</summary>
    public string[]? OptionalFeatures { get; set; }

    /// <summary>
    /// The reference space to measure poses against. Butil falls back through the safer types when
    /// the runtime doesn't offer this one, and reports what it got.
    /// </summary>
    public XrReferenceSpaceType ReferenceSpaceType { get; set; } = XrReferenceSpaceType.LocalFloor;

    /// <summary>
    /// How often, in milliseconds, to push the viewer pose to the session's pose callback. 0 - the
    /// default - pushes nothing, leaving <see cref="XrSessionHandle.GetViewerPose"/> to be polled.
    /// </summary>
    /// <remarks>
    /// Deliberately a throttle rather than a per-frame stream: a headset runs at 90 Hz or more, and
    /// marshalling every frame into .NET costs more than it can possibly be worth. Use this to drive
    /// UI that follows the user (a position readout, a proximity check) and keep the rendering itself
    /// in WebGL.
    /// </remarks>
    public int PoseIntervalMs { get; set; }

    internal XrSessionJsOptions ToJsObject() => new()
    {
        RequiredFeatures = RequiredFeatures,
        OptionalFeatures = OptionalFeatures,
        ReferenceSpaceType = ReferenceSpaceType switch
        {
            XrReferenceSpaceType.Viewer => "viewer",
            XrReferenceSpaceType.Local => "local",
            XrReferenceSpaceType.BoundedFloor => "bounded-floor",
            XrReferenceSpaceType.Unbounded => "unbounded",
            _ => "local-floor"
        },
        PoseIntervalMs = PoseIntervalMs
    };
}
