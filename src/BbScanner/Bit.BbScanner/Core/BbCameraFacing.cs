namespace Bit.BbScanner;

/// <summary>Which camera to prefer when no specific device has been chosen.</summary>
public enum BbCameraFacing
{
    /// <summary>Let the browser decide.</summary>
    Any = 0,

    /// <summary>Prefer a rear facing camera, which is what a scanner normally wants.</summary>
    Rear = 1,

    /// <summary>Prefer a front facing camera.</summary>
    Front = 2,
}
