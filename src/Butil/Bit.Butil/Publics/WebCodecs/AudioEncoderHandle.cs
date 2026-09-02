using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Live handle to an <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioEncoder">AudioEncoder</see>
/// created by <see cref="WebCodecs.CreateAudioEncoder"/>: takes <see cref="AudioDataHandle"/>
/// instances and emits <see cref="EncodedAudioChunk"/> ones on its output callback.
/// </summary>
public sealed class AudioEncoderHandle : WebCodecsHandle
{
    internal const string ChunkMethodName = nameof(InvokeAudioEncoderChunk);
    internal const string ErrorMethodName = nameof(InvokeAudioEncoderError);

    private readonly Action<EncodedAudioChunk> _onChunk;
    private readonly Action<string>? _onError;

    internal AudioEncoderHandle(IJSRuntime js, Guid id, Action<EncodedAudioChunk> onChunk, Action<string>? onError)
        : base(js, id)
    {
        _onChunk = onChunk;
        _onError = onError;
        CallbackRef = DotNetObjectReference.Create(this);
        TrackCallbackRef(CallbackRef);
    }

    internal DotNetObjectReference<AudioEncoderHandle> CallbackRef { get; }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioEncoder/encode">AudioEncoder.encode()</see>:
    /// queues one block of samples.
    /// </summary>
    /// <param name="data">The samples to encode. Their sample rate and channel count have to match the configuration.</param>
    /// <returns>False when the encoder is unconfigured or closed, or the data is already disposed.</returns>
    public ValueTask<bool> Encode(AudioDataHandle data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Js.Invoke<bool>("BitButil.webCodecs.encodeAudio", HandleId, data.Id);
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioEncoder/configure">AudioEncoder.configure()</see>:
    /// changes the configuration of a running encoder.
    /// </summary>
    /// <param name="config">The new configuration.</param>
    /// <returns>False when the configuration is unsupported, or the encoder is closed.</returns>
    public ValueTask<bool> Configure(AudioEncoderConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return Js.Invoke<bool>("BitButil.webCodecs.configure", HandleId, config);
    }

    /// <summary>
    /// Invoked from JS for each encoded packet. Public + <see cref="JSInvokableAttribute"/> so it can
    /// be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ChunkMethodName)]
    public void InvokeAudioEncoderChunk(Guid id, string type, long timestamp, long? duration, byte[] data, byte[]? description)
    {
        if (id != HandleId) return;

        _onChunk.Invoke(new EncodedAudioChunk(ToChunkType(type), timestamp, duration, data, description));
    }

    /// <summary>
    /// Invoked from JS when the encoder raises an error. Public + <see cref="JSInvokableAttribute"/>
    /// so it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ErrorMethodName)]
    public void InvokeAudioEncoderError(Guid id, string message)
    {
        if (id != HandleId) return;

        _onError?.Invoke(message);
    }
}
