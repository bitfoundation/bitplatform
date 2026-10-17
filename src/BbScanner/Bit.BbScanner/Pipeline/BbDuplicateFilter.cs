namespace Bit.BbScanner;

/// <summary>
/// Suppresses repeated readings of the same symbol.
/// </summary>
/// <remarks>
/// A camera pointed at a barcode decodes it on every frame, so without suppression a caller sees
/// twenty identical results a second. The filter is keyed on the value and the symbology, so
/// moving to a different barcode reports immediately rather than waiting out the window.
/// </remarks>
public sealed class BbDuplicateFilter
{
    private string? _lastText;
    private BbBarcodeFormat _lastFormat;
    private long _lastTimestamp;

    /// <summary>Creates a filter.</summary>
    /// <param name="window">How long an identical value stays suppressed. Zero disables suppression.</param>
    public BbDuplicateFilter(TimeSpan window)
    {
        Window = window;
    }

    /// <summary>How long an identical value stays suppressed.</summary>
    public TimeSpan Window { get; set; }

    /// <summary>
    /// Returns whether a result should be reported, and records it when it should.
    /// </summary>
    /// <param name="text">The decoded value.</param>
    /// <param name="format">The symbology.</param>
    /// <param name="timestamp">A monotonic timestamp, normally <see cref="Environment.TickCount64"/>.</param>
    public bool ShouldReport(string text, BbBarcodeFormat format, long timestamp)
    {
        if (Window <= TimeSpan.Zero)
        {
            return true;
        }

        if (_lastText is not null &&
            _lastFormat == format &&
            string.Equals(_lastText, text, StringComparison.Ordinal) &&
            timestamp - _lastTimestamp < (long)Window.TotalMilliseconds)
        {
            // Refresh the timestamp so that a symbol held in front of the camera stays
            // suppressed rather than re-firing once the window elapses.
            _lastTimestamp = timestamp;
            return false;
        }

        _lastText = text;
        _lastFormat = format;
        _lastTimestamp = timestamp;
        return true;
    }

    /// <summary>Forgets the last reported value, so the next scan of it reports immediately.</summary>
    public void Reset()
    {
        _lastText = null;
        _lastFormat = BbBarcodeFormat.None;
        _lastTimestamp = 0;
    }
}
