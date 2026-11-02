using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// One item of a <see cref="BitBhSortable{TItem}"/> (rendered by the sortable). Emits
/// <c>data-dragging</c> while moved and is the element the pointer drags.
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
public class BitBhSortableItem<TItem> : ComponentBase
{
    /// <summary>The owning sortable.</summary>
    [Parameter] public BitBhSortable<TItem> Sortable { get; set; } = default!;

    /// <summary>The item.</summary>
    [Parameter] public TItem Item { get; set; } = default!;

    /// <summary>The item's position.</summary>
    [Parameter] public int Index { get; set; }

    /// <summary>The element tag.</summary>
    [Parameter] public string Tag { get; set; } = "li";

    /// <summary>The item template.</summary>
    [Parameter] public RenderFragment<BitBhSortableItemContext<TItem>>? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BitBhSortableItemContext<TItem>
        {
            Item = Item,
            Index = Index,
            IsDragging = Sortable.IsDragging(Index),
            Disabled = Sortable.IsDisabled,
        };

        builder.OpenElement(0, Tag);
        builder.AddAttribute(1, "data-bit-bh-sortable-item", Sortable.ContainerId);
        if (context.IsDragging) builder.AddAttribute(2, "data-dragging", "");
        if (context.Disabled) builder.AddAttribute(3, "data-disabled", "");
        builder.OpenComponent<CascadingValue<BitBhSortableItemContext<TItem>>>(10);
        builder.AddComponentParameter(11, "Value", context);
        builder.AddComponentParameter(12, "ChildContent", ChildContent?.Invoke(context));
        builder.CloseComponent();
        builder.CloseElement();
    }
}
