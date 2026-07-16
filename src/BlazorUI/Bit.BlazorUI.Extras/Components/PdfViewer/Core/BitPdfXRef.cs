// Cross-reference table and stream reader.

using System.Linq;
using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Reads a PDF cross-reference table or cross-reference stream (PDF 1.5+),
/// follows <c>/Prev</c> and hybrid <c>/XRefStm</c> chains, and resolves
/// indirect references - including objects packed inside object streams.
/// </summary>
public sealed class BitPdfXRef : IBitPdfXRef
{
    private enum EntryType { Free, Uncompressed, Compressed }

    private readonly struct Entry
    {
        public EntryType Type { get; init; }
        public int Field2 { get; init; } // offset, or containing ObjStm number
        public int Field3 { get; init; } // generation, or index within ObjStm
    }

    private readonly byte[] _buffer;
    private readonly Dictionary<int, Entry> _entries = new();
    // Keyed by (object number, generation): a generation-0 object must not mask a
    // fetch for the same number at a different generation (2.3).
    private readonly Dictionary<(int Num, int Gen), object?> _cache = new();
    private readonly Dictionary<int, List<object?>> _objStmCache = new();
    private readonly HashSet<int> _pending = new();

    private BitPdfStandardSecurityHandler? _security;
    private int _encryptRefNum = -1;
    private bool _scanned; // true once RebuildByScanning has run (guards re-entry)
    private readonly int _startOffset; // byte position of the %PDF header (prepended junk)

    /// <summary>The combined trailer dictionary (newest section wins).</summary>
    public BitPdfDict? Trailer { get; private set; }

    /// <summary>The password to try for an encrypted document (user or owner).</summary>
    public string? Password { get; set; }

    /// <summary>The raw user-permission bits (<c>/P</c>) once a handler is active, else <c>null</c>.</summary>
    public int? Permissions => _security?.Permissions;

    /// <summary>
    /// Non-fatal problems encountered while parsing (bad xref sections, recovery
    /// by full-scan, etc.). Populated when the file was damaged but still opened.
    /// </summary>
    public List<string> Warnings { get; } = new();

    public BitPdfXRef(byte[] buffer)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        _startOffset = FindHeaderOffset(_buffer);
    }

    /// <summary>
    /// Position of the <c>%PDF-</c> header. When it is not at byte 0 (junk was
    /// prepended) every stored xref/object offset is relative to it, so this is
    /// added to each offset before seeking (2.5, matching pdf.js's stream reset).
    /// </summary>
    private static int FindHeaderOffset(byte[] buffer)
    {
        var marker = "%PDF-"u8;
        int limit = Math.Min(buffer.Length - marker.Length, 1024);
        for (int i = 0; i <= limit; i++)
        {
            bool match = true;
            for (int j = 0; j < marker.Length; j++)
            {
                if (buffer[i + j] != marker[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
            {
                return i;
            }
        }
        return 0;
    }

    /// <summary>The document catalog (<c>/Root</c>), resolved through the trailer.</summary>
    public BitPdfDict? Root => Trailer?.Get("Root") as BitPdfDict;

    /// <summary>Parses the cross-reference data starting from the file's <c>startxref</c>.</summary>
    public void Parse()
    {
        try
        {
            ReadXRefChain();
        }
        catch (BitPdfUnsupportedEncryptionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Warnings.Add($"Cross-reference parsing failed ({ex.Message}); rebuilding by scanning objects.");
        }

        // If the classic/stream path did not yield a usable catalog, brute-force
        // scan the file for objects and rebuild — matching pdf.js recovery.
        if (Trailer is null || Root is null)
        {
            RebuildByScanning();
        }

        if (Trailer is null)
        {
            throw new BitPdfFormatException("No trailer found and recovery failed.");
        }

        SetupEncryption();
    }

    private void ReadXRefChain()
    {
        int start = FindStartXRef();
        if (start < 0)
        {
            Warnings.Add("Could not locate 'startxref'.");
            return; // fall through to recovery
        }

        var queue = new Queue<int>();
        var visited = new HashSet<int>();
        queue.Enqueue(start);
        bool firstSection = true;

        while (queue.Count > 0)
        {
            int offset = queue.Dequeue();
            if (offset < 0 || offset + _startOffset >= _buffer.Length || !visited.Add(offset))
            {
                continue;
            }

            BitPdfDict? sectionTrailer;
            try
            {
                sectionTrailer = ReadSection(offset);
            }
            catch (Exception ex)
            {
                // Tolerate a broken /Prev or /XRefStm section: keep the entries
                // already parsed. Only the very first section failing forces
                // full recovery (handled by the Trailer/Root check in Parse).
                Warnings.Add($"Skipping damaged xref section at {offset} ({ex.Message}).");
                if (firstSection)
                {
                    throw;
                }
                continue;
            }
            finally
            {
                firstSection = false;
            }

            if (sectionTrailer is null)
            {
                continue;
            }

            MergeTrailer(sectionTrailer);

            if (sectionTrailer.GetRaw("XRefStm") is double xrefStm)
            {
                queue.Enqueue((int)xrefStm);
            }
            if (sectionTrailer.GetRaw("Prev") is double prev)
            {
                queue.Enqueue((int)prev);
            }
        }
    }

    private void SetupEncryption()
    {
        object? encRaw = Trailer!.GetRaw("Encrypt");
        if (encRaw is null)
        {
            return;
        }
        if (encRaw is BitPdfRef er)
        {
            _encryptRefNum = er.Num;
        }

        // The Encrypt dictionary itself is never encrypted; security is still
        // null here, so fetching it returns the plaintext dictionary.
        if (FetchIfRef(encRaw) is not BitPdfDict encDict)
        {
            return;
        }

        byte[]? id0 = null;
        if (Trailer.Get("ID") is List<object?> idArr && idArr.Count > 0 && idArr[0] is BitPdfString s)
        {
            id0 = s.Bytes;
        }

        _security = BitPdfStandardSecurityHandler.TryCreate(encDict, id0, Password);

        // A declared /Encrypt with no usable handler means every string and
        // stream would be returned as ciphertext and the file would "load" as
        // garbage. Fail loudly with a typed exception instead.
        if (_security is null)
        {
            throw new BitPdfUnsupportedEncryptionException(
                "The document is encrypted with an unsupported security handler or revision.");
        }
    }

    private BitPdfDict? ReadSection(int offset)
    {
        // Stored offsets are relative to the %PDF header; probe.Pos is already an
        // absolute buffer index once the substream starts there.
        var probe = new BitPdfLexer(new BitPdfStream(_buffer, offset + _startOffset));
        object first = probe.GetObj();

        if (first is BitPdfCmd { Value: "xref" })
        {
            return ReadXRefTable(new BitPdfLexer(new BitPdfStream(_buffer, probe.Pos - 1)));
        }

        // Otherwise it must be an indirect xref stream object: "n g obj << >> stream".
        return ReadXRefStream(offset);
    }

    private BitPdfDict ReadXRefTable(BitPdfLexer lexer)
    {
        while (true)
        {
            object token = lexer.GetObj();
            if (token is BitPdfCmd { Value: "trailer" })
            {
                break;
            }
            if (ReferenceEquals(token, BitPdfPrimitives.EOF))
            {
                throw new BitPdfFormatException("Unexpected end of xref table.");
            }
            if (token is not double startObj)
            {
                throw new BitPdfFormatException("Malformed xref subsection header.");
            }

            object countObj = lexer.GetObj();
            if (countObj is not double countD)
            {
                throw new BitPdfFormatException("Malformed xref subsection count.");
            }

            int subStart = (int)startObj;
            int count = (int)countD;
            for (int i = 0; i < count; i++)
            {
                object offsetTok = lexer.GetObj();
                object genTok = lexer.GetObj();
                object typeTok = lexer.GetObj();

                if (offsetTok is not double off || genTok is not double gen)
                {
                    throw new BitPdfFormatException("Malformed xref entry.");
                }

                int num = subStart + i;
                bool free = typeTok is BitPdfCmd { Value: "f" };
                _entries.TryAdd(num, new Entry
                    {
                        Type = free ? EntryType.Free : EntryType.Uncompressed,
                        // Normalize a real object offset to an absolute buffer index
                        // (offsets are relative to the %PDF header); a free entry's
                        // field is a next-free object number, not an offset (2.5).
                        Field2 = free ? (int)off : (int)off + _startOffset,
                        Field3 = (int)gen,
                    });
            }
        }

        // After the "trailer" keyword the dictionary follows.
        var parser = new BitPdfParser(lexer, this, allowStreams: false);
        return parser.GetObj() as BitPdfDict
            ?? throw new BitPdfFormatException("Trailer is not a dictionary.");
    }

    private BitPdfDict ReadXRefStream(int offset)
    {
        var parser = new BitPdfParser(new BitPdfLexer(new BitPdfStream(_buffer, offset + _startOffset)), this);
        if (parser.GetObj() is not BitPdfStream stream || stream.Dict is null)
        {
            throw new BitPdfFormatException("Expected an xref stream object.");
        }

        BitPdfDict dict = stream.Dict;
        byte[] data = BitPdfStreamDecoder.Decode(stream);

        if (dict.Get("W") is not List<object?> w || w.Count < 3)
        {
            throw new BitPdfFormatException("xref stream missing /W.");
        }
        int w0 = ToInt(w[0]);
        int w1 = ToInt(w[1]);
        int w2 = ToInt(w[2]);
        // Negative widths would drive ReadField into IndexOutOfRange; oversized
        // widths would overflow the long accumulator. Each field fits in 8 bytes.
        if (w0 < 0 || w1 < 0 || w2 < 0 || w0 > 8 || w1 > 8 || w2 > 8)
        {
            throw new BitPdfFormatException("Invalid /W widths in xref stream.");
        }
        int entryLen = w0 + w1 + w2;
        if (entryLen == 0)
        {
            throw new BitPdfFormatException("Invalid /W in xref stream.");
        }

        // /Index pairs default to [0, Size].
        var index = new List<int>();
        if (dict.Get("Index") is List<object?> idx)
        {
            foreach (var v in idx)
            {
                index.Add(ToInt(v));
            }
        }
        else
        {
            index.Add(0);
            index.Add(ToInt(dict.Get("Size")));
        }

        int pos = 0;
        for (int section = 0; section + 1 < index.Count; section += 2)
        {
            int objStart = index[section];
            int objCount = index[section + 1];
            for (int i = 0; i < objCount && pos + entryLen <= data.Length; i++)
            {
                long f1 = w0 == 0 ? 1 : ReadField(data, pos, w0);
                long f2 = ReadField(data, pos + w0, w1);
                long f3 = ReadField(data, pos + w0 + w1, w2);
                pos += entryLen;

                int num = objStart + i;
                // In a hybrid-reference file the classic section marks compressed
                // objects as free; the parallel /XRefStm carries their real
                // entries. Let a cross-reference stream override an existing *free*
                // entry (but never a real one, preserving newer-section priority).
                if (_entries.TryGetValue(num, out var existing) && existing.Type != EntryType.Free)
                {
                    continue;
                }

                _entries[num] = f1 switch
                {
                    0 => new Entry { Type = EntryType.Free, Field2 = (int)f2, Field3 = (int)f3 },
                    // Type-1 Field2 is an object offset -> normalize to absolute (2.5);
                    // type-2 Field2 is the containing ObjStm object number, not an offset.
                    1 => new Entry { Type = EntryType.Uncompressed, Field2 = (int)f2 + _startOffset, Field3 = (int)f3 },
                    2 => new Entry { Type = EntryType.Compressed, Field2 = (int)f2, Field3 = (int)f3 },
                    _ => new Entry { Type = EntryType.Free },
                };
            }
        }

        return dict;
    }

    private void MergeTrailer(BitPdfDict section)
    {
        if (Trailer is null)
        {
            Trailer = section;
            return;
        }
        foreach (var key in section.Keys)
        {
            if (!Trailer.Has(key))
            {
                Trailer.Set(key, section.GetRaw(key));
            }
        }
    }

    /// <inheritdoc/>
    public object? FetchIfRef(object? value, bool suppressEncryption = false)
        => value is BitPdfRef r ? Fetch(r, suppressEncryption) : value;

    /// <inheritdoc/>
    public object? Fetch(BitPdfRef reference, bool suppressEncryption = false)
    {
        if (_cache.TryGetValue((reference.Num, reference.Gen), out var cached))
        {
            return cached;
        }
        if (!_entries.TryGetValue(reference.Num, out var entry) || entry.Type == EntryType.Free)
        {
            return null;
        }
        if (!_pending.Add(reference.Num))
        {
            return null; // cycle
        }

        try
        {
            object? result = entry.Type == EntryType.Compressed
                ? FetchCompressed(entry)
                : FetchUncompressed(reference, entry);
            _cache[(reference.Num, reference.Gen)] = result;
            return result;
        }
        finally
        {
            _pending.Remove(reference.Num);
        }
    }

    private object? FetchUncompressed(BitPdfRef reference, Entry entry)
    {
        if (entry.Field2 < 0 || entry.Field2 >= _buffer.Length)
        {
            return null;
        }

        // Validate the "num gen obj" header at the offset (both number and
        // generation). A mismatch means the xref offset is wrong (off-by-N,
        // damaged table); rebuild by scanning once and retry from the corrected
        // offset rather than silently returning the wrong object.
        if (!HeaderMatches(entry.Field2, reference.Num, reference.Gen) && !_scanned)
        {
            _scanned = true;
            RebuildByScanning();
            if (_entries.TryGetValue(reference.Num, out var corrected)
                && corrected.Type == EntryType.Uncompressed)
            {
                entry = corrected;
            }
        }

        var parser = new BitPdfParser(new BitPdfLexer(new BitPdfStream(_buffer, entry.Field2)), this);
        object? obj = parser.GetObj();

        if (_security is not null && reference.Num != _encryptRefNum)
        {
            obj = DecryptObject(obj, reference.Num, reference.Gen);
        }
        return obj;
    }

    private object? DecryptObject(object? obj, int num, int gen)
    {
        switch (obj)
        {
            case BitPdfString s:
                return new BitPdfString(_security!.DecryptString(s.Bytes, num, gen));

            case List<object?> list:
                for (int i = 0; i < list.Count; i++)
                {
                    list[i] = DecryptObject(list[i], num, gen);
                }
                return list;

            case BitPdfDict dict:
                DecryptDictStrings(dict, num, gen);
                return dict;

            case BitPdfStream stream when stream.Dict is not null:
                DecryptDictStrings(stream.Dict, num, gen);
                // Cross-reference streams are never encrypted.
                if (BitPdfPrimitives.IsName(stream.Dict.Get("Type"), "XRef"))
                {
                    return stream;
                }
                // A /Filter /Crypt with /Name /Identity (or no name, which defaults
                // to Identity) marks a stream the default StmF must not decrypt (1.7).
                if (HasIdentityCryptFilter(stream.Dict))
                {
                    return stream;
                }
                stream.Reset();
                byte[] raw = stream.GetBytes();
                byte[] decrypted = _security!.DecryptStream(raw, num, gen);
                return new BitPdfStream(decrypted, 0, decrypted.Length, stream.Dict);

            default:
                return obj;
        }
    }

    /// <summary>
    /// True when the stream's <c>/Filter</c> chain contains a <c>Crypt</c> filter
    /// whose <c>/DecodeParms /Name</c> is <c>Identity</c> (or absent, which the
    /// spec defaults to Identity) - meaning the default StmF must not decrypt it.
    /// </summary>
    private bool HasIdentityCryptFilter(BitPdfDict dict)
    {
        object? filter = FetchIfRef(dict.Get("Filter"));
        var filterNames = new List<string>();
        if (filter is BitPdfName single)
        {
            filterNames.Add(single.Value);
        }
        else if (filter is List<object?> arr)
        {
            foreach (var f in arr)
            {
                if (FetchIfRef(f) is BitPdfName fn)
                {
                    filterNames.Add(fn.Value);
                }
            }
        }

        int cryptIndex = filterNames.IndexOf("Crypt");
        if (cryptIndex < 0)
        {
            return false;
        }

        // Find the DecodeParms entry aligned with the Crypt filter.
        object? parms = FetchIfRef(dict.Get("DecodeParms"));
        BitPdfDict? cryptParms = parms switch
        {
            BitPdfDict d => d,
            List<object?> pl when cryptIndex < pl.Count => FetchIfRef(pl[cryptIndex]) as BitPdfDict,
            _ => null,
        };

        // Absent /Name defaults to Identity per PDF 32000-1 §7.4.10.
        string cryptName = (cryptParms?.Get("Name") as BitPdfName)?.Value ?? "Identity";
        return cryptName == "Identity";
    }

    private void DecryptDictStrings(BitPdfDict dict, int num, int gen)
    {
        // A signature dictionary's /Contents holds the raw signature bytes, which
        // are excluded from the document's string encryption (PDF 32000-1 §7.6.2);
        // decrypting them would corrupt the signature. Such dicts carry /ByteRange.
        bool isSignature = dict.Has("ByteRange");

        foreach (var key in dict.Keys.ToList())
        {
            if (isSignature && key == "Contents")
            {
                continue;
            }
            object? raw = dict.GetRaw(key);
            // Indirect references are not decrypted; their targets are when fetched.
            if (raw is BitPdfRef)
            {
                continue;
            }
            dict.Set(key, DecryptObject(raw, num, gen));
        }
    }

    private object? FetchCompressed(Entry entry)
    {
        var objects = GetObjectStream(entry.Field2);
        return entry.Field3 >= 0 && entry.Field3 < objects.Count ? objects[entry.Field3] : null;
    }

    private List<object?> GetObjectStream(int streamNum)
    {
        if (_objStmCache.TryGetValue(streamNum, out var cached))
        {
            return cached;
        }

        var result = new List<object?>();
        _objStmCache[streamNum] = result; // guard re-entrancy

        if (Fetch(new BitPdfRef(streamNum, 0)) is not BitPdfStream stream || stream.Dict is null)
        {
            return result;
        }

        BitPdfDict dict = stream.Dict;
        int n = ToInt(dict.Get("N"));
        int first = ToInt(dict.Get("First"));
        byte[] data = BitPdfStreamDecoder.Decode(stream);

        // The header is N pairs of "objNum offset"; each needs at least ~4 bytes.
        // Clamp the declared /N against the decoded length so a hostile /N cannot
        // drive a huge allocation or multi-billion-iteration loop.
        n = Math.Clamp(n, 0, data.Length / 4);

        // Header: N pairs of "objNum offset".
        var headerLexer = new BitPdfLexer(new BitPdfStream(data));
        var offsets = new List<int>(n);
        for (int i = 0; i < n; i++)
        {
            _ = headerLexer.GetObj();          // object number (unused for positional access)
            object offTok = headerLexer.GetObj();
            offsets.Add(offTok is double d ? (int)d : 0);
        }

        for (int i = 0; i < n; i++)
        {
            int objStart = first + offsets[i];
            if (objStart < 0 || objStart >= data.Length)
            {
                result.Add(null);
                continue;
            }
            var parser = new BitPdfParser(new BitPdfLexer(new BitPdfStream(data, objStart)), this);
            result.Add(parser.GetObj());
        }

        return result;
    }

    // ----- Recovery (pdf.js XRef.indexObjects equivalent) -----

    private bool HeaderMatches(int offset, int num, int gen)
    {
        try
        {
            var lexer = new BitPdfLexer(new BitPdfStream(_buffer, offset));
            if (lexer.GetObj() is not double dn || (int)dn != num)
            {
                return false;
            }
            // The generation should match too; tolerate a header that omits it.
            return lexer.GetObj() is not double dg || (int)dg == gen;
        }
        catch
        {
            return false;
        }
    }

    private void RebuildByScanning()
    {
        _scanned = true;
        Warnings.Add("Rebuilding the cross-reference table by scanning the file.");
        _entries.Clear();
        _cache.Clear();
        _objStmCache.Clear();

        ScanForObjects();
        IndexObjectStreams();
        RecoverTrailer();
    }

    private void ScanForObjects()
    {
        int n = _buffer.Length;
        var objKw = "obj"u8.ToArray();
        int i = 0;
        while (i < n)
        {
            // A header is "<num> <gen> obj" at a token boundary.
            if (!IsDigit(_buffer[i]) || (i > 0 && !IsWhiteOrDelimiter(_buffer[i - 1])))
            {
                i++;
                continue;
            }

            int startNum = i;
            int num = ReadInt(ref i, n);
            int p = i;
            SkipWhite(ref p, n);
            if (p == i || p >= n || !IsDigit(_buffer[p]))
            {
                i = startNum + 1;
                continue;
            }
            int gen = ReadInt(ref p, n);
            int q = p;
            SkipWhite(ref q, n);
            if (q == p || !MatchesAt(q, objKw))
            {
                i = startNum + 1;
                continue;
            }

            // Later definitions of an object number supersede earlier ones
            // (incremental-update semantics).
            _entries[num] = new Entry { Type = EntryType.Uncompressed, Field2 = startNum, Field3 = gen };
            i = q + objKw.Length;
        }
    }

    private void IndexObjectStreams()
    {
        foreach (var (streamNum, entry) in _entries.ToList())
        {
            if (entry.Type != EntryType.Uncompressed)
            {
                continue;
            }
            object? obj;
            try
            {
                obj = FetchUncompressed(new BitPdfRef(streamNum, entry.Field3), entry);
            }
            catch
            {
                continue;
            }
            if (obj is not BitPdfStream s || s.Dict is null || !BitPdfPrimitives.IsName(s.Dict.Get("Type"), "ObjStm"))
            {
                continue;
            }

            byte[] data;
            try
            {
                data = BitPdfStreamDecoder.Decode(s);
            }
            catch
            {
                continue;
            }
            int cnt = Math.Clamp(ToInt(s.Dict.Get("N")), 0, data.Length / 4);
            var lexer = new BitPdfLexer(new BitPdfStream(data));
            for (int idx = 0; idx < cnt; idx++)
            {
                if (lexer.GetObj() is not double dn)
                {
                    break;
                }
                _ = lexer.GetObj(); // offset (unused for positional access)
                int objNum = (int)dn;
                // Don't override a directly-scanned (uncompressed) definition.
                _entries.TryAdd(objNum, new Entry { Type = EntryType.Compressed, Field2 = streamNum, Field3 = idx });
            }
        }
    }

    private void RecoverTrailer()
    {
        if (Root is not null)
        {
            return; // an existing trailer is already usable
        }

        BitPdfDict? found = FindTrailerDict();
        if (found is not null)
        {
            MergeTrailer(found);
            if (Root is not null)
            {
                return;
            }
        }

        // No usable trailer: locate the /Type /Catalog object directly.
        foreach (var (num, entry) in _entries)
        {
            if (entry.Type != EntryType.Uncompressed)
            {
                continue;
            }
            object? obj;
            try
            {
                obj = Fetch(new BitPdfRef(num, entry.Field3));
            }
            catch
            {
                continue;
            }
            if (obj is BitPdfDict d && BitPdfPrimitives.IsName(d.Get("Type"), "Catalog"))
            {
                Trailer ??= new BitPdfDict(this);
                Trailer.Set("Root", new BitPdfRef(num, entry.Field3));
                Warnings.Add($"Recovered the document catalog from object {num}.");
                return;
            }
        }
    }

    private BitPdfDict? FindTrailerDict()
    {
        var kw = "trailer"u8.ToArray();
        BitPdfDict? result = null;
        for (int i = 0; i + kw.Length <= _buffer.Length; i++)
        {
            if (!MatchesAt(i, kw))
            {
                continue;
            }
            try
            {
                var parser = new BitPdfParser(new BitPdfLexer(new BitPdfStream(_buffer, i + kw.Length)), this, allowStreams: false);
                if (parser.GetObj() is BitPdfDict d)
                {
                    result = d; // keep the last (most recent) trailer
                }
            }
            catch
            {
                // Ignore an unparseable trailer candidate.
            }
        }
        return result;
    }

    private int ReadInt(ref int i, int n)
    {
        int value = 0;
        while (i < n && IsDigit(_buffer[i]))
        {
            value = value * 10 + (_buffer[i] - '0');
            i++;
        }
        return value;
    }

    private void SkipWhite(ref int i, int n)
    {
        while (i < n && IsWhite(_buffer[i]))
        {
            i++;
        }
    }

    private static bool IsDigit(byte b) => b is >= (byte)'0' and <= (byte)'9';
    private static bool IsWhite(byte b) => b is 0x20 or 0x09 or 0x0A or 0x0C or 0x0D or 0x00;
    private static bool IsWhiteOrDelimiter(byte b)
        => IsWhite(b) || b is (byte)'<' or (byte)'>' or (byte)'[' or (byte)']'
            or (byte)'(' or (byte)')' or (byte)'/' or (byte)'{' or (byte)'}';

    private int FindStartXRef()
    {
        var keyword = "startxref"u8.ToArray();
        int searchStart = Math.Max(0, _buffer.Length - 2048);
        for (int i = _buffer.Length - keyword.Length; i >= searchStart; i--)
        {
            if (MatchesAt(i, keyword))
            {
                var lexer = new BitPdfLexer(new BitPdfStream(_buffer, i + keyword.Length));
                return lexer.GetObj() is double d ? (int)d : -1;
            }
        }
        return -1;
    }

    private bool MatchesAt(int at, byte[] keyword)
    {
        if (at < 0 || at + keyword.Length > _buffer.Length)
        {
            return false;
        }
        for (int k = 0; k < keyword.Length; k++)
        {
            if (_buffer[at + k] != keyword[k])
            {
                return false;
            }
        }
        return true;
    }

    private static long ReadField(byte[] data, int pos, int width)
    {
        long value = 0;
        for (int i = 0; i < width; i++)
        {
            value = (value << 8) | data[pos + i];
        }
        return value;
    }

    private static int ToInt(object? value) => value switch
    {
        double d => (int)d,
        _ => 0,
    };
}
