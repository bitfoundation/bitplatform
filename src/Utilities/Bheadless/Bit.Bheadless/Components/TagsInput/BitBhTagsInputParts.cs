using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The container of a <see cref="BitBhTagsInput"/>'s tags: a <c>role="grid"</c> whose rows are the
/// <see cref="BitBhTagsInputTag"/>s (<c>role="group"</c> while there are none). Named by the field's label
/// inside a <see cref="BitBhField"/>, otherwise give it an <c>aria-label</c> (default: "Tags").
/// </summary>
public class BitBhTagsInputList : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhTagsInputContext? Context { get; set; }

    /// <summary>The tags.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Context?.Owner is not { } owner) return attrs;
        var empty = owner.CurrentValues.Count == 0;
        attrs["role"] = empty ? "group" : "grid";
        if (GetAdditionalAttribute("aria-label") is null && GetAdditionalAttribute("aria-labelledby") is null)
        {
            if (owner.Field?.LabelledBy is { } label) attrs["aria-labelledby"] = label;
            else attrs["aria-label"] = Strings.Tags;
        }
        SetDataFlag(attrs, "empty", empty);
        SetDataFlag(attrs, "disabled", owner.IsDisabled);
        return attrs;
    }
}

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

/// <summary>
/// The remove button of a <see cref="BitBhTagsInputTag"/>: named "Remove {tag}", out of the tab order (the
/// focused tag's Backspace / Delete do the same from the keyboard).
/// </summary>
public class BitBhTagsInputTagRemove : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhTagsInputContext? Context { get; set; }

    [CascadingParameter]
    private BitBhTagsInputTag? TagPart { get; set; }

    /// <summary>The content (an icon, "×").</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", RemoveAsync));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var disabled = Context?.Owner is not { CanEdit: true };
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (disabled) attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            if (disabled) attrs["aria-disabled"] = "true";
        }
        attrs["tabindex"] = "-1";
        attrs["aria-label"] = Strings.RemoveTag(TagPart?.TagValue ?? string.Empty).Trim();
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "disabled", disabled);
        return attrs;
    }

    private async Task RemoveAsync()
    {
        if (Context?.Owner is not { CanEdit: true } owner || TagPart is null) return;
        await owner.RemoveAtAsync(TagPart.TagIndex);
        await Interop.FocusAsync(owner.InputId);
    }
}

/// <summary>
/// The text input of a <see cref="BitBhTagsInput"/>. Enter or a delimiter adds what was typed; ← or Backspace
/// while it is empty moves to the last tag. Inside a <see cref="BitBhField"/> it takes the field's id, so the
/// field's label names it.
/// </summary>
public class BitBhTagsInputInput : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhTagsInputContext? Context { get; set; }

    protected override string DefaultTag => "input";

    protected override void OnParametersSet() => Context?.Owner.SetInputId(Id);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var owner = Context?.Owner;
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", owner?.InputId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (owner is not null)
        {
            builder.AddAttribute(30, "value", owner.Text);
            builder.AddAttribute(31, "oninput", On<ChangeEventArgs>("oninput", e => owner.OnTextInputAsync(e.Value?.ToString())));
            builder.SetUpdatesAttributeName("value");
            builder.AddAttribute(32, "onkeydown", On<KeyboardEventArgs>("onkeydown", e => e.CtrlKey || e.MetaKey || e.AltKey || e.IsComposing ? Task.CompletedTask : owner.HandleInputKeyDownAsync(e.Key)));
            builder.AddAttribute(33, "onblur", On<FocusEventArgs>("onblur", owner.HandleInputBlurAsync));
        }
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Context?.Owner is not { } owner) return attrs;
        if (Tag.Equals("input", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "text";
        attrs["autocomplete"] = "off";
        if (owner.IsDisabled) attrs["disabled"] = true;
        if (owner.ReadOnly) attrs["readonly"] = true;
        if (owner.IsInvalid) attrs["aria-invalid"] = "true";
        if (owner.IsRequired && owner.CurrentValues.Count == 0) attrs["aria-required"] = "true";
        if (owner.Field?.DescribedBy is { } describedBy) attrs["aria-describedby"] = describedBy;
        // Enter adds the typed text instead of submitting the form - only while there is text to add.
        if (owner.Text.Trim().Length > 0) attrs[BitBhKeyboard.KeysAttribute] = "Enter";
        SetDataFlag(attrs, "disabled", owner.IsDisabled);
        SetDataFlag(attrs, "readonly", owner.ReadOnly);
        SetDataFlag(attrs, "invalid", owner.IsInvalid);
        SetDataFlag(attrs, "max-reached", owner.IsMaxReached);
        return attrs;
    }
}

/// <summary>A button that removes every tag of a <see cref="BitBhTagsInput"/> (named "Clear all tags", out of the tab order).</summary>
public class BitBhTagsInputClear : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhTagsInputContext? Context { get; set; }

    /// <summary>The content (an icon, "Clear").</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", () => Context?.ClearAsync() ?? Task.CompletedTask));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var disabled = Context?.Owner is not { CanEdit: true } owner || owner.CurrentValues.Count == 0;
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (disabled) attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            if (disabled) attrs["aria-disabled"] = "true";
        }
        attrs["tabindex"] = "-1";
        attrs["aria-label"] = Strings.ClearTags;
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "disabled", disabled);
        return attrs;
    }
}
