using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/postMessage">window.postMessage</see>:
/// talking to another browsing context - an embedded iframe, the page that embedded you, or a window
/// you opened - across origins, where nothing else can reach.
/// </summary>
/// <remarks>
/// The same-origin policy stops one document touching another's DOM. This is the sanctioned hole in
/// it: a message, and an origin on each end, and nothing more.
/// <br/>
/// <b>Both origin checks are yours to make.</b> When sending, name the origin you expect in
/// <c>targetOrigin</c> rather than <c>"*"</c>, or the message is readable by whatever document
/// happens to be in that frame - which is not necessarily the one you loaded. When receiving, pass
/// the origins you trust to <see cref="Listen"/>: anyone holding a reference to your window can post
/// to it, so a listener that does not check is a listener that trusts every page on the internet.
/// </remarks>
[ButilService(typeof(WindowMessaging))]
public class WindowMessaging(IJSRuntime js, MessageChannel messageChannel) : IAsyncDisposable
{
    internal const string MessageMethodName = nameof(InvokeWindowMessage);

    private readonly ConcurrentDictionary<Guid, Action<WindowMessage>> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<WindowMessaging>? _dotNetRef;
    private DotNetObjectReference<WindowMessaging> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>window.postMessage</c>, which is everywhere.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (<c>false</c>) rather than
    /// throwing, so the result can't be distinguished from a genuine value.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.windowMessaging.isSupported");

    /// <summary>
    /// Invoked from JS for each message this window receives. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(MessageMethodName)]
    public void InvokeWindowMessage(Guid listenerId, string origin, bool isBinary, string? json, byte[]? data, Guid[] portIds)
    {
        if (_handlers.TryGetValue(listenerId, out var handler) is false) return;

        var ports = Array.ConvertAll(portIds ?? [], id => new MessagePortHandle(js, messageChannel, id));
        handler(new WindowMessage(origin, isBinary, json, data, ports));
    }

    /// <summary>
    /// Listens for messages posted to this window.
    /// </summary>
    /// <param name="onMessage">
    /// Called for every message that passes the origin filter. Messages that arrived carrying
    /// <see cref="MessagePortHandle"/>s hand them over in <see cref="WindowMessage.Ports"/> - they
    /// are yours now, and delivering nothing until started. Where more than one listener accepts a
    /// message, only the first one registered receives its ports; see <see cref="WindowMessage.Ports"/>.
    /// </param>
    /// <param name="allowedOrigins">
    /// The origins to accept, as <c>"https://example.com"</c>. Anything from elsewhere is dropped
    /// before your callback sees it. An empty array or null accepts everything, which is almost
    /// never what you want: a page you have no relationship with can post to any window it can
    /// reach, so an unfiltered listener is an unauthenticated input.
    /// </param>
    /// <returns>A subscription to dispose when you no longer want the callback.</returns>
    [DynamicDependency(nameof(InvokeWindowMessage), typeof(WindowMessaging))]
    public async ValueTask<ButilSubscription> Listen(Action<WindowMessage> onMessage, string[]? allowedOrigins = null)
    {
        ArgumentNullException.ThrowIfNull(onMessage);

        var listenerId = Guid.NewGuid();
        _handlers[listenerId] = onMessage;

        await js.Invoke<bool>("BitButil.windowMessaging.listen", DotNetRef, listenerId, allowedOrigins ?? []);

        return new ButilSubscription(listenerId, async () =>
        {
            _handlers.TryRemove(listenerId, out _);
            await js.InvokeVoid("BitButil.windowMessaging.removeListener", listenerId);
        });
    }

    /// <summary>
    /// A target for the document inside an <c>&lt;iframe&gt;</c>.
    /// </summary>
    /// <param name="iframe">The iframe element. Anything else has no content window, and posting fails.</param>
    /// <remarks>
    /// The window behind an iframe is replaced every time it navigates, so this looks the current
    /// one up on each send rather than holding a reference - which is also why a message sent before
    /// the frame has loaded goes nowhere rather than being queued.
    /// </remarks>
    public WindowMessageTarget Frame(ElementReference iframe) => new(js, "frame", iframe, null);

    /// <summary>
    /// A target for a window this page opened through <see cref="Window.Open(string?, string?, WindowFeatures?)"/>.
    /// </summary>
    /// <param name="windowId">The id <see cref="Window.Open(string?, string?, WindowFeatures?)"/> returned.</param>
    /// <remarks>
    /// Only reachable while the popup is open - a closed one, or an id from a
    /// <see cref="WindowFeatures.NoOpener"/> open (which deliberately hands back no reference at
    /// all), posts nothing.
    /// </remarks>
    public WindowMessageTarget OpenedWindow(string windowId) => new(js, "opened", null, windowId);

    /// <summary>
    /// A target for the document that embedded this one. In a top-level page this is the page
    /// itself, so a message posted here comes straight back to your own listener.
    /// </summary>
    public WindowMessageTarget Parent() => new(js, "parent", null, null);

    /// <summary>
    /// A target for the outermost document of this frame tree - the parent's parent's parent, all
    /// the way up.
    /// </summary>
    public WindowMessageTarget Top() => new(js, "top", null, null);

    /// <summary>
    /// A target for the window that opened this one. Null on the other side of a
    /// <see cref="WindowFeatures.NoOpener"/> open, and posting is then a no-op - which is exactly
    /// what that feature is for.
    /// </summary>
    public WindowMessageTarget Opener() => new(js, "opener", null, null);

    /// <summary>
    /// On scope/circuit teardown, removes every listener whose subscription was never disposed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _handlers.Clear();
            await js.InvokeVoid("BitButil.windowMessaging.disposeAll");
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
