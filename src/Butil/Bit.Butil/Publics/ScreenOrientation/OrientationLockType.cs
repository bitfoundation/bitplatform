namespace Bit.Butil;

/// <summary>
/// What a screen-orientation lock asks for. Locking generally requires the document to be
/// fullscreen, and mobile browsers reject it outright on a page that is not.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/lock">ScreenOrientation.lock()</see>
/// </summary>
public enum OrientationLockType
{
    /// <summary>Any orientation the device supports - effectively releases the lock.</summary>
    Any,

    /// <summary>The device's own natural orientation.</summary>
    Natural,

    /// <summary>Either landscape orientation, whichever way the device is held.</summary>
    Landscape,

    /// <summary>Either portrait orientation, whichever way the device is held.</summary>
    Portrait,

    /// <summary>Portrait, natural way round only.</summary>
    PortraitPrimary,

    /// <summary>Portrait, upside down only.</summary>
    PortraitSecondary,

    /// <summary>Landscape, natural way round only.</summary>
    LandscapePrimary,

    /// <summary>Landscape, upside down only.</summary>
    LandscapeSecondary
}
