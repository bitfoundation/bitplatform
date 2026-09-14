namespace Bit.Butil;

/// <summary>
/// One compressed audio packet, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/EncodedAudioChunk">EncodedAudioChunk</see>.
/// </summary>
/// <param name="Type">Whether the packet stands on its own. Most audio codecs emit only key packets.</param>
/// <param name="Timestamp">Presentation timestamp in microseconds.</param>
/// <param name="Duration">The packet's length in microseconds, when the encoder reports it.</param>
/// <param name="Data">The compressed bytes.</param>
/// <param name="DecoderDescription">
/// The codec's out-of-band header, present only on the first chunk an encoder emits - what
/// <see cref="AudioDecoderConfig.Description"/> wants.
/// </param>
public record EncodedAudioChunk(EncodedChunkType Type,
                                long Timestamp,
                                long? Duration,
                                byte[] Data,
                                byte[]? DecoderDescription = null);
