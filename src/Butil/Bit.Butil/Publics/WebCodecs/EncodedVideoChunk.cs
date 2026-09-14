namespace Bit.Butil;

/// <summary>
/// One compressed video frame, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/EncodedVideoChunk">EncodedVideoChunk</see>.
/// </summary>
/// <remarks>
/// These come out of a <see cref="VideoEncoderHandle"/> and go into a
/// <see cref="VideoDecoderHandle"/> - or into a container the app muxes itself, or onto a transport
/// it sends over. Timestamps are microseconds, which is what WebCodecs works in throughout.
/// </remarks>
/// <param name="Type">Whether the chunk stands on its own.</param>
/// <param name="Timestamp">Presentation timestamp in microseconds.</param>
/// <param name="Duration">How long the frame is shown, in microseconds, when the encoder reports it.</param>
/// <param name="Data">The compressed bytes.</param>
/// <param name="DecoderDescription">
/// The codec's out-of-band parameter sets, present only on the first chunk an encoder emits. Keep
/// them: they are what a matching <see cref="VideoDecoderConfig.Description"/> needs, and a decoder
/// built without them decodes nothing.
/// </param>
public record EncodedVideoChunk(EncodedChunkType Type,
                                long Timestamp,
                                long? Duration,
                                byte[] Data,
                                byte[]? DecoderDescription = null);
