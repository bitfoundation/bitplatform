namespace Bit.Bmage;

/// <summary>One item of a HEIF file: an image, a derived grid, or metadata such as Exif.</summary>
internal sealed class BmageHeifItem
{
    public uint Id;
    public uint Type;
    public string? ContentType;
    public int ConstructionMethod;
    public ulong BaseOffset;
    public readonly List<(ulong Offset, ulong Length)> Extents = [];
    public bool HasLocation;
    public readonly List<BmageHeifProperty> Properties = [];
    /// <summary>References from this item, by type: dimg (grid tiles), auxl (master of an auxiliary), cdsc, prem.</summary>
    public readonly List<(uint Type, uint To)> References = [];
}
