namespace Bit.BbScanner;

/// <summary>
/// Reads the format information, version and codewords out of a sampled QR module grid.
/// </summary>
public sealed class BbQrBitMatrixParser
{
    private readonly BbBitMatrix _matrix;
    private readonly int _dimension;

    private BbQrBitMatrixParser(BbBitMatrix matrix)
    {
        _matrix = matrix;
        _dimension = matrix.Height;
    }

    /// <summary>Creates a parser, or returns <see langword="null"/> when the grid is not a legal QR size.</summary>
    /// <param name="matrix">A sampled module grid.</param>
    public static BbQrBitMatrixParser? Create(BbBitMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        if (matrix.Width != matrix.Height || matrix.Height < 21 || (matrix.Height & 0x03) != 1)
        {
            return null;
        }

        return new BbQrBitMatrixParser(matrix);
    }

    /// <summary>Reads the format information, or <see langword="null"/> when both copies are unreadable.</summary>
    public BbQrFormatInformation? ReadFormatInformation()
    {
        // The first copy hugs the top-left finder pattern, skipping the timing pattern module.
        var bits1 = 0;
        for (var i = 0; i < 6; i++)
        {
            bits1 = CopyBit(i, 8, bits1);
        }

        bits1 = CopyBit(7, 8, bits1);
        bits1 = CopyBit(8, 8, bits1);
        bits1 = CopyBit(8, 7, bits1);
        for (var j = 5; j >= 0; j--)
        {
            bits1 = CopyBit(8, j, bits1);
        }

        // The second copy is split between the top-right and bottom-left finder patterns.
        var bits2 = 0;
        var jMin = _dimension - 7;
        for (var j = _dimension - 1; j >= jMin; j--)
        {
            bits2 = CopyBit(8, j, bits2);
        }

        for (var i = _dimension - 8; i < _dimension; i++)
        {
            bits2 = CopyBit(i, 8, bits2);
        }

        return BbQrFormatInformation.Decode(bits1, bits2);
    }

    /// <summary>Reads the version, or <see langword="null"/> when the version information is unreadable.</summary>
    public BbQrVersion? ReadVersion()
    {
        var provisionalVersion = (_dimension - 17) / 4;
        if (provisionalVersion <= 6)
        {
            // Versions 1 to 6 carry no version information; the size alone identifies them.
            return BbQrVersion.GetVersionForNumber(provisionalVersion);
        }

        // Top-right copy: three columns by six rows.
        var versionBits = 0;
        var ijMin = _dimension - 11;
        for (var j = 5; j >= 0; j--)
        {
            for (var i = _dimension - 9; i >= ijMin; i--)
            {
                versionBits = CopyBit(i, j, versionBits);
            }
        }

        var parsed = BbQrFormatInformation.DecodeVersionInformation(versionBits);
        if (parsed is not null && parsed.DimensionForVersion == _dimension)
        {
            return parsed;
        }

        // Bottom-left copy: six columns by three rows.
        versionBits = 0;
        for (var i = 5; i >= 0; i--)
        {
            for (var j = _dimension - 9; j >= ijMin; j--)
            {
                versionBits = CopyBit(i, j, versionBits);
            }
        }

        parsed = BbQrFormatInformation.DecodeVersionInformation(versionBits);
        return parsed is not null && parsed.DimensionForVersion == _dimension ? parsed : null;
    }

    /// <summary>
    /// Reads the codewords in the zig-zag order the specification defines, undoing the data mask
    /// as it goes.
    /// </summary>
    /// <param name="version">The symbol version.</param>
    /// <param name="maskPattern">The data mask pattern reference.</param>
    /// <returns>The raw codewords, or <see langword="null"/> when the grid does not hold as many as the version needs.</returns>
    public byte[]? ReadCodewords(BbQrVersion version, int maskPattern)
    {
        ArgumentNullException.ThrowIfNull(version);

        using var functionPattern = version.BuildFunctionPattern();

        var result = new byte[version.TotalCodewords];
        var resultOffset = 0;
        var currentByte = 0;
        var bitsRead = 0;
        var readingUp = true;

        // Codewords snake up and down through pairs of columns, right to left.
        for (var j = _dimension - 1; j > 0; j -= 2)
        {
            if (j == 6)
            {
                // Column six is the vertical timing pattern and carries no data.
                j--;
            }

            for (var count = 0; count < _dimension; count++)
            {
                var i = readingUp ? _dimension - 1 - count : count;
                for (var col = 0; col < 2; col++)
                {
                    var x = j - col;
                    if (functionPattern[x, i])
                    {
                        continue;
                    }

                    bitsRead++;
                    currentByte <<= 1;
                    var bit = _matrix[x, i];
                    if (BbQrDataMask.IsMasked(maskPattern, i, x))
                    {
                        bit = !bit;
                    }

                    if (bit)
                    {
                        currentByte |= 1;
                    }

                    if (bitsRead == 8)
                    {
                        if (resultOffset >= result.Length)
                        {
                            return null;
                        }

                        result[resultOffset++] = (byte)currentByte;
                        bitsRead = 0;
                        currentByte = 0;
                    }
                }
            }

            readingUp = !readingUp;
        }

        return resultOffset == version.TotalCodewords ? result : null;
    }

    /// <summary>
    /// Shifts the module at column <paramref name="i"/>, row <paramref name="j"/> into the low
    /// bit of an accumulator, so that the first module read becomes the most significant bit.
    /// </summary>
    private int CopyBit(int i, int j, int versionBits) =>
        _matrix[i, j] ? (versionBits << 1) | 1 : versionBits << 1;
}
