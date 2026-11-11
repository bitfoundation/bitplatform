using System.Text;

namespace Bit.BbScanner;

/// <summary>
/// Puts the symbols of a structured append sequence - a QR Code, Data Matrix or Macro PDF417 message split over
/// several symbols - back together, whatever order they are scanned in.
/// </summary>
/// <remarks>
/// <para>
/// Hand it every result a scanner reports, from <c>OnScan</c> or from <see cref="BbBarcodeDecoder"/>; a result that
/// is not part of a sequence is answered with <see cref="BbStructuredAppendStatus.NotStructuredAppend"/> and left
/// for the application to handle as it is. A sequence is told apart from every other by its symbology, its count and
/// what its symbols share: QR Code's parity byte, Data Matrix's file identification, the Macro PDF417 file ID. Its
/// message is returned once each of its symbols has been read; a symbol read again is ignored - after its message
/// has been returned too, until <see cref="Timeout"/> passes without it - and two different
/// symbols claiming the same place in one sequence end that sequence rather than being combined. QR Code's parity -
/// the exclusive or of every byte of the message - is checked before its message is returned.
/// </para>
/// <para>
/// Memory is bounded: at most <see cref="MaxPendingSequences"/> sequences are held, each of at most
/// <see cref="MaxSegments"/> symbols, and one that has had no new symbol for <see cref="Timeout"/> is dropped. Aztec
/// and MaxiCode structured append are not interpreted by the decoders, so their symbols are reported one by one.
/// The assembler is safe to use from several threads.
/// </para>
/// </remarks>
public sealed class BbStructuredAppendAssembler
{
    private readonly Lock _gate = new();
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, Sequence> _sequences = new(StringComparer.Ordinal);

    // Sequences already returned, kept for Timeout after their last symbol was seen: a camera keeps reporting the
    // symbols still in view, and those must not start the same message over and return it again.
    private readonly Dictionary<string, Sequence> _completed = new(StringComparer.Ordinal);

    /// <summary>Creates an assembler.</summary>
    /// <param name="timeProvider">The clock <see cref="Timeout"/> is measured on; the system clock unless given.</param>
    public BbStructuredAppendAssembler(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// How long a sequence waits for its next symbol before it is dropped; a minute unless set.
    /// <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> keeps it until it completes or is pushed out by newer ones.
    /// </summary>
    public TimeSpan Timeout
    {
        get;
        set
        {
            if (value != System.Threading.Timeout.InfiniteTimeSpan)
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
            }

            field = value;
        }
    } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How many incomplete sequences are held at once, eight unless set; a new one beyond that pushes out the one
    /// that has waited longest for a symbol.
    /// </summary>
    public int MaxPendingSequences
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            field = value;
        }
    } = 8;

    /// <summary>
    /// The most symbols one sequence may have, 64 unless set. QR Code and Data Matrix sequences hold at most sixteen;
    /// a Macro PDF417 file may number its segments up to 99 999, and a segment past this limit is rejected.
    /// </summary>
    public int MaxSegments
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 2);
            field = value;
        }
    } = 64;

    /// <summary>The sequences still waiting for symbols, the one most recently added to first.</summary>
    public IReadOnlyList<BbStructuredAppendProgress> Pending
    {
        get
        {
            lock (_gate)
            {
                Expire(_sequences, _timeProvider.GetUtcNow());
                return [.. _sequences.Values.OrderByDescending(s => s.Updated).Select(s => s.Progress())];
            }
        }
    }

    /// <summary>Adds a scanned symbol.</summary>
    /// <param name="result">A result from a scanner or a decoder.</param>
    /// <param name="message">The whole message, when this symbol completed its sequence; otherwise <see langword="null"/>.</param>
    /// <returns>What became of the symbol.</returns>
    public BbStructuredAppendStatus Add(BbBarcodeResult result, out BbStructuredAppendMessage? message)
    {
        ArgumentNullException.ThrowIfNull(result);
        message = null;

        if (result.Metadata?.StructuredAppendIndex is null)
        {
            return BbStructuredAppendStatus.NotStructuredAppend;
        }

        // A header whose numbers cannot be right is still a header: the symbol holds part of a message, never all of
        // it, so it is rejected rather than handed back as a message of its own.
        if (!TryDescribe(result, out var key, out var position, out var count, out var isLast) ||
            position >= MaxSegments || count > MaxSegments)
        {
            return BbStructuredAppendStatus.Rejected;
        }

        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            Expire(_sequences, now);
            Expire(_completed, now);

            if (_completed.TryGetValue(key, out var done))
            {
                if (done.Holds(position, result))
                {
                    done.Updated = now;
                    return BbStructuredAppendStatus.Duplicate;
                }

                // A symbol the finished message does not have: another message with the same identification.
                _completed.Remove(key);
            }

            var status = BbStructuredAppendStatus.Added;
            if (_sequences.TryGetValue(key, out var sequence))
            {
                var outcome = sequence.Add(result, position, count, isLast);
                if (outcome == BbStructuredAppendStatus.Duplicate)
                {
                    sequence.Updated = now;
                    return outcome;
                }

                if (outcome == BbStructuredAppendStatus.Conflict)
                {
                    // Two different symbols for one place: either two sequences that happen to look alike, or a
                    // misread. Neither may be combined with the other, so the old one goes and this one starts anew.
                    _sequences.Remove(key);
                    sequence = null;
                    status = BbStructuredAppendStatus.Conflict;
                }
            }

            if (sequence is null)
            {
                sequence = new Sequence(result.Format);
                if (sequence.Add(result, position, count, isLast) != BbStructuredAppendStatus.Added)
                {
                    // A symbol that contradicts itself - a Macro PDF417 segment marked last that is not at the place its
                    // own count gives the last one - starts nothing.
                    return BbStructuredAppendStatus.Rejected;
                }

                Evict(_sequences);
                _sequences[key] = sequence;
            }

            sequence.Updated = now;
            if (!sequence.IsComplete)
            {
                return status;
            }

            _sequences.Remove(key);
            message = sequence.Assemble();
            if (message is null)
            {
                return BbStructuredAppendStatus.Rejected;
            }

            Evict(_completed);
            _completed[key] = sequence;
            return BbStructuredAppendStatus.Completed;
        }
    }

    /// <summary>
    /// Forgets every incomplete sequence, and the completed ones whose symbols would otherwise be answered with
    /// <see cref="BbStructuredAppendStatus.Duplicate"/> until <see cref="Timeout"/>: scanning one of those again then
    /// starts its message anew.
    /// </summary>
    public void Reset()
    {
        lock (_gate)
        {
            _sequences.Clear();
            _completed.Clear();
        }
    }

    // Where a result belongs: the sequence's key, the symbol's place counted from 0, and the count if it is known.
    private static bool TryDescribe(BbBarcodeResult result, out string key, out int position, out int? count, out bool isLast)
    {
        key = string.Empty;
        position = 0;
        count = null;
        isLast = false;

        var metadata = result.Metadata!;
        var index = metadata.StructuredAppendIndex!.Value;

        isLast = metadata.StructuredAppendIsLast == true;
        switch (result.Format)
        {
            case BbBarcodeFormat.QrCode when metadata is { StructuredAppendCount: { } total and >= 1 and <= 16, StructuredAppendParity: { } parity }:
                if (index < 0 || index >= total)
                {
                    return false;
                }

                key = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Q|{total}|{parity}");
                position = index;
                count = total;
                return true;

            case BbBarcodeFormat.DataMatrix when metadata is { StructuredAppendCount: { } total and >= 2 and <= 16, StructuredAppendParity: { } fileId }:
                if (index < 1 || index > total)
                {
                    return false;
                }

                key = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"D|{total}|{fileId}");
                position = index - 1;
                count = total;
                return true;

            case BbBarcodeFormat.Pdf417 when metadata.StructuredAppendFileId is { } file:
                if (index < 0 || metadata.StructuredAppendCount is <= 0 || (metadata.StructuredAppendCount is { } segments && index >= segments))
                {
                    return false;
                }

                key = "P|" + file;
                position = index;
                count = metadata.StructuredAppendCount;
                return true;

            default:
                return false;
        }
    }

    private void Expire(Dictionary<string, Sequence> sequences, DateTimeOffset now)
    {
        if (Timeout == System.Threading.Timeout.InfiniteTimeSpan || sequences.Count == 0)
        {
            return;
        }

        List<string>? stale = null;
        foreach (var (key, sequence) in sequences)
        {
            if (now - sequence.Updated >= Timeout)
            {
                (stale ??= []).Add(key);
            }
        }

        if (stale is not null)
        {
            foreach (var key in stale)
            {
                sequences.Remove(key);
            }
        }
    }

    // Makes room for one more: the sequence that has waited longest for a symbol goes first.
    private void Evict(Dictionary<string, Sequence> sequences)
    {
        while (sequences.Count >= MaxPendingSequences)
        {
            var oldest = sequences.MinBy(pair => pair.Value.Updated).Key;
            sequences.Remove(oldest);
        }
    }

    // Whether a symbol's text is its bytes read in the character set guessed from them - not, say, a QR Code Kanji
    // segment, which says its own character set.
    private static bool WasGuessed(BbBarcodeResult segment)
    {
        var bytes = segment.RawBytes.Span;
        return BbCharacterSetEci.GuessEncoding(bytes).GetString(bytes) == segment.Text;
    }

    private sealed class Sequence(BbBarcodeFormat format)
    {
        private readonly SortedDictionary<int, BbBarcodeResult> _segments = [];
        private int? _count;
        private int? _last;

        public DateTimeOffset Updated { get; set; }

        private int? Count => _count ?? (_last + 1);

        public bool IsComplete => Count is { } count && _segments.Count == count;

        /// <summary>Whether the symbol at <paramref name="position"/> is <paramref name="result"/>, read again.</summary>
        public bool Holds(int position, BbBarcodeResult result) =>
            _segments.TryGetValue(position, out var existing) &&
            existing.Text == result.Text &&
            existing.RawBytes.Span.SequenceEqual(result.RawBytes.Span);

        public BbStructuredAppendStatus Add(BbBarcodeResult result, int position, int? count, bool isLast)
        {
            if (_segments.ContainsKey(position))
            {
                return Holds(position, result) ? BbStructuredAppendStatus.Duplicate : BbStructuredAppendStatus.Conflict;
            }

            // What this symbol says about the sequence's length has to agree with what the others said, and with
            // where every symbol read so far sits.
            var knownCount = count ?? _count;
            var knownLast = isLast ? position : _last;
            var highest = _segments.Count > 0 ? Math.Max(position, _segments.Keys.Last()) : position;
            if ((count is not null && _count is not null && count != _count) ||
                (isLast && _last is not null && _last != position) ||
                (knownCount is not null && highest >= knownCount) ||
                (knownLast is not null && highest > knownLast) ||
                (knownCount is not null && knownLast is not null && knownLast != knownCount - 1))
            {
                return BbStructuredAppendStatus.Conflict;
            }

            _count = knownCount;
            _last = knownLast;
            _segments.Add(position, result);
            return BbStructuredAppendStatus.Added;
        }

        public BbStructuredAppendProgress Progress()
        {
            var missing = new List<int>();
            if (Count is { } count)
            {
                for (var position = 0; position < count; position++)
                {
                    if (!_segments.ContainsKey(position))
                    {
                        missing.Add(format == BbBarcodeFormat.DataMatrix ? position + 1 : position);
                    }
                }
            }

            return new BbStructuredAppendProgress(format, _segments.Count, Count, missing);
        }

        public BbStructuredAppendMessage? Assemble()
        {
            var segments = _segments.Values.ToArray();
            var length = 0;
            foreach (var segment in segments)
            {
                length += segment.RawBytes.Length;
            }

            var raw = new byte[length];
            var offset = 0;
            var parity = 0;
            foreach (var segment in segments)
            {
                segment.RawBytes.Span.CopyTo(raw.AsSpan(offset));
                offset += segment.RawBytes.Length;
                foreach (var value in segment.RawBytes.Span)
                {
                    parity ^= value;
                }
            }

            if (format == BbBarcodeFormat.QrCode && parity != segments[0].Metadata!.StructuredAppendParity)
            {
                return null;
            }

            // Each symbol's text was decoded on its own, so a UTF-8 character an encoder split between two of them is
            // broken in both; when every symbol declares UTF-8 the bytes are decoded together instead.
            var builder = new StringBuilder();
            foreach (var segment in segments)
            {
                builder.Append(segment.Text);
            }

            var text = builder.ToString();
            if (segments.All(segment => segment.Metadata?.Eci == 26))
            {
                text = Encoding.UTF8.GetString(raw);
            }
            else if (segments.All(segment => segment.Metadata?.Eci is null && WasGuessed(segment)))
            {
                // Without an ECI each symbol's character set was guessed from its own bytes, and a UTF-8 character an
                // encoder split between two symbols makes both guess Latin-1. The same guess over the bytes together
                // reads them as UTF-8 when, together, they are.
                text = BbCharacterSetEci.GuessEncoding(raw).GetString(raw);
            }

            return new BbStructuredAppendMessage(format, text, raw, segments);
        }
    }
}

/// <summary>What <see cref="BbStructuredAppendAssembler.Add"/> did with a symbol.</summary>
public enum BbStructuredAppendStatus
{
    /// <summary>The symbol is not part of a structured append sequence; handle it as it is.</summary>
    NotStructuredAppend,

    /// <summary>The symbol was added to its sequence, which still waits for others.</summary>
    Added,

    /// <summary>
    /// The symbol had been added already, to a sequence still waiting or to one completed within the last
    /// <see cref="BbStructuredAppendAssembler.Timeout"/>; nothing changed.
    /// </summary>
    Duplicate,

    /// <summary>The symbol was the last its sequence needed; the message is returned.</summary>
    Completed,

    /// <summary>
    /// The symbol contradicted its sequence - a different symbol in the same place, or a different count - so the
    /// sequence was dropped and a new one started with this symbol.
    /// </summary>
    Conflict,

    /// <summary>
    /// The symbol cannot be used: its structured append header is impossible - a place beyond its count or beyond
    /// <see cref="BbStructuredAppendAssembler.MaxSegments"/>, a Macro PDF417 segment without a file ID - it contradicts
    /// itself, or it completed a QR Code sequence whose parity does not match, which was then dropped. The symbol holds
    /// part of a message, so it is not to be used on its own either.
    /// </summary>
    Rejected,
}

/// <summary>A structured append sequence put back together.</summary>
public sealed class BbStructuredAppendMessage
{
    internal BbStructuredAppendMessage(BbBarcodeFormat format, string text, byte[] rawBytes, BbBarcodeResult[] segments)
    {
        Format = format;
        Text = text;
        RawBytes = rawBytes;
        Segments = segments;
    }

    /// <summary>The symbology of the sequence.</summary>
    public BbBarcodeFormat Format { get; }

    /// <summary>The message: the symbols' texts in sequence order.</summary>
    public string Text { get; }

    /// <summary>The symbols' raw bytes in sequence order.</summary>
    public ReadOnlyMemory<byte> RawBytes { get; }

    /// <summary>The symbols, in sequence order.</summary>
    public IReadOnlyList<BbBarcodeResult> Segments { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Format}: {Text}";
}

/// <summary>How far an incomplete structured append sequence has got.</summary>
/// <param name="Format">The symbology of the sequence.</param>
/// <param name="Received">How many of its symbols have been read.</param>
/// <param name="Count">How many symbols it has; <see langword="null"/> for a Macro PDF417 file whose segments do not say and whose last has not been read.</param>
/// <param name="Missing">The places still to be read, numbered as <see cref="BbBarcodeResultMetadata.StructuredAppendIndex"/> numbers them; empty while <paramref name="Count"/> is unknown.</param>
public sealed record BbStructuredAppendProgress(BbBarcodeFormat Format, int Received, int? Count, IReadOnlyList<int> Missing);
