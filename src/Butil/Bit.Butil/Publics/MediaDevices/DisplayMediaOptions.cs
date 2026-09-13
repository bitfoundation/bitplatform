namespace Bit.Butil;

/// <summary>
/// Shapes the surface picker shown by <see cref="MediaDevices.GetDisplayMedia"/>.
/// </summary>
/// <remarks>
/// Every member is a hint, not a guarantee: a browser that doesn't implement one ignores it, and
/// the user can always pick something other than what was preferred. Members left null are not
/// sent at all, so a runtime never sees an option it doesn't understand.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaDevices/getDisplayMedia#options">getDisplayMedia() options</see>
/// </remarks>
public class DisplayMediaOptions
{
    /// <summary>Which surface type to pre-select: <c>"monitor"</c>, <c>"window"</c> or <c>"browser"</c> (a tab).</summary>
    public string? DisplaySurface { get; set; }

    /// <summary>
    /// Whether the tab running this code is offered in the picker: <c>"include"</c> or <c>"exclude"</c>.
    /// Excluding it avoids the hall-of-mirrors effect of a page capturing itself.
    /// </summary>
    public string? SelfBrowserSurface { get; set; }

    /// <summary>
    /// Whether the browser offers a "share this tab instead" control during the capture:
    /// <c>"include"</c> or <c>"exclude"</c>.
    /// </summary>
    public string? SurfaceSwitching { get; set; }

    /// <summary>
    /// Whether system audio is offered alongside the surface: <c>"include"</c> or <c>"exclude"</c>.
    /// Only meaningful when display capture was requested with audio.
    /// </summary>
    public string? SystemAudio { get; set; }

    /// <summary>Whether whole monitors are offered: <c>"include"</c> or <c>"exclude"</c>.</summary>
    public string? MonitorTypeSurfaces { get; set; }

    /// <summary>When true, the current tab is pre-selected in the picker.</summary>
    public bool? PreferCurrentTab { get; set; }

    /// <summary>
    /// Whether the captured tab or window is brought to the front when capture starts:
    /// <c>"focus-captured-surface"</c> (the browser's default) or <c>"no-focus-change"</c>.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CaptureController/setFocusBehavior">CaptureController.setFocusBehavior()</see>
    /// </summary>
    /// <remarks>
    /// The one to set for a recorder or a conferencing tool, where the user picks a surface and then
    /// stays on <i>this</i> page - the default would yank them away from it. Only meaningful for tab
    /// and window capture; capturing a whole monitor focuses nothing either way. Ignored by runtimes
    /// without <c>CaptureController</c>.
    /// </remarks>
    public string? FocusBehavior { get; set; }
}
