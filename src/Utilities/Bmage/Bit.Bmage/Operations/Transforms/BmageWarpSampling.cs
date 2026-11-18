namespace Bit.Bmage;

/// <summary>
/// Sampling method used by warp operations (arbitrary rotation, skew, perspective).
/// </summary>
/// <remarks>
/// The choice dominates the cost of a warp, because the sampler runs once per output pixel. Measured on desktop .NET
/// on a 12 megapixel image rotated by 37 degrees: <see cref="NearestNeighbor"/> about 105 MP/s,
/// <see cref="Bilinear"/> about 95 MP/s, <see cref="Bicubic"/> about 70 MP/s. A rotation by a multiple of 90 degrees
/// never samples at all and runs at roughly 350 MP/s. On WebAssembly's interpreter every figure is many times lower.
/// Interactive editing stays responsive because previews render against a downscaled proxy; pick a cheaper sampler
/// when warping a full-resolution image on the UI thread.
/// </remarks>
public enum BmageWarpSampling
{
    Bilinear = 0,
    NearestNeighbor,
    Bicubic,
}
