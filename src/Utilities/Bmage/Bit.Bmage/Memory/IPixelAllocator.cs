using System.Buffers;

namespace Bit.Bmage.Memory;

/// <summary>
/// Supplies backing memory for <see cref="ImageBuffer"/> instances. Implementations may pool memory.
/// </summary>
public interface IPixelAllocator
{
    /// <summary>Rents a memory owner with at least <paramref name="byteCount"/> bytes.</summary>
    IMemoryOwner<byte> Rent(int byteCount);
}

/// <summary>
/// Default allocator backed by <see cref="ArrayPool{T}.Shared"/> for buffers up to the pool's maximum
/// size, with plain allocation as a fallback. Returned buffers are not cleared by the pool; callers clear
/// them when required.
/// </summary>
public sealed class PooledPixelAllocator : IPixelAllocator
{
    /// <summary>Shared allocator instance.</summary>
    public static PooledPixelAllocator Shared { get; } = new();

    private readonly ArrayPool<byte> _pool;

    public PooledPixelAllocator() : this(ArrayPool<byte>.Shared) { }

    public PooledPixelAllocator(ArrayPool<byte> pool) => _pool = pool ?? throw new ArgumentNullException(nameof(pool));

    public IMemoryOwner<byte> Rent(int byteCount)
    {
        if (byteCount < 0) throw new ArgumentOutOfRangeException(nameof(byteCount));
        return new PooledOwner(_pool, byteCount);
    }

    private sealed class PooledOwner : IMemoryOwner<byte>
    {
        private ArrayPool<byte>? _pool;
        private byte[]? _array;
        private readonly int _length;

        public PooledOwner(ArrayPool<byte> pool, int length)
        {
            _pool = pool;
            _array = pool.Rent(length);
            _length = length;
        }

        public Memory<byte> Memory
        {
            get
            {
                var a = _array ?? throw new ObjectDisposedException(nameof(PooledOwner));
                return new Memory<byte>(a, 0, _length);
            }
        }

        public void Dispose()
        {
            var a = Interlocked.Exchange(ref _array, null);
            if (a is not null)
            {
                _pool?.Return(a);
                _pool = null;
            }
        }
    }
}

/// <summary>Allocator that always allocates fresh, zeroed arrays. Useful for deterministic tests.</summary>
public sealed class HeapPixelAllocator : IPixelAllocator
{
    public static HeapPixelAllocator Instance { get; } = new();

    public IMemoryOwner<byte> Rent(int byteCount) => new HeapOwner(byteCount);

    private sealed class HeapOwner : IMemoryOwner<byte>
    {
        private byte[]? _array;
        public HeapOwner(int length) => _array = new byte[length];
        public Memory<byte> Memory => _array ?? throw new ObjectDisposedException(nameof(HeapOwner));
        public void Dispose() => _array = null;
    }
}
