using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The heading that wraps a <see cref="BitBhAccordionTrigger"/> (the APG requires
/// accordion triggers to sit inside headings). Renders <c>&lt;h3&gt;</c> by default —
/// pick the level that fits your outline with <c>As="h2"</c> etc.
/// </summary>
public class BitBhAccordionHeader : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhAccordionItemContext? ItemContext { get; set; }

    /// <summary>The trigger.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "h3";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, ItemContext?.IsOpen ?? false);
        SetDataFlag(attrs, "disabled", ItemContext?.Disabled ?? false);
        return attrs;
    }
}
