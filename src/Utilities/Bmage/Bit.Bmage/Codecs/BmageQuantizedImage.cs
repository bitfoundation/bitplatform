namespace Bit.Bmage;

/// <summary>An image reduced to a palette: one index per pixel into at most 256 colours.</summary>
/// <param name="Palette">The colours, translucent ones first, then opaque ones.</param>
/// <param name="Indices">One palette index per pixel, row by row.</param>
/// <param name="IsExact">True when the image had no more colours than allowed, so every pixel kept its colour.</param>
public sealed record BmageQuantizedImage(BmageRgba32[] Palette, byte[] Indices, bool IsExact)
{
    /// <summary>How many palette entries have any transparency; they come first.</summary>
    public int TranslucentCount
    {
        get
        {
            var count = 0;
            while (count < Palette.Length && Palette[count].A < 255) count++;
            return count;
        }
    }
}
