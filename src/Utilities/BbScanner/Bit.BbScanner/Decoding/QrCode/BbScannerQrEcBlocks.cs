namespace Bit.BbScanner;

/// <summary>
/// The block layout for one version at one error correction level.
/// </summary>
/// <param name="EcCodewordsPerBlock">Error correction codewords appended to each block.</param>
/// <param name="Groups">One or two groups of blocks; the second group holds one extra data codeword per block.</param>
public sealed record BbScannerQrEcBlocks(int EcCodewordsPerBlock, BbScannerQrEcBlockGroup[] Groups)
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
