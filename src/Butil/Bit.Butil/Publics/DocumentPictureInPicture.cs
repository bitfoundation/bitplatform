using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document_Picture-in-Picture_API">Document Picture-in-Picture API</see>:
/// opens an always-on-top window and lets you put arbitrary DOM in it - not just a video.
/// </summary>
/// <remarks>
/// The distinction from <see cref="PictureInPicture"/> is the whole point: that one floats a
/// <c>&lt;video&gt;</c> element and nothing else, while this opens a real (small, borderless,
/// always-on-top) document you can move any part of your page into - custom playback controls, a
/// call roster, a live chart, a timer that stays visible while the user works elsewhere.
/// <br/>
/// Elements are <em>moved</em>, not copied, and they keep their identity: a Blazor component moved
/// into the window carries on being the same component, with its state and its event handlers
/// intact. Two things follow. Leave a placeholder where the element was if the page's layout needs
/// one, and move a container that a parent component does not re-render around - a re-render that
/// replaces the parent's markup can leave the moved node stranded.
/// <br/>
/// Opening needs a user gesture, and only one such window can exist at a time. Everything that was
/// moved is put back when the window closes, whether that was <see cref="DocumentPictureInPictureWindowHandle.DisposeAsync"/>
/// or the user closing it.
/// </remarks>
[ButilService(typeof(DocumentPictureInPicture))]
public class DocumentPictureInPicture(IJSRuntime js) : IAsyncDisposable
{
    internal const string EnterMethodName = nameof(InvokeDocumentPictureInPictureEnter);

    private readonly ConcurrentDictionary<Guid, Action<DocumentPictureInPictureSize>> _enterHandlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<DocumentPictureInPicture>? _dotNetRef;
    private DotNetObjectReference<DocumentPictureInPicture> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>documentPictureInPicture.requestWindow</c>.</summary>
    /// <remarks>
    /// Chromium-only at the time of writing; other engines have the video-element
    /// <see cref="PictureInPicture"/> but not this.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.documentPictureInPicture.isSupported");

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/DocumentPictureInPicture/requestWindow">DocumentPictureInPicture.requestWindow()</see>:
    /// opens the floating window.
    /// </summary>
    /// <param name="options">Size and behaviour of the window. Leave null for the browser's defaults plus copied stylesheets.</param>
    /// <param name="onClose">
    /// Called when the window closes - including when the user closes it, which is the only way to
    /// learn about that.
    /// </param>
    /// <returns>
    /// A handle to the window, or <c>null</c> when the API is missing, there was no user gesture
    /// behind the call, or a picture-in-picture window is already open.
    /// </returns>
    /// <remarks>
    /// Must be called from a user-gesture handler such as a click. The window opens empty - move
    /// content into it with <see cref="DocumentPictureInPictureWindowHandle.MoveElement"/>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DocumentPictureInPictureOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DocumentPictureInPictureSize))]
    public async ValueTask<DocumentPictureInPictureWindowHandle?> RequestWindow(DocumentPictureInPictureOptions? options = null,
                                                                                Action? onClose = null)
    {
        var id = Guid.NewGuid();
        var handle = new DocumentPictureInPictureWindowHandle(js, id, onClose);

        var size = await js.Invoke<DocumentPictureInPictureSize?>("BitButil.documentPictureInPicture.requestWindow",
                                                                  id, options ?? new DocumentPictureInPictureOptions(),
                                                                  handle.CallbackRef, DocumentPictureInPictureWindowHandle.CloseMethodName);
        if (size is null)
        {
            await handle.DisposeAsync();
            return null;
        }

        handle.Initialize(size);
        return handle;
    }

    /// <summary>
    /// Invoked from JS when a document picture-in-picture window opens. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(EnterMethodName)]
    public void InvokeDocumentPictureInPictureEnter(Guid id, int width, int height)
    {
        if (_enterHandlers.TryGetValue(id, out var handler))
        {
            handler.Invoke(new DocumentPictureInPictureSize { Width = width, Height = height });
        }
    }

    /// <summary>
    /// Watches the
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/DocumentPictureInPicture/enter_event">enter</see>
    /// event - a floating window opening, including one this component did not request.
    /// </summary>
    /// <param name="handler">Called with the new window's inner size.</param>
    /// <returns>A subscription, or <c>null</c> when the API is unavailable.</returns>
    /// <remarks>
    /// Since only one such window can exist at a time, this is how a second part of the app learns
    /// that the one place for it is now taken.
    /// </remarks>
    public async ValueTask<ButilSubscription?> SubscribeEnter(Action<DocumentPictureInPictureSize> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _enterHandlers[id] = handler;

        var subscribed = await js.Invoke<bool>("BitButil.documentPictureInPicture.subscribeEnter", id, DotNetRef, EnterMethodName);
        if (subscribed is false)
        {
            _enterHandlers.TryRemove(id, out _);
            return null;
        }

        return new ButilSubscription(id, async () =>
        {
            _enterHandlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.documentPictureInPicture.unsubscribeEnter", id);
        });
    }

    /// <summary>
    /// On scope/circuit teardown, restores everything that was moved and closes any window whose
    /// handle was never disposed - otherwise the floating window would outlive the page that owns
    /// its content.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _enterHandlers.Clear();
            await js.InvokeVoid("BitButil.documentPictureInPicture.disposeAll");
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
