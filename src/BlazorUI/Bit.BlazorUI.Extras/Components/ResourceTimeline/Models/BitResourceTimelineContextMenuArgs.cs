namespace Bit.BlazorUI;

/// <summary>
/// Describes a right-click on the timeline: what was under the pointer and
/// where on screen the click happened, so the host can show and position its
/// own context menu (the native browser menu is suppressed). Same payload as
/// <see cref="BitResourceTimelinePointerArgs"/> (click / double-click).
/// </summary>
public sealed class BitResourceTimelineContextMenuArgs : BitResourceTimelinePointerArgs
{
}
