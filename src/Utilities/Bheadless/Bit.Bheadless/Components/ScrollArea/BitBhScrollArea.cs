using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>When the scrollbars of a <see cref="BitBhScrollArea"/> are shown.</summary>
public enum BitBhScrollAreaType
{
    /// <summary>
    /// While the pointer is over the area or the content scrolls, when the content overflows on the scrollbar's
    /// axis; hidden <see cref="BitBhScrollArea.ScrollHideDelay"/> after the pointer leaves. The default.
    /// </summary>
    Hover,

    /// <summary>
    /// While the content scrolls (and while the pointer is on a scrollbar or drags its thumb); hidden
    /// <see cref="BitBhScrollArea.ScrollHideDelay"/> after scrolling stops.
    /// </summary>
    Scroll,

    /// <summary>Whenever the content overflows on the scrollbar's axis.</summary>
    Auto,

    /// <summary>Always, whether the content overflows or not.</summary>
    Always,
}

/// <summary>
/// A headless Scroll Area: native scrolling - keyboard, wheel, touch, assistive technology and find-in-page all
/// work as in any scrolling element - with custom scrollbars you can style, in place of the browser's own.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Native scrolling</b> — the <see cref="BitBhScrollAreaViewport"/> is the scroll container; only its native scrollbars are hidden.</item>
///   <item><b>Custom scrollbars</b> — <see cref="BitBhScrollAreaScrollbar"/> + <see cref="BitBhScrollAreaThumb"/> per axis: a thumb sized to the visible fraction, dragged with the pointer (pointer capture, touch too), a press on the track pages, the wheel over a scrollbar scrolls the content.</item>
///   <item><b>Visibility</b> — <see cref="Type"/>: <c>Hover</c> (default), <c>Scroll</c>, <c>Auto</c> or <c>Always</c>, with <see cref="ScrollHideDelay"/>. Scrollbars carry <c>data-state="visible|hidden"</c>; style the hidden state (fade them out) - a hidden scrollbar takes no pointer input.</item>
///   <item><b>Overflow hooks</b> — <c>data-overflow-x</c> / <c>data-overflow-y</c> while the content overflows, <c>data-overflow-y-start</c> / <c>-end</c> (and <c>-x-</c>) while content is hidden past an edge, and the CSS variables <c>--bit-bh-scroll-area-overflow-y-start</c> / <c>-end</c> (and <c>-x-</c>) with the pixels hidden past each edge, for fade masks and shadows.</item>
///   <item><b>Activity hooks</b> — <c>data-scrolling</c> and <c>data-hovering</c> on the root and the scrollbars; <c>data-dragging</c> while a thumb is dragged.</item>
///   <item><b>RTL</b> — the vertical scrollbar sits at the inline end; horizontal thumbs move with the reading direction.</item>
/// </list>
///
/// <para>
/// All of it runs in the browser: no .NET call is made while the content scrolls. A <c>ResizeObserver</c> and a
/// <c>MutationObserver</c> keep the thumbs right when the content or the area changes size. Before the page is
/// interactive (prerendering) the content scrolls natively, with the scrollbars in their initial state.
/// </para>
///
/// <para>
/// <b>Sizing.</b> Give the root a size and <c>overflow: hidden</c>, and the viewport <c>width: 100%; height: 100%</c>
/// (or a <c>max-height</c> for an area that grows up to a limit).
/// </para>
///
/// <code>
/// &lt;BitBhScrollArea class="scroll-area"&gt;
///     &lt;BitBhScrollAreaViewport class="viewport" role="region" aria-label="Tags"&gt;
///         …
///     &lt;/BitBhScrollAreaViewport&gt;
///     &lt;BitBhScrollAreaScrollbar class="scrollbar"&gt;&lt;BitBhScrollAreaThumb class="thumb" /&gt;&lt;/BitBhScrollAreaScrollbar&gt;
///     &lt;BitBhScrollAreaScrollbar class="scrollbar" Orientation="BitBhOrientation.Horizontal"&gt;&lt;BitBhScrollAreaThumb class="thumb" /&gt;&lt;/BitBhScrollAreaScrollbar&gt;
///     &lt;BitBhScrollAreaCorner class="corner" /&gt;
/// &lt;/BitBhScrollArea&gt;
/// </code>
/// </summary>
public class BitBhScrollArea : BitBhComponentBase, IAsyncDisposable
{
    private int _handle;
    private bool _attachTried;
    private bool _syncing;
    private bool _disposed;
    private string? _sentOptions;

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    /// <summary>When the scrollbars are shown. Default: <see cref="BitBhScrollAreaType.Hover"/>.</summary>
    [Parameter]
    public BitBhScrollAreaType Type { get; set; } = BitBhScrollAreaType.Hover;

    /// <summary>
    /// How long, in milliseconds, the scrollbars stay visible after the pointer leaves the area
    /// (<see cref="BitBhScrollAreaType.Hover"/>) or after scrolling stops (<see cref="BitBhScrollAreaType.Scroll"/>). Default: 600.
    /// </summary>
    [Parameter]
    public int ScrollHideDelay { get; set; } = 600;

    /// <summary>
    /// Reading direction: which side the vertical scrollbar sits on and which way the content scrolls
    /// horizontally. Inherits from <see cref="BitBhDirectionProvider"/>; either renders <c>dir</c> on the root.
    /// </summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>A <see cref="BitBhScrollAreaViewport"/>, one <see cref="BitBhScrollAreaScrollbar"/> per axis and an optional <see cref="BitBhScrollAreaCorner"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    internal string RootId => ComponentId;

    internal BitBhScrollAreaType AreaType => Type;

    internal BitBhDirection EffectiveDir => Dir ?? CascadedDir;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhScrollArea>>(40);
        builder.AddComponentParameter(41, "Value", this);
        // Not fixed: the parts follow Type and Dir.
        builder.AddComponentParameter(42, "IsFixed", false);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        // The direction decides which way the viewport scrolls horizontally, so a cascaded right-to-left one is
        // written out too: a provider sets no dir attribute of its own.
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        else if (CascadedDir == BitBhDirection.Rtl) attrs["dir"] = "rtl";
        // Functional: the scrollbars and the corner are positioned against the root.
        attrs["style"] = "position:relative";
        return attrs;
    }

    protected override Task OnAfterRenderAsync(bool firstRender) => SyncAsync();

    // Renders can arrive while the attach is in flight. One loop applies them in order, and an area disposed
    // meanwhile detaches what the in-flight call attached.
    private async Task SyncAsync()
    {
        if (_syncing) return;
        _syncing = true;
        try
        {
            while (_disposed is false)
            {
                var options = $"{Type}|{ScrollHideDelay}";
                if (_handle == 0)
                {
                    // One attempt: a 0 handle (no runtime, no element) isn't retried on every render.
                    if (_attachTried) break;
                    _attachTried = true;
                    _handle = await Interop.ScrollAreaAttachAsync(ComponentId, RuntimeOptions());
                    _sentOptions = options;
                    continue;
                }
                if (options == _sentOptions) break;
                _sentOptions = options;
                await Interop.ScrollAreaUpdateAsync(_handle, RuntimeOptions());
            }
        }
        finally
        {
            _syncing = false;
        }
        if (_disposed) await DetachAsync();
    }

    private Dictionary<string, object?> RuntimeOptions() => new()
    {
        ["type"] = TypeName(Type),
        ["hideDelay"] = Math.Max(0, ScrollHideDelay),
    };

    internal static string TypeName(BitBhScrollAreaType type) => type switch
    {
        BitBhScrollAreaType.Scroll => "scroll",
        BitBhScrollAreaType.Auto => "auto",
        BitBhScrollAreaType.Always => "always",
        _ => "hover",
    };

    private async Task DetachAsync()
    {
        if (_handle <= 0) return;
        var handle = _handle;
        _handle = 0;
        await Interop.ScrollAreaDetachAsync(handle);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        // An attach in flight is undone by SyncAsync once it returns.
        if (_syncing is false) await DetachAsync();
        GC.SuppressFinalize(this);
    }
}
