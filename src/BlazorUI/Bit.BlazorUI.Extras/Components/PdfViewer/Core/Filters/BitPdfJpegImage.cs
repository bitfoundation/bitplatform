// A baseline (sequential DCT, Huffman) JPEG decoder. Ported to the scope needed
// by the PDF image pipeline: it lets us decode CMYK/YCCK JPEGs (which browsers
// render wrong) and apply /SMask, /Mask and /Decode to DCT images. Progressive
// JPEGs are not handled here (the caller keeps browser passthrough for those).

namespace Bit.BlazorUI;

/// <summary>A decoded JPEG: 8-bit interleaved component samples plus geometry.</summary>
internal sealed class BitPdfJpegImage
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int Components { get; init; }
    /// <summary>Interleaved 8-bit samples, <see cref="Components"/> per pixel, in
    /// the JPEG's post-transform space: Gray (1), RGB (3), or CMYK (4).</summary>
    public required byte[] Data { get; init; }
}
