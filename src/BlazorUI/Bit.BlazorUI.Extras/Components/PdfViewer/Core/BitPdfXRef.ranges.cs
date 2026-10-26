namespace Bit.BlazorUI;

public sealed partial class BitPdfXRef
{
    private Dictionary<(int Num, int Gen), object?>? _attemptCache;
    private Dictionary<int, List<object?>>? _attemptObjectStreams;
    private readonly HashSet<int> _pendingObjectStreams = [];
    internal bool IsPartial => _source is IBitPdfPartialByteSource;

    private bool TryGetCachedObject((int Num, int Gen) key, out object? value)
    {
        if (_attemptCache is not null && _attemptCache.TryGetValue(key, out value)) return true;
        return _cache.TryGetValue(key, out value);
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
    // The normal /Prev chain still supplies exact page-tree and non-page entries;
    // hint-table interpretation is deliberately outside this proof of concept.
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
                return offset < bytes.Length ? offset : null;
            }
            return null;

            int? Integer(string key) => dictionary.GetRaw(key) is double number && double.IsFinite(number)
                && number == Math.Floor(number) && number >= 0 && number <= int.MaxValue ? (int)number : null;
        }
        catch (Exception ex) when (BitPdfByteSourceErrors.IsContentError(ex)) { return null; }
    }

    internal PartialAttempt BeginPartialAttempt() => new(this);
    internal sealed class PartialAttempt : IDisposable
    {
        private readonly BitPdfXRef _xref;
        private readonly int _warnings;
        private readonly bool _scanned;
        private readonly Dictionary<BitPdfBaseStream, int> _positions;
        private bool _committed;
        internal PartialAttempt(BitPdfXRef xref)
        {
            if (xref._attemptCache is not null) throw new InvalidOperationException("Nested PDF parser attempts are unsupported.");
            _xref = xref; _warnings = xref.Warnings.Count; _scanned = xref._scanned;
            _positions = xref._cache.Values.OfType<BitPdfBaseStream>().Distinct()
                .ToDictionary(s => s, s => s.Pos);
            xref._attemptCache = []; xref._attemptObjectStreams = [];
        }
        internal void Commit()
        {
            foreach (var pair in _xref._attemptCache!) _xref._cache[pair.Key] = pair.Value;
            foreach (var pair in _xref._attemptObjectStreams!) _xref._objStmCache[pair.Key] = pair.Value;
            _committed = true;
        }
        public void Dispose()
        {
            _xref._attemptCache = null; _xref._attemptObjectStreams = null;
            if (_committed) return;
            _xref._scanned = _scanned;
            if (_xref.Warnings.Count > _warnings) _xref.Warnings.RemoveRange(_warnings, _xref.Warnings.Count - _warnings);
            foreach (var pair in _positions) pair.Key.Pos = pair.Value;
        }
    }
}
