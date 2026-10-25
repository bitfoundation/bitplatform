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
/// An EAN or UPC add-on is part of the value: two issues of a magazine share the main symbol and
/// differ only in the add-on, and each is reported. A reading that missed the add-on is a repeat of
/// any reading of the same main symbol, while a reading that has the add-on an earlier one missed
/// is reported, so that the add-on reaches the application at all; set
/// <see cref="BbScannerOptions.RequireEanExtension"/> where each item must be reported exactly once
/// and with its add-on.
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

    /// <summary>
    /// How many values a filter that never forgets remembers: enough for a stocktake, few enough
    /// that a scanner left running all day cannot grow without bound.
    /// </summary>
    public const int SessionCapacity = 4096;

    private readonly List<Entry> _recent = [];
    private readonly HashSet<Key> _session = [];
    private readonly Queue<Key> _sessionOrder = new();

    /// <summary>
    /// How many of the remembered values each main symbol is part of, so that a reading that missed its add-on is
    /// known for a repeat of any of them.
    /// </summary>
    private readonly Dictionary<(string Text, BbBarcodeFormat Format), int> _sessionSymbols = [];

    /// <summary>Creates a filter.</summary>
    /// <param name="window">
    /// How long an identical value stays suppressed. Zero disables suppression;
    /// <see cref="Timeout.InfiniteTimeSpan"/> reports every value once until <see cref="Reset"/>.
    /// </param>
    public BbDuplicateFilter(TimeSpan window)
    {
        Window = window;
    }

    /// <summary>
    /// How long an identical value stays suppressed. <see cref="Timeout.InfiniteTimeSpan"/>
    /// suppresses it until <see cref="Reset"/>, remembering up to <see cref="SessionCapacity"/>
    /// values, the oldest forgotten first.
    /// </summary>
    public TimeSpan Window { get; set; }

    /// <summary>
    /// Returns whether a result should be reported, and records it when it should.
    /// </summary>
    /// <param name="text">The decoded value.</param>
    /// <param name="format">The symbology.</param>
    /// <param name="timestamp">A monotonic timestamp, normally <see cref="Environment.TickCount64"/>.</param>
    public bool ShouldReport(string text, BbBarcodeFormat format, long timestamp)
        => ShouldReport(text, format, extension: null, timestamp);

    /// <summary>
    /// Returns whether a result should be reported, and records it when it should.
    /// </summary>
    /// <param name="result">The decoded result; its EAN or UPC add-on, if any, is part of its value.</param>
    /// <param name="timestamp">A monotonic timestamp, normally <see cref="Environment.TickCount64"/>.</param>
    public bool ShouldReport(BbBarcodeResult result, long timestamp)
    {
        ArgumentNullException.ThrowIfNull(result);
        return ShouldReport(result.Text, result.Format, result.Metadata?.Extension, timestamp);
    }

    /// <summary>
    /// Returns whether a result should be reported, and records it when it should.
    /// </summary>
    /// <param name="text">The decoded value.</param>
    /// <param name="format">The symbology.</param>
    /// <param name="extension">
    /// The EAN or UPC add-on read beside the symbol, or <see langword="null"/> when none was: such a reading is a
    /// repeat of any reading of the same symbol, with an add-on or without.
    /// </param>
    /// <param name="timestamp">A monotonic timestamp, normally <see cref="Environment.TickCount64"/>.</param>
    public bool ShouldReport(string text, BbBarcodeFormat format, string? extension, long timestamp)
    {
        if (Window == Timeout.InfiniteTimeSpan)
        {
            // Once per session: a stocktake, a check-in desk, a sweep along a shelf.
            var key = new Key(text, format, extension);
            if ((extension is null && _sessionSymbols.ContainsKey((text, format))) || !_session.Add(key))
            {
                return false;
            }

            _sessionOrder.Enqueue(key);
            _sessionSymbols[(text, format)] = _sessionSymbols.GetValueOrDefault((text, format)) + 1;
            if (_sessionOrder.Count > SessionCapacity)
            {
                var oldest = _sessionOrder.Dequeue();
                _session.Remove(oldest);
                var symbol = (oldest.Text, oldest.Format);
                if (--_sessionSymbols[symbol] == 0)
                {
                    _sessionSymbols.Remove(symbol);
                }
            }

            return true;
        }

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

            if (entry.Format == format && string.Equals(entry.Text, text, StringComparison.Ordinal) &&
                (extension is null || string.Equals(entry.Extension, extension, StringComparison.Ordinal)))
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

        _recent.Add(new Entry(text, format, extension, timestamp));
        return true;
    }

    /// <summary>Forgets every reported value, so the next scan of any of them reports immediately.</summary>
    public void Reset()
    {
        _recent.Clear();
        _session.Clear();
        _sessionOrder.Clear();
        _sessionSymbols.Clear();
    }

    private readonly record struct Key(string Text, BbBarcodeFormat Format, string? Extension);

    private readonly record struct Entry(string Text, BbBarcodeFormat Format, string? Extension, long Timestamp);
}
