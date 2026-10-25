using System.Text;

namespace Bit.BbScanner.TestKit;

/// <summary>
/// A QR Code encoder used to generate test symbols and benchmark inputs.
/// </summary>
/// <remarks>
/// It supports the numeric, alphanumeric and byte modes and all forty versions, which is enough
/// to exercise every branch of the decoder: block interleaving, the two version information
/// copies, all eight masks and every error correction level.
/// </remarks>
public static class QrEncoder
{
    private const string AlphanumericChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    private const int ModeNumeric = 0x01;
    private const int ModeAlphanumeric = 0x02;
    private const int ModeByte = 0x04;

    /// <summary>Encodes text into a QR module grid.</summary>
    /// <param name="content">The text to encode.</param>
    /// <param name="level">The error correction level.</param>
    /// <param name="maskPattern">The data mask to apply, 0 to 7.</param>
    /// <param name="minimumVersion">Forces a version of at least this number.</param>
    public static BbBitMatrix Encode(
        string content,
        BbQrErrorCorrectionLevel level = BbQrErrorCorrectionLevel.M,
        int maskPattern = 0,
        int minimumVersion = 1)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentOutOfRangeException.ThrowIfNegative(maskPattern);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maskPattern, 7);

        var mode = ChooseMode(content);
        var payload = mode == ModeByte ? Encoding.UTF8.GetBytes(content) : [];

        var version = ChooseVersion(content, payload, mode, level, minimumVersion)
            ?? throw new ArgumentException("The content does not fit in any QR Code version.", nameof(content));

        var ecBlocks = version.GetEcBlocksForLevel(level);
        var totalDataBits = ecBlocks.TotalDataCodewords * 8;

        var bits = new BitWriter();
        bits.Append(mode, 4);
        var characterCount = mode == ModeByte ? payload.Length : content.Length;
        bits.Append(characterCount, BbQrBitStreamParser.GetCharacterCountBits(version, mode));

        switch (mode)
        {
            case ModeNumeric:
                AppendNumeric(content, bits);
                break;
            case ModeAlphanumeric:
                AppendAlphanumeric(content, bits);
                break;
            default:
                foreach (var b in payload)
                {
                    bits.Append(b, 8);
                }

                break;
        }

        // Terminator, then padding to a codeword boundary, then the alternating pad codewords.
        var terminatorBits = Math.Min(4, totalDataBits - bits.Length);
        if (terminatorBits > 0)
        {
            bits.Append(0, terminatorBits);
        }

        while (bits.Length % 8 != 0)
        {
            bits.Append(0, 1);
        }

        var padAlternate = true;
        while (bits.Length < totalDataBits)
        {
            bits.Append(padAlternate ? 0xEC : 0x11, 8);
            padAlternate = !padAlternate;
        }

        var dataCodewords = bits.ToBytes();
        var interleaved = InterleaveWithEcBytes(dataCodewords, version, ecBlocks);

        var matrix = new BbBitMatrix(version.DimensionForVersion, version.DimensionForVersion);
        EmbedFunctionPatterns(matrix, version);
        EmbedFormatInformation(matrix, level, maskPattern);
        EmbedVersionInformation(matrix, version);
        EmbedData(matrix, version, interleaved, maskPattern);
        return matrix;
    }

    private static int ChooseMode(string content)
    {
        var numeric = true;
        var alphanumeric = true;

        foreach (var c in content)
        {
            if (c is < '0' or > '9')
            {
                numeric = false;
            }

            if (AlphanumericChars.IndexOf(c, StringComparison.Ordinal) < 0)
            {
                alphanumeric = false;
            }
        }

        if (numeric && content.Length > 0)
        {
            return ModeNumeric;
        }

        return alphanumeric && content.Length > 0 ? ModeAlphanumeric : ModeByte;
    }

    private static BbQrVersion? ChooseVersion(
        string content, byte[] payload, int mode, BbQrErrorCorrectionLevel level, int minimumVersion)
    {
        for (var number = Math.Max(1, minimumVersion); number <= 40; number++)
        {
            var version = BbQrVersion.GetVersionForNumber(number);
            var capacityBits = version.GetEcBlocksForLevel(level).TotalDataCodewords * 8;
            var countBits = BbQrBitStreamParser.GetCharacterCountBits(version, mode);

            var dataBits = mode switch
            {
                ModeNumeric => NumericBitLength(content.Length),
                ModeAlphanumeric => AlphanumericBitLength(content.Length),
                _ => payload.Length * 8,
            };

            if (4 + countBits + dataBits <= capacityBits)
            {
                return version;
            }
        }

        return null;
    }

    private static int NumericBitLength(int count) =>
        (count / 3 * 10) + ((count % 3) switch { 1 => 4, 2 => 7, _ => 0 });

    private static int AlphanumericBitLength(int count) => (count / 2 * 11) + ((count % 2) * 6);

    private static void AppendNumeric(string content, BitWriter bits)
    {
        var i = 0;
        while (i + 3 <= content.Length)
        {
            bits.Append(int.Parse(content.AsSpan(i, 3), System.Globalization.CultureInfo.InvariantCulture), 10);
            i += 3;
        }

        var remaining = content.Length - i;
        if (remaining == 2)
        {
            bits.Append(int.Parse(content.AsSpan(i, 2), System.Globalization.CultureInfo.InvariantCulture), 7);
        }
        else if (remaining == 1)
        {
            bits.Append(content[i] - '0', 4);
        }
    }

    private static void AppendAlphanumeric(string content, BitWriter bits)
    {
        var i = 0;
        while (i + 2 <= content.Length)
        {
            var first = AlphanumericChars.IndexOf(content[i], StringComparison.Ordinal);
            var second = AlphanumericChars.IndexOf(content[i + 1], StringComparison.Ordinal);
            bits.Append((first * 45) + second, 11);
            i += 2;
        }

        if (i < content.Length)
        {
            bits.Append(AlphanumericChars.IndexOf(content[i], StringComparison.Ordinal), 6);
        }
    }

    private static byte[] InterleaveWithEcBytes(byte[] dataCodewords, BbQrVersion version, BbQrEcBlocks ecBlocks)
    {
        var encoder = new ReedSolomonEncoder(BbGenericGF.QrCodeField256);

        var blocks = new List<(byte[] Data, byte[] Ec)>();
        var offset = 0;
        foreach (var group in ecBlocks.Groups)
        {
            for (var i = 0; i < group.Count; i++)
            {
                var data = dataCodewords[offset..(offset + group.DataCodewords)];
                offset += group.DataCodewords;

                var toEncode = new int[group.DataCodewords + ecBlocks.EcCodewordsPerBlock];
                for (var j = 0; j < data.Length; j++)
                {
                    toEncode[j] = data[j];
                }

                encoder.Encode(toEncode, ecBlocks.EcCodewordsPerBlock);

                var ec = new byte[ecBlocks.EcCodewordsPerBlock];
                for (var j = 0; j < ec.Length; j++)
                {
                    ec[j] = (byte)toEncode[data.Length + j];
                }

                blocks.Add((data, ec));
            }
        }

        var result = new byte[version.TotalCodewords];
        var index = 0;

        var maxData = 0;
        foreach (var block in blocks)
        {
            maxData = Math.Max(maxData, block.Data.Length);
        }

        for (var i = 0; i < maxData; i++)
        {
            foreach (var block in blocks)
            {
                if (i < block.Data.Length)
                {
                    result[index++] = block.Data[i];
                }
            }
        }

        for (var i = 0; i < ecBlocks.EcCodewordsPerBlock; i++)
        {
            foreach (var block in blocks)
            {
                result[index++] = block.Ec[i];
            }
        }

        return result;
    }

    private static void EmbedFunctionPatterns(BbBitMatrix matrix, BbQrVersion version)
    {
        var dimension = matrix.Width;

        EmbedFinderPattern(matrix, 0, 0);
        EmbedFinderPattern(matrix, dimension - 7, 0);
        EmbedFinderPattern(matrix, 0, dimension - 7);

        // The single always-dark module below the top-left format information.
        matrix[8, dimension - 8] = true;

        var centers = version.AlignmentPatternCenters;
        for (var row = 0; row < centers.Length; row++)
        {
            for (var col = 0; col < centers.Length; col++)
            {
                // The three positions that coincide with finder patterns are not drawn.
                if ((row == 0 && (col == 0 || col == centers.Length - 1)) ||
                    (row == centers.Length - 1 && col == 0))
                {
                    continue;
                }

                EmbedAlignmentPattern(matrix, centers[col] - 2, centers[row] - 2);
            }
        }

        for (var i = 8; i < dimension - 8; i++)
        {
            var dark = i % 2 == 0;
            matrix[6, i] = dark;
            matrix[i, 6] = dark;
        }
    }

    private static void EmbedFinderPattern(BbBitMatrix matrix, int left, int top)
    {
        for (var y = 0; y < 7; y++)
        {
            for (var x = 0; x < 7; x++)
            {
                var onOuterRing = x == 0 || x == 6 || y == 0 || y == 6;
                var inCore = x is >= 2 and <= 4 && y is >= 2 and <= 4;
                matrix[left + x, top + y] = onOuterRing || inCore;
            }
        }
    }

    private static void EmbedAlignmentPattern(BbBitMatrix matrix, int left, int top)
    {
        for (var y = 0; y < 5; y++)
        {
            for (var x = 0; x < 5; x++)
            {
                var onOuterRing = x == 0 || x == 4 || y == 0 || y == 4;
                var isCenter = x == 2 && y == 2;
                matrix[left + x, top + y] = onOuterRing || isCenter;
            }
        }
    }

    /// <summary>Coordinates of the fifteen format information modules around the top-left finder.</summary>
    private static readonly (int X, int Y)[] TypeInfoCoordinates =
    [
        (8, 0), (8, 1), (8, 2), (8, 3), (8, 4), (8, 5), (8, 7), (8, 8),
        (7, 8), (5, 8), (4, 8), (3, 8), (2, 8), (1, 8), (0, 8),
    ];

    private static void EmbedFormatInformation(BbBitMatrix matrix, BbQrErrorCorrectionLevel level, int maskPattern)
    {
        var dimension = matrix.Width;
        var formatBits = BbQrFormatInformation.EncodeFormatBits((((int)level) << 3) | maskPattern);

        for (var i = 0; i < 15; i++)
        {
            // The least significant bit goes to the first coordinate.
            var bit = ((formatBits >> i) & 1) != 0;

            var (x, y) = TypeInfoCoordinates[i];
            matrix[x, y] = bit;

            if (i < 8)
            {
                matrix[dimension - i - 1, 8] = bit;
            }
            else
            {
                matrix[8, dimension - 7 + (i - 8)] = bit;
            }
        }
    }

    private static void EmbedVersionInformation(BbBitMatrix matrix, BbQrVersion version)
    {
        if (version.VersionNumber < 7)
        {
            return;
        }

        var dimension = matrix.Width;
        var versionBits = BbQrFormatInformation.EncodeVersionBits(version.VersionNumber);

        var bitIndex = 17;
        for (var i = 0; i < 6; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                var bit = ((versionBits >> (17 - bitIndex)) & 1) != 0;
                bitIndex--;

                matrix[i, dimension - 11 + j] = bit;
                matrix[dimension - 11 + j, i] = bit;
            }
        }
    }

    private static void EmbedData(BbBitMatrix matrix, BbQrVersion version, byte[] codewords, int maskPattern)
    {
        using var functionPattern = version.BuildFunctionPattern();

        var dimension = matrix.Width;
        var bitIndex = 0;
        var readingUp = true;

        for (var j = dimension - 1; j > 0; j -= 2)
        {
            if (j == 6)
            {
                j--;
            }

            for (var count = 0; count < dimension; count++)
            {
                var i = readingUp ? dimension - 1 - count : count;
                for (var col = 0; col < 2; col++)
                {
                    var x = j - col;
                    if (functionPattern[x, i])
                    {
                        continue;
                    }

                    var bit = false;
                    if (bitIndex < codewords.Length * 8)
                    {
                        bit = ((codewords[bitIndex / 8] >> (7 - (bitIndex % 8))) & 1) != 0;
                        bitIndex++;
                    }

                    if (BbQrDataMask.IsMasked(maskPattern, i, x))
                    {
                        bit = !bit;
                    }

                    matrix[x, i] = bit;
                }
            }

            readingUp = !readingUp;
        }
    }

    /// <summary>A minimal most-significant-bit-first bit accumulator.</summary>
    private sealed class BitWriter
    {
        private readonly List<bool> _bits = new(256);

        public int Length => _bits.Count;

        public void Append(int value, int count)
        {
            for (var i = count - 1; i >= 0; i--)
            {
                _bits.Add(((value >> i) & 1) != 0);
            }
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[_bits.Count / 8];
            for (var i = 0; i < bytes.Length * 8; i++)
            {
                if (_bits[i])
                {
                    bytes[i / 8] |= (byte)(1 << (7 - (i % 8)));
                }
            }

            return bytes;
        }
    }
}
