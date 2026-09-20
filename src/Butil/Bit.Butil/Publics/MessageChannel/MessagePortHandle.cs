using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

/// <summary>
/// One end of a <see cref="MessageChannel"/> - or the port a shared worker answers on.
/// </summary>
/// <remarks>
/// A port belongs to exactly one context at a time. <see cref="PostWithPorts"/> and
/// <see cref="WorkerHandle.PostWithPorts"/> <em>transfer</em> the ports they carry: the receiver
/// gets them and this side's handles stop working, which is not a bug to work around but the
/// guarantee that makes a port a private conversation.
/// <br/>
/// Nothing arrives until <see cref="Start"/> is called. Messages sent before that are queued, not
/// dropped.
/// </remarks>
public sealed class MessagePortHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly MessageChannel _owner;
    private bool _released;

    internal MessagePortHandle(IJSRuntime js, MessageChannel owner, Guid id)
    {
        _js = js;
        _owner = owner;
        Id = id;
    }

    /// <summary>The internal port id - what crosses the interop boundary when a port is transferred.</summary>
    public Guid Id { get; }

    /// <summary>
    /// Runs <paramref name="onMessage"/> for every message this port receives.
    /// </summary>
    /// <returns>A subscription to dispose when you no longer want the callback, or null when the port has been released.</returns>
    /// <remarks>
    /// Listening does not start the port - call <see cref="Start"/> when you are ready to receive.
    /// Several listeners can share one port; they all see every message.
    /// </remarks>
    public ValueTask<ButilSubscription?> OnMessage(Action<ButilMessage> onMessage) => _owner.AddPortListener(Id, onMessage);

    /// <summary>
    /// Begins delivery. Everything the port received while it was closed arrives now, in order.
    /// </summary>
    /// <remarks>
    /// Idempotent. A port that is never started queues messages for as long as it lives, which is a
    /// slow leak rather than an error - so start every port you intend to read from.
    /// </remarks>
    public ValueTask Start() => _js.InvokeVoid("BitButil.messageChannel.start", Id);

    /// <summary>
    /// Posts a message, serialized as JSON.
    /// </summary>
    /// <returns>False when the port has been released, closed, or transferred away.</returns>
    /// <remarks>
    /// The receiver sees the deserialized JSON, not a structured clone of a .NET object: what
    /// survives the trip is what <c>System.Text.Json</c> can write and <c>JSON.parse</c> can read.
    /// Use <see cref="PostBytes"/> for anything binary.
    /// </remarks>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<bool> PostMessage<[DynamicallyAccessedMembers(JsonSerialized)] T>(T value, JsonSerializerOptions? options = null)
        => _js.Invoke<bool>("BitButil.messageChannel.postJson", Id, JsonSerializer.Serialize(value, options));

    /// <summary>
    /// Posts raw bytes.
    /// </summary>
    /// <param name="data">The bytes to send.</param>
    /// <param name="transfer">
    /// When true the underlying buffer is <em>moved</em> to the receiver rather than copied, which
    /// is the reason to send bytes rather than JSON for anything large. The sender's copy is
    /// detached afterwards - on the JavaScript side, at least; the .NET array you passed in is a
    /// separate copy already and is unaffected.
    /// </param>
    /// <returns>False when the port has been released, closed, or transferred away.</returns>
    public ValueTask<bool> PostBytes(byte[] data, bool transfer = true)
    {
        ArgumentNullException.ThrowIfNull(data);
        return _js.Invoke<bool>("BitButil.messageChannel.postBytes", Id, data, transfer);
    }

    /// <summary>
    /// Posts a message that carries other ports with it.
    /// </summary>
    /// <returns>False when this port, or any of the ports being sent, has already been released.</returns>
    /// <remarks>
    /// The ports are transferred: the receiver owns them, and the handles passed here stop working.
    /// This is how a third party is given a private line - hand it one end of a channel it did not
    /// create.
    /// </remarks>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<bool> PostWithPorts<[DynamicallyAccessedMembers(JsonSerialized)] T>(T value, MessagePortHandle[] ports, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(ports);
        return _js.Invoke<bool>("BitButil.messageChannel.postWithPorts", Id,
            JsonSerializer.Serialize(value, options), Array.ConvertAll(ports, p => p.Id));
    }

    /// <summary>
    /// Closes the port. The other end sees no further messages, and nothing more can be sent.
    /// </summary>
    public ValueTask Close() => _js.InvokeVoid("BitButil.messageChannel.close", Id);

    /// <summary>
    /// Closes the port and releases its registry entry. Idempotent, and safe during teardown.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_released) return;
        _released = true;
        _owner.RemovePortListeners(Id);

        try
        {
            await _js.InvokeVoid("BitButil.messageChannel.close", Id);
            await _js.InvokeVoid("BitButil.messageChannel.release", Id);
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
