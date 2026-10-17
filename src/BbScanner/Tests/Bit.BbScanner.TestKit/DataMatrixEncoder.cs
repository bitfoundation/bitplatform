
namespace Bit.BbScanner.TestKit;

/// <summary>
/// A Data Matrix ECC 200 encoder used to generate test symbols and benchmark inputs.
/// </summary>
/// <remarks>
/// Only the ASCII compaction mode is implemented, which is enough to drive every structural part
/// of the decoder: symbol shape selection, block interleaving, the codeword placement and the
/// region borders.
/// </remarks>
public static class DataMatrixEncoder
{
    /// <summary>Encodes text into a Data Matrix module grid, borders included.</summary>
    /// <param name="content">The text to encode; ISO-8859-1 characters only.</param>
    /// <param name="minimumVersion">Forces a symbol shape of at least this version number.</param>
    public static BbBitMatrix Encode(string content, int minimumVersion = 1)
    {
        ArgumentNullException.ThrowIfNull(content);

        var codewords = EncodeAscii(content);
        var version = ChooseVersion(codewords.Count, minimumVersion)
            ?? throw new ArgumentException("The content does not fit in any Data Matrix symbol.", nameof(content));

        return Build(codewords, version);
    }

    /// <summary>
    /// Encodes text into one specific symbol shape, square or rectangular. <see cref="Encode"/>
    /// only ever picks a square shape, so this is how the rectangular ones are reached.
    /// </summary>
    /// <param name="content">The text to encode; ISO-8859-1 characters only.</param>
    /// <param name="version">The shape to produce.</param>
    public static BbBitMatrix Encode(string content, BbDataMatrixVersion version)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(version);

        var codewords = EncodeAscii(content);
        if (codewords.Count > version.TotalDataCodewords)
        {
            throw new ArgumentException(
                $"The content needs {codewords.Count} data codewords; {version} holds {version.TotalDataCodewords}.",
                nameof(content));
        }

        return Build(codewords, version);
    }

    private static BbBitMatrix Build(List<byte> codewords, BbDataMatrixVersion version)
    {
        AddPadding(codewords, version.TotalDataCodewords);

        var raw = InterleaveWithEcCodewords([.. codewords], version);
        var placement = Place(raw, version);
        return AddBorders(placement, version);
    }

    private static List<byte> EncodeAscii(string content)
    {
        var codewords = new List<byte>(content.Length);

        var i = 0;
        while (i < content.Length)
        {
            var c = content[i];

            // Two consecutive digits pack into a single codeword.
            if (char.IsAsciiDigit(c) && i + 1 < content.Length && char.IsAsciiDigit(content[i + 1]))
            {
                var value = ((c - '0') * 10) + (content[i + 1] - '0');
                codewords.Add((byte)(value + 130));
                i += 2;
                continue;
            }

            if (c > 127)
            {
                if (c > 255)
                {
                    throw new ArgumentException("Only ISO-8859-1 characters are supported.", nameof(content));
                }

                codewords.Add(235); // Upper shift.
                codewords.Add((byte)(c - 128 + 1));
            }
            else
            {
                codewords.Add((byte)(c + 1));
            }

            i++;
        }

        return codewords;
    }

    private static void AddPadding(List<byte> codewords, int capacity)
    {
        if (codewords.Count >= capacity)
        {
            return;
        }

        codewords.Add(129);

        // Further pad codewords are randomised so that padding does not create large uniform
        // areas that would confuse a scanner.
        while (codewords.Count < capacity)
        {
            var position = codewords.Count + 1;
            var pseudoRandom = ((149 * position) % 253) + 1;
            var value = 129 + pseudoRandom;
            codewords.Add((byte)(value <= 254 ? value : value - 254));
        }
    }

    private static BbDataMatrixVersion? ChooseVersion(int dataCodewords, int minimumVersion)
    {
        foreach (var version in BbDataMatrixVersion.All)
        {
            if (version.IsRectangular || version.VersionNumber < minimumVersion)
            {
                continue;
            }

            if (version.TotalDataCodewords >= dataCodewords)
            {
                return version;
            }
        }

        return null;
    }

    private static byte[] InterleaveWithEcCodewords(byte[] data, BbDataMatrixVersion version)
    {
        var encoder = new ReedSolomonEncoder(BbGenericGF.DataMatrixField256);

        var totalBlocks = version.NumBlocks;
        var blocks = new List<byte[]>();
        var blockIndexCounter = 0;
        foreach (var group in version.Groups)
        {
            for (var i = 0; i < group.Count; i++)
            {
                // ECC 200 assigns every n-th data codeword to block n, so the block content is
                // read with a stride rather than as a contiguous run.
                var blockNumber = blockIndexCounter++;
                var block = new int[group.DataCodewords + version.EcCodewords];
                for (var j = 0; j < group.DataCodewords; j++)
                {
                    block[j] = data[(j * totalBlocks) + blockNumber];
                }

                encoder.Encode(block, version.EcCodewords);

                var bytes = new byte[block.Length];
                for (var j = 0; j < block.Length; j++)
                {
                    bytes[j] = (byte)block[j];
                }

                blocks.Add(bytes);
            }
        }

        var result = new byte[version.TotalCodewords];
        var index = 0;

        var longerBlocksNumDataCodewords = blocks[0].Length - version.EcCodewords;
        var shorterBlocksNumDataCodewords = longerBlocksNumDataCodewords - 1;

        for (var i = 0; i < shorterBlocksNumDataCodewords; i++)
        {
            for (var j = 0; j < totalBlocks; j++)
            {
                result[index++] = blocks[j][i];
            }
        }

        var specialVersion = version.VersionNumber == 24;
        var numLongerBlocks = specialVersion ? 8 : totalBlocks;
        for (var j = 0; j < numLongerBlocks; j++)
        {
            result[index++] = blocks[j][longerBlocksNumDataCodewords - 1];
        }

        var max = blocks[0].Length;
        for (var i = longerBlocksNumDataCodewords; i < max; i++)
        {
            for (var j = 0; j < totalBlocks; j++)
            {
                var blockIndex = specialVersion ? (j + 8) % totalBlocks : j;
                var codewordIndex = specialVersion && blockIndex > 7 ? i - 1 : i;
                result[index++] = blocks[blockIndex][codewordIndex];
            }
        }

        return result;
    }

    /// <summary>
    /// Writes codewords into the mapping matrix using the ECC 200 placement algorithm.
    /// </summary>
    private static BbBitMatrix Place(byte[] codewords, BbDataMatrixVersion version)
    {
        var numRows = version.SymbolSizeRows / version.DataRegionSizeRows * version.DataRegionSizeRows;
        var numColumns = version.SymbolSizeColumns / version.DataRegionSizeColumns * version.DataRegionSizeColumns;

        var matrix = new BbBitMatrix(numColumns, numRows);
        var written = new BbBitMatrix(numColumns, numRows);

        var chr = 0;
        var row = 4;
        var column = 0;

        do
        {
            if (row == numRows && column == 0)
            {
                WriteCorner1(matrix, written, codewords[chr++], numRows, numColumns);
            }

            if (row == numRows - 2 && column == 0 && numColumns % 4 != 0)
            {
                WriteCorner2(matrix, written, codewords[chr++], numRows, numColumns);
            }

            if (row == numRows - 2 && column == 0 && numColumns % 8 == 4)
            {
                WriteCorner3(matrix, written, codewords[chr++], numRows, numColumns);
            }

            if (row == numRows + 4 && column == 2 && numColumns % 8 == 0)
            {
                WriteCorner4(matrix, written, codewords[chr++], numRows, numColumns);
            }

            do
            {
                if (row < numRows && column >= 0 && !written[column, row])
                {
                    WriteUtah(matrix, written, row, column, codewords[chr++], numRows, numColumns);
                }

                row -= 2;
                column += 2;
            }
            while (row >= 0 && column < numColumns);

            row += 1;
            column += 3;

            do
            {
                if (row >= 0 && column < numColumns && !written[column, row])
                {
                    WriteUtah(matrix, written, row, column, codewords[chr++], numRows, numColumns);
                }

                row += 2;
                column -= 2;
            }
            while (row < numRows && column >= 0);

            row += 3;
            column += 1;
        }
        while (row < numRows || column < numColumns);

        // The bottom right corner is not reached by the placement in some symbol sizes and
        // carries a fixed pattern instead.
        if (!written[numColumns - 1, numRows - 1])
        {
            matrix[numColumns - 1, numRows - 1] = true;
            matrix[numColumns - 2, numRows - 2] = true;
        }

        written.Dispose();
        return matrix;
    }

    private static void WriteModule(
        BbBitMatrix matrix, BbBitMatrix written, int row, int column, byte codeword, int bit, int numRows, int numColumns)
    {
        if (row < 0)
        {
            row += numRows;
            column += 4 - ((numRows + 4) % 8);
        }

        if (column < 0)
        {
            column += numColumns;
            row += 4 - ((numColumns + 4) % 8);
        }

        if (row >= numRows)
        {
            row -= numRows;
        }

        matrix[column, row] = ((codeword >> (8 - bit)) & 1) != 0;
        written[column, row] = true;
    }

    private static void WriteUtah(
        BbBitMatrix matrix, BbBitMatrix written, int row, int column, byte codeword, int numRows, int numColumns)
    {
        WriteModule(matrix, written, row - 2, column - 2, codeword, 1, numRows, numColumns);
        WriteModule(matrix, written, row - 2, column - 1, codeword, 2, numRows, numColumns);
        WriteModule(matrix, written, row - 1, column - 2, codeword, 3, numRows, numColumns);
        WriteModule(matrix, written, row - 1, column - 1, codeword, 4, numRows, numColumns);
        WriteModule(matrix, written, row - 1, column, codeword, 5, numRows, numColumns);
        WriteModule(matrix, written, row, column - 2, codeword, 6, numRows, numColumns);
        WriteModule(matrix, written, row, column - 1, codeword, 7, numRows, numColumns);
        WriteModule(matrix, written, row, column, codeword, 8, numRows, numColumns);
    }

    private static void WriteCorner1(BbBitMatrix matrix, BbBitMatrix written, byte codeword, int numRows, int numColumns)
    {
        WriteModule(matrix, written, numRows - 1, 0, codeword, 1, numRows, numColumns);
        WriteModule(matrix, written, numRows - 1, 1, codeword, 2, numRows, numColumns);
        WriteModule(matrix, written, numRows - 1, 2, codeword, 3, numRows, numColumns);
        WriteModule(matrix, written, 0, numColumns - 2, codeword, 4, numRows, numColumns);
        WriteModule(matrix, written, 0, numColumns - 1, codeword, 5, numRows, numColumns);
        WriteModule(matrix, written, 1, numColumns - 1, codeword, 6, numRows, numColumns);
        WriteModule(matrix, written, 2, numColumns - 1, codeword, 7, numRows, numColumns);
        WriteModule(matrix, written, 3, numColumns - 1, codeword, 8, numRows, numColumns);
    }

    private static void WriteCorner2(BbBitMatrix matrix, BbBitMatrix written, byte codeword, int numRows, int numColumns)
    {
        WriteModule(matrix, written, numRows - 3, 0, codeword, 1, numRows, numColumns);
        WriteModule(matrix, written, numRows - 2, 0, codeword, 2, numRows, numColumns);
        WriteModule(matrix, written, numRows - 1, 0, codeword, 3, numRows, numColumns);
        WriteModule(matrix, written, 0, numColumns - 4, codeword, 4, numRows, numColumns);
        WriteModule(matrix, written, 0, numColumns - 3, codeword, 5, numRows, numColumns);
        WriteModule(matrix, written, 0, numColumns - 2, codeword, 6, numRows, numColumns);
        WriteModule(matrix, written, 0, numColumns - 1, codeword, 7, numRows, numColumns);
        WriteModule(matrix, written, 1, numColumns - 1, codeword, 8, numRows, numColumns);
    }

    private static void WriteCorner3(BbBitMatrix matrix, BbBitMatrix written, byte codeword, int numRows, int numColumns)
    {
        WriteModule(matrix, written, numRows - 3, 0, codeword, 1, numRows, numColumns);
        WriteModule(matrix, written, numRows - 2, 0, codeword, 2, numRows, numColumns);
        WriteModule(matrix, written, numRows - 1, 0, codeword, 3, numRows, numColumns);
        WriteModule(matrix, written, 0, numColumns - 2, codeword, 4, numRows, numColumns);
        WriteModule(matrix, written, 0, numColumns - 1, codeword, 5, numRows, numColumns);
        WriteModule(matrix, written, 1, numColumns - 1, codeword, 6, numRows, numColumns);
        WriteModule(matrix, written, 2, numColumns - 1, codeword, 7, numRows, numColumns);
        WriteModule(matrix, written, 3, numColumns - 1, codeword, 8, numRows, numColumns);
    }

    private static void WriteCorner4(BbBitMatrix matrix, BbBitMatrix written, byte codeword, int numRows, int numColumns)
    {
        WriteModule(matrix, written, numRows - 1, 0, codeword, 1, numRows, numColumns);
        WriteModule(matrix, written, numRows - 1, numColumns - 1, codeword, 2, numRows, numColumns);
        WriteModule(matrix, written, 0, numColumns - 3, codeword, 3, numRows, numColumns);
        WriteModule(matrix, written, 0, numColumns - 2, codeword, 4, numRows, numColumns);
        WriteModule(matrix, written, 0, numColumns - 1, codeword, 5, numRows, numColumns);
        WriteModule(matrix, written, 1, numColumns - 3, codeword, 6, numRows, numColumns);
        WriteModule(matrix, written, 1, numColumns - 2, codeword, 7, numRows, numColumns);
        WriteModule(matrix, written, 1, numColumns - 1, codeword, 8, numRows, numColumns);
    }

    /// <summary>Wraps every data region in its solid finder and dashed timing borders.</summary>
    private static BbBitMatrix AddBorders(BbBitMatrix placement, BbDataMatrixVersion version)
    {
        var regionRows = version.DataRegionSizeRows;
        var regionColumns = version.DataRegionSizeColumns;
        var result = new BbBitMatrix(version.SymbolSizeColumns, version.SymbolSizeRows);

        var numRegionsDown = version.SymbolSizeRows / regionRows;
        var numRegionsAcross = version.SymbolSizeColumns / regionColumns;

        for (var regionRow = 0; regionRow < numRegionsDown; regionRow++)
        {
            var top = regionRow * (regionRows + 2);
            for (var regionColumn = 0; regionColumn < numRegionsAcross; regionColumn++)
            {
                var left = regionColumn * (regionColumns + 2);

                // Left edge: solid. Bottom edge: solid.
                for (var i = 0; i < regionRows + 2; i++)
                {
                    result[left, top + i] = true;
                }

                for (var j = 0; j < regionColumns + 2; j++)
                {
                    result[left + j, top + regionRows + 1] = true;
                }

                // Top edge: alternating clock track, dark at even columns of the symbol.
                for (var j = 0; j < regionColumns + 2; j++)
                {
                    result[left + j, top] = j % 2 == 0;
                }

                // Right edge: alternating clock track, dark at even data rows of the symbol.
                for (var i = 0; i < regionRows; i++)
                {
                    result[left + regionColumns + 1, top + 1 + i] = i % 2 == 0;
                }

                // The two corners where the clock track meets the finder belong to the finder.
                result[left, top] = true;
                result[left, top + regionRows + 1] = true;
                result[left + regionColumns + 1, top + regionRows + 1] = true;

                for (var i = 0; i < regionRows; i++)
                {
                    for (var j = 0; j < regionColumns; j++)
                    {
                        if (placement[(regionColumn * regionColumns) + j, (regionRow * regionRows) + i])
                        {
                            result[left + 1 + j, top + 1 + i] = true;
                        }
                    }
                }
            }
        }

        placement.Dispose();
        return result;
    }
}
