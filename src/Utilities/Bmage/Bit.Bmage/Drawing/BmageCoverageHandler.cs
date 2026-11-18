namespace Bit.Bmage;

/// <summary>
/// Receives the anti-aliased coverage for one row: values in [0,1] for pixels starting at <paramref name="x0"/>.
/// </summary>
/// <param name="y">The row being filled.</param>
/// <param name="x0">The x coordinate of the first value in <paramref name="coverage"/>.</param>
/// <param name="coverage">Per-pixel coverage. Valid only for the duration of the call.</param>
public delegate void BmageCoverageHandler(int y, int x0, ReadOnlySpan<float> coverage);
