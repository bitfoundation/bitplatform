using System.Buffers;

namespace Bit.Bmage;

/// <summary>
/// Supplies backing memory for <see cref="BmageImageBuffer"/> instances. Implementations may pool memory.
/// </summary>
public interface IBmagePixelAllocator
{
    /// <summary>Rents a memory owner with at least <paramref name="byteCount"/> bytes.</summary>
    IMemoryOwner<byte> Rent(int byteCount);
}
