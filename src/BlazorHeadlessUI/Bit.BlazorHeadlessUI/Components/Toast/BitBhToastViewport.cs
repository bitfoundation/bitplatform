using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

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
/// </summary>
public class BitBhToastViewport : BitBhComponentBase, IDisposable
{
    private BitBhToaster? _toaster;

    [CascadingParameter]
    private BitBhToaster? CascadedToaster { get; set; }

    /// <summary>The toaster to render (defaults to the cascaded or registered one).</summary>
    [Parameter]
    public BitBhToaster? Toaster { get; set; }

    /// <summary>Accessible label of the region. Default: "Notifications".</summary>
    [Parameter]
    public string Label { get; set; } = "Notifications";

    /// <summary>Shortcut that moves focus to the viewport. Default: F8.</summary>
    [Parameter]
    public string? Hotkey { get; set; } = "F8";

    /// <summary>Default auto-dismiss delay (ms). Default: 5000.</summary>
    [Parameter]
    public int Duration { get; set; } = 5000;

    /// <summary>Renders a toast.</summary>
    [Parameter]
    public RenderFragment<BitBhToastItem>? ChildContent { get; set; }

    protected override string DefaultTag => "ol";

    internal BitBhToaster EffectiveToaster => _toaster!;

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
        builder.AddAttribute(7, "aria-label", string.IsNullOrWhiteSpace(Hotkey) ? Label : $"{Label} ({Hotkey})");
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

    public void Dispose()
    {
        if (_toaster is not null) _toaster.Changed -= OnChanged;
    }
}
