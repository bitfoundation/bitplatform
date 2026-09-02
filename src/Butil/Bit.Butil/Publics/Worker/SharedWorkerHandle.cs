using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// This page's connection to a shared worker, returned by <see cref="Worker.CreateShared"/>.
/// </summary>
/// <remarks>
/// A shared worker is one instance for every page of the origin that names the same script and name,
/// so this handle is a <em>connection</em> rather than ownership: disposing it drops this page's
/// port, and the worker lives on for whoever else is still connected. There is deliberately no
/// Terminate - no page gets to kill a worker the others are using.
/// <br/>
/// Everything goes through <see cref="Port"/>, which delivers nothing until
/// <see cref="MessagePortHandle.Start"/> is called.
/// </remarks>
public sealed class SharedWorkerHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private bool _released;

    internal SharedWorkerHandle(IJSRuntime js, MessageChannel messageChannel, Guid id, Guid portId)
    {
        _js = js;
        Id = id;
        Port = new MessagePortHandle(js, messageChannel, portId);
    }

    /// <summary>The internal worker id.</summary>
    public Guid Id { get; }

    /// <summary>
    /// The port this page talks to the worker over. The worker's script sees the other end arrive as
    /// a <c>connect</c> event.
    /// </summary>
    public MessagePortHandle Port { get; }

    /// <summary>
    /// Closes this page's port and drops the connection. Idempotent, and safe during teardown.
    /// </summary>
    /// <remarks>
    /// The worker itself ends only when the last connected page has done this (or gone away).
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_released) return;
        _released = true;

        await Port.DisposeAsync();

        try { await _js.InvokeVoid("BitButil.worker.terminate", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
