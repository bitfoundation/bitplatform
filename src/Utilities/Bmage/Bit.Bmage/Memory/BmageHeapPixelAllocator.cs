using System.Buffers;

namespace Bit.Bmage;

/// <summary>Allocator that always allocates fresh, zeroed arrays. Useful for deterministic tests.</summary>
public sealed class BmageHeapPixelAllocator : IBmagePixelAllocator
{
    public static BmageHeapPixelAllocator Instance { get; } = new();

    public IMemoryOwner<byte> Rent(int byteCount) => new HeapOwner(byteCount);

    private sealed class HeapOwner : IMemoryOwner<byte>
    {
        private byte[]? _array;
        public HeapOwner(int length) => _array = new byte[length];
        public Memory<byte> Memory => _array ?? throw new ObjectDisposedException(nameof(HeapOwner));
        public void Dispose() => _array = null;
    }
}
