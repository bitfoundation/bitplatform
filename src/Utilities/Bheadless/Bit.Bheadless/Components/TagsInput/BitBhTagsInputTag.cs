using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// One tag of a <see cref="BitBhTagsInput"/> (<c>role="row"</c>, its content in a <c>role="gridcell"</c>),
/// identified by its <see cref="Index"/> in the tags. Focusable with the arrow keys; Backspace / Delete
/// remove it.
/// </summary>
public class BitBhTagsInputTag : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhTagsInputContext? Context { get; set; }

    /// <summary>The tag's position in the tags.</summary>
    [Parameter, EditorRequired]
    public int Index { get; set; }

    /// <summary>The content: the tag's text, a <see cref="BitBhTagsInputTagRemove"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private string? Value => Context?.Owner is { } owner && Index >= 0 && Index < owner.CurrentValues.Count ? owner.CurrentValues[Index] : null;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var owner = Context?.Owner;
        // A tag that was just removed is rendered once more by the cascade before the list drops it: its content
        // (often "@t.Values[index]") would read past the end of the tags.
        if (!IsInRange) return;
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", owner?.TagId(Index) ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (owner is not null)
            builder.AddAttribute(30, "onkeydown", On<KeyboardEventArgs>("onkeydown", e => e.CtrlKey || e.MetaKey || e.AltKey ? Task.CompletedTask : owner.HandleTagKeyDownAsync(Index, e.Key)));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.OpenElement(50, "span");
        builder.AddAttribute(51, "role", "gridcell");
        builder.OpenComponent<CascadingValue<BitBhTagsInputTag>>(52);
        builder.AddComponentParameter(53, "Value", this);
        builder.AddComponentParameter(54, "IsFixed", true);
        // Evaluated when the content renders, which can come after the tags changed: a removed tag's content is
        // skipped there too.
        builder.AddComponentParameter(55, "ChildContent", (RenderFragment)(content =>
        {
            if (IsInRange) content.AddContent(0, ChildContent);
        }));
        builder.CloseComponent();
        builder.CloseElement();
        builder.CloseElement();
    }

    private bool IsInRange => Context?.Owner is not { } owner || (Index >= 0 && Index < owner.CurrentValues.Count);

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Context?.Owner is not { } owner) return attrs;
        attrs["role"] = "row";
        attrs["tabindex"] = "-1";
        if (Value is { } value && GetAdditionalAttribute("aria-label") is null) attrs["aria-label"] = value;
        attrs[BitBhKeyboard.KeysAttribute] = "ArrowLeft ArrowRight Home End Backspace Delete";
        attrs["data-bit-bh-interactive"] = "";
        SetDataValue(attrs, "index", Index.ToString(CultureInfo.InvariantCulture));
        SetDataFlag(attrs, "disabled", owner.IsDisabled);
        SetDataFlag(attrs, "readonly", owner.ReadOnly);
        return attrs;
    }

    internal int TagIndex => Index;

    internal string? TagValue => Value;
}
