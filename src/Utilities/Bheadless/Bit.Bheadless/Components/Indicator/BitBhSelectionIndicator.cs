using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A decorative element that follows the selected item of its parent - the sliding underline of a tab list
/// (Base UI <c>Tabs.Indicator</c>), the moving pill of a segmented control (Ark <c>SegmentGroup</c>, made from a
/// <see cref="BitBhRadioGroup"/> or a <see cref="BitBhToggleGroup"/>), the marker of the current page in a nav.
///
/// <para>Place it inside the container of the items. The runtime measures the selected item (the first match of
/// <see cref="Selector"/>; by default anything with <c>aria-selected</c>, <c>aria-checked</c> or <c>aria-pressed</c> set to
/// true, or an <c>aria-current</c>) relative to the container - again whenever the selection, a size or the scroll
/// position changes - and exposes it as CSS variables. Nothing goes through .NET.</para>
///
/// <code>
/// &lt;BitBhTabList class="tabs"&gt;
///     &lt;BitBhTab&gt;Account&lt;/BitBhTab&gt;
///     &lt;BitBhTab&gt;Password&lt;/BitBhTab&gt;
///     &lt;BitBhSelectionIndicator class="tab-indicator" /&gt;
/// &lt;/BitBhTabList&gt;
/// </code>
/// <code>
/// .tabs { position: relative; }
/// .tab-indicator {
///     position: absolute; bottom: 0; height: 2px;
///     left: var(--bit-bh-indicator-left); width: var(--bit-bh-indicator-width);
///     transition: left 200ms, width 200ms;
/// }
/// </code>
///
/// <para><b>Styling hooks:</b> <c>--bit-bh-indicator-left</c>, <c>-top</c>, <c>-width</c>, <c>-height</c> (px, in the container's
/// coordinates), <c>data-measured</c> once measured (keep transitions off until then), <c>data-hidden</c> while nothing is
/// selected, and <c>data-activation-direction="left|right|up|down"</c> - where the selection last moved.</para>
/// </summary>
public class BitBhSelectionIndicator : BitBhComponentBase, IAsyncDisposable
{
    private int _handle;
    private bool _attaching;
    private bool _disposed;
    private string? _sentSelector;

    /// <summary>A CSS selector for the selected item among the container's descendants. Default: any ARIA selection state.</summary>
    [Parameter]
    public string? Selector { get; set; }

    /// <summary>Optional content (most indicators are empty and styled).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The selector used when none is given.</summary>
    public const string DefaultSelector =
        "[aria-selected=\"true\"], [aria-checked=\"true\"], [aria-pressed=\"true\"], [aria-current]:not([aria-current=\"false\"])";

    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-hidden"] = "true";
        return attrs;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_attaching || _disposed) return;
        var selector = Selector ?? DefaultSelector;
        if (_handle > 0)
        {
            if (selector == _sentSelector) return;
            _sentSelector = selector;
            await Interop.IndicatorUpdateAsync(_handle, selector);
            return;
        }
        if (!firstRender) return;
        _attaching = true;
        _sentSelector = selector;
        _handle = await Interop.IndicatorAttachAsync(ComponentId, selector);
        _attaching = false;
        if (_disposed) await Interop.IndicatorDetachAsync(_handle);
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (!_attaching && _handle > 0) await Interop.IndicatorDetachAsync(_handle);
        GC.SuppressFinalize(this);
    }
}
