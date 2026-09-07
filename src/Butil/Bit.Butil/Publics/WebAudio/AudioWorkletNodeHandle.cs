using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// An <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioWorkletNode">AudioWorkletNode</see>:
/// the page's own DSP code, running on the audio thread.
/// </summary>
/// <remarks>
/// This is the only place custom per-sample processing can live. The processor itself is JavaScript
/// registered by <see cref="WebAudio.AddWorkletModule"/> - it has to be, because the audio thread
/// cannot call into .NET, and anything that blocks it produces an audible dropout.
/// <br/>
/// What crosses into .NET is the message port: the processor posts what it has measured or decided,
/// and <see cref="PostMessage"/> sends parameters and commands back. Both directions are
/// asynchronous and neither is on the audio thread, which is exactly what makes them safe.
/// </remarks>
public sealed class AudioWorkletNodeHandle : AudioNodeHandle
{
    internal const string MessageMethodName = nameof(InvokeAudioWorkletMessage);

    private readonly Action<string>? _onMessage;
    private DotNetObjectReference<AudioWorkletNodeHandle>? _dotNetRef;

    internal AudioWorkletNodeHandle(IJSRuntime js, Guid id, Action<string>? onMessage) : base(js, id)
    {
        _onMessage = onMessage;
        if (onMessage is not null) _dotNetRef = DotNetObjectReference.Create(this);
    }

    internal DotNetObjectReference<AudioWorkletNodeHandle>? CallbackRef => _dotNetRef;

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioWorkletNode/port">port.postMessage()</see>:
    /// sends a message to the processor.
    /// </summary>
    /// <param name="message">The message text - JSON is the usual choice, since the processor gets it as a string.</param>
    /// <returns>False when the node is gone.</returns>
    /// <remarks>
    /// For values that change continuously, prefer an
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioWorkletNode/parameters">AudioParam</see>
    /// declared by the processor and driven with <see cref="AudioNodeHandle.SetParam"/>: those are
    /// sample-accurate, while messages arrive whenever the thread gets to them.
    /// </remarks>
    public ValueTask<bool> PostMessage(string message)
        => Js.Invoke<bool>("BitButil.webAudio.postWorkletMessage", NodeId, message);

    /// <summary>
    /// Invoked from JS for each message the processor posts. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    /// <remarks>
    /// A message that is not already a string arrives as its JSON text, so a processor can post an
    /// ordinary object without either side agreeing on a type first.
    /// </remarks>
    [JSInvokable(MessageMethodName)]
    public void InvokeAudioWorkletMessage(Guid id, string message)
    {
        if (id != NodeId) return;

        _onMessage?.Invoke(message);
    }

    /// <summary>
    /// Closes the message port, disconnects the node and drops it. Calling it again does nothing.
    /// </summary>
    public override async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
    }
}
