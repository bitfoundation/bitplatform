namespace Bit.BbScanner;

/// <summary>One Data Matrix error correction block group.</summary>
/// <param name="Count">Number of blocks in the group.</param>
/// <param name="DataCodewords">Data codewords per block.</param>
public readonly record struct BbDataMatrixEcBlockGroup(int Count, int DataCodewords);

/// <summary>
/// One of the thirty ECC 200 symbol shapes: twenty four square and six rectangular.
/// </summary>
/// <remarks>
/// A Data Matrix symbol is tiled from data regions, each surrounded by a solid "L" finder on two
/// sides and a dashed timing pattern on the other two. The version therefore describes both the
/// overall symbol size and the size of one region, because the parser has to strip the region
/// borders before the codeword placement makes any sense.
/// </remarks>
public sealed class BbDataMatrixVersion
{
    private static readonly BbDataMatrixVersion[] Versions = BuildVersions();

    private BbDataMatrixVersion(
        int versionNumber,
        int symbolSizeRows,
        int symbolSizeColumns,
        int dataRegionSizeRows,
        int dataRegionSizeColumns,
        int ecCodewords,
        BbDataMatrixEcBlockGroup[] groups)
    {
        VersionNumber = versionNumber;
        SymbolSizeRows = symbolSizeRows;
        SymbolSizeColumns = symbolSizeColumns;
        DataRegionSizeRows = dataRegionSizeRows;
        DataRegionSizeColumns = dataRegionSizeColumns;
        EcCodewords = ecCodewords;
        Groups = groups;

        var total = 0;
        foreach (var group in groups)
        {
            total += group.Count * group.DataCodewords;
        }

        TotalDataCodewords = total;
    }

    /// <summary>The version number, 1 to 30.</summary>
    public int VersionNumber { get; }

    /// <summary>Symbol height in modules, including region borders.</summary>
    public int SymbolSizeRows { get; }

    /// <summary>Symbol width in modules, including region borders.</summary>
    public int SymbolSizeColumns { get; }

    /// <summary>Height of one data region in modules, excluding its borders.</summary>
    public int DataRegionSizeRows { get; }

    /// <summary>Width of one data region in modules, excluding its borders.</summary>
    public int DataRegionSizeColumns { get; }

    /// <summary>Error correction codewords per block.</summary>
    public int EcCodewords { get; }

    /// <summary>The block groups.</summary>
    public BbDataMatrixEcBlockGroup[] Groups { get; }

    /// <summary>Total data codewords across every block.</summary>
    public int TotalDataCodewords { get; }

    /// <summary>Total number of blocks.</summary>
    public int NumBlocks
    {
        get
        {
            var total = 0;
            foreach (var group in Groups)
            {
                total += group.Count;
            }

            return total;
        }
    }

    /// <summary>Total codewords in the symbol, data plus error correction.</summary>
    public int TotalCodewords => TotalDataCodewords + (EcCodewords * NumBlocks);

    /// <summary><see langword="true"/> for the rectangular shapes.</summary>
    public bool IsRectangular => SymbolSizeRows != SymbolSizeColumns;

    /// <summary>Every shape, in ascending version order.</summary>
    public static IReadOnlyList<BbDataMatrixVersion> All => Versions;

    /// <summary>Finds the shape with the given module dimensions.</summary>
    /// <param name="numRows">Symbol height in modules.</param>
    /// <param name="numColumns">Symbol width in modules.</param>
    /// <returns>The shape, or <see langword="null"/> when the dimensions are not a legal symbol size.</returns>
    public static BbDataMatrixVersion? GetVersionForDimensions(int numRows, int numColumns)
    {
        // Every ECC 200 symbol has an even number of rows and columns.
        if ((numRows & 0x01) != 0 || (numColumns & 0x01) != 0)
        {
            return null;
        }

        foreach (var version in Versions)
        {
            if (version.SymbolSizeRows == numRows && version.SymbolSizeColumns == numColumns)
            {
                return version;
            }
        }

        return null;
    }

    private static BbDataMatrixVersion Create(
        int version, int rows, int columns, int regionRows, int regionColumns, int ecCodewords, params int[] groups)
    {
        var blockGroups = new BbDataMatrixEcBlockGroup[groups.Length / 2];
        for (var i = 0; i < blockGroups.Length; i++)
        {
            blockGroups[i] = new BbDataMatrixEcBlockGroup(groups[i * 2], groups[(i * 2) + 1]);
        }

        return new BbDataMatrixVersion(version, rows, columns, regionRows, regionColumns, ecCodewords, blockGroups);
    }

    private static BbDataMatrixVersion[] BuildVersions() =>
    [
        // Square symbols.
        Create(1, 10, 10, 8, 8, 5, 1, 3),
        Create(2, 12, 12, 10, 10, 7, 1, 5),
        Create(3, 14, 14, 12, 12, 10, 1, 8),
        Create(4, 16, 16, 14, 14, 12, 1, 12),
        Create(5, 18, 18, 16, 16, 14, 1, 18),
        Create(6, 20, 20, 18, 18, 18, 1, 22),
        Create(7, 22, 22, 20, 20, 20, 1, 30),
        Create(8, 24, 24, 22, 22, 24, 1, 36),
        Create(9, 26, 26, 24, 24, 28, 1, 44),
        Create(10, 32, 32, 14, 14, 36, 1, 62),
        Create(11, 36, 36, 16, 16, 42, 1, 86),
        Create(12, 40, 40, 18, 18, 48, 1, 114),
        Create(13, 44, 44, 20, 20, 56, 1, 144),
        Create(14, 48, 48, 22, 22, 68, 1, 174),
        Create(15, 52, 52, 24, 24, 42, 2, 102),
        Create(16, 64, 64, 14, 14, 56, 2, 140),
        Create(17, 72, 72, 16, 16, 36, 4, 92),
        Create(18, 80, 80, 18, 18, 48, 4, 114),
        Create(19, 88, 88, 20, 20, 56, 4, 144),
        Create(20, 96, 96, 22, 22, 68, 4, 174),
        Create(21, 104, 104, 24, 24, 56, 6, 136),
        Create(22, 120, 120, 18, 18, 68, 6, 175),
        Create(23, 132, 132, 20, 20, 62, 8, 163),
        Create(24, 144, 144, 22, 22, 62, 8, 156, 2, 155),

        // Rectangular symbols.
        Create(25, 8, 18, 6, 16, 7, 1, 5),
        Create(26, 8, 32, 6, 14, 11, 1, 10),
        Create(27, 12, 26, 10, 24, 14, 1, 16),
        Create(28, 12, 36, 10, 16, 18, 1, 22),
        Create(29, 16, 36, 14, 16, 24, 1, 32),
        Create(30, 16, 48, 14, 22, 28, 1, 49),
    ];

    /// <inheritdoc />
    public override string ToString() =>
        $"{SymbolSizeRows}x{SymbolSizeColumns}";
}
