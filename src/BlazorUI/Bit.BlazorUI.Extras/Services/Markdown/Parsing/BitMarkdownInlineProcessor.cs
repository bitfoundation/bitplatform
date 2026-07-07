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

    internal BitMarkdownInlineProcessor(BitMarkdownPipeline pipeline)
        : this(pipeline, BitMarkdownParseOptions.Default, 0)
    {
    }

    internal BitMarkdownInlineProcessor(BitMarkdownPipeline pipeline, BitMarkdownParseOptions options, int depth)
    {
        Pipeline = pipeline;
        Options = options;
        Depth = depth;
    }

    /// <summary>The owning pipeline.</summary>
    public BitMarkdownPipeline Pipeline { get; }

    /// <summary>The safety limits in effect for this parse.</summary>
    internal BitMarkdownParseOptions Options { get; }

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
        Scan();
        BitMarkdownDelimiterResolver.Process(_tokens, Pipeline);
        return ToNodes(_tokens);
    }

    /// <summary>Parses inline content in an isolated child processor (e.g. for a link label).</summary>
    public List<BitMarkdownNode> ParseInlines(string text) => Pipeline.ParseInlines(text, Options, Depth + 1);

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

    /// <summary>Removes trailing spaces from the pending text run and returns how many were removed.</summary>
    public int TrimPendingTrailingSpaces()
    {
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
            _tokens.Add(new Tok { Kind = TokKind.Text, Text = _literal.ToString() });
            _literal.Clear();
        }
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
                int save = Pos;
                int tokenCount = _tokens.Count;
                string literalSnapshot = _literal.ToString();
                bool handled = false;
                foreach (var p in parsers)
                {
                    // Only honor a parser that both succeeds and consumes input.
                    if (p.TryParse(this) && Pos > save)
                    {
                        handled = true;
                        break;
                    }
                    // Roll back every side effect (position, flushed tokens and pending
                    // text), not just Pos, so a failed or no-op parser can't corrupt the
                    // token stream for the parsers tried next or the normal scan.
                    Pos = save;
                    if (_tokens.Count > tokenCount) _tokens.RemoveRange(tokenCount, _tokens.Count - tokenCount);
                    _literal.Clear();
                    _literal.Append(literalSnapshot);
                }
                if (handled) continue;
            }

            _literal.Append(c);
            Pos++;
        }
        Flush();
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
