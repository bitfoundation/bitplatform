namespace Bit.Bmage;

/// <summary>Interpolation used for resampling.</summary>
public enum BmageResamplingFilter
{
    /// <summary>Choose automatically: area-averaging for downscaling, bicubic for upscaling.</summary>
    Auto = 0,
    /// <summary>Nearest neighbour. Fast, blocky; useful for pixel art.</summary>
    NearestNeighbor,
    /// <summary>Bilinear interpolation. Fast with moderate quality.</summary>
    Bilinear,
    /// <summary>Bicubic (Catmull-Rom style) interpolation. Good general purpose quality.</summary>
    Bicubic,
    /// <summary>Lanczos-3 windowed sinc. Highest quality, slowest.</summary>
    Lanczos3,
    /// <summary>Box (area averaging). Best for downscaling by large factors.</summary>
    Box,
}
