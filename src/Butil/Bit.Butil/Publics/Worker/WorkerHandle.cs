using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

/// <summary>
/// A running dedicated worker, returned by <see cref="Worker.Create"/>. Dispose it to terminate the
/// worker.
/// </summary>
public sealed class WorkerHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Action _onTerminated;
    private bool _terminated;

    internal WorkerHandle(IJSRuntime js, Guid id, Action onTerminated)
    {
        _js = js;
        Id = id;
        _onTerminated = onTerminated;
    }

    /// <summary>The internal worker id.</summary>
    public Guid Id { get; }

    /// <summary>
    /// Posts a message to the worker, serialized as JSON.
    /// </summary>
    /// <returns>False when the worker has already been terminated.</returns>
    /// <remarks>
    /// The worker's <c>onmessage</c> receives the parsed JSON. What survives the trip is what
    /// <c>System.Text.Json</c> can write and <c>JSON.parse</c> can read - not a structured clone of
    /// a .NET object. Use <see cref="PostBytes"/> for anything binary or large.
    /// </remarks>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<bool> PostMessage<[DynamicallyAccessedMembers(JsonSerialized)] T>(T value, JsonSerializerOptions? options = null)
        => _js.Invoke<bool>("BitButil.worker.postJson", Id, JsonSerializer.Serialize(value, options));

    /// <summary>
    /// Posts raw bytes to the worker.
    /// </summary>
    /// <param name="data">The bytes to send.</param>
    /// <param name="transfer">
    /// When true (the default) the <c>ArrayBuffer</c> is moved to the worker rather than copied.
    /// That is the whole reason to send bytes rather than JSON for a large payload: a copy of a
    /// hundred megabytes costs a hundred megabytes, and a transfer costs nothing.
    /// </param>
    /// <returns>False when the worker has already been terminated.</returns>
    public ValueTask<bool> PostBytes(byte[] data, bool transfer = true)
    {
        ArgumentNullException.ThrowIfNull(data);
        return _js.Invoke<bool>("BitButil.worker.postBytes", Id, data, transfer);
    }

    /// <summary>
    /// Posts a message that hands <see cref="MessagePortHandle"/>s to the worker.
    /// </summary>
    /// <returns>False when the worker has been terminated, or a port has already been released.</returns>
    /// <remarks>
    /// The ports are transferred, so the handles passed here stop working. This is the usual way to
    /// give a worker a private line to somewhere else on the page - or to give two workers a line to
    /// each other, by creating one channel and sending an end to each.
    /// </remarks>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<bool> PostWithPorts<[DynamicallyAccessedMembers(JsonSerialized)] T>(T value, MessagePortHandle[] ports, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(ports);
        return _js.Invoke<bool>("BitButil.worker.postWithPorts", Id,
            JsonSerializer.Serialize(value, options), Array.ConvertAll(ports, p => p.Id));
    }

    /// <summary>
    /// Stops the worker immediately.
    /// </summary>
    /// <remarks>
    /// There is no clean shutdown here: whatever the worker was doing is abandoned mid-statement,
    /// and nothing inside it gets a chance to run. If it holds something that needs closing, tell it
    /// to close it and wait for the reply before terminating.
    /// </remarks>
    public ValueTask Terminate() => DisposeAsync();

    /// <summary>
    /// Terminates the worker. Idempotent, and safe during teardown.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_terminated) return;
        _terminated = true;
        _onTerminated();
        try { await _js.InvokeVoid("BitButil.worker.terminate", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
