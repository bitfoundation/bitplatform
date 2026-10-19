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

    public bool TryGetContiguous(out ReadOnlyMemory<byte> memory)
    {
        memory = _bytes;
        return true;
    }

    public BitPdfByteSourceWindow GetMemory(int offset, int length)
    {
        ValidateRange(offset, length);
        return new(_bytes.AsMemory(offset, length));
    }

    public BitPdfBaseStream CreateStream(int start = 0, int? length = null)
    {
        ValidateOffset(start);
        int count = length ?? Length - start;
        ValidateRange(start, count);
        return new BitPdfStream(_bytes, start, count);
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
