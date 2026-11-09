using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// One position of a <see cref="BitBhPinInput"/>: a single-character <c>&lt;input&gt;</c> named
/// "Character 2 of 6" (see <see cref="BitBhPinInput.SlotLabel"/>). Positions are numbered in the order they
/// render. Only the first empty position is in the tab order.
/// </summary>
public class BitBhPinInputSlot : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhPinInputContext? Context { get; set; }

    protected override string DefaultTag => "input";

    private BitBhPinInput? Owner => Context?.Owner;

    private int Index => Owner?.IndexOf(this) ?? -1;

    protected override void OnInitialized() => Owner?.Register(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var index = Index;
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", Owner is { } owner && index >= 0 ? owner.SlotId(index) : ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (Owner is { } o)
        {
            builder.AddAttribute(30, "value", o.CharAt(index));
            builder.AddAttribute(31, "oninput", On<ChangeEventArgs>("oninput", e => o.HandleInputAsync(Index, e.Value?.ToString())));
            builder.SetUpdatesAttributeName("value");
            builder.AddAttribute(32, "onkeydown", On<KeyboardEventArgs>("onkeydown", e => e.CtrlKey || e.MetaKey || e.AltKey ? Task.CompletedTask : o.HandleKeyDownAsync(Index, e.Key)));
            builder.AddAttribute(33, "onfocus", On<FocusEventArgs>("onfocus", () => o.HandleFocusAsync(Index)));
        }
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Owner is not { } owner) return attrs;
        var index = Index;

        if (Tag.Equals("input", StringComparison.OrdinalIgnoreCase)) attrs["type"] = owner.Mask ? "password" : "text";
        attrs["inputmode"] = owner.Pattern is null && owner.Type == BitBhPinInputType.Numeric ? "numeric" : "text";
        attrs["autocomplete"] = owner.Otp && index == 0 ? "one-time-code" : "off";
        attrs["autocapitalize"] = owner.UpperCase ? "characters" : "off";
        attrs["autocorrect"] = "off";
        attrs["spellcheck"] = "false";
        attrs["aria-label"] = owner.SlotName(index);
        attrs["tabindex"] = index == owner.TabStopIndex ? "0" : "-1";
        if (!string.IsNullOrEmpty(owner.Placeholder)) attrs["placeholder"] = owner.Placeholder;
        if (owner.IsDisabled) attrs["disabled"] = true;
        if (owner.ReadOnly) attrs["readonly"] = true;
        if (owner.IsRequired && index == 0) attrs["required"] = true;
        if (owner.IsInvalid) attrs["aria-invalid"] = "true";
        attrs[BitBhKeyboard.KeysAttribute] = "ArrowLeft ArrowRight Home End";
        // The runtime refuses typed characters the pattern doesn't allow and selects the character on focus,
        // so typing replaces it.
        attrs["data-bit-bh-pattern"] = owner.CharPattern;
        if (owner.SelectOnFocus) attrs["data-bit-bh-select-on-focus"] = "";

        var filled = owner.CharAt(index).Length > 0;
        SetDataValue(attrs, "index", index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetDataFlag(attrs, "filled", filled);
        SetDataFlag(attrs, "complete", owner.IsComplete);
        SetDataFlag(attrs, "disabled", owner.IsDisabled);
        SetDataFlag(attrs, "invalid", owner.IsInvalid);
        return attrs;
    }

    public void Dispose() => Owner?.Unregister(this);
}
