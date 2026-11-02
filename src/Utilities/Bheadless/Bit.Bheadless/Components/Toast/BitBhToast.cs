using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A single notification inside a <see cref="BitBhToastViewport"/>. Announced through the
/// shared live region (politely, or assertively for foreground toasts), auto-dismissed
/// after its duration — paused while hovered or focused — and dismissable with
/// Escape or a <see cref="BitBhToastClose"/>. Emits <c>data-state="open|closed"</c>
/// (exit animations supported).
/// </summary>
public class BitBhToast : BitBhComponentBase, IDisposable
{
    private BitBhDelayedAction? _timer;
    private bool _hovered;
    private bool _focused;
    private bool _closing;
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
    private bool Paused => _hovered || _focused;

    protected override void OnInitialized() => StartTimer();

    private void StartTimer()
    {
        if (Duration <= 0 || Paused) return;
        _timer ??= new BitBhDelayedAction(a => InvokeAsync(a));
        _timer.Schedule(Duration, DismissAsync);
    }

    /// <summary>Dismisses this toast (after its exit animation).</summary>
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

    private void UpdateTimer()
    {
        if (Paused) _timer?.Cancel();
        else if (!_closing) StartTimer();
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
        if (!firstRender) return;
        var text = string.Join(". ", new[] { Item.Title, Item.Description }.Where(t => !string.IsNullOrWhiteSpace(t)));
        if (text.Length == 0) return;
        var announcer = GetService<IBitBhAnnouncer>() ?? new BitBhAnnouncer(Interop);
        await announcer.AnnounceAsync(text, Item.Options.Foreground ? BitBhPoliteness.Assertive : BitBhPoliteness.Polite);
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _closedRendered?.TrySetResult();
    }
}
