using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A two-state toggle button (<c>aria-pressed</c>) — e.g. "Bold" in an editor
/// toolbar. Supports <c>@bind-Pressed</c>. Emits <c>data-state="on|off"</c>.
///
/// <code>
/// &lt;BitBhToggle @bind-Pressed="bold" aria-label="Bold"&gt;B&lt;/BitBhToggle&gt;
/// </code>
/// </summary>
public class BitBhToggle : BitBhComponentBase
{
    private bool _pressed;

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    /// <summary>Whether the toggle is pressed. Supplying it makes it controlled (use <c>@bind-Pressed</c>).</summary>
    [Parameter]
    public bool Pressed { get; set; }

    /// <summary>Initial state when uncontrolled.</summary>
    [Parameter]
    public bool DefaultPressed { get; set; }

    /// <summary>Two-way binding callback for <see cref="Pressed"/>.</summary>
    [Parameter]
    public EventCallback<bool> PressedChanged { get; set; }

    /// <summary>Fires whenever the pressed state changes.</summary>
    [Parameter]
    public EventCallback<bool> OnPressedChange { get; set; }

    /// <summary>Disables the toggle.</summary>
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

    /// <summary>Whether the toggle is pressed.</summary>
    public bool IsPressed => IsProvided(nameof(Pressed)) ? Pressed : _pressed;

    private bool IsDisabled => Disabled || (FieldsetContext?.Disabled ?? false);

    protected override void OnInitialized() => _pressed = DefaultPressed;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            On<MouseEventArgs>("onclick", ToggleAsync));
        if (!Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
            builder.AddAttribute(31, "onkeydown",
                // A held key repeats: the state would flip back and forth.
                On<KeyboardEventArgs>("onkeydown", e => e.Key is "Enter" or " " && e.Repeat is false ? ToggleAsync() : Task.CompletedTask));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, IsPressed));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (IsDisabled) attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            attrs["tabindex"] = IsDisabled ? "-1" : "0";
            attrs[BitBhKeyboard.KeysAttribute] = "Enter Space";
            if (IsDisabled) attrs["aria-disabled"] = "true";
        }
        SetAriaBool(attrs, "aria-pressed", IsPressed);
        attrs["data-bit-bh-interactive"] = "";
        SetDataState(attrs, IsPressed, "on", "off");
        SetDataFlag(attrs, "disabled", IsDisabled);
        return attrs;
    }

    private async Task ToggleAsync()
    {
        if (IsDisabled) return;
        var next = !IsPressed;
        if (!IsProvided(nameof(Pressed))) _pressed = next;
        await PressedChanged.InvokeAsync(next);
        await OnPressedChange.InvokeAsync(next);
        StateHasChanged();
    }
}
