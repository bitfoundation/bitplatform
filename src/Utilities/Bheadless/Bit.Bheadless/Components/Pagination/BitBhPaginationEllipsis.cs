using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>A gap in a <see cref="BitBhPagination"/>'s page range ("…"), hidden from assistive technology.</summary>
public class BitBhPaginationEllipsis : BitBhComponentBase
{
    /// <summary>The content. Default: "…".</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        if (ChildContent is null) builder.AddContent(30, "…");
        else builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-hidden"] = "true";
        return attrs;
    }
}
