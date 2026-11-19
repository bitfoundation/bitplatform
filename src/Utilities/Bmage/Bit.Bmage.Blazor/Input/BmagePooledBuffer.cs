using System.Buffers;

namespace Bit.Bmage;

/// <summary>A pooled byte buffer holding encoded image data. Dispose to return it to the pool.</summary>
public sealed class BmagePooledBuffer : IDisposable
{
    private byte[]? _array;

    private BmagePooledBuffer(byte[] array, int length)
    {
        _array = array;
        Length = length;
    }

    /// <summary>Number of valid bytes.</summary>
    public int Length { get; }

    /// <summary>The valid bytes.</summary>
    public ReadOnlyMemory<byte> Memory => _array is null ? ReadOnlyMemory<byte>.Empty : _array.AsMemory(0, Length);

    /// <summary>The valid bytes as a span.</summary>
    public ReadOnlySpan<byte> Span => _array is null ? ReadOnlySpan<byte>.Empty : _array.AsSpan(0, Length);

    /// <summary>Copies existing bytes into a pooled buffer.</summary>
    public static BmagePooledBuffer CopyOf(ReadOnlySpan<byte> bytes)
    {
        var array = ArrayPool<byte>.Shared.Rent(Math.Max(1, bytes.Length));
        bytes.CopyTo(array);
        return new BmagePooledBuffer(array, bytes.Length);
    }

    /// <summary>Reads a stream fully into a pooled buffer, refusing to exceed <paramref name="maxBytes"/>.</summary>
    public static async ValueTask<BmagePooledBuffer> ReadAllAsync(Stream stream, int sizeHint, long maxBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var capacity = Math.Max(4096, sizeHint > 0 ? sizeHint : 64 * 1024);
        var array = ArrayPool<byte>.Shared.Rent(capacity);
        var length = 0;
        try
        {
            while (true)
            {
                if (length == array.Length)
                {
                    if (array.Length >= maxBytes)
                        throw new BmageImageLimitExceededException($"The image exceeds the {maxBytes} byte limit.");
                    var bigger = ArrayPool<byte>.Shared.Rent(Math.Min((long)array.Length * 2, maxBytes + 1) is var n && n > int.MaxValue ? int.MaxValue : (int)n);
                    array.AsSpan(0, length).CopyTo(bigger);
                    ArrayPool<byte>.Shared.Return(array);
                    array = bigger;
                }
                var read = await stream.ReadAsync(array.AsMemory(length), cancellationToken).ConfigureAwait(false);
                if (read == 0) break;
                length += read;
                if (length > maxBytes)
                    throw new BmageImageLimitExceededException($"The image exceeds the {maxBytes} byte limit.");
            }
            return new BmagePooledBuffer(array, length);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(array);
            throw;
        }
    }

    public void Dispose()
    {
        var array = Interlocked.Exchange(ref _array, null);
        if (array is not null) ArrayPool<byte>.Shared.Return(array);
    }
}
