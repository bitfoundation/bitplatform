namespace Bit.Bmage;

/// <summary>Services available while rendering annotations.</summary>
public sealed class BmageAnnotationRenderContext
{
    public BmageAnnotationRenderContext(IBmageTextRasterizer? textRasterizer = null, double scale = 1.0)
    {
        TextRasterizer = textRasterizer;
        Scale = scale;
    }

    /// <summary>Rasteriser used for text annotations. Text is skipped when this is null.</summary>
    public IBmageTextRasterizer? TextRasterizer { get; }

    /// <summary>
    /// The scale of the surface relative to the original image. Annotations are pre-scaled by the renderer, so this is
    /// informational (useful for choosing detail levels).
    /// </summary>
    public double Scale { get; }

    /// <summary>Allocation, limits, threads and cancellation for annotations that run an operation, such as a redaction's blur.</summary>
    internal BmageOperationContext Operation { get; init; } = BmageOperationContext.Default;
}
