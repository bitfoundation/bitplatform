using System.Buffers.Binary;

namespace Bit.Bmage.Text;

/// <summary>One glyph while text is shaped: what it came from, what applies to it, and where it goes.</summary>
internal struct ShapingGlyph
{
    public ushort Glyph;
    /// <summary>The index of the first code point the glyph stands for.</summary>
    public int Cluster;
    /// <summary>The features that apply to the glyph, one bit each; bit 0 is every global feature.</summary>
    public uint Mask;
    /// <summary>GDEF glyph class: 0 unknown, 1 base, 2 ligature, 3 mark, 4 component.</summary>
    public byte GlyphClass;
    public byte MarkAttachClass;
    /// <summary>For a ligature, and for a mark that sat between its components: the ligature it belongs to.</summary>
    public int LigatureId;
    /// <summary>For a mark: the component of <see cref="LigatureId"/> it followed, from 1; 0 for the last one.</summary>
    public byte LigatureComponent;
    /// <summary>For a ligature: how many characters it joins.</summary>
    public byte LigatureComponents;
    /// <summary>A default ignorable code point (ZWNJ, ZWJ, a bidi mark, a soft hyphen): neither drawn nor given any width.</summary>
    public bool Hidden;

    // Positions, in font units.
    public int XAdvance;
    public int XOffset;
    public int YOffset;
    /// <summary>The glyph a mark is attached to, or -1.</summary>
    public int AttachTo;
    public int AttachDx;
    public int AttachDy;
}

/// <summary>A growable run of glyphs being shaped.</summary>
internal sealed class GlyphBuffer
{
    /// <summary>A bound on how far substitutions may grow a run, so a hostile font cannot exhaust memory.</summary>
    public const int MaxLength = 16384;

    private ShapingGlyph[] _items = new ShapingGlyph[16];
    private int _nextLigatureId;

    public int Count { get; private set; }

    public ref ShapingGlyph this[int index] => ref _items[index];

    public Span<ShapingGlyph> AsSpan() => _items.AsSpan(0, Count);

    public int NewLigatureId() => ++_nextLigatureId;

    public void Add(ShapingGlyph glyph)
    {
        if (Count == _items.Length) Array.Resize(ref _items, _items.Length * 2);
        _items[Count++] = glyph;
    }

    public bool Insert(int index, ShapingGlyph glyph)
    {
        if (Count >= MaxLength) return false;
        if (Count == _items.Length) Array.Resize(ref _items, _items.Length * 2);
        Array.Copy(_items, index, _items, index + 1, Count - index);
        _items[index] = glyph;
        Count++;
        return true;
    }

    public void RemoveAt(int index)
    {
        Array.Copy(_items, index + 1, _items, index, Count - index - 1);
        Count--;
    }
}

/// <summary>
/// The OpenType layout tables of a font - GDEF, GSUB and GPOS - and the lookups that substitute and position glyphs:
/// single, multiple and ligature substitution, contextual and chaining contextual substitution and positioning, single
/// and pair adjustment, and mark attachment to bases, ligatures and other marks. That is what joining scripts such as
/// Arabic need, and what kerning and accents need everywhere.
/// </summary>
/// <remarks>
/// Every read is bounds checked, so a damaged or hostile table yields fewer substitutions rather than an exception.
/// Alternate substitution, reverse chaining substitution and cursive attachment are not applied.
/// </remarks>
internal sealed class OpenTypeLayout
{
    private const ushort RightToLeft = 0x0001, IgnoreBaseGlyphs = 0x0002, IgnoreLigatures = 0x0004, IgnoreMarks = 0x0008, UseMarkFilteringSet = 0x0010;
    private const int MaxNesting = 8;

    private readonly FontFile _font;
    private readonly byte[] _gdef, _gsub, _gpos;
    private readonly int _glyphClassDef, _markAttachClassDef, _markGlyphSets;
    private readonly Lookup?[] _gsubLookups, _gposLookups;

    public OpenTypeLayout(FontFile font)
    {
        _font = font;
        _gdef = font.GetTable("GDEF").ToArray();
        _gsub = font.GetTable("GSUB").ToArray();
        _gpos = font.GetTable("GPOS").ToArray();
        if (_gdef.Length >= 12)
        {
            _glyphClassDef = U16(_gdef, 4);
            _markAttachClassDef = U16(_gdef, 10);
            if (U16(_gdef, 0) == 1 && U16(_gdef, 2) >= 2 && _gdef.Length >= 14) _markGlyphSets = U16(_gdef, 12);
        }
        _gsubLookups = new Lookup?[LookupCount(_gsub)];
        _gposLookups = new Lookup?[LookupCount(_gpos)];
    }

    /// <summary>Whether the font has glyph positioning data; without it the legacy kern table applies.</summary>
    public bool HasPositioning => _gpos.Length > 0;

    // ---- Reading ----

    private static ushort U16(ReadOnlySpan<byte> d, int o) => o >= 0 && o + 2 <= d.Length ? BinaryPrimitives.ReadUInt16BigEndian(d[o..]) : (ushort)0;

    private static short S16(ReadOnlySpan<byte> d, int o) => o >= 0 && o + 2 <= d.Length ? BinaryPrimitives.ReadInt16BigEndian(d[o..]) : (short)0;

    private static uint U32(ReadOnlySpan<byte> d, int o) => o >= 0 && o + 4 <= d.Length ? BinaryPrimitives.ReadUInt32BigEndian(d[o..]) : 0;

    /// <summary>A four-letter tag as a number, e.g. 'arab'.</summary>
    public static uint Tag(string tag) => (uint)(tag[0] << 24 | tag[1] << 16 | tag[2] << 8 | tag[3]);

    /// <summary>The index of a glyph in a coverage table, or -1.</summary>
    private static int Coverage(ReadOnlySpan<byte> d, int offset, ushort glyph)
    {
        if (offset <= 0) return -1;
        switch (U16(d, offset))
        {
            case 1:
            {
                int lo = 0, hi = U16(d, offset + 2) - 1;
                while (lo <= hi)
                {
                    var mid = (lo + hi) >>> 1;
                    var g = U16(d, offset + 4 + mid * 2);
                    if (g == glyph) return mid;
                    if (g < glyph) lo = mid + 1; else hi = mid - 1;
                }
                return -1;
            }
            case 2:
            {
                int lo = 0, hi = U16(d, offset + 2) - 1;
                while (lo <= hi)
                {
                    var mid = (lo + hi) >>> 1;
                    var r = offset + 4 + mid * 6;
                    var start = U16(d, r);
                    var end = U16(d, r + 2);
                    if (glyph < start) hi = mid - 1;
                    else if (glyph > end) lo = mid + 1;
                    else return U16(d, r + 4) + glyph - start;
                }
                return -1;
            }
            default:
                return -1;
        }
    }

    /// <summary>The class of a glyph in a class definition table; 0 when it has none.</summary>
    private static int ClassOf(ReadOnlySpan<byte> d, int offset, ushort glyph)
    {
        if (offset <= 0) return 0;
        switch (U16(d, offset))
        {
            case 1:
            {
                var start = U16(d, offset + 2);
                var count = U16(d, offset + 4);
                return glyph >= start && glyph < start + count ? U16(d, offset + 6 + (glyph - start) * 2) : 0;
            }
            case 2:
            {
                int lo = 0, hi = U16(d, offset + 2) - 1;
                while (lo <= hi)
                {
                    var mid = (lo + hi) >>> 1;
                    var r = offset + 4 + mid * 6;
                    if (glyph < U16(d, r)) hi = mid - 1;
                    else if (glyph > U16(d, r + 2)) lo = mid + 1;
                    else return U16(d, r + 4);
                }
                return 0;
            }
            default:
                return 0;
        }
    }

    // ---- GDEF ----

    /// <summary>Fills in the GDEF classes of a glyph after it is created or substituted.</summary>
    public void Classify(ref ShapingGlyph g)
    {
        g.GlyphClass = (byte)ClassOf(_gdef, _glyphClassDef, g.Glyph);
        g.MarkAttachClass = (byte)ClassOf(_gdef, _markAttachClassDef, g.Glyph);
    }

    private bool InMarkSet(int set, ushort glyph)
    {
        if (_markGlyphSets <= 0 || set >= U16(_gdef, _markGlyphSets + 2)) return false;
        var coverage = (int)U32(_gdef, _markGlyphSets + 4 + set * 4);
        return coverage > 0 && Coverage(_gdef, _markGlyphSets + coverage, glyph) >= 0;
    }

    private bool Skip(in ShapingGlyph g, Lookup lookup)
    {
        var flag = lookup.Flag;
        switch (g.GlyphClass)
        {
            case 1 when (flag & IgnoreBaseGlyphs) != 0:
            case 2 when (flag & IgnoreLigatures) != 0:
                return true;
            case 3:
                if ((flag & IgnoreMarks) != 0) return true;
                if ((flag & UseMarkFilteringSet) != 0) return !InMarkSet(lookup.MarkFilteringSet, g.Glyph);
                var type = flag >> 8;
                return type != 0 && g.MarkAttachClass != type;
            default:
                return false;
        }
    }

    // ---- Scripts, features and lookups ----

    private sealed record Lookup(int Type, ushort Flag, ushort MarkFilteringSet, int[] Subtables);

    private static int LookupCount(byte[] table) => table.Length >= 10 ? U16(table, U16(table, 8)) : 0;

    private Lookup GetLookup(bool gpos, int index)
    {
        var lookups = gpos ? _gposLookups : _gsubLookups;
        if (lookups[index] is { } cached) return cached;
        var d = gpos ? _gpos : _gsub;
        var list = U16(d, 8);
        var offset = list + U16(d, list + 2 + index * 2);
        int type = U16(d, offset);
        var flag = U16(d, offset + 2);
        var count = U16(d, offset + 4);
        var subtables = new int[count];
        var extension = gpos ? 9 : 7;
        var resolved = type;
        for (var i = 0; i < count; i++)
        {
            var sub = offset + U16(d, offset + 6 + i * 2);
            if (type == extension)
            {
                // Every subtable of an extension lookup has the same real type; their data may sit anywhere.
                resolved = U16(d, sub + 2);
                sub += (int)Math.Min(U32(d, sub + 4), int.MaxValue / 2);
            }
            subtables[i] = sub;
        }
        var filteringSet = (flag & UseMarkFilteringSet) != 0 ? U16(d, offset + 6 + count * 2) : (ushort)0;
        return lookups[index] = new Lookup(resolved, flag, filteringSet, subtables);
    }

    /// <summary>
    /// The lookups the features call for in a script, in the order they must run (the lookup list's), each with the
    /// mask of the features that call it. A script the font does not list falls back to DFLT, then to latn.
    /// </summary>
    private List<(int Lookup, uint Mask)> CollectLookups(bool gpos, uint script, (uint Tag, uint Mask)[] features)
    {
        // The shaper passes a few fixed feature lists, so the lookups a script needs are worked out once per font.
        var key = (gpos, script, features);
        lock (_collected)
        {
            if (_collected.TryGetValue(key, out var cached)) return cached;
            var result = Collect(gpos, script, features);
            _collected[key] = result;
            return result;
        }
    }

    private readonly Dictionary<(bool, uint, (uint, uint)[]), List<(int, uint)>> _collected = [];

    private List<(int Lookup, uint Mask)> Collect(bool gpos, uint script, (uint Tag, uint Mask)[] features)
    {
        var d = gpos ? _gpos : _gsub;
        var result = new List<(int, uint)>();
        if (d.Length < 10) return result;
        var scriptList = U16(d, 4);
        var featureList = U16(d, 6);
        var langSys = FindLangSys(d, scriptList, script) ?? FindLangSys(d, scriptList, Tag("DFLT")) ?? FindLangSys(d, scriptList, Tag("latn"));
        if (langSys is not { } ls) return result;

        var masks = new Dictionary<int, uint>();
        void AddFeature(int featureIndex, uint mask)
        {
            var record = featureList + 2 + featureIndex * 6;
            var feature = featureList + U16(d, record + 4);
            var count = U16(d, feature + 2);
            for (var k = 0; k < count; k++)
            {
                int lookup = U16(d, feature + 4 + k * 2);
                if (lookup >= (gpos ? _gposLookups.Length : _gsubLookups.Length)) continue;
                masks[lookup] = masks.GetValueOrDefault(lookup) | mask;
            }
        }

        var required = U16(d, ls + 2);
        if (required != 0xFFFF) AddFeature(required, 1);
        var featureCount = U16(d, ls + 4);
        var available = U16(d, featureList);
        for (var i = 0; i < featureCount; i++)
        {
            int index = U16(d, ls + 6 + i * 2);
            if (index >= available) continue;
            var tag = U32(d, featureList + 2 + index * 6);
            foreach (var (wanted, mask) in features)
                if (wanted == tag) AddFeature(index, mask);
        }
        foreach (var (lookup, mask) in masks) result.Add((lookup, mask));
        result.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        return result;
    }

    private static int? FindLangSys(ReadOnlySpan<byte> d, int scriptList, uint script)
    {
        var count = U16(d, scriptList);
        for (var i = 0; i < count; i++)
        {
            var record = scriptList + 2 + i * 6;
            if (U32(d, record) != script) continue;
            var scriptTable = scriptList + U16(d, record + 4);
            var defaultLangSys = U16(d, scriptTable);
            return defaultLangSys == 0 ? null : scriptTable + defaultLangSys;
        }
        return null;
    }

    // ---- Applying lookups ----

    /// <summary>Runs the GSUB lookups of the features over the buffer.</summary>
    public void Substitute(GlyphBuffer buffer, uint script, (uint Tag, uint Mask)[] features)
    {
        foreach (var (index, mask) in CollectLookups(false, script, features))
        {
            var lookup = GetLookup(false, index);
            for (var i = 0; i < buffer.Count;)
            {
                ref var g = ref buffer[i];
                if ((g.Mask & mask) == 0 || Skip(g, lookup)) { i++; continue; }
                i = ApplySubstitution(buffer, i, lookup, 0) ?? i + 1;
            }
        }
    }

    /// <summary>Runs the GPOS lookups of the features over the buffer, whose advances hold the glyphs' own widths.</summary>
    public void Position(GlyphBuffer buffer, uint script, (uint Tag, uint Mask)[] features)
    {
        foreach (var (index, mask) in CollectLookups(true, script, features))
        {
            var lookup = GetLookup(true, index);
            for (var i = 0; i < buffer.Count;)
            {
                ref var g = ref buffer[i];
                if ((g.Mask & mask) == 0 || Skip(g, lookup)) { i++; continue; }
                i = ApplyPositioning(buffer, i, lookup, 0) ?? i + 1;
            }
        }
    }

    /// <summary>Applies the first subtable of a substitution lookup that matches at <paramref name="i"/>; returns where to continue, or null.</summary>
    private int? ApplySubstitution(GlyphBuffer buffer, int i, Lookup lookup, int depth)
    {
        var d = _gsub;
        foreach (var sub in lookup.Subtables)
        {
            var format = U16(d, sub);
            var glyph = buffer[i].Glyph;
            switch (lookup.Type)
            {
                case 1:
                {
                    var index = Coverage(d, sub + U16(d, sub + 2), glyph);
                    if (index < 0) continue;
                    var replacement = format == 1
                        ? (ushort)(glyph + S16(d, sub + 4))
                        : index < U16(d, sub + 4) ? U16(d, sub + 6 + index * 2) : glyph;
                    buffer[i].Glyph = replacement;
                    Classify(ref buffer[i]);
                    return i + 1;
                }
                case 2:
                {
                    var index = Coverage(d, sub + U16(d, sub + 2), glyph);
                    if (index < 0 || index >= U16(d, sub + 4)) continue;
                    var sequence = sub + U16(d, sub + 6 + index * 2);
                    var count = U16(d, sequence);
                    if (count == 0)
                    {
                        buffer.RemoveAt(i);
                        return i;
                    }
                    var template = buffer[i];
                    buffer[i].Glyph = U16(d, sequence + 2);
                    Classify(ref buffer[i]);
                    for (var k = 1; k < count; k++)
                    {
                        var extra = template;
                        extra.Glyph = U16(d, sequence + 2 + k * 2);
                        Classify(ref extra);
                        if (!buffer.Insert(i + k, extra)) return i + k;
                    }
                    return i + count;
                }
                case 4:
                {
                    var index = Coverage(d, sub + U16(d, sub + 2), glyph);
                    if (index < 0 || index >= U16(d, sub + 4)) continue;
                    var set = sub + U16(d, sub + 6 + index * 2);
                    var count = U16(d, set);
                    for (var l = 0; l < count; l++)
                    {
                        var ligature = set + U16(d, set + 2 + l * 2);
                        var components = U16(d, ligature + 2);
                        if (components == 0) continue;
                        Span<int> positions = components <= 32 ? stackalloc int[components] : new int[components];
                        positions[0] = i;
                        var matched = true;
                        for (var k = 1; k < components && matched; k++)
                        {
                            var next = NextIndex(buffer, positions[k - 1], lookup);
                            matched = next >= 0 && buffer[next].Glyph == U16(d, ligature + 4 + (k - 1) * 2);
                            if (matched) positions[k] = next;
                        }
                        if (!matched) continue;
                        FormLigature(buffer, positions, U16(d, ligature));
                        return i + 1;
                    }
                    continue;
                }
                case 5 or 6:
                {
                    var next = ApplyContext(buffer, i, lookup, sub, d, gpos: false, depth);
                    if (next is not null) return next;
                    continue;
                }
            }
        }
        return null;
    }

    /// <summary>Replaces the glyphs at <paramref name="positions"/> with one ligature; marks between them remember which component they followed.</summary>
    private void FormLigature(GlyphBuffer buffer, ReadOnlySpan<int> positions, ushort ligatureGlyph)
    {
        var id = buffer.NewLigatureId();
        var first = positions[0];
        var cluster = buffer[first].Cluster;
        for (var k = 1; k < positions.Length; k++)
        {
            cluster = Math.Min(cluster, buffer[positions[k]].Cluster);
            // The marks between component k and the next one belong to component k.
            for (var m = positions[k - 1] + 1; m < positions[k]; m++)
            {
                buffer[m].LigatureId = id;
                buffer[m].LigatureComponent = (byte)Math.Min(k, 255);
            }
        }
        buffer[first].Glyph = ligatureGlyph;
        buffer[first].Cluster = cluster;
        buffer[first].LigatureId = id;
        buffer[first].LigatureComponents = (byte)Math.Min(positions.Length, 255);
        Classify(ref buffer[first]);
        for (var k = positions.Length - 1; k >= 1; k--) buffer.RemoveAt(positions[k]);
    }

    /// <summary>The next glyph after <paramref name="i"/> the lookup does not skip, or -1.</summary>
    private int NextIndex(GlyphBuffer buffer, int i, Lookup lookup)
    {
        for (var j = i + 1; j < buffer.Count; j++)
            if (!Skip(buffer[j], lookup)) return j;
        return -1;
    }

    /// <summary>The previous glyph before <paramref name="i"/> the lookup does not skip, or -1.</summary>
    private int PreviousIndex(GlyphBuffer buffer, int i, Lookup lookup)
    {
        for (var j = i - 1; j >= 0; j--)
            if (!Skip(buffer[j], lookup)) return j;
        return -1;
    }

    // ---- Contextual and chaining contextual lookups (GSUB 5 and 6, GPOS 7 and 8) ----

    /// <summary>
    /// Matches a contextual or chaining contextual subtable at <paramref name="i"/> and runs its nested lookups on the
    /// matched glyphs. Returns where to continue, or null when it does not match.
    /// </summary>
    private int? ApplyContext(GlyphBuffer buffer, int i, Lookup lookup, int sub, byte[] d, bool gpos, int depth)
    {
        if (depth >= MaxNesting) return null;
        var chained = lookup.Type == (gpos ? 8 : 6);
        var format = U16(d, sub);
        var glyph = buffer[i].Glyph;

        if (format == 3)
        {
            // Coverage based: one coverage table per position.
            int backtrack = 0, backtrackAt = 0, input, inputAt, lookahead = 0, lookaheadAt = 0, records, recordsAt;
            if (chained)
            {
                var p = sub + 2;
                backtrack = U16(d, p);
                backtrackAt = p + 2;
                p += 2 + backtrack * 2;
                input = U16(d, p);
                inputAt = p + 2;
                p += 2 + input * 2;
                lookahead = U16(d, p);
                lookaheadAt = p + 2;
                p += 2 + lookahead * 2;
                records = U16(d, p);
                recordsAt = p + 2;
            }
            else
            {
                // A context subtable keeps its record count ahead of the coverage offsets.
                input = U16(d, sub + 2);
                records = U16(d, sub + 4);
                inputAt = sub + 6;
                recordsAt = inputAt + input * 2;
            }
            if (input == 0) return null;
            bool MatchCoverage(int at, int k, ushort g) => Coverage(d, sub + U16(d, at + k * 2), g) >= 0;
            return Match(buffer, i, lookup, input, (k, g) => MatchCoverage(inputAt, k, g),
                backtrack, (k, g) => MatchCoverage(backtrackAt, k, g),
                lookahead, (k, g) => MatchCoverage(lookaheadAt, k, g),
                records, recordsAt, d, gpos, depth, firstMatchedAlready: false);
        }

        var coverage = Coverage(d, sub + U16(d, sub + 2), glyph);
        if (coverage < 0) return null;

        if (format == 1)
        {
            // Glyph based: rule sets by the first glyph's coverage index.
            var setCount = U16(d, sub + 4);
            if (coverage >= setCount) return null;
            var setOffset = U16(d, sub + 6 + coverage * 2);
            if (setOffset == 0) return null;
            var set = sub + setOffset;
            var ruleCount = U16(d, set);
            for (var r = 0; r < ruleCount; r++)
            {
                var rule = set + U16(d, set + 2 + r * 2);
                var next = MatchRule(buffer, i, lookup, rule, d, gpos, depth, chained, static (_, _, value, glyph) => value == glyph);
                if (next is not null) return next;
            }
            return null;
        }

        if (format == 2)
        {
            // Class based: rule sets by the first glyph's class.
            int backtrackClasses = 0, inputClasses, lookaheadClasses = 0, setCountAt;
            if (chained)
            {
                backtrackClasses = sub + U16(d, sub + 4);
                inputClasses = sub + U16(d, sub + 6);
                lookaheadClasses = sub + U16(d, sub + 8);
                setCountAt = sub + 10;
            }
            else
            {
                inputClasses = sub + U16(d, sub + 4);
                setCountAt = sub + 6;
            }
            var firstClass = ClassOf(d, inputClasses, glyph);
            if (firstClass >= U16(d, setCountAt)) return null;
            var setOffset = U16(d, setCountAt + 2 + firstClass * 2);
            if (setOffset == 0) return null;
            var set = sub + setOffset;
            var ruleCount = U16(d, set);
            for (var r = 0; r < ruleCount; r++)
            {
                var rule = set + U16(d, set + 2 + r * 2);
                var classDefs = (backtrackClasses, inputClasses, lookaheadClasses);
                var next = MatchRule(buffer, i, lookup, rule, d, gpos, depth, chained, (part, _, value, g) => ClassOf(d, part switch
                {
                    0 => classDefs.backtrackClasses,
                    1 => classDefs.inputClasses,
                    _ => classDefs.lookaheadClasses,
                }, g) == value);
                if (next is not null) return next;
            }
            return null;
        }
        return null;
    }

    /// <summary>
    /// Matches one rule of a format 1 or 2 subtable: arrays of glyphs or classes for the backtrack, the input after its
    /// first glyph, and the lookahead. <paramref name="test"/> gets the part (0 backtrack, 1 input, 2 lookahead), the
    /// position within it, the rule's value and the glyph.
    /// </summary>
    private int? MatchRule(GlyphBuffer buffer, int i, Lookup lookup, int rule, byte[] d, bool gpos, int depth, bool chained, Func<int, int, int, ushort, bool> test)
    {
        int p = rule;
        int backtrack = 0, backtrackAt = 0;
        if (chained)
        {
            backtrack = U16(d, p);
            backtrackAt = p + 2;
            p += 2 + backtrack * 2;
        }
        int input, inputAt, lookahead = 0, lookaheadAt = 0, records, recordsAt;
        if (chained)
        {
            input = U16(d, p);
            inputAt = p + 2;
            p += 2 + Math.Max(0, input - 1) * 2;
            lookahead = U16(d, p);
            lookaheadAt = p + 2;
            p += 2 + lookahead * 2;
            records = U16(d, p);
            recordsAt = p + 2;
        }
        else
        {
            input = U16(d, p);
            records = U16(d, p + 2);
            inputAt = p + 4;
            recordsAt = inputAt + Math.Max(0, input - 1) * 2;
        }
        if (input == 0) return null;
        return Match(buffer, i, lookup, input, (k, g) => k == 0 || test(1, k, U16(d, inputAt + (k - 1) * 2), g),
            backtrack, (k, g) => test(0, k, U16(d, backtrackAt + k * 2), g),
            lookahead, (k, g) => test(2, k, U16(d, lookaheadAt + k * 2), g),
            records, recordsAt, d, gpos, depth, firstMatchedAlready: true);
    }

    /// <summary>
    /// Matches input, backtrack and lookahead sequences around <paramref name="i"/> (skipping what the lookup ignores),
    /// then runs the nested lookups of the sequence lookup records on the matched input.
    /// </summary>
    private int? Match(GlyphBuffer buffer, int i, Lookup lookup,
        int inputCount, Func<int, ushort, bool> input,
        int backtrackCount, Func<int, ushort, bool> backtrack,
        int lookaheadCount, Func<int, ushort, bool> lookahead,
        int recordCount, int recordsAt, byte[] d, bool gpos, int depth, bool firstMatchedAlready)
    {
        var positions = new int[inputCount];
        positions[0] = i;
        if (!firstMatchedAlready && !input(0, buffer[i].Glyph)) return null;
        for (var k = 1; k < inputCount; k++)
        {
            var next = NextIndex(buffer, positions[k - 1], lookup);
            if (next < 0 || !input(k, buffer[next].Glyph)) return null;
            positions[k] = next;
        }
        var at = i;
        for (var k = 0; k < backtrackCount; k++)
        {
            at = PreviousIndex(buffer, at, lookup);
            if (at < 0 || !backtrack(k, buffer[at].Glyph)) return null;
        }
        at = positions[^1];
        for (var k = 0; k < lookaheadCount; k++)
        {
            at = NextIndex(buffer, at, lookup);
            if (at < 0 || !lookahead(k, buffer[at].Glyph)) return null;
        }

        var end = positions[^1] + 1;
        for (var r = 0; r < recordCount; r++)
        {
            var sequenceIndex = U16(d, recordsAt + r * 4);
            var lookupIndex = U16(d, recordsAt + r * 4 + 2);
            if (sequenceIndex >= positions.Length || lookupIndex >= (gpos ? _gposLookups.Length : _gsubLookups.Length)) continue;
            var position = positions[sequenceIndex];
            if (position >= buffer.Count) continue;
            var nested = GetLookup(gpos, lookupIndex);
            var before = buffer.Count;
            if (gpos) ApplyPositioning(buffer, position, nested, depth + 1);
            else ApplySubstitution(buffer, position, nested, depth + 1);
            var delta = buffer.Count - before;
            if (delta == 0) continue;
            // A ligature or a multiple substitution moved what follows it.
            for (var k = sequenceIndex + 1; k < positions.Length; k++) positions[k] = Math.Max(position, positions[k] + delta);
            end = Math.Max(position + 1, end + delta);
        }
        return Math.Min(end, buffer.Count);
    }

    // ---- Positioning ----

    private int? ApplyPositioning(GlyphBuffer buffer, int i, Lookup lookup, int depth)
    {
        var d = _gpos;
        foreach (var sub in lookup.Subtables)
        {
            var format = U16(d, sub);
            var glyph = buffer[i].Glyph;
            switch (lookup.Type)
            {
                case 1:
                {
                    var index = Coverage(d, sub + U16(d, sub + 2), glyph);
                    if (index < 0) continue;
                    var valueFormat = U16(d, sub + 4);
                    var value = format == 1 ? sub + 6 : sub + 8 + index * ValueSize(valueFormat);
                    if (format == 2 && index >= U16(d, sub + 6)) continue;
                    ApplyValue(d, value, valueFormat, ref buffer[i]);
                    return i + 1;
                }
                case 2:
                {
                    var index = Coverage(d, sub + U16(d, sub + 2), glyph);
                    if (index < 0) continue;
                    var second = NextIndex(buffer, i, lookup);
                    if (second < 0) continue;
                    var format1 = U16(d, sub + 4);
                    var format2 = U16(d, sub + 6);
                    var size1 = ValueSize(format1);
                    var size2 = ValueSize(format2);
                    int record;
                    if (format == 1)
                    {
                        if (index >= U16(d, sub + 8)) continue;
                        var set = sub + U16(d, sub + 10 + index * 2);
                        var count = U16(d, set);
                        var recordSize = 2 + size1 + size2;
                        record = -1;
                        int lo = 0, hi = count - 1;
                        var target = buffer[second].Glyph;
                        while (lo <= hi)
                        {
                            var mid = (lo + hi) >>> 1;
                            var at = set + 2 + mid * recordSize;
                            var g = U16(d, at);
                            if (g == target) { record = at + 2; break; }
                            if (g < target) lo = mid + 1; else hi = mid - 1;
                        }
                        if (record < 0) continue;
                    }
                    else if (format == 2)
                    {
                        var class1 = ClassOf(d, sub + U16(d, sub + 8), glyph);
                        var class2 = ClassOf(d, sub + U16(d, sub + 10), buffer[second].Glyph);
                        var class1Count = U16(d, sub + 12);
                        var class2Count = U16(d, sub + 14);
                        if (class1 >= class1Count || class2 >= class2Count) continue;
                        record = sub + 16 + (class1 * class2Count + class2) * (size1 + size2);
                    }
                    else continue;
                    ApplyValue(d, record, format1, ref buffer[i]);
                    ApplyValue(d, record + size1, format2, ref buffer[second]);
                    return format2 != 0 ? second + 1 : second;
                }
                case 4 or 5 or 6:
                {
                    if (AttachMark(buffer, i, lookup, sub, d)) return i + 1;
                    continue;
                }
                case 7 or 8:
                {
                    var next = ApplyContext(buffer, i, lookup, sub, d, gpos: true, depth);
                    if (next is not null) return next;
                    continue;
                }
            }
        }
        return null;
    }

    private static int ValueSize(ushort format) => 2 * System.Numerics.BitOperations.PopCount((uint)(format & 0xFF));

    private static void ApplyValue(ReadOnlySpan<byte> d, int offset, ushort format, ref ShapingGlyph g)
    {
        var p = offset;
        if ((format & 0x1) != 0) { g.XOffset += S16(d, p); p += 2; }
        if ((format & 0x2) != 0) { g.YOffset += S16(d, p); p += 2; }
        if ((format & 0x4) != 0) { g.XAdvance += S16(d, p); }
        // Vertical advances and device tables do not apply to horizontal text at one size.
    }

    private static (int X, int Y) Anchor(ReadOnlySpan<byte> d, int offset) => (S16(d, offset + 2), S16(d, offset + 4));

    /// <summary>Mark-to-base, mark-to-ligature and mark-to-mark attachment of the mark at <paramref name="i"/>.</summary>
    private bool AttachMark(GlyphBuffer buffer, int i, Lookup lookup, int sub, byte[] d)
    {
        var markIndex = Coverage(d, sub + U16(d, sub + 2), buffer[i].Glyph);
        if (markIndex < 0) return false;
        var classCount = U16(d, sub + 6);
        var markArray = sub + U16(d, sub + 8);
        if (markIndex >= U16(d, markArray)) return false;
        var markClass = U16(d, markArray + 2 + markIndex * 4);
        var markAnchorOffset = U16(d, markArray + 2 + markIndex * 4 + 2);
        if (markClass >= classCount || markAnchorOffset == 0) return false;
        var markAnchor = Anchor(d, markArray + markAnchorOffset);

        int target;
        int anchorOffset;
        int anchorBase;
        switch (lookup.Type)
        {
            case 4:
            {
                // The base is the nearest glyph before the mark that is not a mark.
                target = i - 1;
                while (target >= 0 && (buffer[target].GlyphClass == 3 || buffer[target].Hidden)) target--;
                if (target < 0) return false;
                var baseIndex = Coverage(d, sub + U16(d, sub + 4), buffer[target].Glyph);
                if (baseIndex < 0) return false;
                var baseArray = sub + U16(d, sub + 10);
                if (baseIndex >= U16(d, baseArray)) return false;
                anchorBase = baseArray;
                anchorOffset = U16(d, baseArray + 2 + (baseIndex * classCount + markClass) * 2);
                break;
            }
            case 5:
            {
                target = i - 1;
                while (target >= 0 && (buffer[target].GlyphClass == 3 || buffer[target].Hidden)) target--;
                if (target < 0) return false;
                var ligatureIndex = Coverage(d, sub + U16(d, sub + 4), buffer[target].Glyph);
                if (ligatureIndex < 0) return false;
                var ligatureArray = sub + U16(d, sub + 10);
                if (ligatureIndex >= U16(d, ligatureArray)) return false;
                var attach = ligatureArray + U16(d, ligatureArray + 2 + ligatureIndex * 2);
                var components = U16(d, attach);
                if (components == 0) return false;
                ref var mark = ref buffer[i];
                var component = mark.LigatureId != 0 && mark.LigatureId == buffer[target].LigatureId && mark.LigatureComponent > 0
                    ? Math.Min(mark.LigatureComponent, components) - 1
                    : components - 1;
                anchorBase = attach;
                anchorOffset = U16(d, attach + 2 + (component * classCount + markClass) * 2);
                break;
            }
            default:
            {
                // Mark to mark: the mark just before, unless the lookup skips it.
                target = PreviousIndex(buffer, i, lookup);
                if (target < 0 || buffer[target].GlyphClass != 3) return false;
                var mark2Index = Coverage(d, sub + U16(d, sub + 4), buffer[target].Glyph);
                if (mark2Index < 0) return false;
                var mark2Array = sub + U16(d, sub + 10);
                if (mark2Index >= U16(d, mark2Array)) return false;
                anchorBase = mark2Array;
                anchorOffset = U16(d, mark2Array + 2 + (mark2Index * classCount + markClass) * 2);
                break;
            }
        }
        if (anchorOffset == 0) return false;
        var baseAnchor = Anchor(d, anchorBase + anchorOffset);
        ref var g = ref buffer[i];
        g.AttachTo = target;
        g.AttachDx = baseAnchor.X - markAnchor.X;
        g.AttachDy = baseAnchor.Y - markAnchor.Y;
        g.XOffset = 0;
        g.YOffset = 0;
        return true;
    }
}
