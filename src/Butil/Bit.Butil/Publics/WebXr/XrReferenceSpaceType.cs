namespace Bit.Butil;

/// <summary>
/// What poses are measured against, the argument of
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/XRSession/requestReferenceSpace">XRSession.requestReferenceSpace()</see>.
/// </summary>
/// <remarks>
/// Not every runtime offers every type, so Butil falls back through
/// <see cref="LocalFloor"/>, <see cref="Local"/> and <see cref="Viewer"/> when the requested one is
/// unavailable, and reports what it settled on as
/// <see cref="XrSessionHandle.ReferenceSpaceType"/>.
/// </remarks>
public enum XrReferenceSpaceType
{
    /// <summary>The origin follows the viewer's head. Always available; useful for content that should stay in front of the user.</summary>
    Viewer,

    /// <summary>The origin is where the viewer was when the session started. Good for seated experiences.</summary>
    Local,

    /// <summary>Like <see cref="Local"/>, but the origin sits on the floor - so a y of 0 is the ground the user is standing on.</summary>
    LocalFloor,

    /// <summary>Floor-relative, and the runtime also knows the boundary of the space the user can safely walk in.</summary>
    BoundedFloor,

    /// <summary>Floor-relative with no boundary, for experiences that let the user walk arbitrarily far.</summary>
    Unbounded
}
