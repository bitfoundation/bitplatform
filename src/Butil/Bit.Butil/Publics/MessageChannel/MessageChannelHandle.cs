using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A channel and its two ports, returned by <see cref="MessageChannel.Create"/>.
/// </summary>
/// <remarks>
/// The two ports are symmetric - there is no client end and no server end. Keep one, give the other
/// away, and remember that giving it away transfers it.
/// </remarks>
public sealed class MessageChannelHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Guid _channelId;
    private bool _released;

    internal MessageChannelHandle(IJSRuntime js, MessageChannel owner, Guid channelId, Guid firstPortId, Guid secondPortId)
    {
        _js = js;
        _channelId = channelId;
        Owner = owner;
        Port1 = new MessagePortHandle(js, owner, firstPortId);
        Port2 = new MessagePortHandle(js, owner, secondPortId);
    }

    private MessageChannel Owner { get; }

    /// <summary>One end of the channel.</summary>
    public MessagePortHandle Port1 { get; }

    /// <summary>The other end. Symmetric with <see cref="Port1"/> - neither is privileged.</summary>
    public MessagePortHandle Port2 { get; }

    /// <summary>
    /// Closes and releases both ports. Idempotent, and safe during teardown.
    /// </summary>
    /// <remarks>
    /// A port that was transferred away is no longer this channel's to close, and releasing it here
    /// is a no-op rather than an error - the receiver's copy keeps working.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_released) return;
        _released = true;
        Owner.RemovePortListeners(Port1.Id);
        Owner.RemovePortListeners(Port2.Id);

        try { await _js.InvokeVoid("BitButil.messageChannel.releaseChannel", _channelId); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
