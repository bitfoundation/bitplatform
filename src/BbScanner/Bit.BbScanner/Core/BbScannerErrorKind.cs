namespace Bit.BbScanner;

/// <summary>Why a scanner could not start.</summary>
public enum BbScannerErrorKind
{
    /// <summary>The cause could not be classified.</summary>
    Unknown = 0,

    /// <summary>The user denied camera permission, or a policy denies it.</summary>
    PermissionDenied = 1,

    /// <summary>No camera matched the request.</summary>
    NoCamera = 2,

    /// <summary>A camera exists but could not be opened, usually because another application holds it.</summary>
    CameraUnavailable = 3,

    /// <summary>The requested constraints cannot be satisfied by any camera.</summary>
    ConstraintsUnsatisfied = 4,

    /// <summary>The browser exposes no camera API, or the page is not in a secure context.</summary>
    NotSupported = 5,
}
