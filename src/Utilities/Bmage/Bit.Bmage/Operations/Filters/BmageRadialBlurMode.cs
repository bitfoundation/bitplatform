namespace Bit.Bmage;

/// <summary>Which way <see cref="BmageRadialBlurFilter"/> streaks the image.</summary>
public enum BmageRadialBlurMode
{
    /// <summary>Along lines through the centre, as zooming the lens during the exposure does: a rush towards the subject.</summary>
    Zoom = 0,

    /// <summary>Along circles around the centre, as turning the camera does: a spinning wheel, a swirl.</summary>
    Spin,
}
