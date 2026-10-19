using System.Runtime.CompilerServices;

namespace Bit.BbScanner;

/// <summary>
/// The eight QR Code data masks.
/// </summary>
/// <remarks>
/// The mask is applied while reading rather than by XORing the whole matrix and undoing it
/// afterwards. That avoids two full passes over the module grid and, more importantly, keeps
/// the detector output immutable so that a failed decode attempt can be retried with different
/// parameters without having to restore state.
/// </remarks>
public static class BbQrDataMask
{
    /// <summary>Returns whether the mask flips the module at the given coordinate.</summary>
    /// <param name="maskPattern">Mask pattern reference, 0 to 7.</param>
    /// <param name="row">Module row.</param>
    /// <param name="column">Module column.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMasked(int maskPattern, int row, int column) => maskPattern switch
    {
        0 => ((row + column) & 0x01) == 0,
        1 => (row & 0x01) == 0,
        2 => column % 3 == 0,
        3 => (row + column) % 3 == 0,
        4 => (((row / 2) + (column / 3)) & 0x01) == 0,
        5 => ((row * column) & 0x01) + ((row * column) % 3) == 0,
        6 => ((((row * column) & 0x01) + ((row * column) % 3)) & 0x01) == 0,
        7 => (((((row + column) & 0x01) + ((row * column) % 3)) & 0x01)) == 0,
        _ => throw new ArgumentOutOfRangeException(nameof(maskPattern), "QR Code defines eight data masks."),
    };
}
