using Bit.Bink;

namespace Bit.Bmage;

/// <summary>A Bink drawing rendered to pixels, with what the managed rasteriser had to leave out.</summary>
/// <param name="Image">The pixels. The caller owns them and disposes them.</param>
/// <param name="SkippedTextElements">
/// Text elements left out because no <see cref="BinkPngExportOptions.TextRasterizer"/> was given: Bink's managed
/// rasteriser draws text only through one.
/// </param>
/// <param name="SkippedImages">Image elements left out because they are not embedded PNG, which is all the managed rasteriser decodes.</param>
public sealed record BmageBinkRenderedImage(BmageImageBuffer Image, int SkippedTextElements, int SkippedImages)
{
    /// <summary>True when every element was drawn.</summary>
    public bool IsComplete => SkippedTextElements == 0 && SkippedImages == 0;
}
