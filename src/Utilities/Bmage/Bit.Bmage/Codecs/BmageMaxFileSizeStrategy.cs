namespace Bit.Bmage;

/// <summary>Strategy used to satisfy <see cref="BmageImageExportOptions.MaxFileSize"/>.</summary>
public enum BmageMaxFileSizeStrategy
{
    /// <summary>Lower quality first (down to <see cref="BmageImageExportOptions.MinQuality"/>), then shrink dimensions.</summary>
    QualityThenDimensions = 0,
    /// <summary>Only lower quality; throw if the limit cannot be met.</summary>
    QualityOnly,
    /// <summary>Keep quality fixed and shrink dimensions only.</summary>
    DimensionsOnly,
}
