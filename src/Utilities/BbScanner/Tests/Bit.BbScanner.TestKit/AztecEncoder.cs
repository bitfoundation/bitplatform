using System.Text;

namespace Bit.BbScanner.TestKit;

/// <summary>
/// An Aztec Code encoder used to generate test symbols, demo samples and benchmark inputs.
/// </summary>
/// <remarks>
/// Upper case letters and spaces are written in the upper case mode and everything else as binary shift runs of
/// UTF-8 bytes - enough to drive every structural part of the decoder: compact and full range symbols, every
/// codeword size, bit stuffing, the reference grid and the mode message. The reference fixtures written by BWIPP cover
/// the other character modes.
/// </remarks>
public static class AztecEncoder
{
    private static readonly BbGenericGF ModeField = new(0x13, 16, 1);
    private static readonly BbGenericGF Field6 = new(0x43, 64, 1);
    private static readonly BbGenericGF Field10 = new(0x409, 1024, 1);
    private static readonly BbGenericGF Field12 = new(0x1069, 4096, 1);

    /// <summary>Encodes text, choosing the smallest symbol that holds it with the error correction asked for.</summary>
    /// <param name="content">The text to encode.</param>
    /// <param name="errorCorrectionPercent">The share of the symbol, beyond three codewords, given to error correction.</param>
    /// <param name="compact">Only a compact symbol (<see langword="true"/>), only a full range one (<see langword="false"/>), or either.</param>
    /// <param name="layers">A specific number of layers, or zero to choose.</param>
    public static BbBitMatrix Encode(string content, int errorCorrectionPercent = 33, bool? compact = null, int layers = 0)
    {
        ArgumentNullException.ThrowIfNull(content);
        var bits = HighLevel(content);

        foreach (var (isCompact, count) in Candidates(compact, layers))
        {
            var wordSize = WordSize(count);
            var stuffed = Stuff(bits, wordSize);
            var totalBits = TotalBits(isCompact, count);
            var totalWords = totalBits / wordSize;
            var dataWords = stuffed.Count;
            var minimumCheck = (dataWords * errorCorrectionPercent / 100) + 3;
            if (dataWords + minimumCheck > totalWords || dataWords > (isCompact ? 64 : 2048))
            {
                continue;
            }

            return Build(isCompact, count, stuffed, totalWords, wordSize);
        }

        throw new ArgumentException("The content does not fit in the Aztec symbol asked for.", nameof(content));
    }

    private static IEnumerable<(bool Compact, int Layers)> Candidates(bool? compact, int layers)
    {
        // Smallest first: compact 1 to 4 layers sit between full range sizes, and are tried in size order.
        var all = new List<(bool, int, int)>();
        for (var l = 1; l <= 4; l++)
        {
            all.Add((true, l, 11 + (4 * l)));
        }

        for (var l = 1; l <= 32; l++)
        {
            all.Add((false, l, Size(false, l)));
        }

        foreach (var (isCompact, count, _) in all.OrderBy(c => c.Item3).ThenBy(c => c.Item1 ? 0 : 1))
        {
            if ((compact is null || compact == isCompact) && (layers == 0 || layers == count))
            {
                yield return (isCompact, count);
            }
        }
    }

    private static int Size(bool compact, int layers)
    {
        if (compact)
        {
            return 11 + (4 * layers);
        }

        var withoutGrid = 14 + (4 * layers);
        return withoutGrid + 1 + (2 * (((withoutGrid / 2) - 1) / 15));
    }

    private static int WordSize(int layers) => layers switch
    {
        <= 2 => 6,
        <= 8 => 8,
        <= 22 => 10,
        _ => 12,
    };

    private static int TotalBits(bool compact, int layers) => ((compact ? 88 : 112) + (16 * layers)) * layers;

    /// <summary>Upper case letters and spaces as themselves; any other run as a binary shift of its UTF-8 bytes.</summary>
    private static List<bool> HighLevel(string content)
    {
        var bits = new List<bool>();
        var i = 0;
        while (i < content.Length)
        {
            var c = content[i];
            if (c == ' ' || c is >= 'A' and <= 'Z')
            {
                Append(bits, c == ' ' ? 1 : c - 'A' + 2, 5);
                i++;
                continue;
            }

            var end = i;
            while (end < content.Length && !(content[end] == ' ' || content[end] is >= 'A' and <= 'Z'))
            {
                end++;
            }

            var bytes = Encoding.UTF8.GetBytes(content[i..end]);
            for (var start = 0; start < bytes.Length;)
            {
                var length = Math.Min(bytes.Length - start, 2047 + 31);
                Append(bits, 31, 5);
                if (length <= 31)
                {
                    Append(bits, length, 5);
                }
                else
                {
                    Append(bits, 0, 5);
                    Append(bits, length - 31, 11);
                }

                for (var b = 0; b < length; b++)
                {
                    Append(bits, bytes[start + b], 8);
                }

                start += length;
            }

            i = end;
        }

        return bits;
    }

    private static void Append(List<bool> bits, int value, int count)
    {
        for (var b = count - 1; b >= 0; b--)
        {
            bits.Add(((value >> b) & 1) != 0);
        }
    }

    /// <summary>
    /// Packs the bits into codewords, padding the last with ones. A codeword whose leading bits are all the same
    /// gets a complementary final bit, the next data bit starting the next codeword, so that none is all zeros or
    /// all ones.
    /// </summary>
    private static List<int> Stuff(List<bool> bits, int wordSize)
    {
        var words = new List<int>();
        var mask = (1 << wordSize) - 2;
        for (var i = 0; i < bits.Count; i += wordSize)
        {
            var word = 0;
            for (var j = 0; j < wordSize; j++)
            {
                if (i + j >= bits.Count || bits[i + j])
                {
                    word |= 1 << (wordSize - 1 - j);
                }
            }

            if ((word & mask) == mask)
            {
                words.Add(word & mask);
                i--;
            }
            else if ((word & mask) == 0)
            {
                words.Add(word | 1);
                i--;
            }
            else
            {
                words.Add(word);
            }
        }

        return words;
    }

    private static BbBitMatrix Build(bool compact, int layers, List<int> dataWords, int totalWords, int wordSize)
    {
        var field = wordSize switch
        {
            6 => Field6,
            8 => BbGenericGF.DataMatrixField256,
            10 => Field10,
            _ => Field12,
        };

        var words = new int[totalWords];
        dataWords.CopyTo(words);
        new ReedSolomonEncoder(field).Encode(words, totalWords - dataWords.Count);

        // The codewords end flush with the outside of the symbol; the bits that do not fill one lead, as zeros.
        var totalBits = TotalBits(compact, layers);
        var message = new bool[totalBits];
        var at = totalBits % wordSize;
        foreach (var word in words)
        {
            for (var b = wordSize - 1; b >= 0; b--)
            {
                message[at++] = ((word >> b) & 1) != 0;
            }
        }

        var size = Size(compact, layers);
        var matrix = new BbBitMatrix(size, size);
        var c = size / 2;
        void Set(int u, int v, bool dark)
        {
            matrix[c + u, c + v] = dark;
        }

        PlaceData(matrix, compact, layers, message);

        if (!compact)
        {
            // The reference grid: lines through the centre and every sixteen modules out, dark where the coordinate
            // along the line is even.
            var reach = (size - 1) / 2;
            for (var g = -(reach / 16) * 16; g <= reach; g += 16)
            {
                for (var k = -reach; k <= reach; k++)
                {
                    Set(k, g, (k & 1) == 0);
                    Set(g, k, (k & 1) == 0);
                }
            }
        }

        // The bullseye, dark at even distances from the centre.
        var rings = compact ? 4 : 6;
        for (var v = -rings; v <= rings; v++)
        {
            for (var u = -rings; u <= rings; u++)
            {
                Set(u, v, (Math.Max(Math.Abs(u), Math.Abs(v)) & 1) == 0);
            }
        }

        PlaceModeMessage(compact, layers, dataWords.Count, Set);
        return matrix;
    }

    private static void PlaceModeMessage(bool compact, int layers, int dataWords, Action<int, int, bool> set)
    {
        var dataNibbles = compact ? 2 : 4;
        var total = compact ? 7 : 10;
        var value = compact ? ((layers - 1) << 6) | (dataWords - 1) : ((layers - 1) << 11) | (dataWords - 1);
        var nibbles = new int[total];
        for (var i = 0; i < dataNibbles; i++)
        {
            nibbles[i] = (value >> (4 * (dataNibbles - 1 - i))) & 0xF;
        }

        new ReedSolomonEncoder(ModeField).Encode(nibbles, total - dataNibbles);
        var bits = new bool[total * 4];
        for (var i = 0; i < bits.Length; i++)
        {
            bits[i] = ((nibbles[i / 4] >> (3 - (i % 4))) & 1) != 0;
        }

        var d = compact ? 5 : 7;
        var perSide = compact ? 7 : 10;
        for (var side = 0; side < 4; side++)
        {
            for (var j = 0; j < perSide; j++)
            {
                // Clockwise from the top left corner, as the decoder reads it: two orientation modules, the data
                // (with the reference grid module in the middle of a full range side), one more orientation module.
                var k = compact ? 2 + j : (j < 5 ? 2 + j : 3 + j);
                var (u, v) = side switch
                {
                    0 => (-d + k, -d),
                    1 => (d, -d + k),
                    2 => (d - k, d),
                    _ => (-d, d - k),
                };

                set(u, v, bits[(side * perSide) + j]);
            }
        }

        // Orientation: three dark modules at the top left corner, two at the top right, one at the bottom right.
        set(-d, -d, true);
        set(-d + 1, -d, true);
        set(-d, -d + 1, true);
        set(d, -d, true);
        set(d, -d + 1, true);
        set(d, d - 1, true);
    }

    /// <summary>The decoder's layer order, written instead of read.</summary>
    private static void PlaceData(BbBitMatrix matrix, bool compact, int layers, bool[] bits)
    {
        var withoutGrid = (compact ? 11 : 14) + (4 * layers);
        var map = new int[withoutGrid];
        if (compact)
        {
            for (var i = 0; i < map.Length; i++)
            {
                map[i] = i;
            }
        }
        else
        {
            var half = withoutGrid / 2;
            var centre = matrix.Width / 2;
            for (var i = 0; i < half; i++)
            {
                var outward = i + (i / 15);
                map[half - i - 1] = centre - outward - 1;
                map[half + i] = centre + outward + 1;
            }
        }

        var at = 0;
        for (var layer = 0; layer < layers; layer++)
        {
            var length = ((layers - layer) * 4) + (compact ? 9 : 12);
            var low = layer * 2;
            var high = withoutGrid - 1 - low;
            for (var j = 0; j < length; j++)
            {
                for (var k = 0; k < 2; k++)
                {
                    var step = (j * 2) + k;
                    matrix[map[low + k], map[low + j]] = bits[at + step];
                    matrix[map[low + j], map[high - k]] = bits[at + (2 * length) + step];
                    matrix[map[high - k], map[high - j]] = bits[at + (4 * length) + step];
                    matrix[map[high - j], map[low + k]] = bits[at + (6 * length) + step];
                }
            }

            at += length * 8;
        }
    }
}
