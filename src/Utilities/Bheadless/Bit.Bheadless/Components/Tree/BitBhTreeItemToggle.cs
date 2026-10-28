using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// An optional expand / collapse indicator inside a <see cref="BitBhTreeItem{TValue}"/>
/// (the chevron). Clicking it toggles the item without selecting it. It is hidden
/// from assistive tech — the tree item itself exposes <c>aria-expanded</c>.
/// </summary>
/// <typeparam name="TValue">The item value type.</typeparam>
public class BitBhTreeItemToggle<TValue> : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhTreeContext<TValue>? TreeContext { get; set; }

    [CascadingParameter]
    private BitBhTreeItem<TValue>? Item { get; set; }

    [CascadingParameter]
    private BitBhTreeItemRenderContext<TValue>? ItemState { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>bool</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<bool>? Template { get; set; }

    protected override string DefaultTag => "span";

    private bool IsExpanded => ItemState?.IsExpanded ?? (Item is not null && (TreeContext?.IsExpanded(Item.Value) ?? false));

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddAttribute(20, "onclick", On<MouseEventArgs>("onclick", HandleClick));
        builder.AddEventStopPropagationAttribute(21, "onclick", true);
        // The host it sits in acts on the owned click; a chevron press expands and nothing else.
        builder.AddEventStopPropagationAttribute(22, BitBhEventHandlers.Click, true);
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, BitBhContent.Pick(Template, ChildContent, IsExpanded));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-hidden"] = "true";
        if (Item is { IsExpandable: true }) SetDataState(attrs, IsExpanded);
        else attrs["data-leaf"] = "";
        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
        => Item is null || TreeContext is null ? Task.CompletedTask : TreeContext.Tree.ToggleExpandedAsync(Item);
}
