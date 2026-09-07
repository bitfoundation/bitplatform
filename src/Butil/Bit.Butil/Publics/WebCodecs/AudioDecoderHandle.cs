using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Live handle to an <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioDecoder">AudioDecoder</see>
/// created by <see cref="WebCodecs.CreateAudioDecoder"/>: takes <see cref="EncodedAudioChunk"/>
/// instances and emits <see cref="AudioDataHandle"/> ones on its output callback.
/// </summary>
/// <remarks>
/// The decoded samples are ordinary PCM: feed them to <see cref="WebAudio"/> through an
/// <see cref="AudioBufferHandle"/>, write them to a file, or analyse them - the decoder itself plays
/// nothing.
/// </remarks>
public sealed class AudioDecoderHandle : WebCodecsHandle
{
    internal const string DataMethodName = nameof(InvokeAudioDecoderData);
    internal const string ErrorMethodName = nameof(InvokeAudioDecoderError);

    private readonly Action<AudioDataHandle> _onData;
    private readonly Action<string>? _onError;

    internal AudioDecoderHandle(IJSRuntime js, Guid id, Action<AudioDataHandle> onData, Action<string>? onError)
        : base(js, id)
    {
        _onData = onData;
        _onError = onError;
        CallbackRef = DotNetObjectReference.Create(this);
        TrackCallbackRef(CallbackRef);
    }

    internal DotNetObjectReference<AudioDecoderHandle> CallbackRef { get; }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioDecoder/decode">AudioDecoder.decode()</see>:
    /// queues one compressed packet.
    /// </summary>
    /// <param name="chunk">The packet to decode.</param>
    /// <returns>False when the decoder is unconfigured or closed, or the packet was rejected.</returns>
    public ValueTask<bool> Decode(EncodedAudioChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        return Js.Invoke<bool>("BitButil.webCodecs.decode", HandleId,
                               chunk.Type == EncodedChunkType.Delta ? "delta" : "key",
                               chunk.Timestamp, chunk.Duration, chunk.Data);
    }

    /// <summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AudioDecoder/configure">AudioDecoder.configure()</see>:
    /// changes the configuration of a running decoder.
    /// </summary>
    /// <param name="config">The new configuration.</param>
    /// <returns>False when the configuration is unsupported, or the decoder is closed.</returns>
    public ValueTask<bool> Configure(AudioDecoderConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return Js.Invoke<bool>("BitButil.webCodecs.configure", HandleId, config);
    }

    /// <summary>
    /// Invoked from JS for each decoded block of samples. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(DataMethodName)]
    public void InvokeAudioDecoderData(Guid id, Guid dataId, long timestamp, long? duration, int sampleRate, int numberOfFrames, int numberOfChannels, string format)
    {
        if (id != HandleId) return;

        _onData.Invoke(new AudioDataHandle(Js, dataId, timestamp, duration, sampleRate, numberOfFrames, numberOfChannels, format));
    }

    /// <summary>
    /// Invoked from JS when the decoder raises an error. Public + <see cref="JSInvokableAttribute"/>
    /// so it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ErrorMethodName)]
    public void InvokeAudioDecoderError(Guid id, string message)
    {
        if (id != HandleId) return;

        _onError?.Invoke(message);
    }
}
