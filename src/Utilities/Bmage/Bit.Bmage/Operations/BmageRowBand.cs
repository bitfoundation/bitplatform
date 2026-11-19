namespace Bit.Bmage;

/// <summary>Processes rows <paramref name="start"/> (inclusive) to <paramref name="end"/> (exclusive) of an image.</summary>
/// <param name="start">The first row of the band.</param>
/// <param name="end">The row after the last row of the band.</param>
/// <param name="context">
/// The context to check cancellation on and report progress through, as a fraction of the whole image. Inside a band
/// that runs beside others it reports nothing, since the bands finish in no particular order; the bands' completion is
/// reported instead, always on the thread that called the operation.
/// </param>
internal delegate void BmageRowBand(int start, int end, BmageOperationContext context);
