using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Optional destination for portalled content (<see cref="BitBhPortal"/> and the
/// <c>Portal</c> option of dialogs, popovers, menus …). Without an outlet a root is
/// appended to <c>&lt;body&gt;</c> on demand. Place it at the end of your root layout
/// when you want portalled content inside your app element (e.g. to inherit
/// theme classes or CSS variables).
///
/// <code>
/// &lt;div class="page"&gt;@Body&lt;/div&gt;
/// &lt;BitBhPortalOutlet /&gt;
/// </code>
/// </summary>
public class BitBhPortalOutlet : ComponentBase
{
    /// <summary>Additional attributes for the outlet element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "id", "bit-bh-portal-root");
        builder.AddAttribute(2, "data-bit-bh-portal-root", "");
        builder.AddMultipleAttributes(3, AdditionalAttributes);
        builder.CloseElement();
    }

    // The outlet's children are managed by the JS runtime; never re-render it.
    protected override bool ShouldRender() => false;
}
