namespace Bit.BbScanner;

/// <summary>
/// Turns a grayscale image into black and white.
/// </summary>
/// <remarks>
/// Binarisation is the stage where most real world scans succeed or fail, so the strategies are
/// separate, interchangeable types that a pipeline of an application's own can choose between;
/// <see cref="BbBarcodeDecoder"/> uses its own fixed pair. Implementations must be safe to reuse
/// across frames and should avoid allocating per frame, since a pipeline calls them on every
/// captured frame.
/// </remarks>
public interface IBbBinarizer
{
    /// <summary>Binarises the whole view into a new matrix.</summary>
    /// <param name="source">Grayscale source.</param>
    /// <returns>A matrix the caller owns and must dispose, or <see langword="null"/> when the image cannot be binarised.</returns>
    BbBitMatrix? GetBlackMatrix(in BbLuminanceView source);

    /// <summary>Binarises a single row.</summary>
    /// <param name="source">Grayscale source.</param>
    /// <param name="y">Row index inside the view.</param>
    /// <param name="row">Row to fill; it is reset by the callee.</param>
    /// <returns><see langword="false"/> when the row carries too little contrast to be worth decoding.</returns>
    bool TryGetBlackRow(in BbLuminanceView source, int y, BbBitRow row);
}
