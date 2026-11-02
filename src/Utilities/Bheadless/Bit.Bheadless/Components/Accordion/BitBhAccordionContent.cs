using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// The collapsible panel of a <see cref="BitBhAccordionItem"/> (<c>role="region"</c>,
/// labelled by its trigger). Hidden while closed, visible during exit animations,
/// and exposes <c>--bit-bh-content-height</c> / <c>--bit-bh-content-width</c> for size animations.
/// </summary>
public class BitBhAccordionContent : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhAccordionItemContext? ItemContext { get; set; }

    /// <summary>
    /// Use <c>hidden="until-found"</c> while closed so find-in-page can reveal
    /// matches (the item opens automatically).
    /// </summary>
    [Parameter]
    public bool HiddenUntilFound { get; set; }

    /// <summary>The panel content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => ItemContext?.IsOpen ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ItemContext?.ContentId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "region";
        if (ItemContext is not null)
            attrs["aria-labelledby"] = ItemContext.TriggerId;
        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", ItemContext?.Disabled ?? false);
        BitBhToggleAttributes.ApplyPanel(attrs, IsOpen, HiddenUntilFound, ItemContext?.TriggerId);
        return attrs;
    }
}
