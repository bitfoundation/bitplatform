using Bit.BlazorUI.Markdown.Syntax;
using static Bit.BlazorUI.Markdown.Parsing.InlineProcessor;

namespace Bit.BlazorUI.Markdown.Parsing;

/// <summary>
/// Resolves delimiter-run tokens (emphasis, strong, strikethrough, ...) into nodes
/// using a CommonMark-style delimiter stack. Which delimiters exist and what nodes
/// they produce is supplied by the pipeline's <see cref="DelimiterProcessor"/>s.
/// </summary>
internal static class DelimiterResolver
{
    public static void Process(List<Tok> tokens, BitMarkdownPipeline pipeline)
    {
        var openersBottom = new Dictionary<(char, int), int>();

        int closerIdx = 0;
        while (closerIdx < tokens.Count)
        {
            var closer = tokens[closerIdx];
            if (closer.Kind != TokKind.Delim || !closer.Active || !closer.CanClose)
            {
                closerIdx++;
                continue;
            }

            char dc = closer.DelimChar;
            var processor = pipeline.DelimiterByChar[dc];

            var key = (dc, closer.Count % 3);
            int bottom = openersBottom.TryGetValue(key, out var b) ? b : -1;

            // Find a matching opener walking backwards.
            int openerIdx = closerIdx - 1;
            bool found = false;
            while (openerIdx > bottom && openerIdx >= 0)
            {
                var opener = tokens[openerIdx];
                if (opener.Kind == TokKind.Delim && opener.Active && opener.CanOpen
                    && opener.DelimChar == dc)
                {
                    // CommonMark "rule of three".
                    bool oddMatch = (closer.CanOpen || opener.CanClose)
                        && (opener.Count + closer.Count) % 3 == 0
                        && !(opener.Count % 3 == 0 && closer.Count % 3 == 0);
                    if (!oddMatch)
                    {
                        found = true;
                        break;
                    }
                }
                openerIdx--;
            }

            if (!found)
            {
                openersBottom[key] = closerIdx - 1;
                if (!closer.CanOpen) closer.Active = false;
                closerIdx++;
                continue;
            }

            var op = tokens[openerIdx];

            var inner = ToNodes(tokens.GetRange(openerIdx + 1, closerIdx - openerIdx - 1));
            int used = processor.TryCreate(dc, op.Count, closer.Count, inner, out var node);

            if (used <= 0 || node is null)
            {
                // This processor can't form a node for these lengths; skip this closer.
                openersBottom[key] = closerIdx - 1;
                if (!closer.CanOpen) closer.Active = false;
                closerIdx++;
                continue;
            }

            // Remove the inner tokens and splice in the wrapping node.
            tokens.RemoveRange(openerIdx + 1, closerIdx - openerIdx - 1);
            closerIdx = openerIdx + 1;

            op.Count -= used;
            closer.Count -= used;

            tokens.Insert(openerIdx + 1, new Tok { Kind = TokKind.Node, Node = node });
            closerIdx = openerIdx + 2;

            if (op.Count == 0)
            {
                tokens.RemoveAt(openerIdx);
                closerIdx--;
            }
            if (closer.Count == 0)
            {
                tokens.RemoveAt(closerIdx);
            }
            // Re-evaluate from the opener neighbourhood to catch newly adjacent delimiters.
        }
    }
}
