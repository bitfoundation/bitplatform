using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// An item of a <see cref="BitBhToggleGroup"/>: <c>role="radio"</c> + <c>aria-checked</c>
/// in a single group that can't be emptied, <c>aria-pressed</c> otherwise. Emits
/// <c>data-state="on|off"</c> and <c>data-disabled</c>.
/// </summary>
public class BitBhToggleGroupItem : BitBhRovingItemBase
{
    [CascadingParameter]
    private BitBhToggleGroupContext? Group { get; set; }

    /// <summary>The value this item represents.</summary>
    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    /// <summary>Disables this item.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>bool</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<bool>? Template { get; set; }

    protected override string DefaultTag => "button";

    private bool IsPressed => Group?.IsPressed(Value) ?? false;

    /// <inheritdoc />
    public override bool IsItemDisabled => Disabled || (Group?.Disabled ?? false);

    protected internal override bool PreferredTabStop => IsPressed;

    protected internal override Task OnRovedToAsync()
        => Group?.Group.SelectsOnFocus == true && !IsPressed ? ToggleAsync() : Task.CompletedTask;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ItemId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(25, "onclick",
            On<MouseEventArgs>("onclick", ToggleAsync));
        AddRovingHandlers(builder, 30);
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, IsPressed));
        builder.CloseElement();
    }

    // A held key repeats: the state would flip back and forth.
    protected override Task OnKeyDownAsync(KeyboardEventArgs e)
        => !Tag.Equals("button", StringComparison.OrdinalIgnoreCase) && e.Key is "Enter" or " " && e.Repeat is false ? ToggleAsync() : Task.CompletedTask;

    private Task ToggleAsync()
        => IsItemDisabled || Group is null ? Task.CompletedTask : Group.Group.ToggleAsync(Value);

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var pressed = IsPressed;
        var isButton = Tag.Equals("button", StringComparison.OrdinalIgnoreCase);

        if (Group?.Group.IsRadioGroup == true)
        {
            attrs["role"] = "radio";
            SetAriaBool(attrs, "aria-checked", pressed);
        }
        else
        {
            if (!isButton) attrs["role"] = "button";
            SetAriaBool(attrs, "aria-pressed", pressed);
        }

        if (isButton)
        {
            attrs["type"] = "button";
            if (IsItemDisabled) attrs["disabled"] = true;
        }
        else
        {
            if (IsItemDisabled) attrs["aria-disabled"] = "true";
            if (attrs.TryGetValue(BitBhKeyboard.KeysAttribute, out var keys))
                attrs[BitBhKeyboard.KeysAttribute] = $"{keys} Enter Space";
        }

        attrs["data-bit-bh-interactive"] = "";
        SetDataState(attrs, pressed, "on", "off");
        SetDataFlag(attrs, "disabled", IsItemDisabled);
        return attrs;
    }
}
