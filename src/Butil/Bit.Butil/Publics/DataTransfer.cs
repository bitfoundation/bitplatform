using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/DataTransfer">DataTransfer</see>:
/// what a drag is actually carrying - the files, the text, the effect and the drag image.
/// </summary>
/// <remarks>
/// Blazor's <c>DragEventArgs</c> gives you the event but not the payload: <c>files</c>,
/// <c>getData</c>/<c>setData</c>, <c>dropEffect</c> and <c>setDragImage</c> are all out of reach, so
/// handling a file dropped onto the page has meant hand-written JavaScript. This is that JavaScript.
/// <br/>
/// Two rules of the drag-and-drop API are handled for you, because getting either wrong produces a
/// target that silently never fires: a <c>drop</c> only happens on an element whose <c>dragover</c>
/// handler called <c>preventDefault</c>, and <c>dropEffect</c> has to be set on <em>every</em>
/// dragover because the browser resets it between events.
/// <br/>
/// A dropped file's contents are readable long after the event that delivered it - the file objects
/// are held for you, and <see cref="ReadFile"/> reads them on your own schedule rather than inside
/// the handler. Release them when you are done.
/// </remarks>
[ButilService(typeof(DataTransfer))]
public class DataTransfer(IJSRuntime js) : IAsyncDisposable
{
    internal const string DropMethodName = nameof(InvokeDrop);

    private readonly ConcurrentDictionary<Guid, Action<DropPayload>> _dropHandlers = new();

    // Per-instance callback reference (see Keyboard): drop targets are isolated per circuit / WASM
    // app and detached on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<DataTransfer>? _dotNetRef;
    private DotNetObjectReference<DataTransfer> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>DataTransfer</c>, which is everywhere.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (<c>false</c>) rather than
    /// throwing, so the result can't be distinguished from a genuine value.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.dataTransfer.isSupported");

    /// <summary>
    /// Invoked from JS when something is dropped. Public + <see cref="JSInvokableAttribute"/> so it
    /// can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(DropMethodName)]
    public void InvokeDrop(Guid id, DroppedFile[] files, Dictionary<string, string> items)
    {
        if (_dropHandlers.TryGetValue(id, out var handler)) handler(new DropPayload(files ?? [], items ?? []));
    }

    /// <summary>
    /// Makes an element a drop target and reports what lands on it.
    /// </summary>
    /// <param name="target">The element to accept drops on.</param>
    /// <param name="onDrop">
    /// Called with the payload: the files, and every non-file item the drag was carrying keyed by
    /// its MIME type - <c>"text/plain"</c>, <c>"text/uri-list"</c>, <c>"text/html"</c>, or whatever
    /// custom type the source set.
    /// </param>
    /// <param name="dropEffect">
    /// The cursor to show while a drag is over the target: <c>"copy"</c> (the default),
    /// <c>"move"</c>, <c>"link"</c> or <c>"none"</c>. It says what <em>will</em> happen; doing it is
    /// still yours.
    /// </param>
    /// <returns>
    /// A subscription; disposing it stops the element being a drop target. Null when the target
    /// could not be wired up - there is no element behind the reference, or there is no JS runtime
    /// to reach it with (prerender/SSR).
    /// </returns>
    /// <remarks>
    /// The <c>dragover</c> handling that makes a drop possible at all is wired up with this, so
    /// there is nothing else to add.
    /// </remarks>
    [DynamicDependency(nameof(InvokeDrop), typeof(DataTransfer))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DroppedFile))]
    public async ValueTask<ButilSubscription?> OnDrop(ElementReference target, Action<DropPayload> onDrop, string dropEffect = "copy")
    {
        ArgumentNullException.ThrowIfNull(onDrop);

        var id = Guid.NewGuid();
        _dropHandlers[id] = onDrop;

        var listening = await js.Invoke<bool>("BitButil.dataTransfer.listenForDrop", DotNetRef, id, target, dropEffect);

        if (listening is false)
        {
            // Nothing is listening, so a subscription would be a handle on nothing - and the
            // handler it kept would never be called and never be removed.
            _dropHandlers.TryRemove(id, out _);
            return null;
        }

        return new ButilSubscription(id, async () =>
        {
            _dropHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.dataTransfer.removeDropListener", id);
        });
    }

    /// <summary>
    /// Makes an element draggable and decides what the drag carries.
    /// </summary>
    /// <param name="source">The element to drag.</param>
    /// <param name="items">
    /// What to carry, keyed by MIME type. <c>"text/plain"</c> is what other applications will read;
    /// a custom type like <c>"application/x-my-app"</c> is how a drop target of your own recognises
    /// its own drags.
    /// </param>
    /// <param name="effectAllowed">
    /// What the source permits: <c>"copy"</c>, <c>"move"</c>, <c>"link"</c>, <c>"copyMove"</c>,
    /// <c>"all"</c> (the default), <c>"none"</c>. A target asking for an effect this does not allow
    /// gets no drop.
    /// </param>
    /// <param name="dragImage">
    /// An element to show under the cursor instead of a snapshot of the source. It has to be
    /// rendered and visible when the drag starts - a <c>display:none</c> one produces no image at
    /// all, which is the usual reason a custom drag image does not appear.
    /// </param>
    /// <param name="dragImageX">Where the cursor sits within the image, horizontally.</param>
    /// <param name="dragImageY">Where the cursor sits within the image, vertically.</param>
    /// <returns>
    /// A subscription; disposing it makes the element undraggable again. Null when the source could
    /// not be wired up - there is no element behind the reference, or there is no JS runtime to
    /// reach it with (prerender/SSR).
    /// </returns>
    /// <remarks>
    /// The payload is settled here rather than produced by a callback because <c>dragstart</c> has
    /// to set its data synchronously, and a round trip to .NET is not synchronous. Call this again
    /// (after disposing) when what the element carries changes.
    /// </remarks>
    public async ValueTask<ButilSubscription?> ConfigureDragSource(ElementReference source,
                                                                   Dictionary<string, string> items,
                                                                   string effectAllowed = "all",
                                                                   ElementReference? dragImage = null,
                                                                   int dragImageX = 0,
                                                                   int dragImageY = 0)
    {
        ArgumentNullException.ThrowIfNull(items);

        var id = Guid.NewGuid();
        var configured = await js.Invoke<bool>("BitButil.dataTransfer.configureSource",
            id, source, items, effectAllowed, dragImage, dragImageX, dragImageY);

        if (configured is false) return null;

        return new ButilSubscription(id, async () => await js.InvokeVoid("BitButil.dataTransfer.removeSource", id));
    }

    /// <summary>
    /// Reads a dropped file's bytes.
    /// </summary>
    /// <returns>The bytes, or null when the file has been released or could not be read.</returns>
    /// <remarks>
    /// Readable long after the drop - the file object is held for you, so there is no need to do
    /// the work inside the handler. Release it when you are done.
    /// </remarks>
    public ValueTask<byte[]?> ReadFile(Guid fileId) => js.Invoke<byte[]?>("BitButil.dataTransfer.readFile", fileId);

    /// <summary>
    /// Reads a dropped file as UTF-8 text.
    /// </summary>
    /// <returns>The text, or null when the file has been released or could not be read.</returns>
    public ValueTask<string?> ReadFileText(Guid fileId) => js.Invoke<string?>("BitButil.dataTransfer.readFileText", fileId);

    /// <summary>
    /// A <c>blob:</c> URL for a dropped file - for putting a dropped image straight into an
    /// <c>&lt;img src&gt;</c> without reading its bytes into .NET at all.
    /// </summary>
    /// <returns>The URL, or null when the file has been released.</returns>
    /// <remarks>
    /// You own the URL: revoke it through <see cref="ObjectUrls"/> when the image is gone, or the
    /// file stays in memory for the life of the page.
    /// </remarks>
    public ValueTask<string?> CreateObjectUrl(Guid fileId) => js.Invoke<string?>("BitButil.dataTransfer.objectUrl", fileId);

    /// <summary>
    /// Forgets a dropped file. Reading it afterwards answers null.
    /// </summary>
    public ValueTask ReleaseFile(Guid fileId) => js.InvokeVoid("BitButil.dataTransfer.releaseFile", fileId);

    /// <summary>
    /// On scope/circuit teardown, detaches every drop target and drag source whose subscription was
    /// never disposed, and forgets every dropped file still being held.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _dropHandlers.Clear();
            await js.InvokeVoid("BitButil.dataTransfer.disposeAll");
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
