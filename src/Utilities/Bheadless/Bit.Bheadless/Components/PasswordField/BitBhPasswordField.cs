using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A headless password input with a show / hide toggle (Radix PasswordToggleField, Ark PasswordInput).
///
/// <para>The toggle is named "Show password" / "Hide password" and controls the input. Toggled with a pointer, the focus
/// goes back to the input (the caret at its end) so typing carries on; toggled from the keyboard, it stays on the
/// toggle. The input gets <c>autocomplete="current-password"</c> (set <c>autocomplete="new-password"</c> on a sign-up form),
/// and never spell-checks or capitalizes.</para>
///
/// <code>
/// &lt;BitBhField&gt;
///     &lt;BitBhLabel&gt;Password&lt;/BitBhLabel&gt;
///     &lt;BitBhPasswordField class="password"&gt;
///         &lt;BitBhPasswordFieldInput @bind-Value="model.Password" /&gt;
///         &lt;BitBhPasswordFieldToggle&gt;
///             &lt;Template Context="visible"&gt;@(visible ? "🙈" : "👁")&lt;/Template&gt;
///         &lt;/BitBhPasswordFieldToggle&gt;
///     &lt;/BitBhPasswordField&gt;
/// &lt;/BitBhField&gt;
/// </code>
/// <para><b>Styling hooks:</b> <c>data-visible</c> on the root, the input and the toggle.</para>
/// </summary>
public class BitBhPasswordField : BitBhComponentBase
{
    private bool _visible;

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    /// <summary>Whether the password is shown. Supplying it makes the state controlled (use <c>@bind-Visible</c>).</summary>
    [Parameter]
    public bool Visible { get; set; }

    /// <summary>Two-way binding callback for <see cref="Visible"/>.</summary>
    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    /// <summary>Initial state when uncontrolled.</summary>
    [Parameter]
    public bool DefaultVisible { get; set; }

    /// <summary>The input and the toggle.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>Whether the password is shown.</summary>
    public bool IsVisible => IsProvided(nameof(Visible)) ? Visible : _visible;

    internal string InputId => FieldContext?.ControlId ?? $"{ComponentId}-input";

    protected override void OnInitialized() => _visible = DefaultVisible;

    /// <summary>Shows or hides the password.</summary>
    public async Task SetVisibleAsync(bool visible)
    {
        if (visible == IsVisible) return;
        if (!IsProvided(nameof(Visible))) _visible = visible;
        await VisibleChanged.InvokeAsync(visible);
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhPasswordField>>(30);
        builder.AddComponentParameter(31, "Value", this);
        builder.AddComponentParameter(32, "IsFixed", false);
        builder.AddComponentParameter(33, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataFlag(attrs, "visible", IsVisible);
        return attrs;
    }
}
