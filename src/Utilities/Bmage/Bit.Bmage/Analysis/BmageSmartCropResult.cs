using System.Drawing;

namespace Bit.Bmage;

/// <summary>The chosen crop and how it scored.</summary>
public readonly record struct BmageSmartCropResult(Rectangle Rectangle, double Score)
{
    /// <summary>Builds a crop operation for this result.</summary>
    public BmageCropOperation ToOperation() => new(Rectangle);
}
