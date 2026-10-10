using System;

namespace Bit.BlazorUI;

/// <summary>
/// Borrows the existing complete PDF buffer. It neither copies nor owns it;
/// views and cursors remain valid while that buffer is retained. Callers must
/// keep the bytes stable while parsing, just as with BitPdfDocument.Load(byte[]).
/// </summary>
internal sealed class BitPdfMemoryByteSource : IBitPdfByteSource
{
    private readonly byte[] _bytes;

    public BitPdfMemoryByteSource(byte[] bytes)
    {
        _bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
    }

    public int Length => _bytes.Length;

    public int ReadByte(int offset)
    {
        ValidateOffset(offset);
        return offset == Length ? -1 : _bytes[offset];
    }

    public int Read(int offset, Span<byte> destination)
    {
        ValidateOffset(offset);
        int count = Math.Min(destination.Length, Length - offset);
        _bytes.AsSpan(offset, count).CopyTo(destination);
        return count;
    }

    public ReadOnlyMemory<byte> GetMemory(int offset, int length)
    {
        ValidateRange(offset, length);
        return _bytes.AsMemory(offset, length);
    }

    public BitPdfBaseStream CreateStream(int start = 0, int? length = null, BitPdfDict? dict = null)
    {
        ValidateOffset(start);
        int count = length ?? Length - start;
        ValidateRange(start, count);
        return new BitPdfStream(_bytes, start, count, dict);
    }

    private void ValidateOffset(int offset)
    {
        if ((uint)offset > (uint)Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
    }

    private void ValidateRange(int offset, int length)
    {
        ValidateOffset(offset);
        // Subtract after validating the offset: offset + length could overflow.
        if (length < 0 || length > Length - offset)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }
    }
}
