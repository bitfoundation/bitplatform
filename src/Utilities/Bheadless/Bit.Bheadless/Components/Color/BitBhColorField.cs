using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A text input for a color (React Aria ColorField): shows the color as hex, and takes hex, <c>rgb()</c> or <c>hsl()</c> when it
/// is left or Enter is pressed; text that isn't a color reverts. Inside a <see cref="BitBhField"/> it takes the field's id.
/// </summary>
public class BitBhColorField : BitBhComponentBase
{
    private BitBhColor? _value;
    private string? _text;

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    /// <summary>The color (null when empty). Supplying it makes the field controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public BitBhColor? Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<BitBhColor?> ValueChanged { get; set; }

    /// <summary>Initial color when uncontrolled.</summary>
    [Parameter]
    public BitBhColor? DefaultValue { get; set; }

    /// <summary>Shows the alpha channel in the hex (<c>#rrggbbaa</c>) even when opaque. Default: only when not opaque.</summary>
    [Parameter]
    public bool ShowAlpha { get; set; }

    /// <summary>Disables the field.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    protected override string DefaultTag => "input";

    /// <summary>The current color.</summary>
    public BitBhColor? CurrentValue => IsProvided(nameof(Value)) ? Value : _value;

    private bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false) || (FieldsetContext?.Disabled ?? false);

    protected override void OnInitialized() => _value = DefaultValue;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", Id ?? FieldContext?.ControlId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "value", _text ?? CurrentValue?.ToHex(ShowAlpha ? true : null) ?? string.Empty);
        builder.AddAttribute(31, "oninput", On<ChangeEventArgs>("oninput", e => { _text = e.Value?.ToString(); }));
        builder.SetUpdatesAttributeName("value");
        builder.AddAttribute(32, "onblur", On<FocusEventArgs>("onblur", CommitAsync));
        builder.AddAttribute(33, "onkeydown", On<KeyboardEventArgs>("onkeydown", e => e.Key == "Enter" ? CommitAsync() : Task.CompletedTask));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Tag.Equals("input", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "text";
        attrs["autocomplete"] = "off";
        attrs["autocorrect"] = "off";
        attrs["spellcheck"] = "false";
        if (IsDisabled) attrs["disabled"] = true;
        if (FieldContext?.DescribedBy is { } describedBy) attrs["aria-describedby"] = describedBy;
        if (CurrentValue is { } color) attrs["style"] = $"--bit-bh-color:{color.ToHex()}";
        SetDataFlag(attrs, "disabled", IsDisabled);
        return attrs;
    }

    private async Task CommitAsync()
    {
        if (_text is not { } text) return;
        _text = null;
        BitBhColor? next;
        if (string.IsNullOrWhiteSpace(text)) next = null;
        else if (BitBhColor.TryParse(text, out var parsed)) next = parsed;
        else
        {
            // Not a color: the input goes back to the color it had.
            StateHasChanged();
            return;
        }
        if (next == CurrentValue) { StateHasChanged(); return; }
        if (!IsProvided(nameof(Value))) _value = next;
        await ValueChanged.InvokeAsync(next);
        StateHasChanged();
    }
}
