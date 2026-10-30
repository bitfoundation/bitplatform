using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The container of the <see cref="BitBhTab"/>s (<c>role="tablist"</c>). Give it an
/// accessible name with <c>aria-label</c> when the tabs' purpose isn't obvious.
/// </summary>
public class BitBhTabList : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhTabsContext? TabsContext { get; set; }

    /// <summary>The tabs.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var orientation = TabsContext?.LayoutOrientation ?? BitBhOrientation.Horizontal;
        attrs["role"] = "tablist";
        attrs["aria-orientation"] = orientation.ToAttribute();
        SetDataValue(attrs, "orientation", orientation.ToAttribute());
        return attrs;
    }
}
