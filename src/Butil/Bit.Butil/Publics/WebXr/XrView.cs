namespace Bit.Butil;

/// <summary>
/// One eye's view of the scene, from
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/XRView">XRView</see>.
/// </summary>
/// <remarks>
/// A headset reports two of these per pose, one per eye, each with its own transform and projection
/// - which is what stereo rendering actually is. An inline or a phone-AR session reports one.
/// </remarks>
public class XrView
{
    /// <summary>Which eye this view is for: <c>"left"</c>, <c>"right"</c>, or <c>"none"</c> for a monoscopic view.</summary>
    public string Eye { get; set; } = string.Empty;

    /// <summary>Where this eye is, in the session's reference space.</summary>
    public XrRigidTransform Transform { get; set; } = new();

    /// <summary>
    /// The 4x4 projection matrix for this eye, in column-major order - the 16 numbers a renderer
    /// hands to its vertex shader. Empty when the runtime didn't provide one.
    /// </summary>
    public double[] ProjectionMatrix { get; set; } = [];
}
