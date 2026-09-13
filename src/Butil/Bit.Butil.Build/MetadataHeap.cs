using System;
using System.IO;
using System.Text;

namespace Bit.Butil.Build;

/// <summary>The file offset and size of one metadata stream. A stream the image does not have is empty.</summary>
public readonly struct MetadataHeap(int offset, int size)
{
    public int Offset { get; } = offset;

    public int Size { get; } = size;

    public bool IsEmpty => Size == 0;
}
