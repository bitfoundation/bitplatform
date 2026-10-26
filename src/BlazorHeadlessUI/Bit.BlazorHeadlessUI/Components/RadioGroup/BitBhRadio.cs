using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A radio in a <see cref="BitBhRadioGroup"/>. Renders <c>&lt;button role="radio"&gt;</c>
/// with <c>aria-checked</c> and roving <c>tabindex</c>; emits
/// <c>data-state="checked|unchecked"</c> and <c>data-disabled</c>. Its text content
/// is its accessible name (or use <c>aria-label</c> / <c>aria-labelledby</c>).
/// </summary>
public class BitBhRadio : BitBhComponentBase, IBitBhCollectionItem, IDisposable
{
    [CascadingParameter]
    private BitBhRadioGroupContext? GroupContext { get; set; }

    /// <summary>The value this radio represents.</summary>
    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    /// <summary>Disables this radio.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhRadioRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhRadioRenderContext>? Template { get; set; }

    protected override string DefaultTag => "button";

    /// <inheritdoc />
    public string ItemId => ComponentId;

    /// <inheritdoc />
    public bool IsItemDisabled => Disabled || (GroupContext?.Disabled ?? false);

    /// <inheritdoc />
    public string? ItemText => Value;

    /// <summary>Whether this radio is checked.</summary>
    public bool IsChecked => GroupContext?.SelectedValue == Value;

    protected override void OnInitialized() => GroupContext?.Radios.Register(this);

    public void Dispose() => GroupContext?.Radios.Unregister(this);

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
            builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, new BitBhRadioRenderContext { IsChecked = IsChecked, Disabled = IsItemDisabled }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "radio";
        SetAriaBool(attrs, "aria-checked", IsChecked);
        attrs["tabindex"] = GroupContext?.Group.IsTabStop(this) == true ? "0" : "-1";
        attrs[BitBhKeyboard.KeysAttribute] = "ArrowUp ArrowDown ArrowLeft ArrowRight Space Enter";
        if (GroupContext is not null)
            attrs["data-bit-bh-item"] = GroupContext.Radios.Key;

        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (IsItemDisabled) attrs["disabled"] = true;
        }
        else if (IsItemDisabled)
        {
            attrs["aria-disabled"] = "true";
        }

        SetDataState(attrs, IsChecked, "checked", "unchecked");
        SetDataFlag(attrs, "disabled", IsItemDisabled);
        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
        => IsItemDisabled || GroupContext is null ? Task.CompletedTask : GroupContext.SelectAsync(Value);

    private Task HandleKeyDown(KeyboardEventArgs e)
        => GroupContext is null || e.Key == "Enter" ? Task.CompletedTask : GroupContext.Group.HandleKeyDownAsync(this, e);
}
