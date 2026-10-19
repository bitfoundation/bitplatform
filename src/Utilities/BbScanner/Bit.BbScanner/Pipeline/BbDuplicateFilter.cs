namespace Bit.BbScanner;

/// <summary>
/// Suppresses repeated readings of the same symbol.
/// </summary>
/// <remarks>
/// <para>
/// A camera pointed at a barcode decodes it on every frame, so without suppression a caller sees
/// twenty identical results a second. The filter is keyed on the value and the symbology, so
/// moving to a different barcode reports immediately rather than waiting out the window.
/// </para>
/// <para>
/// Every symbol seen within the window is remembered, not just the last one. A label carrying two
/// codes, or two codes in view at once, decodes one or the other depending on the noise in each
/// frame; remembering only the last would treat every switch between them as a new symbol, and
/// report both at the frame rate.
/// </para>
/// </remarks>
public sealed class BbDuplicateFilter
{
    /// <summary>
    /// More distinct symbols than this in view at once is a wall of labels, not a scan; past it
    /// the one seen longest ago is forgotten, which keeps a sweep along a shelf from growing the
    /// list without bound.
    /// </summary>
    private const int Capacity = 16;

    private readonly List<Entry> _recent = [];

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

        var window = (long)Window.TotalMilliseconds;
        for (var i = _recent.Count - 1; i >= 0; i--)
        {
            var entry = _recent[i];
            if (timestamp - entry.Timestamp >= window)
            {
                // Out of view for longer than the window: it reports again when it comes back.
                _recent.RemoveAt(i);
                continue;
            }

            if (entry.Format == format && string.Equals(entry.Text, text, StringComparison.Ordinal))
            {
                // Refresh the timestamp so that a symbol held in front of the camera stays
                // suppressed rather than re-firing once the window elapses.
                _recent[i] = entry with { Timestamp = timestamp };
                return false;
            }
        }

        if (_recent.Count >= Capacity)
        {
            var oldest = 0;
            for (var i = 1; i < _recent.Count; i++)
            {
                if (_recent[i].Timestamp < _recent[oldest].Timestamp)
                {
                    oldest = i;
                }
            }

            _recent.RemoveAt(oldest);
        }

        _recent.Add(new Entry(text, format, timestamp));
        return true;
    }

    /// <summary>Forgets every reported value, so the next scan of any of them reports immediately.</summary>
    public void Reset() => _recent.Clear();

    private readonly record struct Entry(string Text, BbBarcodeFormat Format, long Timestamp);
}
