namespace Bit.BbScanner;

/// <summary>
/// The fixed structure of MicroPDF417 (ISO/IEC 24728): its 34 symbol sizes and the row address patterns that number
/// their rows.
/// </summary>
/// <remarks>
/// <para>
/// A MicroPDF417 symbol has one to four data columns and a fixed number of rows for each size, each size its own
/// number of error correction codewords. It has no start or stop pattern and no row indicators: every row begins and
/// ends with a row address pattern (RAP) - ten modules, three bars and three spaces - and three and four column symbols
/// carry a third one between their columns. The left and right RAPs come from one set of 52 patterns and the centre
/// RAP from another; down the symbol each advances one pattern a row, starting where its size says. So the patterns a
/// row carries name both the size and the row, and the row decides the cluster - 0, 3 or 6, the same clusters PDF417
/// uses - of its codewords.
/// </para>
/// <para>
/// Public, like <see cref="BbPdf417SymbolTable"/>, for encoders and tests: the tables are specification data and do
/// not change. They are reproduced from BWIPP (Barcode Writer in Pure PostScript, MIT licensed), whose
/// MicroPDF417 encoder writes the reference symbols the tests read.
/// </para>
/// </remarks>
public static class BbMicroPdf417Tables
{
    /// <summary>Modules in a row address pattern.</summary>
    public const int RapModules = 10;

    /// <summary>
    /// The symbol sizes, in the specification's order: columns, rows, error correction codewords, and the RAP the left,
    /// centre and right patterns of the first row start at, numbered from 1 (the centre 0 where there is none).
    /// </summary>
    public static readonly Variant[] Variants =
    [
        new(1, 11, 7, 1, 0, 9),
        new(1, 14, 7, 8, 0, 8),
        new(1, 17, 7, 36, 0, 36),
        new(1, 20, 8, 19, 0, 19),
        new(1, 24, 8, 9, 0, 17),
        new(1, 28, 8, 25, 0, 33),
        new(2, 8, 8, 1, 0, 1),
        new(2, 11, 9, 1, 0, 9),
        new(2, 14, 9, 8, 0, 8),
        new(2, 17, 10, 36, 0, 36),
        new(2, 20, 11, 19, 0, 19),
        new(2, 23, 13, 9, 0, 17),
        new(2, 26, 15, 27, 0, 35),
        new(3, 6, 12, 1, 1, 1),
        new(3, 8, 14, 7, 7, 7),
        new(3, 10, 16, 15, 15, 15),
        new(3, 12, 18, 25, 25, 25),
        new(3, 15, 21, 37, 37, 37),
        new(3, 20, 26, 1, 17, 33),
        new(3, 26, 32, 1, 9, 17),
        new(3, 32, 38, 21, 29, 37),
        new(3, 38, 44, 15, 31, 47),
        new(3, 44, 50, 1, 25, 49),
        new(4, 4, 8, 47, 19, 43),
        new(4, 6, 12, 1, 1, 1),
        new(4, 8, 14, 7, 7, 7),
        new(4, 10, 16, 15, 15, 15),
        new(4, 12, 18, 25, 25, 25),
        new(4, 15, 21, 37, 37, 37),
        new(4, 20, 26, 1, 17, 33),
        new(4, 26, 32, 1, 9, 17),
        new(4, 32, 38, 21, 29, 37),
        new(4, 38, 44, 15, 31, 47),
        new(4, 44, 50, 1, 25, 49),
    ];

    /// <summary>The 52 left and right row address patterns, ten modules each, most significant bit first, a set bit dark.</summary>
    public static ReadOnlySpan<int> SidePatterns =>
    [
        802, 930, 946, 818, 882, 890, 826, 954, 922, 986, 970, 906, 778,
        794, 786, 914, 978, 982, 980, 916, 948, 932, 934, 942, 940, 936,
        808, 812, 814, 806, 822, 950, 918, 790, 788, 820, 884, 868, 870,
        878, 876, 872, 840, 856, 860, 862, 846, 844, 836, 838, 834, 866,
    ];

    /// <summary>The 52 centre row address patterns.</summary>
    public static ReadOnlySpan<int> CentrePatterns =>
    [
        718, 590, 622, 558, 550, 566, 534, 530, 538, 570, 562, 546, 610,
        626, 634, 762, 754, 758, 630, 628, 612, 614, 582, 578, 706, 738,
        742, 740, 748, 620, 556, 552, 616, 744, 712, 716, 708, 710, 646,
        654, 652, 668, 664, 696, 688, 656, 720, 592, 600, 604, 732, 734,
    ];

    /// <summary>Modules in a row of a symbol with the given number of columns, its closing bar included.</summary>
    public static int RowModules(int columns) => columns switch
    {
        1 => 38,
        2 => 55,
        3 => 82,
        _ => 99,
    };

    /// <summary>A symbol size.</summary>
    /// <param name="Columns">Data columns, 1 to 4.</param>
    /// <param name="Rows">Rows, 4 to 44.</param>
    /// <param name="EcCodewords">Error correction codewords.</param>
    /// <param name="LeftStart">The left RAP of the first row, 1 to 52.</param>
    /// <param name="CentreStart">The centre RAP of the first row, 1 to 52, or 0 for one and two columns.</param>
    /// <param name="RightStart">The right RAP of the first row, 1 to 52.</param>
    public readonly record struct Variant(int Columns, int Rows, int EcCodewords, int LeftStart, int CentreStart, int RightStart)
    {
        /// <summary>The RAP index, 0 to 51, a pattern of the given kind has in row <paramref name="row"/>.</summary>
        public int RapIndex(BbMicroPdf417RapKind kind, int row) => kind switch
        {
            BbMicroPdf417RapKind.Left => (row + LeftStart - 1) % 52,
            BbMicroPdf417RapKind.Centre => (row + CentreStart - 1) % 52,
            _ => (row + RightStart - 1) % 52,
        };

        /// <summary>The row a pattern of the given kind and index stands for, or -1 when this size has no such row.</summary>
        public int RowOf(BbMicroPdf417RapKind kind, int index)
        {
            var start = kind switch
            {
                BbMicroPdf417RapKind.Left => LeftStart,
                BbMicroPdf417RapKind.Centre => CentreStart,
                _ => RightStart,
            };

            if (start == 0)
            {
                return -1;
            }

            var row = ((index - start + 1) % 52 + 52) % 52;
            return row < Rows ? row : -1;
        }

        /// <summary>The cluster, 0, 3 or 6, of the codewords of row <paramref name="row"/>.</summary>
        public int Cluster(int row) => (row + LeftStart - 1) % 3 * 3;
    }
}

/// <summary>Where in a row a row address pattern sits.</summary>
public enum BbMicroPdf417RapKind
{
    /// <summary>At the start of the row.</summary>
    Left,

    /// <summary>Between the columns of a three or four column symbol.</summary>
    Centre,

    /// <summary>At the end of the row, before its closing bar.</summary>
    Right,
}
