namespace Bit.Bmage;

/// <summary>
/// Applies the EXIF orientation recorded in the buffer's metadata so the pixels appear upright, then resets the stored
/// orientation to <see cref="BmageOrientation.Normal"/>. A no-op when there is no orientation metadata.
/// </summary>
public sealed class BmageAutoOrientOperation : BmageImageOperation
{
    public static BmageAutoOrientOperation Instance { get; } = new();

    public override string Name => "Auto orient";

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        var orientation = source.Metadata?.Exif?.Orientation ?? BmageOrientation.Normal;
        if (orientation is BmageOrientation.Normal or BmageOrientation.Unspecified)
            return context.CanMutateSource ? source : source.Clone(context.Allocator);
        var result = new BmageOrientationOperation(orientation).Apply(source, context);
        if (result.Metadata is { } md) result.Metadata = md.WithOrientation(BmageOrientation.Normal);
        return result;
    }
}
