using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// Cascaded by every floating surface (dialog, popover, menu, listbox …) to its
/// content. Nested surfaces use it to register as child layers — so a popover
/// opened inside a modal dialog may receive focus and clicks without dismissing
/// the dialog — and arrows use it to register themselves for positioning.
/// </summary>
public sealed class BitBhFloatingScope
{
    private readonly Func<int> _handle;

    internal BitBhFloatingScope(Func<int> handle)
    {
        _handle = handle;
    }

    /// <summary>The JS handle of the surface (0 while closed).</summary>
    public int Handle => _handle();

    /// <summary>The id of the registered arrow element, if any.</summary>
    public string? ArrowId { get; private set; }

    /// <summary>The id of the surface's content element.</summary>
    public string? Owner { get; internal set; }

    internal void RegisterArrow(string? id) => ArrowId = id;

    /// <summary>Wraps <paramref name="content"/> in a fixed cascading value of <paramref name="scope"/>.</summary>
    internal static void Render(RenderTreeBuilder builder, int sequence, BitBhFloatingScope scope, RenderFragment? content)
    {
        builder.OpenComponent<CascadingValue<BitBhFloatingScope>>(sequence);
        builder.AddComponentParameter(sequence + 1, "Value", scope);
        builder.AddComponentParameter(sequence + 2, "IsFixed", true);
        builder.AddComponentParameter(sequence + 3, "ChildContent", content);
        builder.CloseComponent();
    }
}
