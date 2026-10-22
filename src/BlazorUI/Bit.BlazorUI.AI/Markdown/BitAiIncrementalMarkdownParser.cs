namespace Bit.BlazorUI;

/// <summary>
/// Parses Markdown that arrives a token at a time, reusing the work done for the text already seen.
/// </summary>
/// <remarks>
/// <para>
/// Re-parsing the whole response on every delta is quadratic, and a long answer streamed at forty
/// updates a second makes that obvious. This parser tracks the point beyond which no future text can
/// change the result, keeps the blocks before it, and re-parses only the tail.
/// </para>
/// <para>
/// A stable point is the start of a line that follows a blank line, begins at column zero, and
/// cannot continue a list or quote, and where <see cref="BitAiMarkdownParser"/>, parsing the tail, does
/// start a top-level block. The first test rules out a blank line inside a loose list, which looks
/// like a boundary but is not one. The second leaves every question of context to the parser itself:
/// whether a fence is open, whether it belongs to a list item, whether a <c>$$</c> line opened
/// display math or merely continues a paragraph. A scanner that guessed at those would sooner or
/// later guess differently from the parser and split a block in two.
/// </para>
/// <para>
/// Neither test looks past the candidate line, so a decision never changes as more text arrives, and
/// the blocks before a stable point are kept exactly as the parser produced them.
/// </para>
/// <para>
/// A link reference or footnote definition makes text anywhere depend on text anywhere else, so
/// once a document contains one it is parsed whole on every call. Those documents are rare in chat
/// and the cost is the same as it was before incremental parsing existed.
/// </para>
/// <para>
/// An instance is not thread-safe and belongs to one streaming text part.
/// </para>
/// </remarks>
public sealed class BitAiIncrementalMarkdownParser
{
    private readonly BitAiMarkdownOptions _options;
    private readonly List<BitAiMarkdownBlock> _stable = [];
    private readonly List<int> _blockStarts = [];
    private readonly Queue<int> _candidates = new();
    private readonly HashSet<string> _stableHeadingIds = new(StringComparer.Ordinal);
    private string _source = string.Empty;
    private int _stableLength;
    private int _scanned;
    private int _lastCandidate;
    private bool _hasDefinitions;

    /// <summary>Creates a parser.</summary>
    /// <param name="options">Which extensions to enable.</param>
    public BitAiIncrementalMarkdownParser(BitAiMarkdownOptions? options = null) => _options = options ?? BitAiMarkdownOptions.Default;

    /// <summary>How many characters have been promoted to the stable prefix.</summary>
    /// <remarks>Exposed for diagnostics and tests: it is the measure of how much work is being reused.</remarks>
    public int StableLength => _stableLength;

    /// <summary>How many blocks are in the stable prefix.</summary>
    public int StableBlockCount => _stable.Count;

    /// <summary>
    /// Parses the source as it currently stands.
    /// </summary>
    /// <param name="source">The whole text received so far, not just the new part.</param>
    /// <param name="isComplete"><see langword="true"/> once no more text will arrive.</param>
    /// <returns>The document. A new instance is returned each call; the blocks may be shared.</returns>
    public BitAiMarkdownDocument Parse(string? source, bool isComplete = false)
    {
        source ??= string.Empty;

        if (!source.StartsWith(_source, StringComparison.Ordinal)) Reset();
        _source = source;

        if (_hasDefinitions) return BitAiMarkdownParser.Parse(source, _options, isComplete);

        ScanForCandidates();

        var document = new BitAiMarkdownDocument();
        if (_stableLength < source.Length)
        {
            _blockStarts.Clear();
            var wholeSource = _stableLength == 0;
            var tail = BitAiMarkdownParser.Parse(source[_stableLength..], _options, isComplete, _blockStarts, _stableHeadingIds, out var sawDefinitions);

            if (sawDefinitions)
            {
                // The prefix was parsed without the definition, so it can no longer be trusted.
                _hasDefinitions = true;
                _stable.Clear();
                _candidates.Clear();
                _stableHeadingIds.Clear();
                _stableLength = 0;
                return wholeSource ? tail : BitAiMarkdownParser.Parse(source, _options, isComplete);
            }

            var promoted = PromoteStableBlocks(tail.Blocks);
            document.Blocks.AddRange(_stable);
            document.Blocks.AddRange(tail.Blocks.GetRange(promoted, tail.Blocks.Count - promoted));
        }
        else
        {
            document.Blocks.AddRange(_stable);
        }

        return document;
    }

    /// <summary>Discards all cached state, forcing the next parse to start from scratch.</summary>
    public void Reset()
    {
        _stable.Clear();
        _candidates.Clear();
        _stableHeadingIds.Clear();
        _source = string.Empty;
        _stableLength = 0;
        _scanned = 0;
        _lastCandidate = 0;
        _hasDefinitions = false;
    }

    // Queues the start of each line that could begin a stable block, looking at each line once.
    private void ScanForCandidates()
    {
        var source = _source;
        var i = _scanned;

        while (i < source.Length)
        {
            var lineEnd = source.IndexOf('\n', i);
            if (lineEnd < 0) break;

            var next = lineEnd + 1;
            if (source.AsSpan(i, lineEnd - i).IsWhiteSpace())
            {
                // Undecided means the next line has not arrived yet. Stop here and look at this
                // blank line again next time; moving past it would lose the boundary for good, and
                // with text arriving a token at a time that would be almost every boundary.
                var safe = IsSafeAfterBlank(source, next, out var lineStart);
                if (safe is null) break;

                if (safe.Value && lineStart > _lastCandidate)
                {
                    _candidates.Enqueue(lineStart);
                    _lastCandidate = lineStart;
                }
            }

            i = next;
        }

        _scanned = i;
    }

    /// <summary>
    /// Settles every queued candidate against the parse of the tail and moves the blocks before the
    /// last one that holds into the stable prefix.
    /// </summary>
    /// <returns>How many of the tail's blocks were promoted.</returns>
    private int PromoteStableBlocks(List<BitAiMarkdownBlock> tail)
    {
        var promoted = 0;
        var boundary = 0;
        var block = 0;

        // Each start must name a block; if they ever disagree, reuse nothing rather than the wrong thing.
        if (_blockStarts.Count != tail.Count) _candidates.Clear();

        // Both sequences ascend, so one merge decides them all. A candidate the parser did not start
        // a block on is inside something, such as an unclosed fence, and will stay inside it.
        while (_candidates.TryDequeue(out var candidate))
        {
            var offset = candidate - _stableLength;
            while (block < _blockStarts.Count && _blockStarts[block] < offset) block++;
            if (block < _blockStarts.Count && _blockStarts[block] == offset)
            {
                promoted = block;
                boundary = offset;
            }
        }

        if (promoted == 0) return 0;

        var blocks = tail.GetRange(0, promoted);
        _stable.AddRange(blocks);
        _stableLength += boundary;
        CollectHeadingIds(blocks);
        return promoted;
    }

    // The tail is parsed knowing which identifiers the prefix took, so "## A" twice over gets
    // "-2" on the second heading exactly as a one-shot parse would give it.
    private void CollectHeadingIds(List<BitAiMarkdownBlock> blocks)
    {
        foreach (var block in blocks)
        {
            switch (block)
            {
                case BitAiHeadingBlock { Id: { } id }: _stableHeadingIds.Add(id); break;
                case BitAiQuoteBlock quote: CollectHeadingIds(quote.Blocks); break;
                case BitAiListBlock list: foreach (var item in list.Items) CollectHeadingIds(item.Blocks); break;
            }
        }
    }

    /// <returns>
    /// Whether the first non-blank line after a blank one, which starts at
    /// <paramref name="lineStart"/>, is one no earlier block can claim, or <see langword="null"/>
    /// when too little of it has arrived to tell.
    /// </returns>
    private static bool? IsSafeAfterBlank(string source, int position, out int lineStart)
    {
        for (var i = position; i < source.Length;)
        {
            var lineEnd = source.IndexOf('\n', i);
            var complete = lineEnd >= 0;

            lineStart = i;
            var line = source.AsSpan(i, (complete ? lineEnd : source.Length) - i).TrimEnd('\r');
            if (line.IsWhiteSpace())
            {
                if (!complete) return null;
                i = lineEnd + 1;
                continue;
            }

            if (line[0] is ' ' or '\t' or '>' or '|') return false;
            if (StartsListItem(line)) return false;

            // The first character usually settles it, but "1" or "-" may yet become a list marker.
            if (!complete && (char.IsAsciiDigit(line[0]) || line[0] is '-' or '*' or '+')) return null;
            return true;
        }

        lineStart = position;
        return null;
    }

    private static bool StartsListItem(ReadOnlySpan<char> line)
    {
        if (line.Length >= 2 && line[0] is '-' or '*' or '+' && line[1] is ' ' or '\t') return true;

        var digits = 0;
        while (digits < line.Length && char.IsAsciiDigit(line[digits])) digits++;
        return digits > 0
            && digits + 1 < line.Length
            && line[digits] is '.' or ')'
            && line[digits + 1] is ' ' or '\t';
    }
}
