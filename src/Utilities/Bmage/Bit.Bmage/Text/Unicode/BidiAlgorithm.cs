namespace Bit.Bmage.Text.Unicode;

/// <summary>
/// The Unicode Bidirectional Algorithm (UAX #9) for one paragraph: the embedding level of every character, from which
/// a line's visual order follows. Explicit embeddings, overrides and isolates, bracket pairs and the line rules are all
/// implemented; the tests run Unicode's own BidiTest.txt and BidiCharacterTest.txt against it.
/// </summary>
/// <remarks>
/// Characters removed by rule X9 (the embedding controls and boundary neutrals) are given the level of the character
/// before them, so every character has a level; <see cref="IsRemovedByX9"/> says which they are.
/// </remarks>
internal sealed class BidiParagraph
{
    /// <summary>The deepest explicit embedding the algorithm tracks (BD2).</summary>
    public const int MaxDepth = 125;

    private readonly BidiClass[] _initial;
    private readonly int[]? _codePoints;
    private readonly BidiClass[] _types;
    private readonly byte[] _levels;
    private readonly int[] _matchingPdi;
    private readonly int[] _matchingInitiator;

    /// <summary>The paragraph's embedding level: 0 left to right, 1 right to left.</summary>
    public byte ParagraphLevel { get; }

    /// <summary>The resolved level of each character, before the line rules.</summary>
    public ReadOnlySpan<byte> Levels => _levels;

    /// <summary>Resolves a paragraph of code points. <paramref name="direction"/>: -1 from its text, 0 left to right, 1 right to left.</summary>
    public static BidiParagraph Create(ReadOnlySpan<int> codePoints, int direction = -1)
    {
        var types = new BidiClass[codePoints.Length];
        for (var i = 0; i < types.Length; i++) types[i] = UnicodeData.GetBidiClass(codePoints[i]);
        return new BidiParagraph(types, codePoints.ToArray(), direction);
    }

    /// <summary>Resolves a paragraph given its characters' classes; with no code points, no brackets pair (N0).</summary>
    public BidiParagraph(BidiClass[] types, int[]? codePoints, int direction)
    {
        _initial = types;
        _codePoints = codePoints;
        _types = (BidiClass[])types.Clone();
        _levels = new byte[types.Length];
        _matchingPdi = new int[types.Length];
        _matchingInitiator = new int[types.Length];

        MatchIsolates();
        ParagraphLevel = direction is 0 or 1 ? (byte)direction : FirstStrongLevel(0, types.Length) ?? 0;
        ResolveExplicitLevels();
        foreach (var sequence in IsolatingRunSequences()) sequence.Resolve();
        AssignLevelsToRemovedCharacters();
    }

    /// <summary>Whether rule X9 removes the character: an embedding or override control, PDF, or a boundary neutral.</summary>
    public static bool IsRemovedByX9(BidiClass type) => type is BidiClass.LRE or BidiClass.RLE or BidiClass.LRO or BidiClass.RLO or BidiClass.PDF or BidiClass.BN;

    /// <summary>
    /// The levels of the characters from <paramref name="start"/> (inclusive) to <paramref name="end"/> (exclusive)
    /// laid out as one line, after rule L1: separators, and whitespace before them and at the end of the line, take the
    /// paragraph level.
    /// </summary>
    public byte[] GetLineLevels(int start, int end)
    {
        var levels = _levels[start..end];
        var trailing = true;
        for (var i = levels.Length - 1; i >= 0; i--)
        {
            var type = _initial[start + i];
            if (type is BidiClass.B or BidiClass.S)
            {
                levels[i] = ParagraphLevel;
                trailing = true;
            }
            else if (trailing && IsWhitespaceForL1(type))
            {
                levels[i] = ParagraphLevel;
            }
            else
            {
                trailing = false;
            }
        }
        return levels;
    }

    /// <summary>
    /// The visual order of a line's characters, given their levels (rule L2): the logical index of the character at
    /// each position from left to right.
    /// </summary>
    public static int[] GetVisualOrder(ReadOnlySpan<byte> levels)
    {
        var order = new int[levels.Length];
        for (var i = 0; i < order.Length; i++) order[i] = i;
        if (levels.Length == 0) return order;
        byte highest = 0, lowestOdd = byte.MaxValue;
        foreach (var level in levels)
        {
            if (level > highest) highest = level;
            if ((level & 1) == 1 && level < lowestOdd) lowestOdd = level;
        }
        // Reversing a run of levels at or above one threshold leaves every lower threshold's runs where they were, so
        // the runs can be found in the logical levels throughout.
        for (int level = highest; level >= lowestOdd && level > 0; level--)
        {
            for (var i = 0; i < levels.Length;)
            {
                if (levels[i] < level) { i++; continue; }
                var runStart = i;
                while (i < levels.Length && levels[i] >= level) i++;
                Array.Reverse(order, runStart, i - runStart);
            }
        }
        return order;
    }

    private static bool IsWhitespaceForL1(BidiClass type) => type is BidiClass.WS or BidiClass.LRI or BidiClass.RLI or BidiClass.FSI or BidiClass.PDI
        || IsRemovedByX9(type);

    // ---- BD9: isolate initiators and their PDIs ----

    private void MatchIsolates()
    {
        Array.Fill(_matchingPdi, -1);
        Array.Fill(_matchingInitiator, -1);
        var stack = new Stack<int>();
        for (var i = 0; i < _initial.Length; i++)
        {
            switch (_initial[i])
            {
                case BidiClass.LRI or BidiClass.RLI or BidiClass.FSI:
                    stack.Push(i);
                    break;
                case BidiClass.PDI when stack.Count > 0:
                    var initiator = stack.Pop();
                    _matchingPdi[initiator] = i;
                    _matchingInitiator[i] = initiator;
                    break;
                case BidiClass.B:
                    stack.Clear();
                    break;
            }
        }
    }

    /// <summary>P2, P3: the level of the first strong character, skipping isolates; null when there is none.</summary>
    private byte? FirstStrongLevel(int start, int end)
    {
        for (var i = start; i < end; i++)
        {
            switch (_initial[i])
            {
                case BidiClass.L:
                    return 0;
                case BidiClass.R or BidiClass.AL:
                    return 1;
                case BidiClass.LRI or BidiClass.RLI or BidiClass.FSI:
                    if (_matchingPdi[i] < 0) return null;
                    i = _matchingPdi[i];
                    break;
                case BidiClass.B:
                    return null;
            }
        }
        return null;
    }

    // ---- X1-X8: explicit levels and directions ----

    private readonly record struct Status(byte Level, BidiClass Override, bool Isolate);

    private void ResolveExplicitLevels()
    {
        var stack = new Status[MaxDepth + 2];
        var depth = 0;
        stack[depth++] = new Status(ParagraphLevel, BidiClass.ON, false);
        int overflowIsolates = 0, overflowEmbeddings = 0, validIsolates = 0;

        for (var i = 0; i < _types.Length; i++)
        {
            var type = _initial[i];
            var top = stack[depth - 1];
            switch (type)
            {
                case BidiClass.RLE or BidiClass.LRE or BidiClass.RLO or BidiClass.LRO:
                {
                    var rtl = type is BidiClass.RLE or BidiClass.RLO;
                    var level = (byte)(rtl ? (top.Level + 1) | 1 : (top.Level + 2) & ~1);
                    _levels[i] = top.Level;
                    if (level <= MaxDepth && overflowIsolates == 0 && overflowEmbeddings == 0)
                    {
                        var direction = type switch { BidiClass.RLO => BidiClass.R, BidiClass.LRO => BidiClass.L, _ => BidiClass.ON };
                        stack[depth++] = new Status(level, direction, false);
                        _levels[i] = level;
                    }
                    else if (overflowIsolates == 0)
                    {
                        overflowEmbeddings++;
                    }
                    break;
                }
                case BidiClass.RLI or BidiClass.LRI or BidiClass.FSI:
                {
                    _levels[i] = top.Level;
                    if (top.Override != BidiClass.ON) _types[i] = top.Override;
                    var rtl = type == BidiClass.RLI
                        || (type == BidiClass.FSI && FirstStrongLevel(i + 1, _matchingPdi[i] < 0 ? _types.Length : _matchingPdi[i]) == 1);
                    var level = (byte)(rtl ? (top.Level + 1) | 1 : (top.Level + 2) & ~1);
                    if (level <= MaxDepth && overflowIsolates == 0 && overflowEmbeddings == 0)
                    {
                        validIsolates++;
                        stack[depth++] = new Status(level, BidiClass.ON, true);
                    }
                    else
                    {
                        overflowIsolates++;
                    }
                    break;
                }
                case BidiClass.PDI:
                {
                    if (overflowIsolates > 0)
                    {
                        overflowIsolates--;
                    }
                    else if (validIsolates > 0)
                    {
                        overflowEmbeddings = 0;
                        while (!stack[depth - 1].Isolate) depth--;
                        depth--;
                        validIsolates--;
                    }
                    top = stack[depth - 1];
                    _levels[i] = top.Level;
                    if (top.Override != BidiClass.ON) _types[i] = top.Override;
                    break;
                }
                case BidiClass.PDF:
                {
                    _levels[i] = top.Level;
                    if (overflowIsolates > 0)
                    {
                    }
                    else if (overflowEmbeddings > 0)
                    {
                        overflowEmbeddings--;
                    }
                    else if (!top.Isolate && depth >= 2)
                    {
                        depth--;
                    }
                    break;
                }
                case BidiClass.B:
                    _levels[i] = ParagraphLevel;
                    break;
                case BidiClass.BN:
                    _levels[i] = top.Level;
                    break;
                default:
                    _levels[i] = top.Level;
                    if (top.Override != BidiClass.ON) _types[i] = top.Override;
                    break;
            }
        }
    }

    // ---- X10: level runs and isolating run sequences ----

    private List<IsolatingRunSequence> IsolatingRunSequences()
    {
        // Level runs over the characters X9 keeps.
        var runs = new List<List<int>>();
        List<int>? current = null;
        var currentLevel = -1;
        for (var i = 0; i < _types.Length; i++)
        {
            if (IsRemovedByX9(_initial[i])) continue;
            if (current is null || _levels[i] != currentLevel)
            {
                current = [];
                runs.Add(current);
                currentLevel = _levels[i];
            }
            current.Add(i);
        }

        var runOf = new Dictionary<int, int>();
        for (var r = 0; r < runs.Count; r++) runOf[runs[r][0]] = r;

        var sequences = new List<IsolatingRunSequence>();
        foreach (var run in runs)
        {
            // A run that starts with a PDI matching an initiator continues that initiator's sequence.
            var first = run[0];
            if (_initial[first] == BidiClass.PDI && _matchingInitiator[first] >= 0) continue;
            var indexes = new List<int>();
            var r = run;
            while (true)
            {
                indexes.AddRange(r);
                var last = r[^1];
                if (_initial[last] is not (BidiClass.LRI or BidiClass.RLI or BidiClass.FSI) || _matchingPdi[last] < 0) break;
                var pdi = _matchingPdi[last];
                if (!runOf.TryGetValue(pdi, out var next)) break;
                r = runs[next];
            }
            sequences.Add(new IsolatingRunSequence(this, [.. indexes]));
        }
        return sequences;
    }

    private void AssignLevelsToRemovedCharacters()
    {
        for (var i = 0; i < _levels.Length; i++)
        {
            if (!IsRemovedByX9(_initial[i])) continue;
            _levels[i] = i == 0 ? ParagraphLevel : _levels[i - 1];
        }
    }

    private static BidiClass DirectionOf(int level) => (level & 1) == 0 ? BidiClass.L : BidiClass.R;

    /// <summary>One isolating run sequence (BD13), resolved by rules W1-W7, N0-N2 and I1-I2.</summary>
    private sealed class IsolatingRunSequence
    {
        private readonly BidiParagraph _p;
        private readonly int[] _indexes;
        private readonly BidiClass[] _types;
        private readonly byte _level;
        private readonly BidiClass _sos, _eos;

        public IsolatingRunSequence(BidiParagraph paragraph, int[] indexes)
        {
            _p = paragraph;
            _indexes = indexes;
            _types = new BidiClass[indexes.Length];
            for (var i = 0; i < indexes.Length; i++) _types[i] = paragraph._types[indexes[i]];
            _level = paragraph._levels[indexes[0]];

            var before = indexes[0] - 1;
            while (before >= 0 && IsRemovedByX9(paragraph._initial[before])) before--;
            var previousLevel = before >= 0 ? paragraph._levels[before] : paragraph.ParagraphLevel;
            _sos = DirectionOf(Math.Max(previousLevel, _level));

            var last = indexes[^1];
            int nextLevel;
            if (paragraph._initial[last] is BidiClass.LRI or BidiClass.RLI or BidiClass.FSI)
            {
                nextLevel = paragraph.ParagraphLevel;
            }
            else
            {
                var after = last + 1;
                while (after < paragraph._types.Length && IsRemovedByX9(paragraph._initial[after])) after++;
                nextLevel = after < paragraph._types.Length ? paragraph._levels[after] : paragraph.ParagraphLevel;
            }
            _eos = DirectionOf(Math.Max(nextLevel, _level));
        }

        public void Resolve()
        {
            ResolveWeakTypes();
            ResolvePairedBrackets();
            ResolveNeutralTypes();
            ResolveImplicitLevels();
        }

        private static bool IsIsolateControl(BidiClass t) => t is BidiClass.LRI or BidiClass.RLI or BidiClass.FSI or BidiClass.PDI;

        private static bool IsNeutralOrIsolate(BidiClass t) => t is BidiClass.B or BidiClass.S or BidiClass.WS or BidiClass.ON || IsIsolateControl(t);

        private void ResolveWeakTypes()
        {
            var n = _types.Length;

            // W1: a nonspacing mark takes the type of what it follows - other neutral after an isolate control.
            for (var i = 0; i < n; i++)
            {
                if (_types[i] != BidiClass.NSM) continue;
                if (i == 0) _types[i] = _sos;
                else _types[i] = IsIsolateControl(_types[i - 1]) ? BidiClass.ON : _types[i - 1];
            }

            // W2: European numbers after Arabic letters are Arabic numbers.
            var lastStrong = _sos;
            for (var i = 0; i < n; i++)
            {
                var t = _types[i];
                if (t is BidiClass.L or BidiClass.R or BidiClass.AL) lastStrong = t;
                else if (t == BidiClass.EN && lastStrong == BidiClass.AL) _types[i] = BidiClass.AN;
            }

            // W3: Arabic letters are right to left.
            for (var i = 0; i < n; i++) if (_types[i] == BidiClass.AL) _types[i] = BidiClass.R;

            // W4: a single separator between two numbers of a kind joins them.
            for (var i = 1; i < n - 1; i++)
            {
                var t = _types[i];
                if (t == BidiClass.ES && _types[i - 1] == BidiClass.EN && _types[i + 1] == BidiClass.EN) _types[i] = BidiClass.EN;
                else if (t == BidiClass.CS && _types[i - 1] == BidiClass.EN && _types[i + 1] == BidiClass.EN) _types[i] = BidiClass.EN;
                else if (t == BidiClass.CS && _types[i - 1] == BidiClass.AN && _types[i + 1] == BidiClass.AN) _types[i] = BidiClass.AN;
            }

            // W5: terminators next to European numbers are European numbers.
            for (var i = 0; i < n; i++)
            {
                if (_types[i] != BidiClass.ET) continue;
                var runStart = i;
                var runEnd = i;
                while (runEnd < n && _types[runEnd] == BidiClass.ET) runEnd++;
                var adjacent = (runStart > 0 && _types[runStart - 1] == BidiClass.EN) || (runEnd < n && _types[runEnd] == BidiClass.EN);
                if (adjacent) for (var k = runStart; k < runEnd; k++) _types[k] = BidiClass.EN;
                i = runEnd - 1;
            }

            // W6: remaining separators and terminators are other neutrals.
            for (var i = 0; i < n; i++)
                if (_types[i] is BidiClass.ES or BidiClass.ET or BidiClass.CS) _types[i] = BidiClass.ON;

            // W7: European numbers in left-to-right context are left to right.
            lastStrong = _sos;
            for (var i = 0; i < n; i++)
            {
                var t = _types[i];
                if (t is BidiClass.L or BidiClass.R) lastStrong = t;
                else if (t == BidiClass.EN && lastStrong == BidiClass.L) _types[i] = BidiClass.L;
            }
        }

        // ---- N0: paired brackets ----

        private void ResolvePairedBrackets()
        {
            if (_p._codePoints is not { } codePoints) return;
            var pairs = LocateBracketPairs(codePoints);
            if (pairs.Count == 0) return;
            var embedding = DirectionOf(_level);
            foreach (var (open, close) in pairs)
            {
                var inside = StrongInside(open, close, embedding);
                if (inside is null) continue;
                BidiClass direction;
                if (inside == embedding)
                {
                    direction = embedding;
                }
                else
                {
                    // Only the opposite direction inside: it wins if the context before the bracket agrees.
                    var before = StrongBefore(open);
                    direction = before == inside ? inside.Value : embedding;
                }
                SetBracket(open, direction);
                SetBracket(close, direction);
            }
        }

        /// <summary>BD16: the bracket pairs of the sequence, by the position of their opening bracket.</summary>
        private List<(int Open, int Close)> LocateBracketPairs(int[] codePoints)
        {
            const int StackSize = 63;
            var stack = new List<(int Pair, int Position)>();
            var pairs = new List<(int, int)>();
            for (var i = 0; i < _indexes.Length; i++)
            {
                if (_types[i] != BidiClass.ON) continue;
                var cp = codePoints[_indexes[i]];
                if (!UnicodeData.TryGetBracket(cp, out var pair, out var opening)) continue;
                if (opening)
                {
                    if (stack.Count == StackSize) break;
                    stack.Add((Canonical(pair), i));
                }
                else
                {
                    var closer = Canonical(cp);
                    for (var s = stack.Count - 1; s >= 0; s--)
                    {
                        if (stack[s].Pair != closer) continue;
                        pairs.Add((stack[s].Position, i));
                        stack.RemoveRange(s, stack.Count - s);
                        break;
                    }
                }
            }
            pairs.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            return pairs;

            // The angle brackets U+2329 and U+232A are canonically equivalent to U+3008 and U+3009.
            static int Canonical(int c) => c switch { 0x2329 => 0x3008, 0x232A => 0x3009, _ => c };
        }

        private static BidiClass? StrongOf(BidiClass t) => t switch
        {
            BidiClass.L => BidiClass.L,
            BidiClass.R or BidiClass.AL or BidiClass.EN or BidiClass.AN => BidiClass.R,
            _ => null,
        };

        private BidiClass? StrongInside(int open, int close, BidiClass embedding)
        {
            BidiClass? opposite = null;
            for (var i = open + 1; i < close; i++)
            {
                if (StrongOf(_types[i]) is not { } strong) continue;
                if (strong == embedding) return embedding;
                opposite = strong;
            }
            return opposite;
        }

        private BidiClass StrongBefore(int open)
        {
            for (var i = open - 1; i >= 0; i--)
                if (StrongOf(_types[i]) is { } strong) return strong;
            return _sos;
        }

        private void SetBracket(int position, BidiClass direction)
        {
            _types[position] = direction;
            // Nonspacing marks after a bracket take its new direction too.
            for (var i = position + 1; i < _types.Length && _p._initial[_indexes[i]] == BidiClass.NSM; i++) _types[i] = direction;
        }

        // ---- N1, N2: neutrals ----

        private void ResolveNeutralTypes()
        {
            var n = _types.Length;
            var embedding = DirectionOf(_level);
            for (var i = 0; i < n; i++)
            {
                if (!IsNeutralOrIsolate(_types[i])) continue;
                var runStart = i;
                var runEnd = i;
                while (runEnd < n && IsNeutralOrIsolate(_types[runEnd])) runEnd++;
                var before = runStart == 0 ? _sos : AsStrong(_types[runStart - 1]);
                var after = runEnd == n ? _eos : AsStrong(_types[runEnd]);
                var resolved = before == after ? before : embedding;
                for (var k = runStart; k < runEnd; k++) _types[k] = resolved;
                i = runEnd - 1;
            }

            // European and Arabic numbers count as right to left for the neutrals around them.
            static BidiClass AsStrong(BidiClass t) => t is BidiClass.EN or BidiClass.AN or BidiClass.R ? BidiClass.R : t;
        }

        // ---- I1, I2: implicit levels ----

        private void ResolveImplicitLevels()
        {
            for (var i = 0; i < _indexes.Length; i++)
            {
                var t = _types[i];
                var level = _p._levels[_indexes[i]];
                if ((level & 1) == 0)
                {
                    if (t == BidiClass.R) level += 1;
                    else if (t is BidiClass.AN or BidiClass.EN) level += 2;
                }
                else if (t is BidiClass.L or BidiClass.EN or BidiClass.AN)
                {
                    level += 1;
                }
                _p._levels[_indexes[i]] = level;
            }
        }
    }
}
