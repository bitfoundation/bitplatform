using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// The drag handle of a <see cref="BitBhSortable{TItem}"/> item — required for keyboard
/// reordering. Renders a <c>&lt;button&gt;</c> with <c>aria-roledescription="sortable"</c>,
/// <c>aria-describedby</c> instructions and <c>aria-pressed</c> while the item is
/// picked up. Give it a name with <c>aria-label</c> (e.g. "Reorder Buy milk").
/// When present, pointer drags start only from the handle.
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
public class BitBhSortableHandle<TItem> : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhSortableItemContext<TItem>? ItemContext { get; set; }

    [CascadingParameter]
    private BitBhSortable<TItem>? Sortable { get; set; }

    /// <summary>The handle content (e.g. a grip icon).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var index = ItemContext?.Index ?? -1;
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", Sortable is not null && index >= 0 ? Sortable.HandleId(index) : ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onkeydown", On<Microsoft.AspNetCore.Components.Web.KeyboardEventArgs>("onkeydown",
            e => Sortable?.HandleKeyDownAsync(index, e) ?? Task.CompletedTask));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var index = ItemContext?.Index ?? -1;
        var grabbed = Sortable?.IsGrabbed(index) ?? false;
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "button";
        else attrs["tabindex"] = "0";
        attrs["data-bit-bh-sortable-handle"] = "";
        attrs["aria-roledescription"] = "sortable";
        SetAriaBool(attrs, "aria-pressed", grabbed);
        if (Sortable is not null) attrs["aria-describedby"] = Sortable.InstructionsId;
        attrs[BitBhKeyboard.KeysAttribute] = grabbed
            ? "ArrowUp ArrowDown ArrowLeft ArrowRight Space Enter Escape Tab"
            : "Space Enter";
        if (ItemContext?.Disabled == true) attrs["aria-disabled"] = "true";
        SetDataFlag(attrs, "dragging", grabbed);
        return attrs;
    }
}
