using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A single notification inside a <see cref="BitBhToastViewport"/>. Announced through the
/// shared live region (politely, or assertively for foreground toasts), auto-dismissed
/// after its duration — paused while hovered or focused, while the viewport is expanded and
/// while the page is hidden, then resumed with the time it had left — and dismissable with
/// Escape, a swipe (see <see cref="BitBhToastViewport.SwipeDirection"/>) or a <see cref="BitBhToastClose"/>.
/// A toast updated through <see cref="BitBhToaster.Update(string, BitBhToastOptions)"/> starts its timer over and
/// announces itself again.
///
/// <para><b>Styling hooks:</b> <c>data-state="open|closed"</c> (exit animations supported),
/// <c>data-variant</c>, <c>data-type="foreground|background"</c>, <c>data-paused</c>; set by the viewport's runtime:
/// <c>data-front</c> (the newest toast), <c>data-index</c> and <c>--bit-bh-toast-index</c> (0 = newest),
/// <c>--bit-bh-toast-offset</c> (height of the newer toasts plus gaps) and <c>--bit-bh-toast-height</c> for
/// stacked layouts, and while swiped <c>data-swipe="move|cancel|end"</c>, <c>data-swipe-direction</c>,
/// <c>--bit-bh-toast-swipe-move-x/y</c>, <c>--bit-bh-toast-swipe-end-x/y</c>.</para>
/// </summary>
public class BitBhToast : BitBhComponentBase, IDisposable
{
    private readonly Stopwatch _clock = new();
    private BitBhDelayedAction? _timer;
    private bool _hovered;
    private bool _focused;
    private bool _closing;
    private int _remaining = -1;
    private int _version = -1;
    private bool _announce;
    private bool _registered;
    private TaskCompletionSource? _closedRendered;

    [CascadingParameter]
    private BitBhToastViewport? Viewport { get; set; }

    /// <summary>The toast to render (the viewport template's context).</summary>
    [Parameter, EditorRequired]
    public BitBhToastItem Item { get; set; } = default!;

    /// <summary>Title, description, action and close parts.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "li";

    private int Duration => Item.Options.Duration ?? Viewport?.Duration ?? 5000;

    // Hover and focus pause independently: the pointer leaving must not restart the timer
    // while keyboard focus is still inside.
    private bool Paused => _hovered || _focused || (Viewport?.PauseAll ?? false);

    protected override void OnParametersSet()
    {
        if (!_registered && Viewport is not null)
        {
            Viewport.Register(ComponentId, this);
            _registered = true;
        }
        if (_version == Item.Version) return;
        // A new toast, or one updated in place: a full duration, and an announcement of what it says now.
        _version = Item.Version;
        _remaining = Duration;
        _announce = true;
        _timer?.Cancel();
        _clock.Reset();
        if (!_closing) StartTimer();
    }

    private void StartTimer()
    {
        if (_remaining <= 0 || Paused || Duration <= 0) return;
        _timer ??= new BitBhDelayedAction(a => InvokeAsync(a));
        _clock.Restart();
        _timer.Schedule(_remaining, DismissAsync);
    }

    private void PauseTimer()
    {
        if (_timer?.IsPending != true) return;
        _timer.Cancel();
        _remaining = Math.Max(1, _remaining - (int)_clock.ElapsedMilliseconds);
        _clock.Reset();
    }

    /// <summary>Dismisses the toast: it plays its exit animation, then leaves the toaster.</summary>
    public async Task DismissAsync()
    {
        if (_closing) return;
        _closing = true;
        _timer?.Cancel();
        // From an event handler the render is deferred until the handler yields, so wait for
        // data-state="closed" to reach the DOM before looking for its exit animation.
        _closedRendered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StateHasChanged();
        await _closedRendered.Task;
        await Interop.WaitForExitAsync(ComponentId);
        // Removing the toast that holds the focus would drop it to <body>: the list keeps it instead, so the
        // next Tab or F8 carries on from the toasts.
        if (_focused && Viewport is not null)
            await Viewport.FocusListAsync();
        Viewport?.EffectiveToaster.Dismiss(Item.Id);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onpointerenter", On<PointerEventArgs>("onpointerenter", () => SetHovered(true)));
        builder.AddAttribute(31, "onpointerleave", On<PointerEventArgs>("onpointerleave", () => SetHovered(false)));
        builder.AddAttribute(32, "onfocusin", On<FocusEventArgs>("onfocusin", () => SetFocused(true)));
        builder.AddAttribute(33, "onfocusout", On<FocusEventArgs>("onfocusout", () => SetFocused(false)));
        builder.AddAttribute(34, "onkeydown", On<KeyboardEventArgs>("onkeydown", e => e.Key == "Escape" ? DismissAsync() : Task.CompletedTask));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhToast>>(50);
        builder.AddComponentParameter(51, "Value", this);
        builder.AddComponentParameter(52, "IsFixed", true);
        builder.AddComponentParameter(53, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["tabindex"] = "0";
        attrs["data-type"] = Item.Options.Foreground ? "foreground" : "background";
        // The viewport's runtime finds the toasts it lays out and lets be swiped by this attribute.
        attrs["data-bit-bh-toast"] = Item.Id;
        if (Item.Variant is { Length: > 0 } variant) attrs["data-variant"] = variant;
        if (Viewport?.SwipeDirection is { } swipe) attrs["data-swipe-direction"] = BitBhToastViewport.ToAttribute(swipe);
        SetDataState(attrs, !_closing);
        SetDataFlag(attrs, "paused", Paused);
        return attrs;
    }

    private void SetHovered(bool hovered)
    {
        _hovered = hovered;
        UpdateTimer();
    }

    private void SetFocused(bool focused)
    {
        _focused = focused;
        UpdateTimer();
    }

    /// <summary>Called by the viewport when it pauses or resumes every toast.</summary>
    internal void UpdateTimer()
    {
        if (_closing) return;
        if (Paused) PauseTimer();
        else if (_timer?.IsPending != true) StartTimer();
        StateHasChanged();
    }

    internal async Task RunActionAsync()
    {
        if (Item.Options.OnAction is not null) await Item.Options.OnAction();
        await DismissAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // data-state="closed" is on the page now: a pending dismissal can look for its exit animation.
        _closedRendered?.TrySetResult();

        // Newly inserted live regions are unreliable across screen readers, so the
        // toast is announced through the shared announcer (which exists up front).
        if (!_announce) return;
        _announce = false;
        var text = string.Join(". ", new[] { Item.Title, Item.Description }.Where(t => !string.IsNullOrWhiteSpace(t)));
        if (text.Length == 0) return;
        var announcer = GetService<IBitBhAnnouncer>() ?? new BitBhAnnouncer(Interop);
        await announcer.AnnounceAsync(text, Item.Options.Foreground ? BitBhPoliteness.Assertive : BitBhPoliteness.Polite);
    }

    public void Dispose()
    {
        if (_registered) Viewport?.Unregister(ComponentId, this);
        _timer?.Dispose();
        _closedRendered?.TrySetResult();
    }
}
