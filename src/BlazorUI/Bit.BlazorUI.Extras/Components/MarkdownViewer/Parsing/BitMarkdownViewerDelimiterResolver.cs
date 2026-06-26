using static Bit.BlazorUI.BitMarkdownViewerInlineProcessor;

namespace Bit.BlazorUI;

/// <summary>
/// Resolves delimiter-run tokens (emphasis, strong, strikethrough, ...) into nodes
/// using a CommonMark-style delimiter stack. Which delimiters exist and what nodes
/// they produce is supplied by the pipeline's <see cref="BitMarkdownViewerDelimiterProcessor"/>s.
/// </summary>
internal static class BitMarkdownViewerDelimiterResolver
{
    public static void Process(List<Tok> tokens, BitMarkdownViewerPipeline pipeline)
    {
        // Cache the "bottom" delimiter (per char + length-mod-3 + can-open) below
        // which no matching opener exists. Closers that can also open must use a
        // separate opener-bottom bucket from those that cannot, per CommonMark.
        // We store Tok *references* rather than indices because the tokens list is
        // mutated (RemoveRange/Insert/RemoveAt) while resolving, which would
        // invalidate cached integer indices.
        var openersBottom = new Dictionary<(char, int, bool), Tok?>();

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

            var key = (dc, closer.Count % 3, closer.CanOpen);
            Tok? bottom = openersBottom.TryGetValue(key, out var b) ? b : null;

            // Find a matching opener walking backwards (stopping just above the
            // cached bottom token, if any). A candidate that the processor cannot
            // turn into a node only disqualifies that opener/closer pairing, so we
            // keep scanning for an earlier opener rather than giving up on the closer.
            int openerIdx = closerIdx - 1;
            bool found = false;
            BitMarkdownViewerMarkdownNode? node = null;
            int used = 0;
            while (openerIdx >= 0)
            {
                var opener = tokens[openerIdx];
                if (bottom is not null && ReferenceEquals(opener, bottom)) break;
                if (opener.Kind == TokKind.Delim && opener.Active && opener.CanOpen
                    && opener.DelimChar == dc)
                {
                    // CommonMark "rule of three" — scoped to emphasis processors only,
                    // so non-emphasis pairs (e.g. ~~) aren't rejected before TryCreate runs.
                    bool oddMatch = processor.AppliesRuleOfThree
                        && (closer.CanOpen || opener.CanClose)
                        && (opener.Count + closer.Count) % 3 == 0
                        && !(opener.Count % 3 == 0 && closer.Count % 3 == 0);
                    if (!oddMatch)
                    {
                        var candidateInner = ToNodes(tokens.GetRange(openerIdx + 1, closerIdx - openerIdx - 1));
                        used = processor.TryCreate(dc, opener.Count, closer.Count, candidateInner, out node);
                        if (used > 0 && node is not null)
                        {
                            found = true;
                            break;
                        }
                        // This processor can't form a node for these lengths; keep
                        // searching for an earlier opener instead of dropping the closer.
                    }
                }
                openerIdx--;
            }

            if (!found)
            {
                openersBottom[key] = closerIdx > 0 ? tokens[closerIdx - 1] : null;
                if (!closer.CanOpen) closer.Active = false;
                closerIdx++;
                continue;
            }

            var op = tokens[openerIdx];

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
