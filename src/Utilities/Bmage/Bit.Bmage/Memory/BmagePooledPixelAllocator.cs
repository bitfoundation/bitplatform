using System.Buffers;

namespace Bit.Bmage;

/// <summary>
/// Default allocator backed by <see cref="ArrayPool{T}.Shared"/> for buffers up to the pool's maximum
/// size, with plain allocation as a fallback. Returned buffers are not cleared by the pool; callers clear
/// them when required.
/// </summary>
public sealed class BmagePooledPixelAllocator : IBmagePixelAllocator
{
    /// <summary>Shared allocator instance.</summary>
    public static BmagePooledPixelAllocator Shared { get; } = new();

    private readonly ArrayPool<byte> _pool;

    public BmagePooledPixelAllocator() : this(ArrayPool<byte>.Shared) { }

    public BmagePooledPixelAllocator(ArrayPool<byte> pool) => _pool = pool ?? throw new ArgumentNullException(nameof(pool));

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
