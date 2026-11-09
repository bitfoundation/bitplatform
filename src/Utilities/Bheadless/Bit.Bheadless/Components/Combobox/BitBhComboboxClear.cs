using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A button that clears a <see cref="BitBhCombobox{TValue}"/> - its value (every value of a multiple one) and the typed
/// text - and puts the focus back in the input (Base UI <c>Combobox.Clear</c>, Ark's clear trigger). Named "Clear"
/// (<see cref="BitBhStrings.ClearValue"/>), out of the tab order (Escape in the input does the same from the keyboard),
/// disabled with <c>data-disabled</c> while there is nothing to clear.
/// </summary>
/// <typeparam name="TValue">The combobox's value type.</typeparam>
public class BitBhComboboxClear<TValue> : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhComboboxContext<TValue>? ComboboxContext { get; set; }

    /// <summary>The content (an icon, "×").</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    private bool IsDisabled => ComboboxContext is not { CanClear: true };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", () => ComboboxContext?.ClearAsync() ?? Task.CompletedTask));
        // The press leaves the focus in the input (the list doesn't close on a blur).
        builder.AddAttribute(31, "onmousedown", On<MouseEventArgs>("onmousedown", static () => { }));
        builder.AddEventPreventDefaultAttribute(32, "onmousedown", true);
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var disabled = IsDisabled;
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
        attrs["aria-label"] = Strings.ClearValue;
        if (ComboboxContext is not null) attrs["aria-controls"] = ComboboxContext.InputId;
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "disabled", disabled);
        return attrs;
    }
}
