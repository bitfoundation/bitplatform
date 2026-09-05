using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Selection">Selection</see> and
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Range">Range</see> APIs: what the user
/// has selected, and the range operations that change it.
/// </summary>
/// <remarks>
/// <see cref="Window.GetSelection"/> reads the selection; this is the rest of it - selecting an
/// element's contents, wrapping a selection in a <c>&lt;mark&gt;</c>, replacing it, measuring where it
/// is on screen, saving and restoring a caret across a re-render, and finding the text position under
/// a pointer. The pieces a highlighter, an inline comment thread or a rich-text editor is built from.
/// <br/>
/// A <c>Range</c> object cannot cross the interop boundary - it holds live DOM node references - so
/// this works the way the browser's own editing commands do: every call acts on the current selection,
/// or on a range expressed as character offsets within one element you name.
/// </remarks>
[ButilService(typeof(Selection))]
public class Selection(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeSelectionChange);

    private readonly ConcurrentDictionary<Guid, Action> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Selection>? _dotNetRef;
    private DotNetObjectReference<Selection> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>window.getSelection</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.selection.isSupported");

    /// <summary>
    /// The current selection - its text, whether it is collapsed to a caret, and its offsets.
    /// The same value <see cref="Window.GetSelection"/> returns.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WindowSelection))]
    public ValueTask<WindowSelection?> Get() => js.Invoke<WindowSelection?>("BitButil.selection.get");

    /// <summary>The selected text, or an empty string when nothing is selected.</summary>
    public ValueTask<string> GetText() => js.Invoke<string>("BitButil.selection.getText");

    /// <summary>
    /// The selected markup rather than its text - what a "copy as HTML" or a quote-this-passage
    /// feature needs. An empty string when nothing is selected.
    /// </summary>
    /// <remarks>
    /// The fragment is serialized from a detached copy of the selected nodes, so nothing in it is
    /// loaded, run or announced. It is still page content: treat it as untrusted before storing or
    /// re-displaying it, e.g. through <see cref="Sanitizer"/>.
    /// </remarks>
    public ValueTask<string> GetHtml() => js.Invoke<string>("BitButil.selection.getHtml");

    /// <summary>
    /// One rectangle per line box the selection covers, in viewport coordinates - a selection that
    /// spans wrapped text is not a rectangle. Empty when nothing is selected.
    /// </summary>
    /// <remarks>Where to draw a highlight overlay, or where to anchor a comment marker.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Rect))]
    public ValueTask<Rect[]> GetRects() => js.Invoke<Rect[]>("BitButil.selection.getRects");

    /// <summary>
    /// The single rectangle enclosing the whole selection, in viewport coordinates. Null when nothing
    /// is selected.
    /// </summary>
    /// <remarks>Where to put a floating toolbar over a selection.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Rect))]
    public ValueTask<Rect?> GetBoundingRect() => js.Invoke<Rect?>("BitButil.selection.getBoundingRect");

    /// <summary>
    /// Whether an element is inside the selection.
    /// </summary>
    /// <param name="element">The element to test.</param>
    /// <param name="partly">When true, an element the selection only partly covers counts too.</param>
    public ValueTask<bool> ContainsElement(ElementReference element, bool partly = true)
        => js.Invoke<bool>("BitButil.selection.containsElement", element, partly);

    /// <summary>Selects an element and its contents, replacing the current selection.</summary>
    public ValueTask<bool> SelectElement(ElementReference element)
        => js.Invoke<bool>("BitButil.selection.selectElement", element);

    /// <summary>Selects everything inside an element but not the element itself, replacing the current selection.</summary>
    public ValueTask<bool> SelectElementContents(ElementReference element)
        => js.Invoke<bool>("BitButil.selection.selectElementContents", element);

    /// <summary>
    /// Selects a character range inside one element - the counterpart of
    /// <see cref="GetRangeIn"/>, and what restores a caret after a re-render.
    /// </summary>
    /// <param name="element">The element the offsets are counted within.</param>
    /// <param name="start">Start offset, in characters of the element's text.</param>
    /// <param name="end">End offset. Pass the same value as <paramref name="start"/> to place a caret.</param>
    /// <returns>False when the offsets fall outside the element's text.</returns>
    /// <remarks>
    /// Offsets count the element's text only, walking its text nodes in order - element boundaries
    /// are not characters. So the numbers survive markup changing around the text, which is exactly
    /// what makes them usable across a re-render.
    /// </remarks>
    public ValueTask<bool> SelectRange(ElementReference element, int start, int end)
        => js.Invoke<bool>("BitButil.selection.selectRange", element, start, end);

    /// <summary>
    /// Where the selection sits inside one element, in characters. Null when there is no selection,
    /// or when it isn't inside this element.
    /// </summary>
    /// <remarks>Save this before re-rendering an editable element, then hand it back to <see cref="SelectRange"/>.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SelectionOffsets))]
    public ValueTask<SelectionOffsets?> GetRangeIn(ElementReference element)
        => js.Invoke<SelectionOffsets?>("BitButil.selection.getRangeIn", element);

    /// <summary>Clears the selection.</summary>
    public ValueTask RemoveAll() => js.InvokeVoid("BitButil.selection.removeAll");

    /// <summary>
    /// Collapses the selection to a caret at one of its ends.
    /// </summary>
    /// <param name="toStart">True for the start, false for the end.</param>
    public ValueTask<bool> Collapse(bool toStart = false)
        => js.Invoke<bool>("BitButil.selection.collapse", toStart);

    /// <summary>
    /// Wraps the selection in a new element - <c>Range.surroundContents()</c>. The highlighting
    /// primitive: select some text, wrap it in a <c>&lt;mark&gt;</c>.
    /// </summary>
    /// <param name="tagName">The element to wrap in, e.g. <c>"mark"</c> or <c>"span"</c>.</param>
    /// <param name="className">Optional class for the new element.</param>
    /// <param name="style">Optional inline style for the new element.</param>
    /// <returns>
    /// False when the selection can't be wrapped - which is normal, not exceptional: a selection
    /// whose ends are in different elements (half a paragraph and half of the next one) has no single
    /// element that could contain it, and the browser refuses.
    /// </returns>
    public ValueTask<bool> Surround(string tagName, string? className = null, string? style = null)
        => js.Invoke<bool>("BitButil.selection.surround", tagName, className, style);

    /// <summary>
    /// Replaces the selection with plain text, leaving the caret after it.
    /// </summary>
    /// <returns>False when there is no selection to replace.</returns>
    public ValueTask<bool> ReplaceWithText(string text)
        => js.Invoke<bool>("BitButil.selection.replaceWithText", text ?? string.Empty);

    /// <summary>Deletes the selected content, leaving a caret in its place.</summary>
    /// <returns>False when there is no selection.</returns>
    public ValueTask<bool> DeleteContents() => js.Invoke<bool>("BitButil.selection.deleteContents");

    /// <summary>
    /// True when the runtime can resolve a point to a text position - <c>caretPositionFromPoint</c>,
    /// or WebKit's older <c>caretRangeFromPoint</c>.
    /// </summary>
    public ValueTask<bool> IsCaretFromPointSupported() => js.Invoke<bool>("BitButil.selection.isCaretFromPointSupported");

    /// <summary>
    /// The text position under a point, in viewport coordinates - which character of which node the
    /// pointer is over.
    /// </summary>
    /// <param name="x">Viewport x, e.g. a pointer event's <c>ClientX</c>.</param>
    /// <param name="y">Viewport y, e.g. a pointer event's <c>ClientY</c>.</param>
    /// <returns>The position, or null when there is no text there or the runtime doesn't support it.</returns>
    /// <remarks>What a drag-to-insert, a hover dictionary or a click-to-annotate feature is built on.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CaretPosition))]
    public ValueTask<CaretPosition?> CaretFromPoint(double x, double y)
        => js.Invoke<CaretPosition?>("BitButil.selection.caretFromPoint", x, y);

    /// <summary>
    /// Invoked from JS when the selection changes. Public + <see cref="JSInvokableAttribute"/> so it
    /// can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeSelectionChange(Guid id)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke();
    }

    /// <summary>
    /// Calls <paramref name="handler"/> whenever the selection changes anywhere in the document
    /// (<c>selectionchange</c>). Dispose the returned subscription to stop.
    /// </summary>
    /// <remarks>
    /// The event fires on every caret move, so it is frequent: read the selection inside the handler
    /// rather than doing work per event, and debounce anything expensive.
    /// </remarks>
    [DynamicDependency(nameof(InvokeSelectionChange), typeof(Selection))]
    public async ValueTask<ButilSubscription> OnChange(Action handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _handlers[id] = handler;
        await js.InvokeVoid("BitButil.selection.onChange", DotNetRef, InvokeMethodName, id);

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.selection.offChange", id);
        });
    }

    /// <summary>
    /// On scope/circuit teardown, detaches any <c>selectionchange</c> listener whose
    /// <see cref="ButilSubscription"/> was never disposed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _handlers.Clear();
            await js.InvokeVoid("BitButil.selection.disposeAll");
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }
}
