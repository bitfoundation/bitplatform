using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Bit.Bheadless;

/// <summary>
/// Virtualizes a list whose items differ in height (the options panels' <c>VariableItemSize</c>). It renders a window
/// of <see cref="Items"/> between two spacers and leaves the rest to the runtime (<c>dom.attachVirtualList</c> in
/// bit-bheadless.ts): it measures the items as they render and whenever one resizes, sizes the spacers from those
/// measurements (through the CSSOM - no style attribute is rendered), keeps the view where it was as the measurements
/// move the offsets above it, and asks for the window the scroll position needs (<see cref="SetRange"/>).
/// <see cref="ItemSize"/> is the height assumed for the items not measured yet.
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
internal sealed class BitBhVirtualizer<TItem> : ComponentBase, IAsyncDisposable
{
    /// <summary>The items rendered before the runtime has seen the view: enough for a typical options panel.</summary>
    internal const int InitialCount = 20;

    private BitBhInterop? _interop;
    private bool _seen;
    private int _seenCount;
    private TItem? _seenFirst;
    private TItem? _seenLast;
    private int _seenStart;
    private readonly List<TItem> _seenWindow = [];
    private int _start;
    private int _end;
    private int _epoch;
    private ElementReference _before;
    private ElementReference _after;
    private DotNetObjectReference<BitBhVirtualizer<TItem>>? _selfRef;
    private int _handle;
    private bool _attachTried;
    private (float Size, int Overscan) _sentOptions;
    private bool _disposed;

    [Inject] private IServiceProvider Services { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>The items.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>The height (px) assumed for an item until it has rendered and been measured.</summary>
    [Parameter]
    public float ItemSize { get; set; } = 36;

    /// <summary>Items rendered above and below the view.</summary>
    [Parameter]
    public int OverscanCount { get; set; } = 5;

    /// <summary>The spacers' element: <c>li</c> in a list, <c>div</c> elsewhere.</summary>
    [Parameter]
    public string SpacerElement { get; set; } = "div";

    /// <summary>Renders an item: one element per item, the runtime measures them in order.</summary>
    [Parameter, EditorRequired]
    public RenderFragment<BitBhIndexed<TItem>> ItemContent { get; set; } = default!;

    private BitBhInterop Interop => _interop ??= BitBhInterop.Resolve(Services, JSRuntime);

    /// <summary>The first rendered item.</summary>
    internal int Start => _start;

    /// <summary>The item after the last rendered one.</summary>
    internal int End => _end;

    protected override void OnParametersSet()
    {
        // Another list (a new filter) is not the one measured: the runtime starts its sizes over (the epoch). Compared with
        // what the list held when last seen, not with the list itself: a list rebuilt with the same items -
        // Items="@list.Where(…).ToList()" makes a new one on every render - is not another, and one sorted in place is.
        if (!_seen || !SameAsSeen(Items)) _epoch++;
        var count = Items.Count;
        var length = Math.Max(_end - _start, InitialCount);
        _end = Math.Min(count, _start + length);
        _start = Math.Max(0, Math.Min(_start, _end - length));
        See();
    }

    /// <summary>
    /// Whether <paramref name="items"/> holds the items last seen (<see cref="See"/>), judged by the count, the ends and
    /// the rendered window: the cost is the window's however long the list (it runs on every render of a list rebuilt by
    /// every render). A list changed only outside the window keeps the sizes measured on it, estimates the runtime
    /// corrects as those items render.
    /// </summary>
    private bool SameAsSeen(IReadOnlyList<TItem> items)
    {
        var count = _seenCount;
        if (count != items.Count) return false;
        if (count == 0) return true;
        var comparer = EqualityComparer<TItem>.Default;
        if (!comparer.Equals(_seenFirst, items[0]) || !comparer.Equals(_seenLast, items[count - 1])) return false;
        for (var i = 0; i < _seenWindow.Count; i++)
            if (!comparer.Equals(_seenWindow[i], items[_seenStart + i])) return false;
        return true;
    }

    /// <summary>
    /// Keeps a copy of what <see cref="SameAsSeen"/> compares - not the list, which its owner may change in place (sort
    /// it) - for the next render.
    /// </summary>
    private void See()
    {
        var items = Items;
        _seen = true;
        _seenCount = items.Count;
        _seenFirst = _seenCount > 0 ? items[0] : default;
        _seenLast = _seenCount > 0 ? items[_seenCount - 1] : default;
        _seenStart = _start;
        _seenWindow.Clear();
        for (var i = _start; i < _end; i++) _seenWindow.Add(items[i]);
    }

    /// <summary>
    /// Called by the runtime with the window of items the scroll position needs, for the list of
    /// <paramref name="epoch"/> (a request made for a list since replaced is dropped).
    /// </summary>
    [JSInvokable]
    public void SetRange(int start, int end, int epoch)
    {
        if (_disposed || epoch != _epoch) return;
        var count = Items.Count;
        start = Math.Clamp(start, 0, count);
        end = Math.Clamp(end, start, count);
        if (start == _start && end == _end) return;
        // Changed in place since the last render (no new parameters to say so): the sizes are not its items' any more.
        if (!SameAsSeen(Items)) _epoch++;
        _start = start;
        _end = end;
        See();
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, SpacerElement);
        builder.AddAttribute(1, "data-bit-bh-spacer", "before");
        // What the runtime reads after every render: which items are in the window, of how many, of which list.
        builder.AddAttribute(2, "data-bit-bh-range", string.Create(CultureInfo.InvariantCulture, $"{_start} {_end} {Items.Count} {_epoch}"));
        builder.AddAttribute(3, "aria-hidden", "true");
        builder.AddElementReferenceCapture(4, r => _before = r);
        builder.CloseElement();

        builder.OpenRegion(10);
        for (var i = _start; i < _end; i++)
            ItemContent(new BitBhIndexed<TItem>(i, Items[i]))(builder);
        builder.CloseRegion();

        builder.OpenElement(20, SpacerElement);
        builder.AddAttribute(21, "data-bit-bh-spacer", "after");
        builder.AddAttribute(22, "aria-hidden", "true");
        builder.AddElementReferenceCapture(23, r => _after = r);
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed) return;
        var options = (ItemSize, OverscanCount);
        if (_attachTried is false)
        {
            // One attempt: a 0 handle (no runtime - prerendering, tests) isn't retried on every render.
            _attachTried = true;
            _sentOptions = options;
            _selfRef ??= DotNetObjectReference.Create(this);
            _handle = await Interop.VirtualListAttachAsync(_selfRef, _before, _after, ItemSize, OverscanCount);
            if (_disposed && _handle > 0) await Interop.VirtualListDetachAsync(_handle);
        }
        else if (_handle > 0 && options != _sentOptions)
        {
            _sentOptions = options;
            await Interop.VirtualListUpdateAsync(_handle, ItemSize, OverscanCount);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        var handle = _handle;
        _handle = 0;
        if (handle > 0) await Interop.VirtualListDetachAsync(handle);
        _selfRef?.Dispose();
    }
}
