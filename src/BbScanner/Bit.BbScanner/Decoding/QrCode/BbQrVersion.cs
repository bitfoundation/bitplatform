namespace Bit.BbScanner;

/// <summary>
/// One error correction block group: <paramref name="Count"/> blocks that each carry
/// <paramref name="DataCodewords"/> data codewords.
/// </summary>
/// <param name="Count">Number of blocks in the group.</param>
/// <param name="DataCodewords">Data codewords per block.</param>
public readonly record struct BbQrEcBlockGroup(int Count, int DataCodewords);

/// <summary>
/// The block layout for one version at one error correction level.
/// </summary>
/// <param name="EcCodewordsPerBlock">Error correction codewords appended to each block.</param>
/// <param name="Groups">One or two groups of blocks; the second group holds one extra data codeword per block.</param>
public sealed record BbQrEcBlocks(int EcCodewordsPerBlock, BbQrEcBlockGroup[] Groups)
{
    /// <summary>Total number of error correction blocks.</summary>
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

    /// <summary>Total number of error correction codewords across every block.</summary>
    public int TotalEcCodewords => EcCodewordsPerBlock * NumBlocks;

    /// <summary>Total number of data codewords across every block.</summary>
    public int TotalDataCodewords
    {
        get
        {
            var total = 0;
            foreach (var group in Groups)
            {
                total += group.Count * group.DataCodewords;
            }

            return total;
        }
    }
}

/// <summary>
/// Everything the decoder needs to know about one of the forty QR Code versions.
/// </summary>
/// <remarks>
/// The block tables are the part of the QR specification that cannot be derived, so they are
/// transcribed here. Everything that can be derived, including the format and version
/// information error correction codes, is computed at start-up instead, which removes a large
/// class of transcription bugs.
/// </remarks>
public sealed class BbQrVersion
{
    private static readonly BbQrVersion[] Versions = BuildVersions();

    private BbQrVersion(int versionNumber, int[] alignmentPatternCenters, BbQrEcBlocks[] ecBlocks)
    {
        VersionNumber = versionNumber;
        AlignmentPatternCenters = alignmentPatternCenters;
        EcBlocks = ecBlocks;

        var total = 0;
        var blocks = ecBlocks[0];
        total += blocks.TotalEcCodewords;
        total += blocks.TotalDataCodewords;
        TotalCodewords = total;
    }

    /// <summary>The version number, 1 to 40.</summary>
    public int VersionNumber { get; }

    /// <summary>Row and column coordinates the alignment pattern centres are drawn at.</summary>
    public int[] AlignmentPatternCenters { get; }

    /// <summary>Block layout indexed by <see cref="BbQrErrorCorrectionLevelExtensions.ToTableIndex"/>.</summary>
    public BbQrEcBlocks[] EcBlocks { get; }

    /// <summary>Total codewords in the symbol, data plus error correction.</summary>
    public int TotalCodewords { get; }

    /// <summary>Width and height of the symbol in modules.</summary>
    public int DimensionForVersion => 17 + (4 * VersionNumber);

    /// <summary>Returns the block layout for a level.</summary>
    /// <param name="level">The error correction level.</param>
    public BbQrEcBlocks GetEcBlocksForLevel(BbQrErrorCorrectionLevel level) => EcBlocks[level.ToTableIndex()];

    /// <summary>Returns a version by number.</summary>
    /// <param name="versionNumber">The version number, 1 to 40.</param>
    public static BbQrVersion GetVersionForNumber(int versionNumber)
    {
        if (versionNumber is < 1 or > 40)
        {
            throw new ArgumentOutOfRangeException(nameof(versionNumber), "QR Code versions run from 1 to 40.");
        }

        return Versions[versionNumber - 1];
    }

    /// <summary>Every version, in ascending order.</summary>
    public static IReadOnlyList<BbQrVersion> All => Versions;

    /// <summary>
    /// Returns the version whose symbol is <paramref name="dimension"/> modules across,
    /// or <see langword="null"/> when the dimension is not a legal QR size.
    /// </summary>
    /// <param name="dimension">Symbol width in modules.</param>
    public static BbQrVersion? GetProvisionalVersionForDimension(int dimension)
    {
        if (dimension % 4 != 1 || dimension < 21 || dimension > 177)
        {
            return null;
        }

        return Versions[((dimension - 17) / 4) - 1];
    }

    /// <summary>
    /// Builds the mask that marks every function pattern module, so that the parser knows which
    /// modules carry data.
    /// </summary>
    public BbBitMatrix BuildFunctionPattern()
    {
        var dimension = DimensionForVersion;
        var bitMatrix = new BbBitMatrix(dimension, dimension);

        // Finder patterns with their separators, top left, top right and bottom left.
        bitMatrix.SetRegion(0, 0, 9, 9);
        bitMatrix.SetRegion(dimension - 8, 0, 8, 9);
        bitMatrix.SetRegion(0, dimension - 8, 9, 8);

        // Alignment patterns, skipping the three positions that collide with the finders.
        var max = AlignmentPatternCenters.Length;
        for (var x = 0; x < max; x++)
        {
            var i = AlignmentPatternCenters[x] - 2;
            for (var y = 0; y < max; y++)
            {
                if ((x == 0 && (y == 0 || y == max - 1)) || (x == max - 1 && y == 0))
                {
                    continue;
                }

                bitMatrix.SetRegion(AlignmentPatternCenters[y] - 2, i, 5, 5);
            }
        }

        // Timing patterns.
        bitMatrix.SetRegion(6, 9, 1, dimension - 17);
        bitMatrix.SetRegion(9, 6, dimension - 17, 1);

        if (VersionNumber > 6)
        {
            // Version information, duplicated near the top right and bottom left finders.
            bitMatrix.SetRegion(dimension - 11, 0, 3, 6);
            bitMatrix.SetRegion(0, dimension - 11, 6, 3);
        }

        return bitMatrix;
    }

    /// <inheritdoc />
    public override string ToString() => VersionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static BbQrVersion Create(int version, int[] centers, params int[][] levels)
    {
        var blocks = new BbQrEcBlocks[4];
        for (var i = 0; i < 4; i++)
        {
            var spec = levels[i];
            blocks[i] = spec.Length == 3
                ? new BbQrEcBlocks(spec[0], [new BbQrEcBlockGroup(spec[1], spec[2])])
                : new BbQrEcBlocks(spec[0], [new BbQrEcBlockGroup(spec[1], spec[2]), new BbQrEcBlockGroup(spec[3], spec[4])]);
        }

        return new BbQrVersion(version, centers, blocks);
    }

    /// <summary>
    /// The block tables from ISO/IEC 18004 annex, one row per version. Each level is given as
    /// <c>[ecCodewordsPerBlock, count1, data1]</c> or
    /// <c>[ecCodewordsPerBlock, count1, data1, count2, data2]</c>, in the order L, M, Q, H.
    /// </summary>
    private static BbQrVersion[] BuildVersions() =>
    [
        Create(1, [], [7, 1, 19], [10, 1, 16], [13, 1, 13], [17, 1, 9]),
        Create(2, [6, 18], [10, 1, 34], [16, 1, 28], [22, 1, 22], [28, 1, 16]),
        Create(3, [6, 22], [15, 1, 55], [26, 1, 44], [18, 2, 17], [22, 2, 13]),
        Create(4, [6, 26], [20, 1, 80], [18, 2, 32], [26, 2, 24], [16, 4, 9]),
        Create(5, [6, 30], [26, 1, 108], [24, 2, 43], [18, 2, 15, 2, 16], [22, 2, 11, 2, 12]),
        Create(6, [6, 34], [18, 2, 68], [16, 4, 27], [24, 4, 19], [28, 4, 15]),
        Create(7, [6, 22, 38], [20, 2, 78], [18, 4, 31], [18, 2, 14, 4, 15], [26, 4, 13, 1, 14]),
        Create(8, [6, 24, 42], [24, 2, 97], [22, 2, 38, 2, 39], [22, 4, 18, 2, 19], [26, 4, 14, 2, 15]),
        Create(9, [6, 26, 46], [30, 2, 116], [22, 3, 36, 2, 37], [20, 4, 16, 4, 17], [24, 4, 12, 4, 13]),
        Create(10, [6, 28, 50], [18, 2, 68, 2, 69], [26, 4, 43, 1, 44], [24, 6, 19, 2, 20], [28, 6, 15, 2, 16]),
        Create(11, [6, 30, 54], [20, 4, 81], [30, 1, 50, 4, 51], [28, 4, 22, 4, 23], [24, 3, 12, 8, 13]),
        Create(12, [6, 32, 58], [24, 2, 92, 2, 93], [22, 6, 36, 2, 37], [26, 4, 20, 6, 21], [28, 7, 14, 4, 15]),
        Create(13, [6, 34, 62], [26, 4, 107], [22, 8, 37, 1, 38], [24, 8, 20, 4, 21], [22, 12, 11, 4, 12]),
        Create(14, [6, 26, 46, 66], [30, 3, 115, 1, 116], [24, 4, 40, 5, 41], [20, 11, 16, 5, 17], [24, 11, 12, 5, 13]),
        Create(15, [6, 26, 48, 70], [22, 5, 87, 1, 88], [24, 5, 41, 5, 42], [30, 5, 24, 7, 25], [24, 11, 12, 7, 13]),
        Create(16, [6, 26, 50, 74], [24, 5, 98, 1, 99], [28, 7, 45, 3, 46], [24, 15, 19, 2, 20], [30, 3, 15, 13, 16]),
        Create(17, [6, 30, 54, 78], [28, 1, 107, 5, 108], [28, 10, 46, 1, 47], [28, 1, 22, 15, 23], [28, 2, 14, 17, 15]),
        Create(18, [6, 30, 56, 82], [30, 5, 120, 1, 121], [26, 9, 43, 4, 44], [28, 17, 22, 1, 23], [28, 2, 14, 19, 15]),
        Create(19, [6, 30, 58, 86], [28, 3, 113, 4, 114], [26, 3, 44, 11, 45], [26, 17, 21, 4, 22], [26, 9, 13, 16, 14]),
        Create(20, [6, 34, 62, 90], [28, 3, 107, 5, 108], [26, 3, 41, 13, 42], [30, 15, 24, 5, 25], [28, 15, 15, 10, 16]),
        Create(21, [6, 28, 50, 72, 94], [28, 4, 116, 4, 117], [26, 17, 42], [28, 17, 22, 6, 23], [30, 19, 16, 6, 17]),
        Create(22, [6, 26, 50, 74, 98], [28, 2, 111, 7, 112], [28, 17, 46], [30, 7, 24, 16, 25], [24, 34, 13]),
        Create(23, [6, 30, 54, 78, 102], [30, 4, 121, 5, 122], [28, 4, 47, 14, 48], [30, 11, 24, 14, 25], [30, 16, 15, 14, 16]),
        Create(24, [6, 28, 54, 80, 106], [30, 6, 117, 4, 118], [28, 6, 45, 14, 46], [30, 11, 24, 16, 25], [30, 30, 16, 2, 17]),
        Create(25, [6, 32, 58, 84, 110], [26, 8, 106, 4, 107], [28, 8, 47, 13, 48], [30, 7, 24, 22, 25], [30, 22, 15, 13, 16]),
        Create(26, [6, 30, 58, 86, 114], [28, 10, 114, 2, 115], [28, 19, 46, 4, 47], [28, 28, 22, 6, 23], [30, 33, 16, 4, 17]),
        Create(27, [6, 34, 62, 90, 118], [30, 8, 122, 4, 123], [28, 22, 45, 3, 46], [30, 8, 23, 26, 24], [30, 12, 15, 28, 16]),
        Create(28, [6, 26, 50, 74, 98, 122], [30, 3, 117, 10, 118], [28, 3, 45, 23, 46], [30, 4, 24, 31, 25], [30, 11, 15, 31, 16]),
        Create(29, [6, 30, 54, 78, 102, 126], [30, 7, 116, 7, 117], [28, 21, 45, 7, 46], [30, 1, 23, 37, 24], [30, 19, 15, 26, 16]),
        Create(30, [6, 26, 52, 78, 104, 130], [30, 5, 115, 10, 116], [28, 19, 47, 10, 48], [30, 15, 24, 25, 25], [30, 23, 15, 25, 16]),
        Create(31, [6, 30, 56, 82, 108, 134], [30, 13, 115, 3, 116], [28, 2, 46, 29, 47], [30, 42, 24, 1, 25], [30, 23, 15, 28, 16]),
        Create(32, [6, 34, 60, 86, 112, 138], [30, 17, 115], [28, 10, 46, 23, 47], [30, 10, 24, 35, 25], [30, 19, 15, 35, 16]),
        Create(33, [6, 30, 58, 86, 114, 142], [30, 17, 115, 1, 116], [28, 14, 46, 21, 47], [30, 29, 24, 19, 25], [30, 11, 15, 46, 16]),
        Create(34, [6, 34, 62, 90, 118, 146], [30, 13, 115, 6, 116], [28, 14, 46, 23, 47], [30, 44, 24, 7, 25], [30, 59, 16, 1, 17]),
        Create(35, [6, 30, 54, 78, 102, 126, 150], [30, 12, 121, 7, 122], [28, 12, 47, 26, 48], [30, 39, 24, 14, 25], [30, 22, 15, 41, 16]),
        Create(36, [6, 24, 50, 76, 102, 128, 154], [30, 6, 121, 14, 122], [28, 6, 47, 34, 48], [30, 46, 24, 10, 25], [30, 2, 15, 64, 16]),
        Create(37, [6, 28, 54, 80, 106, 132, 158], [30, 17, 122, 4, 123], [28, 29, 46, 14, 47], [30, 49, 24, 10, 25], [30, 24, 15, 46, 16]),
        Create(38, [6, 32, 58, 84, 110, 136, 162], [30, 4, 122, 18, 123], [28, 13, 46, 32, 47], [30, 48, 24, 14, 25], [30, 42, 15, 32, 16]),
        Create(39, [6, 26, 54, 82, 110, 138, 166], [30, 20, 117, 4, 118], [28, 40, 47, 7, 48], [30, 43, 24, 22, 25], [30, 10, 15, 67, 16]),
        Create(40, [6, 30, 58, 86, 114, 142, 170], [30, 19, 118, 6, 119], [28, 18, 47, 31, 48], [30, 34, 24, 34, 25], [30, 20, 15, 61, 16]),
    ];
}
