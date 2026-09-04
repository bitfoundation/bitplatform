namespace Bit.Butil;

/// <summary>
/// The orientation the screen is currently in. "Primary" and "secondary" are the two ways round a
/// given axis can be held, relative to the device's natural orientation - which is portrait on most
/// phones and landscape on most tablets and laptops.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/type">ScreenOrientation.type</see>
/// </summary>
public enum ScreenOrientationType
{
    /// <summary>Portrait, the way round the device calls natural.</summary>
    PortraitPrimary,

    /// <summary>Portrait, upside down from <see cref="PortraitPrimary"/>.</summary>
    PortraitSecondary,

    /// <summary>Landscape, the way round the device calls natural.</summary>
    LandscapePrimary,

    /// <summary>Landscape, upside down from <see cref="LandscapePrimary"/>.</summary>
    LandscapeSecondary
}
