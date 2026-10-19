namespace Bit.Bcrdt.Testing;

/// <summary>
/// Produces malformed updates, to prove that decoding hostile input fails safely.
/// </summary>
/// <remarks>
/// <para>
/// Updates arrive from other people's machines. A peer can be buggy, a proxy can mangle a payload,
/// and somebody can be deliberately hostile. The requirement in every case is the same: the decoder
/// rejects the payload with <see cref="BcrdtFormatException"/>, and the receiving document
/// is left exactly as it was - no partial mutation, no crash, no unbounded allocation, no hang.
/// </para>
/// <para>
/// Every corruption here is derived from a valid update, which is what makes it a useful test.
/// Random bytes are almost always rejected at the first tag; a payload that is <i>nearly</i> valid
/// is what finds the missing bounds check.
/// </para>
/// </remarks>
public static class BcrdtUpdateCorrupter
{
    /// <summary>Every truncation of an update, from one byte to one short of complete.</summary>
    public static IEnumerable<byte[]> Truncations(byte[] update)
    {
        ArgumentNullException.ThrowIfNull(update);
        for (int length = 1; length < update.Length; length++)
        {
            yield return update[..length];
        }
    }

    /// <summary>Copies of an update with a single byte altered, at every position.</summary>
    public static IEnumerable<byte[]> SingleByteMutations(byte[] update, int seed = 1)
    {
        ArgumentNullException.ThrowIfNull(update);

        var random = new Random(seed);
        for (int index = 0; index < update.Length; index++)
        {
            byte[] mutated = (byte[])update.Clone();
            byte replacement;
            do
            {
                replacement = (byte)random.Next(256);
            }
            while (replacement == mutated[index]);

            mutated[index] = replacement;
            yield return mutated;
        }
    }

    /// <summary>Copies of an update with extra bytes spliced in.</summary>
    public static IEnumerable<byte[]> Insertions(byte[] update, int seed = 1, int count = 32)
    {
        ArgumentNullException.ThrowIfNull(update);

        var random = new Random(seed);
        for (int i = 0; i < count; i++)
        {
            int at = random.Next(update.Length + 1);
            byte[] mutated = new byte[update.Length + 1];
            update.AsSpan(0, at).CopyTo(mutated);
            mutated[at] = (byte)random.Next(256);
            update.AsSpan(at).CopyTo(mutated.AsSpan(at + 1));
            yield return mutated;
        }
    }

    /// <summary>Payloads that are not updates at all.</summary>
    /// <remarks>
    /// Includes the specific shapes that break naïve decoders: an empty payload, a lone continuation
    /// byte, and a varint claiming an enormous collection length. The last is the one that matters
    /// most - a decoder that sizes a list from a declared count before checking it can be made to
    /// exhaust memory from five bytes of input.
    /// </remarks>
    public static IEnumerable<byte[]> Garbage(int seed = 1)
    {
        yield return [];
        yield return [0xFF];
        yield return [0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80];

        // "This update contains 2^35 client groups."
        yield return [0xFF, 0xFF, 0xFF, 0xFF, 0x7F, 0x00];

        // A single group claiming an absurd number of blocks.
        yield return [0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F, 0x01, 0x00];

        var random = new Random(seed);
        for (int length = 1; length <= 64; length *= 2)
        {
            byte[] noise = new byte[length];
            random.NextBytes(noise);
            yield return noise;
        }
    }
}
