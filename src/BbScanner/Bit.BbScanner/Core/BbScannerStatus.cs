namespace Bit.BbScanner;

/// <summary>Lifecycle state of a scanner.</summary>
public enum BbScannerStatus
{
    /// <summary>Created but not started.</summary>
    Idle = 0,

    /// <summary>Waiting for camera permission and for the track to open.</summary>
    Starting = 1,

    /// <summary>Running and decoding frames.</summary>
    Scanning = 2,

    /// <summary>Camera still open, but frames are not being decoded.</summary>
    Paused = 3,

    /// <summary>Stopped and the camera released.</summary>
    Stopped = 4,

    /// <summary>Stopped because of an error; see the reported exception.</summary>
    Faulted = 5,
}
