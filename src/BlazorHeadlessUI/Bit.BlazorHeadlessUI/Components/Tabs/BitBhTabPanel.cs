using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The content shown for a <see cref="BitBhTab"/> (<c>role="tabpanel"</c>, labelled by
/// its tab). Pairs with the tab of the same <see cref="Value"/>, or by position.
/// Hidden while inactive (exit animations supported); set <see cref="Unmount"/>
/// to remove inactive panels from the DOM.
/// </summary>
public class BitBhTabPanel : BitBhComponentBase, IBitBhCollectionItem, IDisposable
{
    [CascadingParameter]
    private BitBhTabsContext? TabsContext { get; set; }

    /// <summary>Optional value pairing this panel with the <see cref="BitBhTab"/> of the same value.</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Remove the panel from the DOM while inactive. Default: false (kept, hidden).</summary>
    [Parameter]
    public bool Unmount { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhTabPanelRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhTabPanelRenderContext>? Template { get; set; }

    protected override string DefaultTag => "div";

    /// <inheritdoc />
    public string ItemId => ComponentId;

    /// <inheritdoc />
    public bool IsItemDisabled => false;

    /// <inheritdoc />
    public string? ItemText => null;

    private BitBhTab? Tab => TabsContext?.Group.TabForPanel(this);
    private bool IsSelected => Tab is { } tab && tab.IsSelected;

    /// <summary>Whether the panel element is in the DOM (an unmounted inactive panel is not).</summary>
    internal bool IsRendered => !Unmount || IsSelected;

    protected override void OnInitialized() => TabsContext?.Panels.Register(this);

    public void Dispose() => TabsContext?.Panels.Unregister(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var selected = IsSelected;
        if (Unmount && !selected) return;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ItemId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, new BitBhTabPanelRenderContext { IsSelected = selected }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var selected = IsSelected;
        attrs["role"] = "tabpanel";
        if (Tab is { } tab)
            attrs["aria-labelledby"] = tab.ItemId;
        // Focusable so keyboard users reach panel content that has no focusable elements.
        attrs["tabindex"] = "0";
        if (TabsContext is not null)
            attrs["data-bit-bh-item"] = TabsContext.Panels.Key;
        attrs["data-bit-bh-presence"] = "";
        if (!selected)
            attrs["hidden"] = true;
        SetDataState(attrs, selected, "active", "inactive");
        return attrs;
    }
}
