using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaStreamAudioDestinationNode">MediaStreamAudioDestinationNode</see>:
/// the end of a graph that produces a <c>MediaStream</c> instead of sound in the speakers.
/// </summary>
/// <remarks>
/// This is how processed audio leaves the Web Audio world: record the graph's output with
/// <see cref="MediaRecorder"/>, send it over a peer connection, or play it back through an element.
/// A microphone routed through a filter chain and out through one of these is a recording of the
/// processed signal, not the raw one.
/// <br/>
/// Note that the stream carries only what is connected into this node - not what other parts of the
/// graph send to the speakers.
/// </remarks>
public sealed class MediaStreamAudioDestinationHandle : AudioNodeHandle
{
    private readonly Guid _streamId;

    internal MediaStreamAudioDestinationHandle(IJSRuntime js, Guid id, Guid streamId) : base(js, id)
    {
        _streamId = streamId;
    }

    /// <summary>
    /// The node's output as an ordinary Butil media stream, ready for
    /// <see cref="MediaRecorder.Start"/> or <see cref="MediaStreamHandle.AttachTo"/>.
    /// </summary>
    /// <remarks>
    /// The returned handle names the same stream every time. Disposing it stops the stream's tracks,
    /// which cannot be undone - so let the node's own disposal deal with teardown unless you mean to
    /// end the stream early.
    /// </remarks>
    public MediaStreamHandle GetStream() => new(Js, _streamId);
}
