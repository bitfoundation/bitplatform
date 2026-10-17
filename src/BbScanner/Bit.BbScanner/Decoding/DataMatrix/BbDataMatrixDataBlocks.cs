namespace Bit.BbScanner;

/// <summary>
/// Splits the interleaved codeword stream of a Data Matrix symbol back into its error
/// correction blocks.
/// </summary>
public static class BbDataMatrixDataBlocks
{
    /// <summary>One de-interleaved block.</summary>
    /// <param name="NumDataCodewords">How many leading codewords carry data.</param>
    /// <param name="Codewords">Data codewords followed by error correction codewords.</param>
    public readonly record struct Block(int NumDataCodewords, byte[] Codewords);

    /// <summary>De-interleaves the codewords of a symbol.</summary>
    /// <param name="rawCodewords">The codewords in placement order.</param>
    /// <param name="version">The symbol shape.</param>
    /// <returns>The blocks, or <see langword="null"/> when the input length does not match the shape.</returns>
    public static Block[]? Split(byte[] rawCodewords, BbDataMatrixVersion version)
    {
        ArgumentNullException.ThrowIfNull(rawCodewords);
        ArgumentNullException.ThrowIfNull(version);

        if (rawCodewords.Length != version.TotalCodewords)
        {
            return null;
        }

        var totalBlocks = version.NumBlocks;
        var result = new Block[totalBlocks];
        var index = 0;
        foreach (var group in version.Groups)
        {
            for (var i = 0; i < group.Count; i++)
            {
                result[index++] = new Block(group.DataCodewords, new byte[group.DataCodewords + version.EcCodewords]);
            }
        }

        // Unlike QR, the longer blocks come first, so the shared prefix is one codeword shorter
        // than the first block holds.
        var longerBlocksNumDataCodewords = result[0].Codewords.Length - version.EcCodewords;
        var shorterBlocksNumDataCodewords = longerBlocksNumDataCodewords - 1;

        var offset = 0;
        for (var i = 0; i < shorterBlocksNumDataCodewords; i++)
        {
            for (var j = 0; j < totalBlocks; j++)
            {
                result[j].Codewords[i] = rawCodewords[offset++];
            }
        }

        // The 144 by 144 symbol is the only one whose blocks are not all the same length, and it
        // interleaves them with an eight block rotation.
        var specialVersion = version.VersionNumber == 24;
        var numLongerBlocks = specialVersion ? 8 : totalBlocks;
        for (var j = 0; j < numLongerBlocks; j++)
        {
            result[j].Codewords[longerBlocksNumDataCodewords - 1] = rawCodewords[offset++];
        }

        var max = result[0].Codewords.Length;
        for (var i = longerBlocksNumDataCodewords; i < max; i++)
        {
            for (var j = 0; j < totalBlocks; j++)
            {
                var blockIndex = specialVersion ? (j + 8) % totalBlocks : j;
                var codewordIndex = specialVersion && blockIndex > 7 ? i - 1 : i;
                result[blockIndex].Codewords[codewordIndex] = rawCodewords[offset++];
            }
        }

        return offset == rawCodewords.Length ? result : null;
    }
}
