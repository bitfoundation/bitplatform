using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A headless Hover Card — surfaces preview content for sighted users when they
/// hover or focus a link (Radix-style API).
///
/// <para><b>Anatomy:</b></para>
/// <code>
/// &lt;BitBhHoverCard&gt;
///     &lt;BitBhHoverCardTrigger href="/users/ada" /&gt;
///     &lt;BitBhHoverCardContent Anchor="@("bottom")"&gt;
///         &lt;BitBhHoverCardArrow /&gt;  @* optional *@
///     &lt;/BitBhHoverCardContent&gt;
/// &lt;/BitBhHoverCard&gt;
/// </code>
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Hover and focus</b> with <see cref="OpenDelay"/> / <see cref="CloseDelay"/>; touch is ignored so tapping the link just follows it.</item>
///   <item><b>Hoverable content</b> — the pointer can move into the card; it closes once the pointer leaves both.</item>
///   <item><b>Dismissable</b> — Escape closes it from anywhere; so does pressing outside.</item>
///   <item><b>Progressive enhancement</b> — the trigger stays a normal link; the card holds supplementary content only.</item>
/// </list>
/// </summary>
public class BitBhHoverCard : BitBhComponentBase, IDisposable
{
    private bool _open;
    private BitBhDelayedAction? _timer;

    /// <summary>Whether the card is open. Supplying it makes the card controlled (use <c>@bind-Open</c>).</summary>
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

    /// <summary>Delay (ms) before opening. Default: 700.</summary>
    [Parameter]
    public int OpenDelay { get; set; } = 700;

    /// <summary>Delay (ms) before closing once the pointer / focus leaves. Default: 300.</summary>
    [Parameter]
    public int CloseDelay { get; set; } = 300;

    /// <summary>Child content. Should contain a <see cref="BitBhHoverCardTrigger"/> and a <see cref="BitBhHoverCardContent"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => IsProvided(nameof(Open)) ? Open : _open;

    private BitBhDelayedAction Timer => _timer ??= new BitBhDelayedAction(a => InvokeAsync(a));

    protected override void OnInitialized() => _open = DefaultOpen;

    public void Dispose() => _timer?.Dispose();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BitBhHoverCardContext(
            isOpen: IsOpen,
            baseId: ComponentId,
            scheduleOpenAsync: ScheduleOpenAsync,
            scheduleCloseAsync: ScheduleCloseAsync,
            setOpenAsync: SetOpenAsync);

        builder.OpenComponent<CascadingValue<BitBhHoverCardContext>>(0);
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
        SetDataState(attrs, IsOpen);
        if (GetAdditionalAttribute("style") is null && GetAdditionalAttribute("class") is null)
            attrs["style"] = "display:contents";
        return attrs;
    }

    private Task ScheduleOpenAsync()
    {
        if (IsOpen)
            Timer.Cancel();
        else
            Timer.Schedule(OpenDelay, () => SetOpenAsync(true));
        return Task.CompletedTask;
    }

    private Task ScheduleCloseAsync()
    {
        if (!IsOpen)
            Timer.Cancel();
        else
            Timer.Schedule(CloseDelay, () => SetOpenAsync(false));
        return Task.CompletedTask;
    }

    private async Task SetOpenAsync(bool value)
    {
        Timer.Cancel();
        if (IsOpen == value) return;
        if (!IsProvided(nameof(Open)))
            _open = value;
        await OpenChanged.InvokeAsync(value);
        await OnOpenChange.InvokeAsync(value);
        StateHasChanged();
    }
}
