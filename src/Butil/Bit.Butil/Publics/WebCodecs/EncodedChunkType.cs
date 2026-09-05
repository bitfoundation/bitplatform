namespace Bit.Butil;

/// <summary>
/// Whether a chunk can be decoded on its own, the <c>type</c> of an
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/EncodedVideoChunk/type">EncodedVideoChunk</see>.
/// </summary>
public enum EncodedChunkType
{
    /// <summary>
    /// A key frame: decodable without anything before it. The only place a decoder can start, and
    /// therefore the only place a stream can be cut or seeked to.
    /// </summary>
    Key,

    /// <summary>A delta frame: meaningless without the chunks it was predicted from.</summary>
    Delta
}
