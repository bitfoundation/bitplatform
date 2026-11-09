using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The handle between two <see cref="BitBhSplitterPanel"/>s of a <see cref="BitBhSplitter"/> - a focusable
/// window splitter (<c>role="separator"</c>, WAI-ARIA Window Splitter pattern). Its value is the size in percent
/// of the panel before it (<c>aria-valuenow</c>, with the range the handle can give that panel in
/// <c>aria-valuemin</c> / <c>aria-valuemax</c>), which it controls (<c>aria-controls</c>). Its
/// <c>aria-orientation</c> is that of the bar - vertical between side-by-side panels. Named "Resize panels"
/// (<see cref="BitBhStrings.ResizePanels"/>) unless you give it an <c>aria-label</c> / <c>aria-labelledby</c> -
/// best, the name of the panel it resizes ("Resize sidebar").
///
/// <para>
/// <b>Keyboard:</b> Left / Right (Up / Down in a vertical splitter; mirrored in RTL) resize by the splitter's
/// <c>KeyboardStep</c>, with Shift by its <c>LargeKeyboardStep</c>; Home / End give the panel before it its smallest
/// / largest size; Enter collapses or restores the collapsible panel next to it.
/// </para>
///
/// <para>
/// <b>Styling hooks:</b> <c>data-orientation</c> (the splitter's), <c>data-dragging</c>, <c>data-disabled</c>, and the
/// runtime's <c>data-hover</c> / <c>data-focus-visible</c>. Give it a thickness; a thin visible bar can sit in a
/// wider hit area (24px or more).
/// </para>
/// </summary>
public class BitBhSplitterHandle : BitBhComponentBase, IBitBhCollectionItem, IDisposable
{
    [CascadingParameter]
    private BitBhSplitter? Splitter { get; set; }

    /// <summary>Disables this handle: it leaves the tab order and can't be dragged.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Optional content (a grip).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <inheritdoc />
    public string ItemId => ComponentId;

    /// <inheritdoc />
    public bool IsItemDisabled => IsHandleDisabled;

    /// <inheritdoc />
    public string? ItemText => null;

    internal bool IsHandleDisabled => Disabled || (Splitter?.SplitterDisabled ?? false);

    protected override void OnInitialized() => Splitter?.HandleCollection.Register(this);

    /// <inheritdoc />
    public void Dispose() => Splitter?.HandleCollection.Unregister(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onkeydown",
            On<KeyboardEventArgs>("onkeydown", e => Splitter?.HandleKeyDownAsync(this, e) ?? Task.CompletedTask));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "separator";
        if (Splitter is null) return attrs;

        var orientation = Splitter.SplitterOrientation;
        var disabled = IsHandleDisabled;
        attrs["data-bit-bh-item"] = Splitter.HandleCollection.Key;
        attrs["data-bit-bh-splitter-handle"] = Splitter.RootId;
        // The bar between side-by-side panels is vertical, and the other way round (APG).
        attrs["aria-orientation"] = orientation == BitBhOrientation.Horizontal ? "vertical" : "horizontal";
        SetDataValue(attrs, "orientation", orientation.ToAttribute());
        SetDataFlag(attrs, "disabled", disabled);
        SetDataFlag(attrs, "dragging", Splitter.IsDragging(this));

        if (Splitter.StateOf(this) is { } state)
        {
            // A focusable separator is a widget: its value is the size of the panel before it.
            attrs["tabindex"] = disabled ? "-1" : "0";
            attrs["aria-valuenow"] = Format(state.Now);
            attrs["aria-valuemin"] = Format(state.Min);
            attrs["aria-valuemax"] = Format(state.Max);
            attrs["aria-controls"] = state.Before.ItemId;
            if (disabled) attrs["aria-disabled"] = "true";
            if (GetAdditionalAttribute("aria-label") is null && GetAdditionalAttribute("aria-labelledby") is null)
                attrs["aria-label"] = Strings.ResizePanels;
            var keys = orientation == BitBhOrientation.Horizontal ? "ArrowLeft ArrowRight Home End" : "ArrowUp ArrowDown Home End";
            if (BitBhSplitter.CollapseTarget(state) is not null) keys += " Enter";
            attrs[BitBhKeyboard.KeysAttribute] = keys;
            attrs["data-bit-bh-interactive"] = "";
        }
        else
        {
            // Not between two panels: a plain, static separator.
            SetDataFlag(attrs, "disabled", true);
        }
        // Functional: a touch drag resizes instead of panning the page, dragging never selects text, and the
        // handle keeps its thickness.
        attrs["style"] = "touch-action:none;user-select:none;-webkit-user-select:none;flex:0 0 auto";
        return attrs;
    }

    private static string Format(double value) => Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
}
