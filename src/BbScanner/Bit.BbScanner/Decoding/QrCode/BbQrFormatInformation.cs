using System.Numerics;

namespace Bit.BbScanner;

/// <summary>
/// Decodes the fifteen bit format information field, which carries the error correction level
/// and the data mask.
/// </summary>
/// <remarks>
/// The field is protected by a BCH(15, 5) code and then XORed with a fixed mask so that an
/// all-zero field cannot occur. Rather than transcribing the thirty two legal bit patterns, the
/// code is generated here from the same polynomial division the encoder uses, and decoding is a
/// nearest neighbour search over those generated patterns. That makes the implementation
/// self-checking: an error in the generator would fail every round trip immediately.
/// </remarks>
public sealed class BbQrFormatInformation
{
    /// <summary>The XOR mask the specification applies to the encoded field.</summary>
    public const int FormatInfoMask = 0x5412;

    /// <summary>Generator polynomial of the BCH(15, 5) code: x^10 + x^8 + x^5 + x^4 + x^2 + x + 1.</summary>
    private const int FormatGenerator = 0x537;

    /// <summary>Generator polynomial of the BCH(18, 6) version code: x^12 + x^11 + x^10 + x^9 + x^8 + x^5 + x^2 + 1.</summary>
    private const int VersionGenerator = 0x1F25;

    private static readonly int[] FormatCodes = BuildFormatCodes();
    private static readonly int[] VersionCodes = BuildVersionCodes();

    private BbQrFormatInformation(BbQrErrorCorrectionLevel level, int dataMask)
    {
        ErrorCorrectionLevel = level;
        DataMask = dataMask;
    }

    /// <summary>The error correction level the symbol was encoded at.</summary>
    public BbQrErrorCorrectionLevel ErrorCorrectionLevel { get; }

    /// <summary>The data mask pattern reference, 0 to 7.</summary>
    public int DataMask { get; }

    /// <summary>
    /// Decodes the format information from its two copies in the symbol.
    /// </summary>
    /// <param name="maskedFormatInfo1">The copy read around the top-left finder pattern.</param>
    /// <param name="maskedFormatInfo2">The copy split between the other two finder patterns.</param>
    /// <returns>The decoded field, or <see langword="null"/> when neither copy is close enough to a legal value.</returns>
    public static BbQrFormatInformation? Decode(int maskedFormatInfo1, int maskedFormatInfo2)
    {
        var result = DoDecode(maskedFormatInfo1, maskedFormatInfo2);
        if (result is not null)
        {
            return result;
        }

        // Some encoders in the wild omit the mask. Retrying without it costs nothing and
        // recovers symbols that would otherwise be unreadable.
        return DoDecode(maskedFormatInfo1 ^ FormatInfoMask, maskedFormatInfo2 ^ FormatInfoMask);
    }

    private static BbQrFormatInformation? DoDecode(int candidate1, int candidate2)
    {
        var bestDifference = int.MaxValue;
        var bestFormatInfo = 0;

        for (var data = 0; data < 32; data++)
        {
            var target = FormatCodes[data];
            if (target == candidate1 || target == candidate2)
            {
                return FromDataBits(data);
            }

            var difference = BitOperations.PopCount((uint)(candidate1 ^ target));
            if (difference < bestDifference)
            {
                bestFormatInfo = data;
                bestDifference = difference;
            }

            if (candidate1 == candidate2)
            {
                continue;
            }

            difference = BitOperations.PopCount((uint)(candidate2 ^ target));
            if (difference < bestDifference)
            {
                bestFormatInfo = data;
                bestDifference = difference;
            }
        }

        // The code has a minimum distance of seven, so three bit errors are still unambiguous.
        return bestDifference <= 3 ? FromDataBits(bestFormatInfo) : null;
    }

    private static BbQrFormatInformation FromDataBits(int data) =>
        new((BbQrErrorCorrectionLevel)((data >> 3) & 0x03), data & 0x07);

    /// <summary>
    /// Decodes the eighteen bit version information carried by symbols of version 7 and above.
    /// </summary>
    /// <param name="versionBits">The raw eighteen bit field.</param>
    /// <returns>The version, or <see langword="null"/> when the field is too damaged to place.</returns>
    public static BbQrVersion? DecodeVersionInformation(int versionBits)
    {
        var bestDifference = int.MaxValue;
        var bestVersion = 0;

        for (var i = 0; i < VersionCodes.Length; i++)
        {
            var target = VersionCodes[i];
            var version = i + 7;
            if (target == versionBits)
            {
                return BbQrVersion.GetVersionForNumber(version);
            }

            var difference = BitOperations.PopCount((uint)(versionBits ^ target));
            if (difference < bestDifference)
            {
                bestDifference = difference;
                bestVersion = version;
            }
        }

        // The version code has a minimum distance of eight, so three errors remain correctable.
        return bestDifference <= 3 ? BbQrVersion.GetVersionForNumber(bestVersion) : null;
    }

    /// <summary>Encodes a five bit format value the way the specification does. Exposed for tests and encoders.</summary>
    /// <param name="data">Two bits of error correction level followed by three bits of data mask.</param>
    public static int EncodeFormatBits(int data)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(data);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(data, 31);
        return FormatCodes[data];
    }

    /// <summary>Encodes the eighteen bit version information field. Exposed for tests and encoders.</summary>
    /// <param name="version">The version number, 7 to 40.</param>
    public static int EncodeVersionBits(int version)
    {
        if (version is < 7 or > 40)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Only versions 7 to 40 carry version information.");
        }

        return VersionCodes[version - 7];
    }

    private static int[] BuildFormatCodes()
    {
        var codes = new int[32];
        for (var data = 0; data < 32; data++)
        {
            codes[data] = ((data << 10) | Remainder(data << 10, FormatGenerator, 10)) ^ FormatInfoMask;
        }

        return codes;
    }

    private static int[] BuildVersionCodes()
    {
        var codes = new int[34];
        for (var version = 7; version <= 40; version++)
        {
            codes[version - 7] = (version << 12) | Remainder(version << 12, VersionGenerator, 12);
        }

        return codes;
    }

    /// <summary>Polynomial remainder over GF(2), which is how both BCH codes here are defined.</summary>
    private static int Remainder(int value, int generator, int checkBits)
    {
        var generatorDegree = 31 - BitOperations.LeadingZeroCount((uint)generator);
        while (31 - BitOperations.LeadingZeroCount((uint)value) >= generatorDegree && value != 0)
        {
            var shift = (31 - BitOperations.LeadingZeroCount((uint)value)) - generatorDegree;
            value ^= generator << shift;
        }

        return value & ((1 << checkBits) - 1);
    }
}
