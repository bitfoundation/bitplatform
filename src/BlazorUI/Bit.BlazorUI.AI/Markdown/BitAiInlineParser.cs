using System.Globalization;
using System.Net;
using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Parses the inline content of a block: emphasis, code, links, images, mathematics, and citation
/// markers.
/// </summary>
/// <remarks>
/// <para>
/// Emphasis, strikethrough, and links follow the CommonMark delimiter algorithm. One left-to-right
/// pass turns the text into a list of nodes, pushing each run of <c>*</c>, <c>_</c>, or <c>~~</c>
/// onto a delimiter stack and each <c>[</c> onto a bracket stack. A link is resolved as its closing
/// bracket arrives; emphasis is resolved afterwards by matching each closer with the nearest
/// compatible opener before it. Flanking is simplified to "not next to whitespace", plus the
/// intraword rule for underscores, which forgives the punctuation-heavy text models produce.
/// </para>
/// <para>
/// Parsing is linear in the length of the text, including on the pathological inputs the algorithm
/// exists to cope with, such as tens of thousands of unmatched openers. A closer never searches
/// below the point where an earlier closer of the same kind gave up, a bracket made inactive by a
/// link is discarded on sight, and every forward scan (code spans, mathematics, angle autolinks,
/// and link destinations) is memoised or bounded so that no character is rescanned more than a
/// constant number of times.
/// </para>
/// <para>
/// When the source is still streaming, an unmatched opener and a delimiter run at the very end are
/// dropped rather than shown literally. Without that, every bold phrase in a response flashes a
/// stray asterisk on its way in. Delimiters inside a closed span are literal either way.
/// </para>
/// <para>
/// One deliberate departure from CommonMark: a run such as the second <c>**</c> in
/// <c>**bold **text</c>, which whitespace keeps from closing, closes anyway when nothing else
/// claims it. Models write that often enough that showing the asterisks would be the wrong call.
/// </para>
/// </remarks>
public static class BitAiInlineParser
{
    /// <summary>
    /// Parses inline content.
    /// </summary>
    /// <param name="text">The source.</param>
    /// <param name="options">Which extensions to enable.</param>
    /// <param name="isComplete">
    /// <see langword="false"/> while more text is expected, which hides unmatched trailing delimiters.
    /// </param>
    /// <returns>The parsed inlines.</returns>
    public static List<BitAiMarkdownInline> Parse(string? text, BitAiMarkdownOptions? options = null, bool isComplete = true) =>
        Parse(text, options ?? BitAiMarkdownOptions.Default, isComplete, null);

    /// <summary>
    /// Parses inline content against the definitions collected while parsing the surrounding
    /// document, so reference links and footnotes resolve.
    /// </summary>
    internal static List<BitAiMarkdownInline> Parse(string? text, BitAiMarkdownOptions options, bool isComplete, BitAiParseContext? context)
    {
        if (string.IsNullOrEmpty(text)) return [];
        return new Scanner(text, options, isComplete, context).Run();
    }

    // CommonMark's limit on the length of a link label.
    private const int MaxLabelLength = 999;

    // CommonMark's limit on unclosed parentheses in a link destination. It also bounds how far a
    // destination that never closes is scanned, which keeps "[a](" repeated from being quadratic.
    private const int MaxParentheses = 32;

    // Only ASCII punctuation can be escaped; before anything else a backslash is literal.
    private static bool IsAsciiPunctuation(char c) =>
        c is (>= '!' and <= '/') or (>= ':' and <= '@') or (>= '[' and <= '`') or (>= '{' and <= '~');

    /// <summary>Whether the text between <c>&lt;</c> and <c>&gt;</c> is a tag, comment, or declaration.</summary>
    internal static bool LooksLikeTag(ReadOnlySpan<char> inner)
    {
        if (inner.Length == 0) return false;

        if (inner[0] == '!')
        {
            if (inner.Length < 2) return false;
            if (inner[1] == '-') return inner.Length >= 5 && inner.StartsWith("!--") && inner.EndsWith("--");
            return char.IsAsciiLetter(inner[1]) || inner[1] == '[';
        }

        if (inner[0] == '?') return inner.Length >= 2 && inner[^1] == '?';

        var i = 0;
        if (inner[i] == '/') i++;
        if (i >= inner.Length || !char.IsAsciiLetter(inner[i])) return false;
        while (i < inner.Length && (char.IsAsciiLetterOrDigit(inner[i]) || inner[i] == '-')) i++;

        return i == inner.Length || inner[i] is ' ' or '\t' or '\n' or '/';
    }

    private static int RunLength(string text, int start, char c)
    {
        var i = start;
        while (i < text.Length && text[i] == c) i++;
        return i - start;
    }

    /// <summary>One entry in the flat list the scan produces, in source order.</summary>
    private sealed class Node
    {
        /// <summary>Literal text, when the node is neither an inline nor a delimiter run.</summary>
        public string? Text;

        /// <summary>A finished inline.</summary>
        public BitAiMarkdownInline? Inline;

        /// <summary>
        /// The first node of a chain this node stands in for: the content of a pair that was too
        /// deeply nested to wrap, which is emitted at this level with <see cref="Text"/> as the
        /// literal markers on either side. The chain ends at a node whose <see cref="Next"/> is null.
        /// </summary>
        public Node? GroupFirst;

        /// <summary>The delimiter run this node holds, whose remaining characters are its text.</summary>
        public Delimiter? Delimiter;

        /// <summary>How deeply nested the inline is: zero for a leaf.</summary>
        public int Depth;

        public Node? Previous;
        public Node? Next;
    }

    /// <summary>A run of <c>*</c>, <c>_</c>, or <c>~~</c> that may open or close a span.</summary>
    private sealed class Delimiter
    {
        public required Node Node;
        public char Char;

        /// <summary>Push order, so a bound on the stack survives the removal of the entry it names.</summary>
        public int Index;

        /// <summary>Where the run starts in the source.</summary>
        public int Start;

        /// <summary>The run's original length, which the rule of three is stated in.</summary>
        public int Length;

        /// <summary>Characters used from the front by spans this run closed.</summary>
        public int Front;

        /// <summary>Characters used from the back by spans this run opened.</summary>
        public int Back;

        public bool CanOpen;
        public bool CanClose;

        /// <summary>The run ends the text, so while streaming it may be a longer run still arriving.</summary>
        public bool IsTrailing;

        /// <summary>The run follows a letter or digit.</summary>
        public bool AfterWord;

        public Delimiter? Previous;
        public Delimiter? Next;

        public int Count => Length - Front - Back;

        /// <summary>
        /// Whether what is left of the run is dropped, rather than shown, while streaming: it could
        /// still open a span, or it ends the text and may be a longer run still arriving. An
        /// underscore ending a word is shown, since "snake_" is far more likely "snake_case" coming.
        /// </summary>
        public bool HideWhileStreaming => CanOpen || (IsTrailing && (Char != '_' || !AfterWord || Front > 0));
    }

    /// <summary>An opening <c>[</c> or <c>![</c> waiting for its <c>]</c>.</summary>
    private sealed class Bracket
    {
        public required Node Node;
        public bool IsImage;

        /// <summary>Where the label starts in the source, for an image's alternative text.</summary>
        public int LabelStart;

        /// <summary>How many links had closed when this bracket opened; any more makes it inactive.</summary>
        public int LinksBefore;

        /// <summary>The top of the delimiter stack when this bracket opened, which bounds its label.</summary>
        public Delimiter? PreviousDelimiter;

        public Bracket? Previous;
    }

    private sealed class Scanner(string text, BitAiMarkdownOptions options, bool isComplete, BitAiParseContext? context)
    {
        private readonly StringBuilder _pending = new();
        private readonly StringBuilder _merged = new();

        // Emphasis and strikethrough nest. A span whose content is already this deep is kept as
        // its source text, so no input can build a tree deep enough to overflow a renderer's stack.
        private readonly int _maxDepth = Math.Max(1, options.MaxNestingDepth);

        // Where the last citation marker ended, so "[1][2]" reads as two markers even though the
        // second follows a bracket.
        private int _lastCitationEnd = -1;

        private Node? _head;
        private Node? _tail;
        private Delimiter? _delimiters;
        private int _delimiterCount;
        private Bracket? _brackets;
        private int _links;

        // Each autolink's source, restored when the autolink turns out to be inside a link's text.
        private Dictionary<BitAiLinkInline, string>? _autoLinkSources;

        // Memoised forward scans. Each records what a scan found so a later scan from further on
        // can reuse the answer rather than walk the same text again.
        private int _nextAngle = -1;
        private int _mathFrom = -1;
        private int _mathCloser;
        private Dictionary<int, int>? _lastTickRun;
        private bool _ticksExhausted;

        public List<BitAiMarkdownInline> Run()
        {
            var i = 0;

            while (i < text.Length)
            {
                var c = text[i];

                switch (c)
                {
                    case '\\' when i + 1 < text.Length && IsAsciiPunctuation(text[i + 1]):
                        _pending.Append(text[i + 1]);
                        i += 2;
                        continue;

                    case '\\' when i + 1 < text.Length && text[i + 1] == '\n':
                        Add(new BitAiLineBreakInline(true));
                        i += 2;
                        continue;

                    case '`':
                        var ticks = RunLength(text, i, '`');
                        if (TryCodeSpan(i, ticks, out var codeEnd, out var code))
                        {
                            Add(new BitAiCodeInline(code));
                            i = codeEnd;
                            continue;
                        }

                        // An unmatched run is literal as a whole; retrying from its second backtick
                        // would pair a shorter run with a later one. While streaming it is hidden
                        // like a dangling asterisk: its partner has simply not arrived.
                        if (isComplete) _pending.Append('`', ticks);
                        i += ticks;
                        continue;

                    case '&' when TryEntity(i, out var entityEnd, out var decoded):
                        _pending.Append(decoded);
                        i = entityEnd;
                        continue;

                    case '$' when options.Math && TryMath(i, out var mathEnd, out var expression):
                        Add(new BitAiMathInline(expression));
                        i = mathEnd;
                        continue;

                    case '!' when i + 1 < text.Length && text[i + 1] == '[' && !IsCitationMarker(i + 1):
                        PushBracket("![", i + 2, isImage: true);
                        i += 2;
                        continue;

                    case '[' when options.Footnotes && TryFootnoteReference(i, out var footnoteEnd, out var label):
                        Add(new BitAiFootnoteReferenceInline(label));
                        i = footnoteEnd;
                        continue;

                    case '[' when options.CitationMarkers && TryCitationMarker(i, out var citeEnd, out var number):
                        Add(new BitAiCitationMarkerInline(number));
                        i = citeEnd;
                        _lastCitationEnd = i;
                        continue;

                    case '[':
                        PushBracket("[", i + 1, isImage: false);
                        i++;
                        continue;

                    case ']':
                        i = CloseBracket(i);
                        continue;

                    case '<' when TryAutoLink(i, out var autoEnd, out var autoUrl):
                        AddAutoLink(autoUrl, i, autoEnd);
                        i = autoEnd;
                        continue;

                    case '<' when options.AllowRawHtml && TryRawHtml(i, out var htmlEnd):
                        Add(new BitAiRawHtmlInline(text[i..htmlEnd]));
                        i = htmlEnd;
                        continue;

                    case '~' when options.Strikethrough:
                        var tildes = RunLength(text, i, '~');
                        if (tildes == 2)
                        {
                            i = PushDelimiter(i, tildes);
                            continue;
                        }

                        // A lone tilde at the very end of streaming text may be the first of a pair.
                        if (isComplete || tildes != 1 || i + 1 < text.Length) _pending.Append('~', tildes);
                        i += tildes;
                        continue;

                    case '*':
                    case '_':
                        i = PushDelimiter(i, RunLength(text, i, c));
                        continue;

                    case '\n':
                        Add(new BitAiLineBreakInline(options.SoftBreaksAsHardBreaks || EndsWithHardBreak(i)));
                        i++;
                        while (i < text.Length && text[i] is ' ' or '\t') i++;
                        continue;

                    case 'h':
                    case 'w':
                        if (options.AutoLinks && TryBareUrl(i, out var bareEnd, out var bareUrl))
                        {
                            AddAutoLink(bareUrl, i, bareEnd);
                            i = bareEnd;
                            continue;
                        }

                        break;
                }

                _pending.Append(c);
                i++;
            }

            FlushPending();
            ProcessEmphasis(null);

            var output = new List<BitAiMarkdownInline>();
            Emit(_head, null, output, topLevel: true);
            return output;
        }

        private Node AddNode(Node node)
        {
            node.Previous = _tail;
            if (_tail is null) _head = node;
            else _tail.Next = node;
            _tail = node;
            return node;
        }

        private void RemoveNode(Node node)
        {
            if (node.Previous is null) _head = node.Next;
            else node.Previous.Next = node.Next;

            if (node.Next is null) _tail = node.Previous;
            else node.Next.Previous = node.Previous;
        }

        // Drops the given node and everything after it, then appends the replacement.
        private void ReplaceFrom(Node from, Node replacement)
        {
            _tail = from.Previous;
            if (_tail is null) _head = null;
            else _tail.Next = null;
            AddNode(replacement);
        }

        private void FlushPending()
        {
            if (_pending.Length == 0) return;
            AddNode(new Node { Text = _pending.ToString() });
            _pending.Clear();
        }

        private void Add(BitAiMarkdownInline inline)
        {
            FlushPending();
            AddNode(new Node { Inline = inline });
        }

        private void AddAutoLink(string url, int start, int end)
        {
            var link = new BitAiLinkInline(url) { IsAutoLink = true };
            link.Inlines.Add(new BitAiTextInline(url));
            (_autoLinkSources ??= new())[link] = text[start..end];
            Add(link);
        }

        /// <summary>
        /// Appends the nodes from <paramref name="from"/> up to <paramref name="to"/> to an inline
        /// list, merging adjacent text.
        /// </summary>
        /// <returns>The deepest nesting among the nodes.</returns>
        private int Emit(Node? from, Node? to, List<BitAiMarkdownInline> output, bool topLevel)
        {
            var depth = 0;

            // Collapsed chains are walked with an explicit stack, since thousands of them may nest.
            Stack<(Node? Resume, Node? To, string? Markers)>? pending = null;
            var node = from;

            while (true)
            {
                if (node is null || node == to)
                {
                    if (pending is null || pending.Count == 0) break;

                    (node, to, var markers) = pending.Pop();
                    _merged.Append(markers);
                    continue;
                }

                depth = Math.Max(depth, node.Depth);

                if (node.GroupFirst is { } first)
                {
                    _merged.Append(node.Text);
                    (pending ??= new()).Push((node.Next, to, node.Text));
                    node = first;
                    to = null;
                    continue;
                }

                if (node.Inline is { } inline)
                {
                    FlushMerged(output);
                    output.Add(inline);
                }
                else if (node.Delimiter is { } delimiter)
                {
                    // Only a delimiter outside every span can still be waiting for its partner.
                    var hidden = topLevel && pending is not { Count: > 0 } && !isComplete && delimiter.HideWhileStreaming;
                    if (!hidden) _merged.Append(delimiter.Char, delimiter.Count);
                }
                else
                {
                    _merged.Append(node.Text);
                }

                node = node.Next;
            }

            FlushMerged(output);
            return depth;
        }

        private void FlushMerged(List<BitAiMarkdownInline> output)
        {
            if (_merged.Length == 0) return;
            output.Add(new BitAiTextInline(_merged.ToString()));
            _merged.Clear();
        }

        private int PushDelimiter(int start, int run)
        {
            var c = text[start];
            var before = start > 0 ? text[start - 1] : '\n';
            var after = start + run < text.Length ? text[start + run] : '\n';

            FlushPending();
            var node = AddNode(new Node());
            var delimiter = new Delimiter
            {
                Node = node,
                Char = c,
                Index = _delimiterCount++,
                Start = start,
                Length = run,
                CanOpen = !char.IsWhiteSpace(after) && !(c == '_' && char.IsLetterOrDigit(before)),
                CanClose = !char.IsWhiteSpace(before) && !(c == '_' && char.IsLetterOrDigit(after)),
                IsTrailing = start + run == text.Length,
                AfterWord = char.IsLetterOrDigit(before),
                Previous = _delimiters,
            };

            node.Delimiter = delimiter;
            if (_delimiters is not null) _delimiters.Next = delimiter;
            _delimiters = delimiter;
            return start + run;
        }

        private void RemoveDelimiter(Delimiter delimiter)
        {
            if (delimiter.Previous is not null) delimiter.Previous.Next = delimiter.Next;
            if (delimiter.Next is null) _delimiters = delimiter.Previous;
            else delimiter.Next.Previous = delimiter.Previous;
        }

        private void RemoveDelimitersAbove(int bottom)
        {
            while (_delimiters is not null && _delimiters.Index > bottom) RemoveDelimiter(_delimiters);
        }

        /// <summary>Matches the delimiters above <paramref name="bottom"/> into spans.</summary>
        /// <remarks>
        /// CommonMark's "process emphasis": each closer, in order, looks back for the nearest
        /// opener it can pair with. Whatever lies between a matched pair is inside the span and
        /// leaves the stack, and a closer that finds nothing records how far it looked, so the
        /// next closer of the same kind starts where it stopped instead of rescanning.
        /// </remarks>
        private void ProcessEmphasis(Delimiter? bottom)
        {
            var bottomIndex = bottom?.Index ?? -1;

            Span<int> openersBottom = stackalloc int[13];
            openersBottom.Fill(bottomIndex);

            Delimiter? closer = null;
            for (var d = _delimiters; d is not null && d.Index > bottomIndex; d = d.Previous) closer = d;

            while (closer is not null)
            {
                if (!closer.CanClose)
                {
                    closer = closer.Next;
                    continue;
                }

                var kind = Kind(closer);
                var opener = closer.Previous;
                while (opener is not null && opener.Index > openersBottom[kind] && !Pairs(opener, closer)) opener = opener.Previous;

                if (opener is null || opener.Index <= openersBottom[kind])
                {
                    openersBottom[kind] = closer.Index - 1;
                    var next = closer.Next;
                    if (!closer.CanOpen) RemoveDelimiter(closer);
                    closer = next;
                    continue;
                }

                var used = closer.Char == '~' || (opener.Count >= 2 && closer.Count >= 2) ? 2 : 1;
                opener.Back += used;
                closer.Front += used;
                Wrap(opener, closer, used);

                while (closer.Previous is { } inside && inside != opener) RemoveDelimiter(inside);

                if (opener.Count == 0)
                {
                    RemoveNode(opener.Node);
                    RemoveDelimiter(opener);
                }

                if (closer.Count == 0)
                {
                    var next = closer.Next;
                    RemoveNode(closer.Node);
                    RemoveDelimiter(closer);
                    closer = next;
                }
            }

            PairLooseClosers(bottomIndex);
            RemoveDelimitersAbove(bottomIndex);
        }

        /// <summary>
        /// Pairs what the CommonMark pass left behind in the way a reader would: a run that cannot
        /// close only because whitespace precedes it closes the nearest unmatched opener of the
        /// same kind and size, as in <c>**bold **text</c>.
        /// </summary>
        /// <remarks>
        /// The whitespace moves out of the span, so the result is <c>&lt;strong&gt;bold&lt;/strong&gt; text</c>.
        /// A run at the very end of streaming text is left alone, since it may still grow.
        /// </remarks>
        private void PairLooseClosers(int bottomIndex)
        {
            Delimiter? first = null;
            for (var d = _delimiters; d is not null && d.Index > bottomIndex; d = d.Previous) first = d;

            Delimiter? starOpener = null;
            Delimiter? underscoreOpener = null;

            for (var closer = first; closer is not null;)
            {
                var next = closer.Next;
                if (closer.Char is not ('*' or '_'))
                {
                    closer = next;
                    continue;
                }

                ref var opener = ref closer.Char == '*' ? ref starOpener : ref underscoreOpener;
                var count = closer.Count;

                if (opener is not null
                    && closer.Start > 0
                    && closer.CanOpen
                    && !closer.CanClose
                    && !closer.IsTrailing
                    && count is 1 or 2
                    && count == opener.Count
                    && text[closer.Start - 1] is ' ' or '\t'
                    && closer.Node.Previous != opener.Node)
                {
                    opener.Back += count;
                    closer.Front += count;
                    Wrap(opener, closer, count);
                    MoveTrailingWhitespaceOut(opener.Node.Next!, closer.Node);

                    while (closer.Previous is { } inside && inside != opener) RemoveDelimiter(inside);
                    RemoveNode(opener.Node);
                    RemoveDelimiter(opener);
                    RemoveNode(closer.Node);
                    RemoveDelimiter(closer);
                    opener = null;
                }
                else if (closer.CanOpen && count is 1 or 2)
                {
                    opener = closer;
                }

                closer = next;
            }
        }

        // The content of a loosely closed span ends with the whitespace that kept its closer from
        // closing; that whitespace belongs after the span.
        private void MoveTrailingWhitespaceOut(Node span, Node closerNode)
        {
            if (span.Inline is not BitAiEmphasisInline emphasis || emphasis.Inlines.Count == 0) return;
            if (emphasis.Inlines[^1] is not BitAiTextInline last) return;

            var trimmed = last.Text.TrimEnd();
            if (trimmed.Length == last.Text.Length) return;

            var whitespace = new Node { Text = last.Text[trimmed.Length..], Previous = span, Next = closerNode };
            if (trimmed.Length == 0) emphasis.Inlines.RemoveAt(emphasis.Inlines.Count - 1);
            else last.Text = trimmed;

            span.Next = whitespace;
            closerNode.Previous = whitespace;
        }

        // Closers that pair with the same openers share a kind, and with it a search bound.
        private static int Kind(Delimiter closer) => closer.Char switch
        {
            '~' => 12,
            '*' => (closer.CanOpen ? 3 : 0) + closer.Length % 3,
            _ => 6 + (closer.CanOpen ? 3 : 0) + closer.Length % 3,
        };

        private bool Pairs(Delimiter opener, Delimiter closer)
        {
            if (opener.Char != closer.Char || !opener.CanOpen) return false;
            if (closer.Char == '~') return true;

            // CommonMark's rule of three: when either run could both open and close, the two pair
            // only if their lengths do not sum to a multiple of three. It keeps the "*" in
            // "**2*3 is six**" from claiming the closing "**".
            if ((opener.CanClose || closer.CanOpen) && closer.Length % 3 != 0 && (opener.Length + closer.Length) % 3 == 0) return false;

            // While streaming, "**bold*" is a strong closer half arrived, not a stray star.
            return isComplete || !closer.IsTrailing || closer.Count >= Math.Min(opener.Count, 3);
        }

        // Moves the nodes between a matched pair into a new span. Past the nesting limit the pair
        // becomes literal text instead, and what it would have wrapped stays where it is.
        private void Wrap(Delimiter opener, Delimiter closer, int used)
        {
            var depth = 0;
            for (var inner = opener.Node.Next; inner is not null && inner != closer.Node; inner = inner.Next) depth = Math.Max(depth, inner.Depth);

            Node node;
            if (depth >= _maxDepth)
            {
                // The content is detached as a chain behind one node, in constant time, so a run
                // of thousands of nested pairs costs each wrap no walk over everything inside.
                var first = opener.Node.Next!;
                var last = closer.Node.Previous!;
                first.Previous = null;
                last.Next = null;
                node = new Node { GroupFirst = first, Text = new string(closer.Char, used), Depth = depth };
            }
            else
            {
                var span = closer.Char == '~' ? (BitAiMarkdownInline)new BitAiStrikethroughInline() : new BitAiEmphasisInline(used == 2);
                var inlines = span is BitAiEmphasisInline emphasis ? emphasis.Inlines : ((BitAiStrikethroughInline)span).Inlines;
                Emit(opener.Node.Next, closer.Node, inlines, topLevel: false);
                node = new Node { Inline = span, Depth = depth + 1 };
            }

            node.Previous = opener.Node;
            node.Next = closer.Node;
            opener.Node.Next = node;
            closer.Node.Previous = node;
        }

        private void PushBracket(string literal, int labelStart, bool isImage)
        {
            FlushPending();
            _brackets = new Bracket
            {
                Node = AddNode(new Node { Text = literal }),
                IsImage = isImage,
                LabelStart = labelStart,
                LinksBefore = _links,
                PreviousDelimiter = _delimiters,
                Previous = _brackets,
            };
        }

        // Resolves a "]" against the nearest open bracket, and returns where scanning resumes.
        private int CloseBracket(int close)
        {
            var opener = _brackets;
            if (opener is not null) _brackets = opener.Previous;

            // A link cannot contain another link: once one closes, every "[" before it is literal.
            // An image's brackets stay live, since an image's text may hold a link.
            var active = opener is not null && (opener.IsImage || opener.LinksBefore == _links);

            if (!active)
            {
                _pending.Append(']');
                return close + 1;
            }

            int end;
            string url;
            string? title;
            if (close + 1 < text.Length && text[close + 1] == '(' && TryLinkTail(close + 1, out end, out url, out title))
            {
                // An inline link.
            }
            else if (!TryReferenceLink(opener!, close, out end, out url, out title))
            {
                _pending.Append(']');
                return close + 1;
            }

            FlushPending();
            var bottom = opener!.PreviousDelimiter?.Index ?? -1;

            if (opener.IsImage)
            {
                RemoveDelimitersAbove(bottom);
                ReplaceFrom(opener.Node, new Node { Inline = new BitAiImageInline(url, text[opener.LabelStart..close], title) });
                return end;
            }

            ProcessEmphasis(opener.PreviousDelimiter);

            var link = new BitAiLinkInline(url, title);
            UnwrapAutoLinks(opener.Node.Next);
            var depth = Emit(opener.Node.Next, null, link.Inlines, topLevel: false);
            ReplaceFrom(opener.Node, new Node { Inline = link, Depth = depth + 1 });
            _links++;
            return end;
        }

        /// <summary>
        /// Resolves a reference link after its <c>]</c>: full (<c>[text][label]</c>), collapsed
        /// (<c>[text][]</c>), or shortcut (<c>[text]</c>), against the document's definitions.
        /// </summary>
        private bool TryReferenceLink(Bracket opener, int close, out int end, out string url, out string? title)
        {
            end = close + 1;
            url = string.Empty;
            title = null;
            if (context is null || !context.ResolvesReferences) return false;

            var label = text.AsSpan(opener.LabelStart, close - opener.LabelStart);

            if (close + 1 < text.Length && text[close + 1] == '[')
            {
                var window = text.AsSpan(close + 2, Math.Min(MaxLabelLength + 1, text.Length - close - 2));
                var labelEnd = window.IndexOf(']');
                if (labelEnd >= 0 && !window[..labelEnd].Contains('['))
                {
                    // A full reference uses its own label, and does not fall back to a shortcut.
                    if (!window[..labelEnd].IsWhiteSpace()) label = window[..labelEnd];
                    end = close + 2 + labelEnd + 1;
                    return LookUp(label, ref url, ref title);
                }
            }

            return LookUp(label, ref url, ref title);

            bool LookUp(ReadOnlySpan<char> key, ref string destination, ref string? name)
            {
                if (key.Length > MaxLabelLength || !context.TryGetLink(key, out var definition)) return false;
                destination = definition.Url;
                name = definition.Title;
                return true;
            }
        }

        // A bare or angle-bracket URL inside a link's text is text: an anchor inside an anchor is
        // invalid markup, and "[https://x.dev](https://x.dev)" means one link.
        private void UnwrapAutoLinks(Node? from)
        {
            if (_autoLinkSources is null) return;

            Stack<Node?>? pending = null;
            var node = from;

            while (true)
            {
                if (node is null)
                {
                    if (pending is null || pending.Count == 0) return;
                    node = pending.Pop();
                    continue;
                }

                if (node.Inline is BitAiLinkInline { IsAutoLink: true } auto)
                {
                    node.Text = _autoLinkSources[auto];
                    node.Inline = null;
                }
                else if (node.Inline is not null)
                {
                    UnwrapAutoLinks(node.Inline);
                }
                else if (node.GroupFirst is { } first)
                {
                    (pending ??= new()).Push(node.Next);
                    node = first;
                    continue;
                }

                node = node.Next;
            }
        }

        private void UnwrapAutoLinks(BitAiMarkdownInline inline)
        {
            var inlines = inline switch
            {
                BitAiEmphasisInline e => e.Inlines,
                BitAiStrikethroughInline s => s.Inlines,
                _ => null,
            };

            if (inlines is not null) UnwrapAutoLinks(inlines);
        }

        private void UnwrapAutoLinks(List<BitAiMarkdownInline> inlines)
        {
            for (var i = 0; i < inlines.Count; i++)
            {
                if (inlines[i] is BitAiLinkInline { IsAutoLink: true } auto) inlines[i] = new BitAiTextInline(_autoLinkSources![auto]);
                else UnwrapAutoLinks(inlines[i]);
            }
        }

        /// <summary>Reads a link's destination and optional title, from the opening parenthesis.</summary>
        /// <remarks>
        /// More forgiving than CommonMark in one way: a destination may contain spaces, as in
        /// <c>[report](Q3 summary.pdf)</c>. Whitespace still introduces a title in double quotes,
        /// single quotes, or parentheses. A destination that never closes is abandoned at the end
        /// of its line or at too many unclosed parentheses, so a failed attempt is bounded, and
        /// nothing is allocated until the tail is known to be a link.
        /// </remarks>
        private bool TryLinkTail(int open, out int end, out string url, out string? title)
        {
            end = open;
            url = string.Empty;
            title = null;

            var i = SkipWhitespace(open + 1);

            if (i < text.Length && text[i] == '<')
            {
                var from = i + 1;
                for (i = from; i < text.Length && text[i] != '>'; i++)
                {
                    if (text[i] is '<' or '\n') return false;
                    if (text[i] == '\\' && i + 1 < text.Length && IsAsciiPunctuation(text[i + 1])) i++;
                }

                if (i >= text.Length || TryLinkEnd(i + 1, out end, out title) != true) return false;
                url = Unescape(from, i);
                return true;
            }

            var start = i;
            var depth = 0;
            while (i < text.Length)
            {
                var c = text[i];

                if (c == '\\' && i + 1 < text.Length && IsAsciiPunctuation(text[i + 1]))
                {
                    i += 2;
                    continue;
                }

                if (c == '(' && ++depth > MaxParentheses) return false;
                if (c == ')' && depth-- == 0) break;

                if (char.IsWhiteSpace(c))
                {
                    if (depth == 0 && TryLinkEnd(i, out end, out title) is { } ended)
                    {
                        url = Unescape(start, i);
                        return ended;
                    }

                    // The whitespace is part of the destination, unless it breaks the line.
                    var next = SkipWhitespace(i);
                    if (text.AsSpan(i, next - i).Contains('\n')) return false;
                    i = next;
                    continue;
                }

                i++;
            }

            if (i >= text.Length) return false;

            end = i + 1;
            url = Unescape(start, i);
            return true;
        }

        /// <summary>
        /// Reads what may follow a destination: optional whitespace, then the closing parenthesis,
        /// or a title that whitespace separates from the destination and the parenthesis after it.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the link ends here; <see langword="false"/> when a title was
        /// read but something else follows it, so there is no link at all; and
        /// <see langword="null"/> when neither follows, so the text may still be destination.
        /// </returns>
        private bool? TryLinkEnd(int start, out int end, out string? title)
        {
            end = start;
            title = null;

            var i = SkipWhitespace(start);
            if (i >= text.Length) return null;

            if (text[i] == ')')
            {
                end = i + 1;
                return true;
            }

            if (i == start || text[i] is not ('"' or '\'' or '(') || !TryTitle(i, out var titleEnd)) return null;

            var close = SkipWhitespace(titleEnd);
            if (close >= text.Length || text[close] != ')') return false;

            title = Unescape(i + 1, titleEnd - 1);
            end = close + 1;
            return true;
        }

        // Finds the end of a title in double quotes, single quotes, or parentheses.
        private bool TryTitle(int start, out int end)
        {
            var close = text[start] == '(' ? ')' : text[start];

            for (var i = start + 1; i < text.Length; i++)
            {
                var c = text[i];
                if (c == '\\' && i + 1 < text.Length && IsAsciiPunctuation(text[i + 1])) i++;
                else if (c == close)
                {
                    end = i + 1;
                    return true;
                }
                else if (c == '(' && close == ')') break;
            }

            end = start;
            return false;
        }

        // The text between two positions, with backslash escapes resolved.
        private string Unescape(int start, int end)
        {
            var span = text.AsSpan(start, end - start);
            if (!span.Contains('\\')) return span.ToString();

            var builder = new StringBuilder(span.Length);
            for (var i = 0; i < span.Length; i++)
            {
                if (span[i] == '\\' && i + 1 < span.Length && IsAsciiPunctuation(span[i + 1])) i++;
                builder.Append(span[i]);
            }

            return builder.ToString();
        }

        private int SkipWhitespace(int i)
        {
            while (i < text.Length && text[i] is ' ' or '\t' or '\n') i++;
            return i;
        }

        private bool EndsWithHardBreak(int newline)
        {
            var spaces = 0;
            for (var i = newline - 1; i >= 0 && text[i] == ' '; i--) spaces++;
            return spaces >= 2;
        }

        /// <remarks>
        /// A scan that reaches the end records the last run of every length it passed, after which
        /// an opener with no later run of its length fails at once. Successful scans consume what
        /// they cover, so the total is linear however many runs go unmatched.
        /// </remarks>
        private bool TryCodeSpan(int start, int ticks, out int end, out string code)
        {
            end = start;
            code = string.Empty;

            if (_ticksExhausted && (_lastTickRun is null || !_lastTickRun.TryGetValue(ticks, out var last) || last <= start)) return false;

            var i = start + ticks;
            while ((i = text.IndexOf('`', i)) >= 0)
            {
                var run = RunLength(text, i, '`');
                (_lastTickRun ??= new())[run] = i;

                if (run == ticks)
                {
                    var inner = text.AsSpan((start + ticks)..i);
                    if (inner.Length > 1 && inner[0] == ' ' && inner[^1] == ' ') inner = inner[1..^1];
                    code = inner.ToString().Replace('\n', ' ');
                    end = i + run;
                    return true;
                }

                i += run;
            }

            _ticksExhausted = true;
            return false;
        }

        private bool TryMath(int start, out int end, out string expression)
        {
            end = start;
            expression = string.Empty;
            if (start + 1 >= text.Length || text[start + 1] is ' ' or '$' or '\n') return false;
            if (IsPrice(start + 1)) return false;

            var closer = NextMathCloser(start + 2);
            if (closer >= text.Length || text[closer] != '$') return false;

            expression = text[(start + 1)..closer];
            end = closer + 1;
            return true;
        }

        /// <summary>The first closing dollar at or after a position, or the end of its line.</summary>
        /// <remarks>
        /// Whether a dollar closes does not depend on where the expression opened, so one answer
        /// serves every opener before it: "$a $b $c" on one line is scanned once, not once per sign.
        /// </remarks>
        private int NextMathCloser(int from)
        {
            if (_mathFrom >= 0 && _mathFrom <= from && from <= _mathCloser) return _mathCloser;

            var i = from;
            while (i < text.Length && text[i] != '\n' && !IsMathCloser(i)) i++;

            _mathFrom = from;
            _mathCloser = i;
            return i;
        }

        // A dollar sign followed by a number and then a space or the end of the sentence is a
        // price, not an opener, whatever follows it on the line.
        private bool IsPrice(int i)
        {
            if (!char.IsAsciiDigit(text[i])) return false;
            while (i < text.Length && (char.IsAsciiDigit(text[i]) || text[i] is ',' or '.')) i++;
            return i >= text.Length || char.IsWhiteSpace(text[i]) || text[i] is ')' or ';' or ':' or '!' or '?';
        }

        // A closer hugs its expression and is not followed by a digit, so that prices such as
        // "$5 and $10" stay text instead of becoming one expression.
        private bool IsMathCloser(int i) =>
            text[i] == '$'
            && text[i - 1] != '\\'
            && !char.IsWhiteSpace(text[i - 1])
            && !(i + 1 < text.Length && char.IsAsciiDigit(text[i + 1]));

        private bool IsCitationMarker(int start) =>
            options.CitationMarkers && TryCitationMarker(start, out _, out _);

        private bool TryCitationMarker(int start, out int end, out int number)
        {
            end = start;
            number = 0;

            // "arr[0]" is an index, not a source. A bracket straight after another marker is fine,
            // as in "[1][2]"; one straight after a word or a reference label is not.
            if (start > 0)
            {
                var before = text[start - 1];
                if (char.IsLetterOrDigit(before) || (before == ']' && _lastCitationEnd != start)) return false;
            }

            var i = start + 1;
            var digits = 0;
            while (i < text.Length && char.IsAsciiDigit(text[i])) { i++; digits++; }
            if (digits == 0 || digits > 4 || i >= text.Length || text[i] != ']') return false;
            if (i + 1 < text.Length && text[i + 1] == '(') return false;

            // A definition for the same label makes it a reference link instead.
            if (context is not null && context.TryGetLink(text.AsSpan((start + 1)..i), out _)) return false;

            number = int.Parse(text.AsSpan((start + 1)..i));
            end = i + 1;
            return true;
        }

        // "[^label]" where the label has no whitespace and a definition exists; otherwise text.
        private bool TryFootnoteReference(int start, out int end, out string label)
        {
            end = start;
            label = string.Empty;
            if (context is null || !context.ResolvesReferences) return false;
            if (start + 2 >= text.Length || text[start + 1] != '^') return false;

            var i = start + 2;
            while (i < text.Length && i - start - 2 <= MaxLabelLength && text[i] is not (']' or '[' or ' ' or '\t' or '\n')) i++;
            if (i == start + 2 || i >= text.Length || text[i] != ']') return false;

            var candidate = text.AsSpan((start + 2)..i);
            if (!context.HasFootnote(candidate)) return false;

            label = candidate.ToString();
            end = i + 1;
            return true;
        }

        /// <summary>
        /// Decodes an HTML entity (<c>&amp;amp;</c>), decimal (<c>&amp;#39;</c>), or hexadecimal
        /// (<c>&amp;#x27;</c>) character reference. Anything that is not a valid reference stays
        /// as written.
        /// </summary>
        private bool TryEntity(int start, out int end, out string decoded)
        {
            end = start;
            decoded = string.Empty;

            var i = start + 1;
            if (i >= text.Length) return false;

            if (text[i] == '#')
            {
                i++;
                var hex = i < text.Length && text[i] is 'x' or 'X';
                if (hex) i++;

                var digitsStart = i;
                var max = hex ? 6 : 7;
                while (i < text.Length && i - digitsStart <= max && (hex ? char.IsAsciiHexDigit(text[i]) : char.IsAsciiDigit(text[i]))) i++;

                var digits = i - digitsStart;
                if (digits == 0 || digits > max || i >= text.Length || text[i] != ';') return false;

                var value = int.Parse(text.AsSpan(digitsStart, digits), hex ? NumberStyles.AllowHexSpecifier : NumberStyles.None, CultureInfo.InvariantCulture);
                decoded = value == 0 || value > 0x10FFFF || value is >= 0xD800 and <= 0xDFFF ? "\uFFFD" : char.ConvertFromUtf32(value);
                end = i + 1;
                return true;
            }

            var nameStart = i;
            while (i < text.Length && i - nameStart < 32 && char.IsAsciiLetterOrDigit(text[i])) i++;
            if (i - nameStart < 2 || i >= text.Length || text[i] != ';') return false;

            var entity = text[start..(i + 1)];
            decoded = WebUtility.HtmlDecode(entity);
            if (decoded == entity) return false;

            end = i + 1;
            return true;
        }

        // The next ">" at or after a position, remembered so a line of unclosed "<" is scanned once.
        private int NextAngleClose(int from)
        {
            if (_nextAngle < from)
            {
                var found = text.IndexOf('>', from);
                _nextAngle = found < 0 ? int.MaxValue : found;
            }

            return _nextAngle == int.MaxValue ? -1 : _nextAngle;
        }

        private bool TryAutoLink(int start, out int end, out string url)
        {
            end = start;
            url = string.Empty;

            var close = NextAngleClose(start + 1);
            if (close <= start + 1) return false;

            var inner = text.AsSpan((start + 1)..close);
            foreach (var c in inner)
            {
                if (char.IsWhiteSpace(c) || c == '<') return false;
            }

            // Anything with a scheme is a URI, even when it contains an at sign as in
            // https://medium.com/@user; only a scheme-less address becomes a mailto link.
            if (inner.Contains(':')) url = inner.ToString();
            else if (inner.Contains('@')) url = "mailto:" + inner.ToString();
            else return false;
            end = close + 1;
            return true;
        }

        // Only something shaped like a tag, comment, or declaration is HTML; "a < b > c" is text.
        private bool TryRawHtml(int start, out int end)
        {
            end = start;
            var close = NextAngleClose(start + 1);
            if (close < 0 || !LooksLikeTag(text.AsSpan(start + 1, close - start - 1))) return false;

            end = close + 1;
            return true;
        }

        private bool TryBareUrl(int start, out int end, out string url)
        {
            end = start;
            url = string.Empty;

            if (start > 0 && (char.IsLetterOrDigit(text[start - 1]) || text[start - 1] is '/' or '.' or '@')) return false;

            var rest = text.AsSpan(start);
            var length = 0;
            if (rest.StartsWith("https://")) length = 8;
            else if (rest.StartsWith("http://")) length = 7;
            else if (rest.StartsWith("www.")) length = 4;
            else return false;

            // Inside brackets a "]" ends the URL, or "[https://x.dev](https://x.dev)" would lose its link.
            var inBrackets = _brackets is not null;
            var opened = 0;
            var closed = 0;
            var i = start + length;
            while (i < text.Length)
            {
                var c = text[i];
                if (char.IsWhiteSpace(c) || c is '<' or '>' or '"' or '`' || (c == ']' && inBrackets)) break;
                if (c == '(') opened++;
                else if (c == ')') closed++;
                i++;
            }

            while (i > start + length)
            {
                var last = text[i - 1];
                if (last is '.' or ',' or ';' or ':' or '!' or '?' or ']' or '*' or '_' or '~') i--;
                // A closing parenthesis belongs to the URL when it balances one inside it, as in
                // https://en.wikipedia.org/wiki/Foo_(bar), and to the sentence otherwise.
                else if (last == ')' && closed > opened) { closed--; i--; }
                else break;
            }

            if (i <= start + length) return false;

            var raw = text[start..i];
            url = raw.StartsWith("www.", StringComparison.Ordinal) ? "https://" + raw : raw;
            end = i;
            return true;
        }
    }
}
