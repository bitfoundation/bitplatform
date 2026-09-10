using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Drives inline parsing for a single block of text. It scans the text, delegating
/// to the pipeline's <see cref="BitMarkdownInlineParser"/>s at their trigger characters and
/// collecting delimiter runs, then resolves the delimiters into emphasis-like nodes
/// via the pipeline's <see cref="BitMarkdownDelimiterProcessor"/>s.
/// </summary>
public sealed class BitMarkdownInlineProcessor
{
    private readonly StringBuilder _literal = new();
    private readonly List<Tok> _tokens = new();
    private Dictionary<(Type Parser, int Variant), int>? _noMatchFrom;
    // Set while a parser is being offered a position it may not take, so the pending text run can
    // be put back if it does not. See TryParsers.
    private bool _speculating;
    private int _literalMark;
    private string? _literalBackup;

    internal BitMarkdownInlineProcessor(BitMarkdownPipeline pipeline)
        : this(pipeline, BitMarkdownParseContext.Empty, 0)
    {
    }

    internal BitMarkdownInlineProcessor(BitMarkdownPipeline pipeline, BitMarkdownParseContext context, int depth)
    {
        Pipeline = pipeline;
        Context = context;
        Depth = depth;
    }

    /// <summary>The owning pipeline.</summary>
    public BitMarkdownPipeline Pipeline { get; }

    /// <summary>The state shared by every parser taking part in this parse.</summary>
    internal BitMarkdownParseContext Context { get; }

    /// <summary>The safety limits in effect for this parse.</summary>
    internal BitMarkdownParseOptions Options => Context.Options;

    /// <summary>
    /// True when the document declares a <c>[label]:</c> definition line for the supplied
    /// (already normalized) label. Inline parsers consult this before turning bracketed
    /// text into a reference, since definitions may appear after their references.
    /// </summary>
    public bool HasReferenceLabel(string normalizedLabel) => Context.ReferenceLabels.Contains(normalizedLabel);

    /// <summary>The current nesting depth of this processor within the document.</summary>
    internal int Depth { get; }

    /// <summary>The text currently being parsed.</summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>The current scan position within <see cref="Text"/>.</summary>
    public int Pos { get; set; }

    /// <summary>Parses inline content from the supplied text.</summary>
    public List<BitMarkdownNode> Parse(string text)
    {
        Text = text;
        Pos = 0;
        _literal.Clear();
        _tokens.Clear();
        _noMatchFrom?.Clear();
        _speculating = false;
        _literalMark = 0;
        _literalBackup = null;
        Scan();
        BitMarkdownDelimiterResolver.Process(_tokens, Pipeline);
        return ToNodes(_tokens);
    }

    /// <summary>Parses inline content in an isolated child processor (e.g. for a link label).</summary>
    public List<BitMarkdownNode> ParseInlines(string text) => Pipeline.ParseInlines(text, Context, Depth + 1);

    // -- API used by inline parsers ----------------------------------------

    /// <summary>Appends a literal character to the pending text run.</summary>
    public void AppendChar(char c) => _literal.Append(c);

    /// <summary>Appends literal text to the pending text run.</summary>
    public void AppendText(string s) => _literal.Append(s);

    /// <summary>Flushes pending text and appends a resolved node.</summary>
    public void AppendNode(BitMarkdownNode node)
    {
        Flush();
        _tokens.Add(new Tok { Kind = TokKind.Node, Node = node });
    }

    /// <summary>
    /// Records that <paramref name="parser"/> has scanned from <paramref name="position"/> to the
    /// end of <see cref="Text"/> without finding what it needs to close its construct, so a later
    /// attempt starting further along cannot find one either.
    /// </summary>
    /// <remarks>
    /// This is what keeps a paragraph full of trigger characters that never close - a page of
    /// prices, each with a <c>$</c> - linear instead of quadratic: without it every one of them
    /// rescans the whole remainder of the paragraph. Only a parser whose closing delimiter is
    /// valid or not on its own terms, independently of where the scan began, may say so; the memo
    /// lives on this processor, which is created per block, so it is per-parse state and does not
    /// make the parser itself stateful. <paramref name="variant"/> separates the shapes a parser
    /// looks for when a failure to find one says nothing about the others - for mathematics, the
    /// one- and two-character delimiters.
    /// </remarks>
    public void SetNoMatchFrom(BitMarkdownInlineParser parser, int variant, int position)
    {
        var key = (parser.GetType(), variant);
        _noMatchFrom ??= new Dictionary<(Type, int), int>();
        // The earliest such position is the strongest statement, and it implies every later one.
        if (_noMatchFrom.TryGetValue(key, out int known) is false || position < known)
            _noMatchFrom[key] = position;
    }

    /// <summary>
    /// True when <see cref="SetNoMatchFrom"/> has already ruled out a match from
    /// <paramref name="position"/> onwards, so this attempt can fail without scanning.
    /// </summary>
    public bool HasNoMatchFrom(BitMarkdownInlineParser parser, int variant, int position)
        => _noMatchFrom is not null
           && _noMatchFrom.TryGetValue((parser.GetType(), variant), out int known)
           && position >= known;

    /// <summary>Removes trailing spaces from the pending text run and returns how many were removed.</summary>
    public int TrimPendingTrailingSpaces()
    {
        BackUpPendingText();

        int removed = 0;
        while (_literal.Length > 0 && _literal[^1] == ' ')
        {
            _literal.Length--;
            removed++;
        }
        return removed;
    }

    private void Flush()
    {
        if (_literal.Length > 0)
        {
            BackUpPendingText();
            _tokens.Add(new Tok { Kind = TokKind.Text, Text = _literal.ToString() });
            _literal.Clear();
        }
    }

    // Keeps a copy of the pending text run before something about to happen removes part of it, so
    // a speculative parse that then fails can be undone. Taken here, at the moment the run is
    // actually disturbed, rather than before every attempt: the run grows with the text between one
    // node and the next, and copying it at each trigger character is what made a paragraph of
    // prices - a "$" every few words, none of them opening any mathematics - quadratic in its own
    // length. Appends need no copy at all; the mark the attempt took is enough to trim them back.
    private void BackUpPendingText()
    {
        if (_speculating is false || _literalBackup is not null) return;

        // Only what was pending before this attempt began. Anything the parser appended on its way
        // here is undone by the rollback in any case, and copying it too would put it back.
        _literalBackup = _literal.ToString(0, Math.Min(_literalMark, _literal.Length));
    }

    // -- Scanning -----------------------------------------------------------

    private void Scan()
    {
        int n = Text.Length;
        while (Pos < n)
        {
            char c = Text[Pos];

            // Delimiter runs (emphasis-like) are collected for later resolution.
            if (Pipeline.DelimiterChars.Contains(c))
            {
                int run = BitMarkdownInlineHelpers.CountRun(Text, Pos, c);
                char prev = Pos > 0 ? Text[Pos - 1] : '\0';
                char next = Pos + run < n ? Text[Pos + run] : '\0';
                ComputeFlanking(prev, next, out bool left, out bool right);
                var (canOpen, canClose) = Pipeline.DelimiterByChar[c].GetFlanking(c, left, right, prev, next);

                Flush();
                _tokens.Add(new Tok
                {
                    Kind = TokKind.Delim,
                    DelimChar = c,
                    Count = run,
                    CanOpen = canOpen,
                    CanClose = canClose
                });
                Pos += run;
                continue;
            }

            // Trigger-based inline parsers.
            if (Pipeline.InlineParsersByChar.TryGetValue(c, out var parsers))
            {
                if (TryParsers(parsers)) continue;
            }

            _literal.Append(c);
            Pos++;
        }
        Flush();
    }

    // Offers the current position to each parser registered for the character there, in order,
    // until one both succeeds and consumes input. Returns whether one did.
    private bool TryParsers(IReadOnlyList<BitMarkdownInlineParser> parsers)
    {
        int save = Pos;
        int tokenCount = _tokens.Count;
        int literalMark = _literal.Length;

        bool wasSpeculating = _speculating;
        int outerMark = _literalMark;
        string? outerBackup = _literalBackup;
        _speculating = true;
        _literalMark = literalMark;
        _literalBackup = null;

        try
        {
            foreach (var p in parsers)
            {
                if (p.TryParse(this) && Pos > save) return true;

                // Roll back every side effect (position, flushed tokens and pending text), not
                // just Pos, so a failed or no-op parser can't corrupt the token stream for the
                // parsers tried next or the normal scan. A parser that only appended to the
                // pending run is undone by trimming it back to the mark; one that flushed or
                // trimmed it left a copy behind to restore from.
                Pos = save;
                if (_tokens.Count > tokenCount) _tokens.RemoveRange(tokenCount, _tokens.Count - tokenCount);
                if (_literalBackup is null)
                {
                    _literal.Length = literalMark;
                }
                else
                {
                    _literal.Clear();
                    _literal.Append(_literalBackup);
                    _literalBackup = null;
                }
            }

            return false;
        }
        finally
        {
            // A parser may parse inline content of its own, which runs another scan through this
            // same method; the outer attempt's mark and copy are its own to keep.
            _speculating = wasSpeculating;
            _literalMark = outerMark;
            _literalBackup = outerBackup;
        }
    }

    private static void ComputeFlanking(char prev, char next, out bool leftFlanking, out bool rightFlanking)
    {
        bool nextWhitespace = next == '\0' || char.IsWhiteSpace(next);
        bool prevWhitespace = prev == '\0' || char.IsWhiteSpace(prev);
        bool nextPunct = next != '\0' && BitMarkdownInlineHelpers.IsPunctuation(next);
        bool prevPunct = prev != '\0' && BitMarkdownInlineHelpers.IsPunctuation(prev);

        leftFlanking = !nextWhitespace && (!nextPunct || prevWhitespace || prevPunct);
        rightFlanking = !prevWhitespace && (!prevPunct || nextWhitespace || nextPunct);
    }

    // -- Token model --------------------------------------------------------

    internal enum TokKind { Text, Node, Delim }

    internal sealed class Tok
    {
        public TokKind Kind;
        public string Text = string.Empty;
        public BitMarkdownNode? Node;

        public char DelimChar;
        public int Count;
        public bool CanOpen;
        public bool CanClose;
        public bool Active = true;
    }

    internal static List<BitMarkdownNode> ToNodes(List<Tok> tokens)
    {
        var result = new List<BitMarkdownNode>();
        foreach (var t in tokens)
        {
            switch (t.Kind)
            {
                case TokKind.Text:
                    if (t.Text.Length > 0) result.Add(new BitMarkdownTextNode(t.Text));
                    break;
                case TokKind.Node:
                    if (t.Node is not null) result.Add(t.Node);
                    break;
                case TokKind.Delim:
                    if (t.Count > 0) result.Add(new BitMarkdownTextNode(new string(t.DelimChar, t.Count)));
                    break;
            }
        }
        return result;
    }
}
