using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The children of a <see cref="BitBhTreeItem{TValue}"/> (<c>&lt;ul role="group"&gt;</c>).
/// Hidden while the parent is collapsed (exit animations and
/// <c>--bit-bh-content-height</c> supported); set <see cref="Unmount"/> to skip rendering
/// collapsed branches (large or lazily loaded trees).
/// </summary>
/// <typeparam name="TValue">The item value type.</typeparam>
public class BitBhTreeGroup<TValue> : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhTreeContext<TValue>? TreeContext { get; set; }

    [CascadingParameter]
    private BitBhTreeItem<TValue>? ParentItem { get; set; }

    /// <summary>
    /// Don't render children while collapsed. The parent item then counts as expandable (like
    /// <c>HasChildren</c>), since its children are unknown until it expands.
    /// Note: hierarchical checkbox states only account for rendered items.
    /// </summary>
    [Parameter]
    public bool Unmount { get; set; }

    /// <summary>Child <see cref="BitBhTreeItem{TValue}"/>s.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "ul";

    private bool IsOpen => ParentItem is null || (TreeContext?.IsExpanded(ParentItem.Value) ?? true);

    // Whether the item is expandable depends on Unmount: it can't count unmounted children.
    protected override void OnParametersSet() => ParentItem?.SetGroup(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Unmount && !IsOpen) return;
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "group";
        attrs["data-bit-bh-presence"] = "";
        if (!IsOpen) attrs["hidden"] = true;
        SetDataState(attrs, IsOpen);
        return attrs;
    }

    public void Dispose()
    {
        if (ReferenceEquals(ParentItem?.Group, this)) ParentItem!.SetGroup(null);
    }
}
