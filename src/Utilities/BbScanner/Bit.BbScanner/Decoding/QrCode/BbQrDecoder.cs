namespace Bit.BbScanner;

/// <summary>
/// Decodes a sampled QR module grid into its payload.
/// </summary>
/// <remarks>
/// This stage assumes detection has already produced a square, upright module grid. Keeping it
/// separate from detection means it can be driven directly from a synthetic matrix, which is
/// what the round-trip tests do, and it makes the expensive detection stage replaceable without
/// touching the specification-heavy decoding logic.
/// </remarks>
public sealed class BbQrDecoder
{
    private readonly BbReedSolomonDecoder _reedSolomon = new(BbGenericGF.QrCodeField256);

    /// <summary>Decodes a module grid.</summary>
    /// <param name="matrix">The sampled grid, one bit per module.</param>
    /// <returns>The payload, or <see langword="null"/> when the grid does not decode.</returns>
    public BbDecoderResult? Decode(BbBitMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        var result = DecodeUpright(matrix);
        if (result is not null)
        {
            return result;
        }

        // A symbol photographed through a mirror, or read from the back of a transparent
        // surface, is transposed. Retrying the transpose is far cheaper than failing the frame.
        using var mirrored = Transpose(matrix);
        return DecodeUpright(mirrored);
    }

    private BbDecoderResult? DecodeUpright(BbBitMatrix matrix)
    {
        var parser = BbQrBitMatrixParser.Create(matrix);
        if (parser is null)
        {
            return null;
        }

        var format = parser.ReadFormatInformation();
        if (format is null)
        {
            return null;
        }

        var version = parser.ReadVersion();
        if (version is null || version.DimensionForVersion != matrix.Height)
        {
            return null;
        }

        var codewords = parser.ReadCodewords(version, format.DataMask);
        if (codewords is null)
        {
            return null;
        }

        var blocks = BbQrDataBlocks.Split(codewords, version, format.ErrorCorrectionLevel);
        if (blocks is null)
        {
            return null;
        }

        var ecBlocks = version.GetEcBlocksForLevel(format.ErrorCorrectionLevel);
        var totalData = ecBlocks.TotalDataCodewords;
        var data = new byte[totalData];
        var offset = 0;
        var errorsCorrected = 0;

        foreach (var block in blocks)
        {
            var codewordBytes = block.Codewords;
            var window = new int[codewordBytes.Length];
            for (var i = 0; i < codewordBytes.Length; i++)
            {
                window[i] = codewordBytes[i];
            }

            if (!_reedSolomon.TryDecode(window, codewordBytes.Length - block.NumDataCodewords, out var corrected))
            {
                return null;
            }

            errorsCorrected += corrected;
            for (var i = 0; i < block.NumDataCodewords; i++)
            {
                data[offset++] = (byte)window[i];
            }
        }

        return offset != totalData
            ? null
            : BbQrBitStreamParser.Decode(data, version, format.ErrorCorrectionLevel, errorsCorrected);
    }

    private static BbBitMatrix Transpose(BbBitMatrix matrix)
    {
        var result = new BbBitMatrix(matrix.Height, matrix.Width);
        for (var y = 0; y < matrix.Height; y++)
        {
            for (var x = 0; x < matrix.Width; x++)
            {
                if (matrix[x, y])
                {
                    result[y, x] = true;
                }
            }
        }

        return result;
    }
}
