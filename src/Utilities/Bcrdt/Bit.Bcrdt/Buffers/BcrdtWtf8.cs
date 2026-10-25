using System.Text;

namespace Bit.Bcrdt;

/// <summary>
/// A UTF-8 encoder that round-trips every possible .NET string, including unpaired surrogates.
/// </summary>
/// <remarks>
/// <para>
/// This exists because of a subtle but fatal interaction between run-length text encoding and
/// UTF-8. A block of text is split whenever anything needs to address a position inside it, and that
/// position can fall between the two halves of a surrogate pair - an emoji, say. Each half is then a
/// lone surrogate, which is a perfectly ordinary .NET <see cref="char"/> but is not representable in
/// strict UTF-8.
/// </para>
/// <para>
/// Standard <see cref="Encoding.UTF8"/> silently replaces such a char with U+FFFD. That turns a
/// harmless internal split into permanent data loss - and worse, the loss depends on <i>whether a
/// given replica happened to split that block</i>, which differs between replicas and therefore
/// breaks convergence. A replica that split the emoji would show two replacement characters where
/// its peers still show the emoji, and no amount of further synchronization would reconcile them.
/// </para>
/// <para>
/// The fix is WTF-8: identical to UTF-8 for every well-formed string, with the single addition that
/// an unpaired surrogate is encoded as its own three-byte sequence rather than destroyed. Splits
/// become lossless, the halves recombine into the original character when read back, and replicas
/// converge regardless of how their blocks happen to be divided.
/// </para>
/// </remarks>
public static class BcrdtWtf8
{
    // The default UTF8Encoding substitutes U+FFFD for malformed input instead of failing, which
    // would let a corrupt payload through as plausible-looking text. This one throws.
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Measures the encoded size of a string.</summary>
    /// <param name="value">The string to measure.</param>
    /// <param name="isStrictUtf8">
    /// Whether the string contains no unpaired surrogates, and can therefore take the fast path
    /// through the framework's own encoder.
    /// </param>
    public static int GetByteCount(string value, out bool isStrictUtf8)
    {
        int total = 0;
        isStrictUtf8 = true;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];

            if (c < 0x80)
            {
                total += 1;
            }
            else if (c < 0x800)
            {
                total += 2;
            }
            else if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
            {
                total += 4;
                i++;
            }
            else
            {
                if (char.IsSurrogate(c)) isStrictUtf8 = false;
                total += 3;
            }
        }

        return total;
    }

    /// <summary>Encodes a string into a buffer sized by <see cref="GetByteCount"/>.</summary>
    public static int Encode(string value, Span<byte> destination)
    {
        int written = 0;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];

            if (c < 0x80)
            {
                destination[written++] = (byte)c;
            }
            else if (c < 0x800)
            {
                destination[written++] = (byte)(0xC0 | (c >> 6));
                destination[written++] = (byte)(0x80 | (c & 0x3F));
            }
            else if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
            {
                int codePoint = char.ConvertToUtf32(c, value[i + 1]);
                i++;
                destination[written++] = (byte)(0xF0 | (codePoint >> 18));
                destination[written++] = (byte)(0x80 | ((codePoint >> 12) & 0x3F));
                destination[written++] = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
                destination[written++] = (byte)(0x80 | (codePoint & 0x3F));
            }
            else
            {
                // Reached by ordinary three-byte characters and by unpaired surrogates alike. The
                // surrogate is encoded as itself rather than replaced, which is the whole point.
                destination[written++] = (byte)(0xE0 | (c >> 12));
                destination[written++] = (byte)(0x80 | ((c >> 6) & 0x3F));
                destination[written++] = (byte)(0x80 | (c & 0x3F));
            }
        }

        return written;
    }

    /// <summary>Decodes bytes produced by <see cref="Encode"/>.</summary>
    /// <exception cref="BcrdtFormatException">The bytes are not valid WTF-8.</exception>
    public static string Decode(ReadOnlySpan<byte> source)
    {
        // Well-formed strings are the overwhelming majority, and only a surrogate or a character in
        // U+D000-U+DFFF can begin with 0xED. Its absence proves the payload is strict UTF-8, so the
        // framework's vectorized decoder can handle it.
        if (source.IndexOf((byte)0xED) < 0)
        {
            try
            {
                return StrictUtf8.GetString(source);
            }
            catch (DecoderFallbackException ex)
            {
                throw new BcrdtFormatException("String is not valid UTF-8.", ex);
            }
        }

        var builder = new StringBuilder(source.Length);
        int index = 0;

        while (index < source.Length)
        {
            byte first = source[index];

            if (first < 0x80)
            {
                builder.Append((char)first);
                index++;
            }
            else if ((first & 0xE0) == 0xC0)
            {
                Require(source, index, 2);
                int codePoint = ((first & 0x1F) << 6) | Continuation(source, index + 1);
                if (codePoint < 0x80) throw new BcrdtFormatException("Overlong UTF-8 sequence.");
                builder.Append((char)codePoint);
                index += 2;
            }
            else if ((first & 0xF0) == 0xE0)
            {
                Require(source, index, 3);
                int codePoint = ((first & 0x0F) << 12)
                    | (Continuation(source, index + 1) << 6)
                    | Continuation(source, index + 2);
                if (codePoint < 0x800) throw new BcrdtFormatException("Overlong UTF-8 sequence.");
                builder.Append((char)codePoint);
                index += 3;
            }
            else if ((first & 0xF8) == 0xF0)
            {
                Require(source, index, 4);
                int codePoint = ((first & 0x07) << 18)
                    | (Continuation(source, index + 1) << 12)
                    | (Continuation(source, index + 2) << 6)
                    | Continuation(source, index + 3);
                if (codePoint is < 0x10000 or > 0x10FFFF)
                {
                    throw new BcrdtFormatException("UTF-8 sequence is out of range.");
                }
                builder.Append(char.ConvertFromUtf32(codePoint));
                index += 4;
            }
            else
            {
                throw new BcrdtFormatException($"Invalid UTF-8 lead byte 0x{first:X2}.");
            }
        }

        return builder.ToString();
    }

    private static void Require(ReadOnlySpan<byte> source, int index, int length)
    {
        if (index + length > source.Length)
        {
            throw BcrdtFormatException.Truncated("UTF-8 sequence", length, source.Length - index);
        }
    }

    private static int Continuation(ReadOnlySpan<byte> source, int index)
    {
        byte b = source[index];
        if ((b & 0xC0) != 0x80) throw new BcrdtFormatException("Malformed UTF-8 continuation byte.");
        return b & 0x3F;
    }
}
