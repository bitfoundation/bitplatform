namespace Bit.BlazorUI;

public sealed partial class BitPdfXRef
{
    [ThreadStatic] private static PartialAttempt? _currentAttempt;
    internal static void TrackPartialStream(BitPdfBaseStream stream) => _currentAttempt?.Track(stream);
    private Dictionary<(int Num, int Gen), object?>? _attemptCache;
    private Dictionary<int, List<object?>>? _attemptObjectStreams;
    private readonly HashSet<int> _pendingObjectStreams = [];
    private BitPdfLinearizationHints.Header? _linearization;
    private BitPdfLinearizationHints? _hints;
    private bool _hintsRead;
    internal bool IsPartial => _source is IBitPdfPartialByteSource;

    private bool TryGetCachedObject((int Num, int Gen) key, out object? value)
    {
        // Buffered reads have no transaction and keep the direct dictionary lookup.
        if (_attemptCache is null) return _cache.TryGetValue(key, out value);
        bool found = _attemptCache.TryGetValue(key, out value) || _cache.TryGetValue(key, out value);
        if (found && value is BitPdfBaseStream stream) TrackPartialStream(stream);
        return found;
    }
    private bool TryGetCachedObjectStream(int number, out List<object?> value)
    {
        if (_attemptObjectStreams is not null && _attemptObjectStreams.TryGetValue(number, out value!)) return true;
        return _objStmCache.TryGetValue(number, out value!);
    }
    internal BitPdfRef? FindCachedReference(BitPdfDict dictionary)
    {
        if (_attemptCache is not null)
            foreach (var pair in _attemptCache)
                if (ReferenceEquals(pair.Value, dictionary)) return new BitPdfRef(pair.Key.Num, pair.Key.Gen);
        foreach (var pair in _cache)
            if (ReferenceEquals(pair.Value, dictionary)) return new BitPdfRef(pair.Key.Num, pair.Key.Gen);
        return null;
    }

    // A stale /L means an incremental update was appended: its tail wins.
    // A validated header lets us start at the first-page xref without a tail probe.
    // The normal /Prev chain still supplies exact page-tree and non-page entries.
    // Validated hint tables can then prefetch page and shared-object byte ranges.
    private int? ReadLinearizedStart()
    {
        if (!IsPartial || _startOffset != 0) return null;
        try
        {
            using var window = _source.GetMemory(0, Math.Min(1024, _source.Length));
            var bytes = window.ToArray();
            var parser = new BitPdfParser(new BitPdfLexer(new BitPdfStream(bytes)), allowStreams: false);
            if (parser.GetObj() is not BitPdfDict dictionary
                || dictionary.GetRaw("Linearized") is not double format || format < 1 || format >= 2
                || Integer("L") != _source.Length || Integer("N") is not > 0
                || Integer("O") is not > 0 || Integer("E") is not int end || end <= 0 || end >= _source.Length
                || Integer("T") is not int main || main <= end || main >= _source.Length
                || dictionary.GetRaw("H") is not List<object?> hints || hints.Count is not (2 or 4)) return null;
            for (int i = 0; i < hints.Count; i += 2)
                if (hints[i] is not double start || hints[i + 1] is not double length
                    || start < 0 || length <= 0 || start != Math.Floor(start) || length != Math.Floor(length)
                    || start + length > _source.Length) return null;
            if (dictionary.Has("P") && (Integer("P") is not int page || page < 0 || page >= Integer("N"))) return null;
            var lexer = new BitPdfLexer(new BitPdfStream(bytes));
            for (int i = 0; i < 1024; i++)
            {
                var token = lexer.GetObj();
                if (ReferenceEquals(token, BitPdfPrimitives.EOF)) break;
                if (token is not BitPdfCmd { Value: "endobj" }) continue;
                int offset = lexer.Pos - 1;
                while (offset < bytes.Length && IsWhite(bytes[offset])) offset++;
                if (offset >= bytes.Length) return null;
                _linearization = new(Integer("N")!.Value, Integer("P") ?? 0, Integer("O")!.Value, end,
                    Enumerable.Range(0, hints.Count / 2).Select(i => ((int)(double)hints[i * 2]!, (int)(double)hints[i * 2 + 1]!)).ToArray());
                return offset;
            }
            return null;

            int? Integer(string key) => dictionary.GetRaw(key) is double number && double.IsFinite(number)
                && number == Math.Floor(number) && number >= 0 && number <= int.MaxValue ? (int)number : null;
        }
        catch (Exception ex) when (BitPdfByteSourceErrors.IsContentError(ex)) { return null; }
    }

    internal List<(int Offset, int Length)> HintRanges(int page, int pageCount, int maxBytes)
    {
        if (!_hintsRead)
        {
            var header = _linearization;
            // Encrypted or unusually large optional hints use the ordinary xref path.
            if (header is null || header.PageCount != pageCount || _security is not null
                || header.Streams.Sum(s => (long)s.Length) > Math.Min(maxBytes, 1024 * 1024))
            { _hintsRead = true; return []; }
            try
            {
                using var data = new System.IO.MemoryStream();
                int sharedOffset = -1;
                bool primary = true;
                foreach (var window in header.Streams)
                {
                    var parser = new BitPdfParser(new BitPdfLexer(_source.CreateStream(window.Offset, window.Length)), this, _reader);
                    if (parser.GetObj() is not BitPdfBaseStream stream || stream.Dict is null
                        || stream.Start < window.Offset || (long)stream.Start + stream.Length > (long)window.Offset + window.Length)
                        throw new BitPdfFormatException("Invalid linearization hint stream.");
                    if (primary)
                        sharedOffset = stream.Dict.Get("S") is double number && number == Math.Floor(number)
                            && number >= 0 && number <= int.MaxValue ? (int)number : -1;
                    primary = false;
                    byte[] decoded = BitPdfStreamDecoder.Decode(stream);
                    if (decoded.Length > 4 * 1024 * 1024 - data.Length) throw new BitPdfFormatException("Linearization hints exceed the optional hint budget.");
                    data.Write(decoded);
                }
                _hints = BitPdfLinearizationHints.Read(data.ToArray(), sharedOffset, header, _source.Length,
                    (number, offset) => _entries.TryGetValue(number, out var entry)
                        && entry.Type == EntryType.Uncompressed && entry.Field3 == 0 && entry.Field2 == offset);
                _hintsRead = true;
            }
            catch (Exception ex) when (BitPdfByteSourceErrors.IsContentError(ex)) { _hintsRead = true; }
        }
        return _hints?.Ranges(page, maxBytes) ?? [];
    }

    internal (int Offset, int Length)? MetadataRange(object? value, int chunkSize)
    {
        if (value is not BitPdfRef reference || TryGetCachedObject((reference.Num, reference.Gen), out _)
            || !_entries.TryGetValue(reference.Num, out var entry)) return null;
        if (entry.Type == EntryType.Compressed)
        {
            if (!_entries.TryGetValue(entry.Field2, out entry)) return null;
        }
        if (entry.Type != EntryType.Uncompressed) return null;
        long offset = (long)entry.Field2 + _startOffset;
        if (offset < 0 || offset >= _source.Length) return null;
        int start = (int)offset / chunkSize * chunkSize;
        return (start, Math.Min(chunkSize, _source.Length - start));
    }

    internal PartialAttempt BeginPartialAttempt() => new(this);
    internal sealed class PartialAttempt : IDisposable
    {
        private readonly BitPdfXRef _xref;
        private readonly int _warnings;
        private readonly bool _scanned;
        private readonly Dictionary<BitPdfBaseStream, int> _positions = [];
        private readonly PartialAttempt? _previous;
        private bool _committed;
        internal PartialAttempt(BitPdfXRef xref)
        {
            if (xref._attemptCache is not null) throw new InvalidOperationException("Nested PDF parser attempts are unsupported.");
            _xref = xref; _warnings = xref.Warnings.Count; _scanned = xref._scanned;
            // CPU attempts are synchronous; record cursors only when this thread touches them.
            _previous = _currentAttempt;
            _currentAttempt = this;
            xref._attemptCache = []; xref._attemptObjectStreams = [];
        }
        internal void Track(BitPdfBaseStream stream) => _positions.TryAdd(stream, stream.Pos);
        internal void Commit()
        {
            foreach (var pair in _xref._attemptCache!) _xref._cache[pair.Key] = pair.Value;
            foreach (var pair in _xref._attemptObjectStreams!) _xref._objStmCache[pair.Key] = pair.Value;
            _committed = true;
        }
        public void Dispose()
        {
            _currentAttempt = _previous;
            _xref._attemptCache = null; _xref._attemptObjectStreams = null;
            if (_committed) return;
            _xref._scanned = _scanned;
            if (_xref.Warnings.Count > _warnings) _xref.Warnings.RemoveRange(_warnings, _xref.Warnings.Count - _warnings);
            foreach (var pair in _positions) pair.Key.Pos = pair.Value;
        }
    }
}
