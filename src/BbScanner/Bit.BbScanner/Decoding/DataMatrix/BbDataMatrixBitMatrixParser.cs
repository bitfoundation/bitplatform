namespace Bit.BbScanner;

/// <summary>
/// Reads the codewords out of a sampled Data Matrix module grid.
/// </summary>
/// <remarks>
/// <para>
/// Data Matrix places each codeword as an L-shaped group of eight modules that wraps around the
/// edges of the symbol, so a codeword can be split between the top and the bottom of the grid.
/// The placement walks diagonally up and to the right, then down and to the left, and four
/// special corner shapes fill the gaps that the diagonal walk leaves in particular symbol sizes.
/// </para>
/// <para>
/// The region borders, the solid finder and the dashed timing pattern, are stripped first so
/// that the placement operates on a contiguous mapping matrix.
/// </para>
/// </remarks>
public sealed class BbDataMatrixBitMatrixParser : IDisposable
{
    private readonly BbBitMatrix _mapping;
    private readonly BbBitMatrix _read;

    private BbDataMatrixBitMatrixParser(BbBitMatrix mapping, BbDataMatrixVersion version)
    {
        _mapping = mapping;
        _read = new BbBitMatrix(mapping.Width, mapping.Height);
        Version = version;
    }

    /// <summary>The symbol shape.</summary>
    public BbDataMatrixVersion Version { get; }

    /// <summary>Creates a parser for a sampled grid, or <see langword="null"/> when the grid is not a legal symbol.</summary>
    /// <param name="matrix">The sampled grid, including region borders.</param>
    public static BbDataMatrixBitMatrixParser? Create(BbBitMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        var version = BbDataMatrixVersion.GetVersionForDimensions(matrix.Height, matrix.Width);
        if (version is null)
        {
            return null;
        }

        var mapping = ExtractDataRegions(matrix, version);
        return new BbDataMatrixBitMatrixParser(mapping, version);
    }

    /// <summary>Reads every codeword in placement order.</summary>
    /// <returns>The codewords, or <see langword="null"/> when the placement does not fill the symbol exactly.</returns>
    public byte[]? ReadCodewords()
    {
        var result = new byte[Version.TotalCodewords];
        var offset = 0;

        var row = 4;
        var column = 0;
        var numRows = _mapping.Height;
        var numColumns = _mapping.Width;

        var corner1Read = false;
        var corner2Read = false;
        var corner3Read = false;
        var corner4Read = false;

        do
        {
            if (row == numRows && column == 0 && !corner1Read)
            {
                if (offset >= result.Length) return null;
                result[offset++] = (byte)ReadCorner1(numRows, numColumns);
                row -= 2;
                column += 2;
                corner1Read = true;
            }
            else if (row == numRows - 2 && column == 0 && (numColumns & 0x03) != 0 && !corner2Read)
            {
                if (offset >= result.Length) return null;
                result[offset++] = (byte)ReadCorner2(numRows, numColumns);
                row -= 2;
                column += 2;
                corner2Read = true;
            }
            else if (row == numRows + 4 && column == 2 && (numColumns & 0x07) == 0 && !corner3Read)
            {
                if (offset >= result.Length) return null;
                result[offset++] = (byte)ReadCorner3(numRows, numColumns);
                row -= 2;
                column += 2;
                corner3Read = true;
            }
            else if (row == numRows - 2 && column == 0 && (numColumns & 0x07) == 4 && !corner4Read)
            {
                if (offset >= result.Length) return null;
                result[offset++] = (byte)ReadCorner4(numRows, numColumns);
                row -= 2;
                column += 2;
                corner4Read = true;
            }
            else
            {
                // Upward diagonal sweep.
                do
                {
                    if (row < numRows && column >= 0 && !_read[column, row])
                    {
                        if (offset >= result.Length) return null;
                        result[offset++] = (byte)ReadUtah(row, column, numRows, numColumns);
                    }

                    row -= 2;
                    column += 2;
                }
                while (row >= 0 && column < numColumns);

                row += 1;
                column += 3;

                // Downward diagonal sweep.
                do
                {
                    if (row >= 0 && column < numColumns && !_read[column, row])
                    {
                        if (offset >= result.Length) return null;
                        result[offset++] = (byte)ReadUtah(row, column, numRows, numColumns);
                    }

                    row += 2;
                    column -= 2;
                }
                while (row < numRows && column >= 0);

                row += 3;
                column += 1;
            }
        }
        while (row < numRows || column < numColumns);

        return offset == Version.TotalCodewords ? result : null;
    }

    /// <summary>
    /// Reads one module, wrapping coordinates that fall outside the mapping matrix the way the
    /// placement algorithm prescribes, and marks the module as consumed.
    /// </summary>
    private bool ReadModule(int row, int column, int numRows, int numColumns)
    {
        if (row < 0)
        {
            row += numRows;
            column += 4 - ((numRows + 4) & 0x07);
        }

        if (column < 0)
        {
            column += numColumns;
            row += 4 - ((numColumns + 4) & 0x07);
        }

        if (row >= numRows)
        {
            row -= numRows;
        }

        _read[column, row] = true;
        return _mapping[column, row];
    }

    /// <summary>Reads the standard eight module codeword shape.</summary>
    private int ReadUtah(int row, int column, int numRows, int numColumns)
    {
        var currentByte = 0;
        currentByte = Shift(currentByte, ReadModule(row - 2, column - 2, numRows, numColumns));
        currentByte = Shift(currentByte, ReadModule(row - 2, column - 1, numRows, numColumns));
        currentByte = Shift(currentByte, ReadModule(row - 1, column - 2, numRows, numColumns));
        currentByte = Shift(currentByte, ReadModule(row - 1, column - 1, numRows, numColumns));
        currentByte = Shift(currentByte, ReadModule(row - 1, column, numRows, numColumns));
        currentByte = Shift(currentByte, ReadModule(row, column - 2, numRows, numColumns));
        currentByte = Shift(currentByte, ReadModule(row, column - 1, numRows, numColumns));
        currentByte = (currentByte << 1) | (ReadModule(row, column, numRows, numColumns) ? 1 : 0);
        return currentByte;
    }

    private static int Shift(int value, bool bit) => (value << 1) | (bit ? 1 : 0);

    private int ReadCorner1(int numRows, int numColumns) => ReadCorner(
        numRows, numColumns,
        [(numRows - 1, 0), (numRows - 1, 1), (numRows - 1, 2), (0, numColumns - 2), (0, numColumns - 1),
         (1, numColumns - 1), (2, numColumns - 1), (3, numColumns - 1)]);

    private int ReadCorner2(int numRows, int numColumns) => ReadCorner(
        numRows, numColumns,
        [(numRows - 3, 0), (numRows - 2, 0), (numRows - 1, 0), (0, numColumns - 4), (0, numColumns - 3),
         (0, numColumns - 2), (0, numColumns - 1), (1, numColumns - 1)]);

    private int ReadCorner3(int numRows, int numColumns) => ReadCorner(
        numRows, numColumns,
        [(numRows - 1, 0), (numRows - 1, numColumns - 1), (0, numColumns - 3), (0, numColumns - 2),
         (0, numColumns - 1), (1, numColumns - 3), (1, numColumns - 2), (1, numColumns - 1)]);

    private int ReadCorner4(int numRows, int numColumns) => ReadCorner(
        numRows, numColumns,
        [(numRows - 3, 0), (numRows - 2, 0), (numRows - 1, 0), (0, numColumns - 2), (0, numColumns - 1),
         (1, numColumns - 1), (2, numColumns - 1), (3, numColumns - 1)]);

    private int ReadCorner(int numRows, int numColumns, ReadOnlySpan<(int Row, int Column)> modules)
    {
        var currentByte = 0;
        foreach (var (row, column) in modules)
        {
            currentByte = (currentByte << 1) | (ReadModule(row, column, numRows, numColumns) ? 1 : 0);
        }

        return currentByte;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _mapping.Dispose();
        _read.Dispose();
    }

    /// <summary>Removes the finder and timing borders, leaving a contiguous mapping matrix.</summary>
    /// <param name="matrix">The sampled grid.</param>
    /// <param name="version">The symbol shape.</param>
    public static BbBitMatrix ExtractDataRegions(BbBitMatrix matrix, BbDataMatrixVersion version)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(version);

        var regionRows = version.DataRegionSizeRows;
        var regionColumns = version.DataRegionSizeColumns;
        var numRegionsDown = version.SymbolSizeRows / regionRows;
        var numRegionsAcross = version.SymbolSizeColumns / regionColumns;

        var result = new BbBitMatrix(numRegionsAcross * regionColumns, numRegionsDown * regionRows);

        for (var regionRow = 0; regionRow < numRegionsDown; regionRow++)
        {
            var writeRowBase = regionRow * regionRows;
            for (var regionColumn = 0; regionColumn < numRegionsAcross; regionColumn++)
            {
                var writeColumnBase = regionColumn * regionColumns;
                for (var i = 0; i < regionRows; i++)
                {
                    var readRow = (regionRow * (regionRows + 2)) + 1 + i;
                    for (var j = 0; j < regionColumns; j++)
                    {
                        var readColumn = (regionColumn * (regionColumns + 2)) + 1 + j;
                        if (matrix[readColumn, readRow])
                        {
                            result[writeColumnBase + j, writeRowBase + i] = true;
                        }
                    }
                }
            }
        }

        return result;
    }
}
