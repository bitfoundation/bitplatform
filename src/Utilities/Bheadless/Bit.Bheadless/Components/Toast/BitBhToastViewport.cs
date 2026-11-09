using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Bit.Bheadless;

/// <summary>The direction a toast is swiped to dismiss it.</summary>
public enum BitBhToastSwipeDirection
{
    /// <summary>Toward the right edge.</summary>
    Right,

    /// <summary>Toward the left edge.</summary>
    Left,

    /// <summary>Toward the top edge.</summary>
    Up,

    /// <summary>Toward the bottom edge.</summary>
    Down,
}

/// <summary>
/// The landmark region where toasts from a <see cref="BitBhToaster"/> appear: a
/// <c>role="region"</c> labelled "Notifications (F8)" wrapping the toast list
/// (<c>&lt;ol&gt;</c>, which receives your attributes). F8 moves focus to the list so
/// keyboard users can reach toast actions; Escape dismisses the focused toast.
/// Render each toast with the template — typically a <see cref="BitBhToast"/>.
///
/// <code>
/// &lt;BitBhToastViewport class="toasts" Context="t"&gt;
///     &lt;BitBhToast Item="t" class="toast"&gt;
///         &lt;BitBhToastTitle&gt;@t.Title&lt;/BitBhToastTitle&gt;
///         &lt;BitBhToastDescription&gt;@t.Description&lt;/BitBhToastDescription&gt;
///         &lt;BitBhToastClose aria-label="Dismiss"&gt;×&lt;/BitBhToastClose&gt;
///     &lt;/BitBhToast&gt;
/// &lt;/BitBhToastViewport&gt;
/// </code>
///
/// <para><b>Stacking, swiping and pausing</b> run in the browser: every toast gets its stack position
/// (<c>--bit-bh-toast-index</c>, <c>--bit-bh-toast-offset</c>, <c>--bit-bh-toast-height</c>, <c>data-front</c>) and the list
/// <c>--bit-bh-toast-count</c>, <c>--bit-bh-toast-front-height</c> and <c>data-expanded</c> while it is hovered or holds the focus -
/// enough to draw a collapsed stack (Sonner-style) with CSS alone. A toast is swiped away in <see cref="SwipeDirection"/>
/// past <see cref="SwipeThreshold"/> pixels (or flicked). Every toast's timer pauses while the list is expanded and while
/// the page is hidden.</para>
/// </summary>
public class BitBhToastViewport : BitBhComponentBase, IAsyncDisposable
{
    private readonly Dictionary<string, BitBhToast> _rendered = new(StringComparer.Ordinal);
    private BitBhToaster? _toaster;
    private DotNetObjectReference<RuntimeCallbacks>? _ref;
    private int _handle;
    private bool _attaching;
    private bool _disposed;
    private bool _expanded;
    private bool _pageHidden;
    private string? _sentOptions;

    [CascadingParameter]
    private BitBhToaster? CascadedToaster { get; set; }

    /// <summary>The toaster to render (defaults to the cascaded or registered one).</summary>
    [Parameter]
    public BitBhToaster? Toaster { get; set; }

    /// <summary>Accessible label of the region. Default: <see cref="BitBhStrings.Notifications"/>.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Shortcut that moves focus to the viewport. Default: F8.</summary>
    [Parameter]
    public string? Hotkey { get; set; } = "F8";

    /// <summary>Default auto-dismiss delay (ms). Default: 5000.</summary>
    [Parameter]
    public int Duration { get; set; } = 5000;

    /// <summary>The direction toasts are swiped to dismiss them; null turns swiping off. Default: <see cref="BitBhToastSwipeDirection.Right"/>.</summary>
    [Parameter]
    public BitBhToastSwipeDirection? SwipeDirection { get; set; } = BitBhToastSwipeDirection.Right;

    /// <summary>How far (px) a toast must be swiped to be dismissed. Default: 50.</summary>
    [Parameter]
    public int SwipeThreshold { get; set; } = 50;

    /// <summary>Space (px) between stacked toasts counted into <c>--bit-bh-toast-offset</c>. Default: 8.</summary>
    [Parameter]
    public int Gap { get; set; } = 8;

    /// <summary>Renders a toast.</summary>
    [Parameter]
    public RenderFragment<BitBhToastItem>? ChildContent { get; set; }

    protected override string DefaultTag => "ol";

    internal BitBhToaster EffectiveToaster => _toaster!;

    /// <summary>Whether every toast's timer is paused (the list is expanded or the page is hidden).</summary>
    internal bool PauseAll => _expanded || _pageHidden;

    internal void Register(string elementId, BitBhToast toast) => _rendered[elementId] = toast;

    internal void Unregister(string elementId, BitBhToast toast)
    {
        if (_rendered.TryGetValue(elementId, out var existing) && ReferenceEquals(existing, toast)) _rendered.Remove(elementId);
    }

    internal static string ToAttribute(BitBhToastSwipeDirection direction) => direction switch
    {
        BitBhToastSwipeDirection.Left => "left",
        BitBhToastSwipeDirection.Up => "up",
        BitBhToastSwipeDirection.Down => "down",
        _ => "right",
    };

    private Dictionary<string, object?> RuntimeOptions() => new()
    {
        ["swipe"] = SwipeDirection is { } d ? ToAttribute(d) : null,
        ["threshold"] = SwipeThreshold,
        ["gap"] = Gap,
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_attaching || _disposed) return;
        var options = RuntimeOptions();
        var key = string.Join('|', options.Values.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture)));
        if (_handle > 0)
        {
            if (key == _sentOptions) return;
            _sentOptions = key;
            await Interop.ToastsUpdateAsync(_handle, options);
            return;
        }
        if (!firstRender) return;
        _attaching = true;
        _sentOptions = key;
        _ref = DotNetObjectReference.Create(new RuntimeCallbacks(this));
        _handle = await Interop.ToastsAttachAsync(_ref, ComponentId, options);
        _attaching = false;
        if (_disposed) await Interop.ToastsDetachAsync(_handle);
    }

    private sealed class RuntimeCallbacks(BitBhToastViewport owner)
    {
        /// <summary>The runtime calls it by this name when a toast was swiped past the threshold.</summary>
        [JSInvokable]
        public Task OnToastSwiped(string elementId)
            => owner.RunFromRuntimeAsync(() => owner._rendered.TryGetValue(elementId, out var toast) ? toast.DismissAsync() : Task.CompletedTask);

        /// <summary>The runtime calls it by this name when the list expands / collapses or the page is hidden / shown.</summary>
        [JSInvokable]
        public Task OnToastsPause(bool expanded, bool pageHidden) => owner.RunFromRuntimeAsync(() =>
        {
            if (owner._expanded == expanded && owner._pageHidden == pageHidden) return Task.CompletedTask;
            owner._expanded = expanded;
            owner._pageHidden = pageHidden;
            foreach (var toast in owner._rendered.Values.ToArray()) toast.UpdateTimer();
            return Task.CompletedTask;
        });
    }

    /// <summary>Focuses the toast list itself (as the hotkey does).</summary>
    internal ValueTask<bool> FocusListAsync() => Interop.FocusAsync(ComponentId);

    protected override void OnParametersSet()
    {
        var next = Toaster ?? CascadedToaster ?? GetService<BitBhToaster>() ?? _toaster ?? new BitBhToaster();
        if (!ReferenceEquals(next, _toaster))
        {
            if (_toaster is not null) _toaster.Changed -= OnChanged;
            _toaster = next;
            _toaster.Changed += OnChanged;
        }
    }

    private void OnChanged() => InvokeAsync(StateHasChanged);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!string.IsNullOrWhiteSpace(Hotkey))
        {
            builder.OpenComponent<BitBhHotkey>(0);
            builder.AddComponentParameter(1, nameof(BitBhHotkey.Keys), Hotkey);
            builder.AddComponentParameter(2, nameof(BitBhHotkey.AllowInInputs), true);
            builder.AddComponentParameter(3, nameof(BitBhHotkey.OnTriggered), EventCallback.Factory.Create(this, () => Interop.FocusAsync(ComponentId).AsTask()));
            builder.CloseComponent();
        }

        // The landmark wraps the list so the list keeps its semantics.
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "role", "region");
        var label = Label ?? Strings.Notifications;
        builder.AddAttribute(7, "aria-label", string.IsNullOrWhiteSpace(Hotkey) ? label : $"{label} ({Hotkey})");
        builder.AddAttribute(8, "data-bit-bh-toast-region", "");

        builder.OpenElement(10, Tag);
        builder.AddAttribute(11, "id", ComponentId);
        builder.AddMultipleAttributes(12, GetFinalAttributes());
        builder.AddElementReferenceCapture(13, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhToastViewport>>(20);
        builder.AddComponentParameter(21, "Value", this);
        builder.AddComponentParameter(22, "IsFixed", true);
        builder.AddComponentParameter(23, "ChildContent", (RenderFragment)(inner =>
        {
            if (ChildContent is null) return;
            foreach (var toast in EffectiveToaster.Toasts)
            {
                inner.OpenComponent<BitBhKeyed>(0);
                inner.SetKey(toast.Id);
                inner.AddComponentParameter(1, nameof(BitBhKeyed.ChildContent), ChildContent(toast));
                inner.CloseComponent();
            }
        }));
        builder.CloseComponent();
        builder.CloseElement();

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["tabindex"] = "-1";
        return attrs;
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_toaster is not null) _toaster.Changed -= OnChanged;
        if (!_attaching && _handle > 0) await Interop.ToastsDetachAsync(_handle);
        _ref?.Dispose();
        GC.SuppressFinalize(this);
    }
}
