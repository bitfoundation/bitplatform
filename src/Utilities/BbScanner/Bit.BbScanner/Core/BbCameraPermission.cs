namespace Bit.BbScanner;

/// <summary>
/// What the browser would do with a request for the camera, as <see cref="BbScanner.GetCameraPermissionAsync"/>
/// reports it - so a page can explain why it needs the camera before the prompt appears, or say how to allow it
/// once it has been refused, instead of finding out from a failed start.
/// </summary>
public enum BbCameraPermission
{
    /// <summary>
    /// The browser does not say: it has no Permissions API, does not know the camera permission by name, or the
    /// scanner is not running in a browser at all. Starting the camera is the only way to find out.
    /// </summary>
    Unknown = 0,

    /// <summary>Starting the camera will ask the user.</summary>
    Prompt = 1,

    /// <summary>Starting the camera will open it without asking.</summary>
    Granted = 2,

    /// <summary>
    /// The user or a policy has refused the camera; starting it will fail with
    /// <see cref="BbScannerErrorKind.PermissionDenied"/>, and only the user, in the browser's site settings, can
    /// change that.
    /// </summary>
    Denied = 3,
}
