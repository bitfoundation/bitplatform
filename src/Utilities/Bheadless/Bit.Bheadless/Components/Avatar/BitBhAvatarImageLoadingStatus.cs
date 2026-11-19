namespace Bit.Bheadless;

/// <summary>
/// The loading status of an <see cref="BitBhAvatarImage"/>, mirroring Radix UI's
/// <c>ImageLoadingStatus</c>. Drives whether the image or the
/// <see cref="BitBhAvatarFallback"/> is shown.
/// </summary>
public enum BitBhAvatarImageLoadingStatus
{
    /// <summary>No image source has been resolved yet (initial / server-render state).</summary>
    Idle,

    /// <summary>The image is downloading.</summary>
    Loading,

    /// <summary>The image finished downloading successfully and is displayed.</summary>
    Loaded,

    /// <summary>The image failed to load (or no source was provided).</summary>
    Error,
}
