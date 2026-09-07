using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Live handle to a <see href="https://developer.mozilla.org/en-US/docs/Web/API/PresentationConnection">PresentationConnection</see>:
/// the two-way channel between this page and the page running on the second screen.
/// </summary>
/// <remarks>
/// Closing and terminating are not the same thing, and the difference is the whole point of the API:
/// <see cref="Close"/> lets go of the connection while the presentation keeps running - so it can be
/// picked up again later with <see cref="Presentation.Reconnect"/> - while <see cref="Terminate"/>
/// ends the presentation itself. Disposing the handle closes, so a page that navigates away does not
/// take the second screen down with it.
/// </remarks>
public sealed class PresentationConnectionHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private readonly Action _unregister;
    private bool _disposed;

    internal PresentationConnectionHandle(IJSRuntime js, Guid id, string connectionId, string url, Action unregister)
    {
        _js = js;
        _id = id;
        _unregister = unregister;
        ConnectionId = connectionId;
        Url = url;
    }

    /// <summary>The internal connection id.</summary>
    public Guid Id => _id;

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PresentationConnection/id">PresentationConnection.id</see>:
    /// the presentation's own id, which <see cref="Presentation.Reconnect"/> needs. Worth persisting.
    /// </summary>
    public string ConnectionId { get; }

    /// <summary>The URL the receiver accepted and is showing.</summary>
    public string Url { get; }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PresentationConnection/send">PresentationConnection.send()</see>
    /// with text.
    /// </summary>
    /// <param name="message">What to send - typically JSON the receiving page knows how to read.</param>
    /// <returns>False when the connection is closed or still connecting.</returns>
    public ValueTask<bool> Send(string message)
        => _js.Invoke<bool>("BitButil.presentation.send", _id, message);

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PresentationConnection/send">PresentationConnection.send()</see>
    /// with binary data.
    /// </summary>
    /// <param name="data">The bytes to send.</param>
    /// <returns>False when the connection is closed or still connecting.</returns>
    public ValueTask<bool> Send(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return _js.Invoke<bool>("BitButil.presentation.sendBytes", _id, data);
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PresentationConnection/state">PresentationConnection.state</see>.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async ValueTask<PresentationConnectionState> GetState()
        => Presentation.ToConnectionState(await _js.Invoke<string>("BitButil.presentation.state", _id));

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PresentationConnection/close">PresentationConnection.close()</see>:
    /// lets go of the connection, leaving the presentation running on the other screen.
    /// </summary>
    /// <remarks>
    /// Keep <see cref="ConnectionId"/> if you intend to come back to it -
    /// <see cref="Presentation.Reconnect"/> is the only way in, and it needs that id.
    /// </remarks>
    public ValueTask Close() => DisposeAsync();

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PresentationConnection/terminate">PresentationConnection.terminate()</see>:
    /// ends the presentation itself, closing the page on the second screen.
    /// </summary>
    /// <returns>False when the connection was already gone.</returns>
    public async ValueTask<bool> Terminate()
    {
        if (_disposed) return false;
        _disposed = true;
        _unregister();

        return await _js.Invoke<bool>("BitButil.presentation.terminate", _id);
    }

    /// <summary>
    /// Closes the connection and detaches its listeners, leaving the presentation running. Calling it
    /// again does nothing.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _unregister();
        try { await _js.InvokeVoid("BitButil.presentation.close", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
