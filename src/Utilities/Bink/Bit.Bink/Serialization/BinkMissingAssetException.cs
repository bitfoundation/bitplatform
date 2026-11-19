namespace Bit.Bink;

/// <summary>
/// An operation referred to an image by its content id, and the reader does not hold that image. The operation is not
/// malformed, so this is not a <see cref="BinkFormatException"/>: the image went over the wire earlier, before this
/// participant could keep it, and the session fetches it rather than rejecting the change.
/// </summary>
internal sealed class BinkMissingAssetException(string id) : Exception($"The image {id} the change refers to is not available here.")
{
    /// <summary>The content id of the missing image.</summary>
    public string AssetId { get; } = id;
}
