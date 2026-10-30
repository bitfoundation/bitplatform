using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Renders its content as a keyed unit. Keys can only be set on elements and
/// components, so list renderers wrap each templated item in one of these to keep
/// item state (timers, focus, animations) attached to the right item.
/// </summary>
internal sealed class BitBhKeyed : ComponentBase
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, ChildContent);
}
