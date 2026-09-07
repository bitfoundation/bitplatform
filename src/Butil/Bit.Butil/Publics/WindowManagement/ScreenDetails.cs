namespace Bit.Butil;

/// <summary>
/// Every screen attached to the machine, and which of them this window is on.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenDetails">ScreenDetails</see>
/// </summary>
public class ScreenDetails
{
    /// <summary>True when more than one screen is attached.</summary>
    public bool IsExtended { get; set; }

    /// <summary>
    /// The index into <see cref="Screens"/> of the screen this window is currently on, or -1 when
    /// the browser reports a current screen that is not in <see cref="Screens"/>.
    /// </summary>
    public int CurrentScreenIndex { get; set; }

    /// <summary>
    /// The attached screens. Before window-management permission is granted a browser reports only
    /// the current one, so the list length is itself a hint about the permission state.
    /// </summary>
    public ScreenDetailInfo[] Screens { get; set; } = [];
}
