using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bit.BlazorUI;

/// <summary>
/// Borrows bounded blocks for synchronous raw-file scans. This avoids dispatching
/// through the source interface for every byte. The memory source returns views,
/// so filling a block allocates no byte buffer. A complete memory source uses
/// its existing array directly, preserving the buffered scan's fast path.
/// </summary>
internal sealed class BitPdfByteSourceReader
{
    private const int BlockSize = 64 * 1024;
    private readonly IBitPdfByteSource _source;
    private readonly byte[]? _memoryBytes;
    private readonly int _memoryOffset;
    private ReadOnlyMemory<byte> _block;
    private int _blockStart;

    public BitPdfByteSourceReader(IBitPdfByteSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        Length = source.Length;
        if (source.TryGetContiguous(out var memory))
        {
            if (memory.Length != Length)
            {
                throw new ArgumentException("The contiguous buffer must cover the complete source.", nameof(source));
            }
            if (MemoryMarshal.TryGetArray(memory, out var buffer))
            {
                _memoryBytes = buffer.Array;
                _memoryOffset = buffer.Offset;
            }
        }
    }

    public int Length { get; }

    internal IBitPdfByteSource Source => _source;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadByte(int offset)
    {
        if (_memoryBytes is { } bytes)
        {
            return (uint)offset < (uint)Length ? bytes[_memoryOffset + offset] : ReadNextBlock(offset);
        }
        int index = offset - _blockStart;
        return (uint)index < (uint)_block.Length ? _block.Span[index] : ReadNextBlock(offset);
    }

    /// <summary>Finds a raw keyword, including matches across block boundaries.</summary>
    public int IndexOf(ReadOnlySpan<byte> keyword, int start)
    {
        if ((uint)start > (uint)Length)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }
        if (keyword.Length > BlockSize)
        {
            throw new ArgumentOutOfRangeException(nameof(keyword));
        }
        if (keyword.IsEmpty)
        {
            return start;
        }
        if (keyword.Length > Length - start)
        {
            return -1;
        }
        if (_memoryBytes is { } bytes)
        {
            int index = bytes.AsSpan(_memoryOffset + start, Length - start).IndexOf(keyword);
            return index < 0 ? -1 : start + index;
        }

        int lastStart = Length - keyword.Length;
        while (start <= lastStart)
        {
            int count = Math.Min(BlockSize, lastStart - start + 1);
            // Overlap enough bytes to find a keyword crossing this block's end.
            var block = _source.GetMemory(start, count + (keyword.Length - 1)).Span;
            int index = block.IndexOf(keyword);
            if (index >= 0)
            {
                return start + index;
            }
            start += count;
        }
        return -1;
    }

    private int ReadNextBlock(int offset)
    {
        if ((uint)offset > (uint)Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        if (offset == Length)
        {
            return -1;
        }

        int blockStart = offset / BlockSize * BlockSize;
        var block = _source.GetMemory(blockStart, Math.Min(BlockSize, Length - blockStart));
        _blockStart = blockStart;
        _block = block;
        return _block.Span[offset - _blockStart];
    }
}
