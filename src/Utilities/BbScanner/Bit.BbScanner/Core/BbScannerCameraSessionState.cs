namespace Bit.BbScanner;

/// <summary>What an open camera is doing.</summary>
public enum BbScannerCameraSessionState
{
    /// <summary>Delivering frames.</summary>
    Running = 0,

    /// <summary>
    /// Not delivering frames for now - the app is in the background, or the platform took the camera away from it for a
    /// while - and will again once it is back. The scanner keeps the camera meanwhile.
    /// </summary>
    Interrupted = 1,

    /// <summary>Gone for good: unplugged, taken by another app, or failed. Open it again to scan.</summary>
    Ended = 2,
}
