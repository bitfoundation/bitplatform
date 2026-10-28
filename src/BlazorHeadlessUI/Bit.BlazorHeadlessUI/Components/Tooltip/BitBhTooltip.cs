using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A headless, accessible Tooltip — a small popup that describes a control when
/// it is hovered or focused (WAI-ARIA Tooltip pattern, Radix-style API).
///
/// <para><b>Anatomy:</b></para>
/// <code>
/// &lt;BitBhTooltipProvider&gt;       @* optional, configures timings globally *@
///     &lt;BitBhTooltip&gt;
///         &lt;BitBhTooltipTrigger /&gt;
///         &lt;BitBhTooltipContent&gt;
///             &lt;BitBhTooltipArrow /&gt;  @* optional *@
///         &lt;/BitBhTooltipContent&gt;
///     &lt;/BitBhTooltip&gt;
/// &lt;/BitBhTooltipProvider&gt;
/// </code>
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Pointer and keyboard</b> — opens on hover (after a delay) or keyboard focus; not on the focus caused by a click.</item>
///   <item><b>WCAG 1.4.13</b> — dismissable with Escape without moving the pointer or focus, hoverable (the pointer can move into the content), persistent while hovered.</item>
///   <item><b>Delay coordination</b> — <see cref="BitBhTooltipProvider"/> skips the delay when moving quickly between triggers.</item>
///   <item><b>Accessible description</b> — the trigger references the content through <c>aria-describedby</c>.</item>
///   <item><b>Data attributes</b> — <c>data-state="closed" | "delayed-open" | "instant-open"</c>, plus <c>data-side</c> / <c>data-align</c> when anchored.</item>
/// </list>
/// </summary>
public class BitBhTooltip : BitBhComponentBase, IDisposable
{
    private bool _open;
    private bool _delayedOpen;
    private bool _hasContent;
    private BitBhDelayedAction? _timer;

    [CascadingParameter]
    private BitBhTooltipProviderContext? ProviderContext { get; set; }

    /// <summary>Whether the tooltip is open. Supplying it makes the tooltip controlled (use <c>@bind-Open</c>).</summary>
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

    /// <summary>Delay (ms) before opening on hover. Falls back to the provider, then 700.</summary>
    [Parameter]
    public int? DelayDuration { get; set; }

    /// <summary>When true, moving the pointer from the trigger towards the content closes the tooltip.</summary>
    [Parameter]
    public bool? DisableHoverableContent { get; set; }

    /// <summary>Prevents the tooltip from opening.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Child content. Should contain a <see cref="BitBhTooltipTrigger"/> and a <see cref="BitBhTooltipContent"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => IsProvided(nameof(Open)) ? Open : _open;
    private int EffectiveDelay => DelayDuration ?? ProviderContext?.DelayDuration ?? 700;
    private bool EffectiveDisableHoverable => DisableHoverableContent ?? ProviderContext?.DisableHoverableContent ?? false;

    private BitBhDelayedAction Timer => _timer ??= new BitBhDelayedAction(a => InvokeAsync(a));

    protected override void OnInitialized() => _open = DefaultOpen;

    public void Dispose() => _timer?.Dispose();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BitBhTooltipContext(
            isOpen: IsOpen,
            delayedOpen: _delayedOpen,
            baseId: ComponentId,
            disableHoverableContent: EffectiveDisableHoverable,
            hasContent: _hasContent,
            setOpenAsync: open => SetOpenAsync(open, delayed: false),
            scheduleOpenAsync: ScheduleOpenAsync,
            scheduleCloseAsync: ScheduleCloseAsync,
            registerContent: RegisterContent);

        builder.OpenComponent<CascadingValue<BitBhTooltipContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());
            inner.AddElementReferenceCapture(30, CaptureElement);
            inner.AddContent(40, ChildContent);
            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["data-state"] = !IsOpen ? "closed" : _delayedOpen ? "delayed-open" : "instant-open";
        // Wrapper only; doesn't affect layout of inline triggers.
        if (GetAdditionalAttribute("style") is null && GetAdditionalAttribute("class") is null)
            attrs["style"] = "display:contents";
        return attrs;
    }

    private void RegisterContent(bool present)
    {
        if (_hasContent == present) return;
        _hasContent = present;
        StateHasChanged();
    }

    private Task ScheduleOpenAsync()
    {
        if (Disabled) return Task.CompletedTask;
        if (IsOpen)
        {
            Timer.Cancel();
            return Task.CompletedTask;
        }
        // Skip the delay when another tooltip closed a moment ago.
        var delay = ProviderContext?.IsRecentlyClosed() == true ? 0 : EffectiveDelay;
        Timer.Schedule(delay, () => SetOpenAsync(true, delayed: delay > 0));
        return Task.CompletedTask;
    }

    private Task ScheduleCloseAsync()
    {
        if (!IsOpen)
        {
            Timer.Cancel();
            return Task.CompletedTask;
        }
        // A short grace period lets the pointer travel into hoverable content.
        Timer.Schedule(EffectiveDisableHoverable ? 0 : 120, () => SetOpenAsync(false, delayed: false));
        return Task.CompletedTask;
    }

    private async Task SetOpenAsync(bool value, bool delayed)
    {
        Timer.Cancel();
        if (value && Disabled) return;
        if (IsOpen == value && _delayedOpen == (value && delayed)) return;

        _delayedOpen = value && delayed;
        if (!IsProvided(nameof(Open)))
            _open = value;
        if (!value)
            ProviderContext?.NotifyClosed();

        await OpenChanged.InvokeAsync(value);
        await OnOpenChange.InvokeAsync(value);
        StateHasChanged();
    }
}
