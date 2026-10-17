namespace Bit.BbScanner;

/// <summary>
/// Splits the interleaved codeword stream of a QR symbol back into its error correction blocks.
/// </summary>
/// <remarks>
/// A QR symbol interleaves its blocks so that a physical smudge damages a few codewords of many
/// blocks rather than destroying one block completely. De-interleaving is therefore the first
/// step of decoding, and it has to account for the fact that the last blocks of a version may
/// carry one more data codeword than the first ones.
/// </remarks>
public static class BbQrDataBlocks
{
    /// <summary>One de-interleaved block: data codewords followed by error correction codewords.</summary>
    /// <param name="NumDataCodewords">How many leading codewords carry data.</param>
    /// <param name="Codewords">Data codewords followed by error correction codewords.</param>
    public readonly record struct Block(int NumDataCodewords, byte[] Codewords);

    /// <summary>
    /// De-interleaves <paramref name="rawCodewords"/>.
    /// </summary>
    /// <param name="rawCodewords">The codewords in symbol order.</param>
    /// <param name="version">The symbol version.</param>
    /// <param name="level">The error correction level.</param>
    /// <returns>The blocks, or <see langword="null"/> when the input length does not match the version.</returns>
    public static Block[]? Split(byte[] rawCodewords, BbQrVersion version, BbQrErrorCorrectionLevel level)
    {
        ArgumentNullException.ThrowIfNull(rawCodewords);
        ArgumentNullException.ThrowIfNull(version);

        if (rawCodewords.Length != version.TotalCodewords)
        {
            return null;
        }

        var ecBlocks = version.GetEcBlocksForLevel(level);
        var totalBlocks = ecBlocks.NumBlocks;
        var result = new Block[totalBlocks];

        var index = 0;
        foreach (var group in ecBlocks.Groups)
        {
            for (var i = 0; i < group.Count; i++)
            {
                var blockLength = ecBlocks.EcCodewordsPerBlock + group.DataCodewords;
                result[index++] = new Block(group.DataCodewords, new byte[blockLength]);
            }
        }

        var shorterBlocksTotalCodewords = result[0].Codewords.Length;
        var longerBlocksStartAt = result.Length - 1;
        while (longerBlocksStartAt >= 0 &&
               result[longerBlocksStartAt].Codewords.Length != shorterBlocksTotalCodewords)
        {
            longerBlocksStartAt--;
        }

        longerBlocksStartAt++;

        var shorterBlocksNumDataCodewords = shorterBlocksTotalCodewords - ecBlocks.EcCodewordsPerBlock;
        var offset = 0;

        // Data codewords are interleaved across every block, one codeword at a time.
        for (var i = 0; i < shorterBlocksNumDataCodewords; i++)
        {
            for (var j = 0; j < totalBlocks; j++)
            {
                result[j].Codewords[i] = rawCodewords[offset++];
            }
        }

        // The longer blocks then contribute their one extra data codeword.
        for (var j = longerBlocksStartAt; j < totalBlocks; j++)
        {
            result[j].Codewords[shorterBlocksNumDataCodewords] = rawCodewords[offset++];
        }

        // Finally the error correction codewords, again interleaved.
        var max = result[0].Codewords.Length;
        for (var i = shorterBlocksNumDataCodewords; i < max; i++)
        {
            for (var j = 0; j < totalBlocks; j++)
            {
                var target = j < longerBlocksStartAt ? i : i + 1;
                result[j].Codewords[target] = rawCodewords[offset++];
            }
        }

        return offset == rawCodewords.Length ? result : null;
    }
}
