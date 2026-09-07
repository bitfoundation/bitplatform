namespace Bit.Butil;

/// <summary>
/// A position and an orientation in the session's reference space, flattened from
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/XRRigidTransform">XRRigidTransform</see>.
/// </summary>
/// <remarks>
/// Metres for the position, a unit quaternion for the orientation - WebXR's units throughout. The
/// origin is wherever the reference space put it: the floor under the user for
/// <see cref="XrReferenceSpaceType.LocalFloor"/>, the headset's starting point for
/// <see cref="XrReferenceSpaceType.Local"/>.
/// </remarks>
public class XrRigidTransform
{
    /// <summary>Position along the x axis, in metres. Positive is to the user's right.</summary>
    public double X { get; set; }

    /// <summary>Position along the y axis, in metres. Positive is up.</summary>
    public double Y { get; set; }

    /// <summary>Position along the z axis, in metres. Negative is the direction the user faces.</summary>
    public double Z { get; set; }

    /// <summary>The orientation quaternion's x component.</summary>
    public double OrientationX { get; set; }

    /// <summary>The orientation quaternion's y component.</summary>
    public double OrientationY { get; set; }

    /// <summary>The orientation quaternion's z component.</summary>
    public double OrientationZ { get; set; }

    /// <summary>The orientation quaternion's w component. 1 with the other three at 0 means no rotation.</summary>
    public double OrientationW { get; set; } = 1;
}
