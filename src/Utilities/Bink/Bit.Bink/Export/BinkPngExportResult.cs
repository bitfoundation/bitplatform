namespace Bit.Bink;

/// <summary>The outcome of a raster export.</summary>
/// <param name="Bytes">The encoded PNG.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="SkippedTextElements">Text elements that were not drawn because no text renderer was supplied.</param>
/// <param name="SkippedImages">
/// Images that could not be decoded by the built-in decoder, or that were left out because decoding them would
/// have gone past <see cref="BinkPngExportOptions.MaxPixels"/> or was far larger than where they are drawn.
/// </param>
public readonly record struct BinkPngExportResult(byte[] Bytes, int Width, int Height, int SkippedTextElements, int SkippedImages)
{
    /// <summary>True when everything in the document was drawn.</summary>
    public bool IsComplete => SkippedTextElements == 0 && SkippedImages == 0;
}
