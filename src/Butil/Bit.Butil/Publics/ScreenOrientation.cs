using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The ScreenOrientation interface of the Screen Orientation API provides information about the current orientation of the document.
/// <br/>
/// More info: <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation">https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation</see>
/// </summary>
public class ScreenOrientation(IJSRuntime js)
{
    /// <summary>
    /// Returns the document's current orientation type.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/type">https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/type</see>
    /// </summary>
    public async Task<ScreenOrientationType> GetOrientationType()
    {
        var type = await js.InvokeAsync<string>("Bit.Butil.screenOrientation.type");

        return type switch
        {
            "portrait-primary" => ScreenOrientationType.PortraitPrimary,
            "portrait-secondary" => ScreenOrientationType.PortraitSecondary,
            "landscape-primary" => ScreenOrientationType.LandscapePrimary,
            "landscape-secondary" => ScreenOrientationType.LandscapeSecondary,
            _ => ScreenOrientationType.LandscapePrimary
        };
    }

    /// <summary>
    /// Returns the document's current orientation angle.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/angle">https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/angle</see>
    /// </summary>
    public async Task<ushort> GetAngle()
        => await js.InvokeAsync<ushort>("Bit.Butil.screenOrientation.angle");

    /// <summary>
    /// Locks the orientation of the containing document to the specified orientation.
    /// Typically orientation locking is only enabled on mobile devices, and when the browser context is full screen.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/lock">https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/lock</see>
    /// </summary>
    public async Task Lock(OrientationLockType lockType)
    {
        var type = lockType switch 
        {
            OrientationLockType.Any => "any",
            OrientationLockType.Natural => "natural",
            OrientationLockType.Landscape => "landscape",
            OrientationLockType.Portrait => "portrait",
            OrientationLockType.PortraitPrimary => "portrait-primary",
            OrientationLockType.PortraitSecondary => "portrait-secondary",
            OrientationLockType.LandscapePrimary => "landscape-primary",
            OrientationLockType.LandscapeSecondary => "landscape-secondary",
            _ => "any"
        };

        await js.InvokeVoidAsync("Bit.Butil.screenOrientation.lock", type);
    }

    /// <summary>
    /// Unlocks the orientation of the containing document from its default orientation.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/unlock">https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/unlock</see>
    /// </summary>
    public async Task Unlock()
        => await js.InvokeVoidAsync("Bit.Butil.screenOrientation.unlock");
}
