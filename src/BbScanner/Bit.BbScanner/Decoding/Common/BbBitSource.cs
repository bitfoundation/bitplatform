namespace Bit.BbScanner;

/// <summary>
/// Reads a big-endian bit stream out of a byte array.
/// </summary>
/// <remarks>
/// Declared as a <see langword="ref struct"/> so that the bit stream parsers of the matrix
/// symbologies operate entirely on the stack, with no allocation and no indirection through a
/// heap object on the per-symbol path.
/// </remarks>
public ref struct BbBitSource
{
    private readonly ReadOnlySpan<byte> _bytes;
    private int _byteOffset;
    private int _bitOffset;

    /// <summary>Creates a reader over <paramref name="bytes"/>.</summary>
    /// <param name="bytes">The codeword bytes, read most significant bit first.</param>
    public BbBitSource(ReadOnlySpan<byte> bytes)
    {
        _bytes = bytes;
        _byteOffset = 0;
        _bitOffset = 0;
    }

    /// <summary>Number of bits that have not been consumed yet.</summary>
    public readonly int Available => (8 * (_bytes.Length - _byteOffset)) - _bitOffset;

    /// <summary>Index of the byte the next bit will come from.</summary>
    public readonly int ByteOffset => _byteOffset;

    /// <summary>Number of bits already consumed from the current byte, 0 to 7.</summary>
    public readonly int BitOffset => _bitOffset;

    /// <summary>Reads <paramref name="count"/> bits, most significant first.</summary>
    /// <param name="count">Number of bits to read, 1 to 32.</param>
    public int ReadBits(int count)
    {
        if (count is < 1 or > 32 || count > Available)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Not enough bits remain in the stream.");
        }

        var result = 0;

        if (_bitOffset > 0)
        {
            var bitsLeftInByte = 8 - _bitOffset;
            var toRead = Math.Min(count, bitsLeftInByte);
            var bitsToNotRead = bitsLeftInByte - toRead;
            var mask = (0xFF >> (8 - toRead)) << bitsToNotRead;
            result = (_bytes[_byteOffset] & mask) >> bitsToNotRead;
            count -= toRead;
            _bitOffset += toRead;
            if (_bitOffset == 8)
            {
                _bitOffset = 0;
                _byteOffset++;
            }
        }

        if (count > 0)
        {
            while (count >= 8)
            {
                result = (result << 8) | _bytes[_byteOffset];
                _byteOffset++;
                count -= 8;
            }

            if (count > 0)
            {
                var bitsToNotRead = 8 - count;
                var mask = (0xFF >> bitsToNotRead) << bitsToNotRead;
                result = (result << count) | ((_bytes[_byteOffset] & mask) >> bitsToNotRead);
                _bitOffset += count;
            }
        }

        return result;
    }

    /// <summary>Attempts to read <paramref name="count"/> bits, returning <see langword="false"/> when the stream is exhausted.</summary>
    /// <param name="count">Number of bits to read.</param>
    /// <param name="value">The bits that were read.</param>
    public bool TryReadBits(int count, out int value)
    {
        if (count is < 1 or > 32 || count > Available)
        {
            value = 0;
            return false;
        }

        value = ReadBits(count);
        return true;
    }
}
