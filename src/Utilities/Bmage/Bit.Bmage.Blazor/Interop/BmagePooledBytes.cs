using System.Buffers;

namespace Bit.Bmage;

/// <summary>A rented byte buffer exposing exactly the bytes that were read.</summary>
public readonly struct BmagePooledBytes : IDisposable
{
    private readonly IMemoryOwner<byte>? _owner;

    public BmagePooledBytes(IMemoryOwner<byte> owner, int length)
    {
        _owner = owner;
        Length = length;
    }

    /// <summary>Number of valid bytes.</summary>
    public int Length { get; }

    /// <summary>The valid bytes.</summary>
    public ReadOnlyMemory<byte> Memory => _owner is null ? ReadOnlyMemory<byte>.Empty : _owner.Memory[..Length];

    /// <summary>The valid bytes as a span.</summary>
    public ReadOnlySpan<byte> Span => Memory.Span;

    /// <summary>Copies the bytes into a new array. Prefer <see cref="Span"/> where a copy is not needed.</summary>
    public byte[] ToArray() => Span.ToArray();

    public void Dispose() => _owner?.Dispose();
}
