using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A headless Disclosure — a single show/hide region toggled by a button
/// (WAI-ARIA Disclosure pattern, Headless UI-style API). The simpler sibling of
/// <see cref="BitBhAccordion"/>; equivalent to <see cref="BitBhCollapsible"/>.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Binding</b> — <c>@bind-Open</c>, or uncontrolled with <see cref="DefaultOpen"/>.</item>
///   <item><b>ARIA</b> — <c>aria-expanded</c> / <c>aria-controls</c> wired by the cascading context.</item>
///   <item><b>Render-prop context</b> — <c>IsOpen</c> and <c>Close()</c> for state-driven UI.</item>
///   <item><b>Animations</b> — exit animations and <c>--bit-bh-content-height</c> on the panel.</item>
/// </list>
///
/// <code>
/// &lt;BitBhDisclosure&gt;
///     &lt;BitBhDisclosureButton Context="d"&gt;@(d.IsOpen ? "Hide" : "Show")&lt;/BitBhDisclosureButton&gt;
///     &lt;BitBhDisclosurePanel&gt;Content here.&lt;/BitBhDisclosurePanel&gt;
/// &lt;/BitBhDisclosure&gt;
/// </code>
/// </summary>
public class BitBhDisclosure : BitBhComponentBase
{
    private bool _open;

    /// <summary>Whether the panel is open. Supplying it makes the disclosure controlled (use <c>@bind-Open</c>).</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Initial open state when uncontrolled.</summary>
    [Parameter]
    public bool DefaultOpen { get; set; }

    /// <summary>Two-way binding callback for <see cref="Open"/>.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Fires whenever the open state changes.</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>Prevents toggling.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>A <see cref="BitBhDisclosureButton"/> and a <see cref="BitBhDisclosurePanel"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => IsProvided(nameof(Open)) ? Open : _open;

    protected override void OnInitialized() => _open = DefaultOpen;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BitBhDisclosureContext
        {
            IsOpen = IsOpen,
            Disabled = Disabled,
            ButtonId = $"{ComponentId}-button",
            PanelId = $"{ComponentId}-panel",
            Toggle = () => _ = ToggleAsync(),
            Close = () => _ = CloseAsync(),
            ToggleAsync = ToggleAsync,
            CloseAsync = CloseAsync,
        };

        builder.OpenComponent<CascadingValue<IBitBhCloseContext>>(0);
        builder.AddComponentParameter(1, "Value", (IBitBhCloseContext)context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(closeOuter =>
        {
            closeOuter.OpenComponent<CascadingValue<BitBhDisclosureContext>>(0);
            closeOuter.AddComponentParameter(1, "Value", context);
            closeOuter.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenElement(0, Tag);
                inner.AddAttribute(10, "id", ComponentId);
                inner.AddMultipleAttributes(20, GetFinalAttributes());
                inner.AddElementReferenceCapture(30, CaptureElement);
                inner.AddContent(40, ChildContent);
                inner.CloseElement();
            }));
            closeOuter.CloseComponent();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    private Task ToggleAsync() => Disabled ? Task.CompletedTask : SetOpenAsync(!IsOpen);

    private Task CloseAsync() => IsOpen ? SetOpenAsync(false) : Task.CompletedTask;

    private async Task SetOpenAsync(bool value)
    {
        if (!IsProvided(nameof(Open)))
            _open = value;
        await OpenChanged.InvokeAsync(value);
        await OnOpenChange.InvokeAsync(value);
        StateHasChanged();
    }
}
