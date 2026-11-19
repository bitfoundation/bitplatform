using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>An item (<c>&lt;li&gt;</c>) of a <see cref="BitBhBreadcrumbs"/>.</summary>
public class BitBhBreadcrumbItem : BitBhComponentBase
{
    /// <summary>The content: a <see cref="BitBhBreadcrumbLink"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "li";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }
}
