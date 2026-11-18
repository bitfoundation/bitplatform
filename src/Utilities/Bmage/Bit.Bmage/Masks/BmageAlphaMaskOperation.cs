namespace Bit.Bmage;

/// <summary>
/// Multiplies the image's alpha by a mask, so everything outside it becomes transparent: a round avatar, a shaped
/// cut-out, or a background removed by a segmentation model's matte through a <see cref="BmageBitmapMask"/>.
/// </summary>
/// <remarks>Exporting to a format without alpha, such as JPEG, fills the cleared area with the export background.</remarks>
public sealed class BmageAlphaMaskOperation : BmageImageOperation
{
    public BmageAlphaMaskOperation(BmageMask mask) => Mask = mask ?? throw new ArgumentNullException(nameof(mask));

    /// <summary>What stays visible.</summary>
    public BmageMask Mask { get; }

    public override string Name => "Mask";

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        ArgumentNullException.ThrowIfNull(source);
        var target = GetMutableCopy(source, context);
        try
        {
            using var mask = Mask.Render(source.Size, source, context.WithProgress(null));
            mask.ApplyToAlpha(target);
            context.ReportProgress(1);
            return target;
        }
        catch
        {
            if (!ReferenceEquals(target, source)) target.Dispose();
            throw;
        }
    }

    public override IBmageImageOperation ForScale(double scale) => scale == 1 ? this : new BmageAlphaMaskOperation(Mask.ForScale(scale));

    public override string ToString() => $"Mask({Mask.Kind})";
}
