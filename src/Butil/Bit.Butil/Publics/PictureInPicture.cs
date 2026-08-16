using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Picture-in-Picture_API">Picture-in-Picture API</see>:
/// floats a <c>&lt;video&gt;</c> in an always-on-top window the user can move around, outside the
/// page and even outside the browser.
/// </summary>
/// <remarks>
/// Only one element per document can be in picture-in-picture at a time, and only a
/// <c>&lt;video&gt;</c> qualifies. Entering requires a user gesture; leaving does not, which is why
/// <see cref="Exit"/> can be called from anywhere.
/// <br/>
/// The user can close the floating window themselves at any point, so treat
/// <see cref="Subscribe"/> as the source of truth rather than assuming a successful
/// <see cref="Request"/> stays true.
/// </remarks>
[ButilService(typeof(PictureInPicture))]
public class PictureInPicture(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokePictureInPictureChange);

    private readonly ConcurrentDictionary<Guid, Action<PictureInPictureChange>> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<PictureInPicture>? _dotNetRef;
    private DotNetObjectReference<PictureInPicture> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the document allows picture-in-picture at all (<c>document.pictureInPictureEnabled</c>).</summary>
    /// <remarks>
    /// False inside an iframe without the <c>picture-in-picture</c> permissions-policy allowance,
    /// even on an engine that implements the API.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.pictureInPicture.isSupported");

    /// <summary>
    /// Whether this particular element can enter picture-in-picture right now: it is a
    /// <c>&lt;video&gt;</c>, the document allows it, the element hasn't opted out with
    /// <c>disablePictureInPicture</c>, and its metadata has loaded.
    /// </summary>
    /// <param name="videoElement">The <c>&lt;video&gt;</c> to test.</param>
    /// <remarks>
    /// Returns false for a video whose source hasn't loaded yet - wait for the element's
    /// <c>loadedmetadata</c> event before offering the button.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsAvailableFor(ElementReference videoElement)
        => js.Invoke<bool>("BitButil.pictureInPicture.isAvailableFor", videoElement);

    /// <summary>
    /// Moves <paramref name="videoElement"/> into the floating window.
    /// </summary>
    /// <param name="videoElement">The <c>&lt;video&gt;</c> to float.</param>
    /// <returns>False when the request was refused - no user gesture, no video track yet, the
    /// element opted out, or the user dismissed it.</returns>
    /// <remarks>
    /// Must be called from a user-gesture handler such as a click.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Request(ElementReference videoElement)
        => js.Invoke<bool>("BitButil.pictureInPicture.request", videoElement);

    /// <summary>
    /// Closes the floating window and returns the video to the page.
    /// </summary>
    /// <returns>False when nothing was in picture-in-picture.</returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Exit() => js.Invoke<bool>("BitButil.pictureInPicture.exit");

    /// <summary>True when anything in this document is currently in picture-in-picture.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsActive() => js.Invoke<bool>("BitButil.pictureInPicture.isActive", null);

    /// <summary>True when this specific element is the one in picture-in-picture.</summary>
    /// <param name="videoElement">The <c>&lt;video&gt;</c> to test.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsActive(ElementReference videoElement)
        => js.Invoke<bool>("BitButil.pictureInPicture.isActive", videoElement);

    /// <summary>
    /// Invoked from JS when an element enters or leaves picture-in-picture. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokePictureInPictureChange(Guid id, bool active, int width, int height)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(new PictureInPictureChange(active, width, height));
    }

    /// <summary>
    /// Watches an element entering and leaving picture-in-picture, including when the user closes
    /// the floating window themselves - the only way to learn about that.
    /// </summary>
    /// <param name="videoElement">The <c>&lt;video&gt;</c> to watch.</param>
    /// <param name="handler">Called on each transition, with the floating window's size while active.</param>
    /// <returns>A subscription - dispose it to detach the listeners.</returns>
    public async ValueTask<ButilSubscription> Subscribe(ElementReference videoElement, Action<PictureInPictureChange> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _handlers[id] = handler;
        await js.InvokeVoid("BitButil.pictureInPicture.subscribe", DotNetRef, id, videoElement);

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.pictureInPicture.unsubscribe", id);
        });
    }

    /// <summary>
    /// On scope/circuit teardown, detaches any listener whose <see cref="ButilSubscription"/> was
    /// never disposed. The floating window itself is the user's - it is left alone.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _handlers.Clear();
            await js.InvokeVoid("BitButil.pictureInPicture.disposeAll");
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
