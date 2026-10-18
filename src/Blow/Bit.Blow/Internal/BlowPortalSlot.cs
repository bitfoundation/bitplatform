using Microsoft.AspNetCore.Components;

namespace Bit.Blow;

/// <summary>
/// What a <c>BlowViewportPortal</c> hands the canvas to draw: its current content, plus the hook
/// it uses to have just that content redrawn when it changes.
/// </summary>
public sealed class BlowPortalSlot
{
    /// <summary>The content to draw inside the viewport layer.</summary>
    internal RenderFragment? Content { get; set; }

    /// <summary>Redraws the host currently showing this slot, if there is one.</summary>
    internal Action? Changed { get; set; }
}
