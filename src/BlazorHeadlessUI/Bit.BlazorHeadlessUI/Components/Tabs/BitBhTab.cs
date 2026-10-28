using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A tab in a <see cref="BitBhTabList"/>. Renders <c>&lt;button role="tab"&gt;</c> with
/// <c>aria-selected</c>, <c>aria-controls</c> and roving <c>tabindex</c>. Emits
/// <c>data-state="active|inactive"</c> and <c>data-disabled</c>.
/// </summary>
public class BitBhTab : BitBhComponentBase, IBitBhCollectionItem, IDisposable
{
    [CascadingParameter]
    private BitBhTabsContext? TabsContext { get; set; }

    /// <summary>Optional value pairing this tab with the <see cref="BitBhTabPanel"/> of the same value.</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Disables this tab; keyboard navigation skips it.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhTabRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhTabRenderContext>? Template { get; set; }

    protected override string DefaultTag => "button";

    /// <inheritdoc />
    public string ItemId => ComponentId;

    /// <inheritdoc />
    public bool IsItemDisabled => Disabled || (TabsContext?.Disabled ?? false);

    /// <inheritdoc />
    public string? ItemText => null;

    /// <summary>Whether this tab is selected.</summary>
    public bool IsSelected => TabsContext?.Group.IsSelected(this) ?? false;

    protected override void OnInitialized() => TabsContext?.Tabs.Register(this);

    public void Dispose() => TabsContext?.Tabs.Unregister(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ItemId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            On<MouseEventArgs>("onclick", HandleClick));
        builder.AddAttribute(31, "onkeydown",
            On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, new BitBhTabRenderContext { IsSelected = IsSelected, Disabled = IsItemDisabled }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var selected = IsSelected;

        attrs["role"] = "tab";
        SetAriaBool(attrs, "aria-selected", selected);
        // aria-controls must not dangle: an unmounted panel is not referenced.
        if (TabsContext?.Group.PanelForTab(this) is { IsRendered: true } panel)
            attrs["aria-controls"] = panel.ItemId;

        // Roving tabindex: only the selected tab (or the first enabled one) is a tab stop.
        var tabStop = selected || (TabsContext?.Group.SelectedTab is null && TabsContext?.Tabs.First() == this);
        attrs["tabindex"] = tabStop ? "0" : "-1";

        var isButton = Tag.Equals("button", StringComparison.OrdinalIgnoreCase);
        if (isButton)
        {
            attrs["type"] = "button";
            if (IsItemDisabled) attrs["disabled"] = true;
        }
        else if (IsItemDisabled)
        {
            attrs["aria-disabled"] = "true";
        }

        if (TabsContext is not null)
        {
            attrs["data-bit-bh-item"] = TabsContext.Tabs.Key;
            var vertical = TabsContext.LayoutOrientation == BitBhOrientation.Vertical;
            attrs[BitBhKeyboard.KeysAttribute] = (vertical ? "ArrowUp ArrowDown" : "ArrowLeft ArrowRight") + " Home End"
                + (TabsContext.Manual || !isButton ? " Enter Space" : string.Empty);
            SetDataValue(attrs, "orientation", TabsContext.LayoutOrientation.ToAttribute());
        }

        SetDataState(attrs, selected, "active", "inactive");
        SetDataFlag(attrs, "selected", selected);
        SetDataFlag(attrs, "disabled", IsItemDisabled);
        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
        => IsItemDisabled || TabsContext is null ? Task.CompletedTask : TabsContext.Group.SelectAsync(this);

    private Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (TabsContext is null) return Task.CompletedTask;
        if (e.Key is "Enter" or " " && !TabsContext.Manual)
            return Tag.Equals("button", StringComparison.OrdinalIgnoreCase) ? Task.CompletedTask : HandleClick(new MouseEventArgs());
        return TabsContext.Group.HandleKeyDownAsync(this, e);
    }
}
