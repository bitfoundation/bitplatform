using System.Drawing;

namespace Bit.Bmage;

/// <summary>What a pipeline run is expected to allocate, from <see cref="BmageImagePipeline.EstimateMemory"/>.</summary>
/// <param name="PeakBytes">The most pixel memory the run holds at once, its result included, the source not.</param>
/// <param name="OutputBytes">The size of the result.</param>
/// <param name="OutputSize">The dimensions of the result.</param>
public sealed record BmagePipelineMemoryEstimate(long PeakBytes, long OutputBytes, Size OutputSize);
