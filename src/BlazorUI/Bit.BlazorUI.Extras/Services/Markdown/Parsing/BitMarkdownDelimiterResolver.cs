using static Bit.BlazorUI.BitMarkdownInlineProcessor;

namespace Bit.BlazorUI;

/// <summary>
/// Resolves delimiter-run tokens (emphasis, strong, strikethrough, ...) into nodes
/// using a CommonMark-style delimiter stack. Which delimiters exist and what nodes
/// they produce is supplied by the pipeline's <see cref="BitMarkdownDelimiterProcessor"/>s.
/// </summary>
internal static class BitMarkdownDelimiterResolver
{
    public static void Process(List<Tok> tokens, BitMarkdownPipeline pipeline, int maxHeight)
    {
        // Cache the "bottom" delimiter (per char + length-mod-3 + can-open) below
        // which no matching opener exists. Closers that can also open must use a
        // separate opener-bottom bucket from those that cannot, per CommonMark.
        // We store Tok *references* rather than indices because the tokens list is
        // mutated (RemoveRange/Insert/RemoveAt) while resolving, which would
        // invalidate cached integer indices.
        var openersBottom = new Dictionary<(char, int, bool), Tok?>();

        var list = new LinkedList<Tok>(tokens);
        var closerNode = list.First;
        while (closerNode is not null)
        {
            var closer = closerNode.Value;
            if (closer.Kind != TokKind.Delim || !closer.Active || !closer.CanClose)
            {
                closerNode = closerNode.Next;
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
            var openerNode = closerNode.Previous;
            bool found = false;
            BitMarkdownNode? node = null;
            int used = 0;
            int height = 0;
            while (openerNode is not null)
            {
                var opener = openerNode.Value;
                if (bottom is not null && ReferenceEquals(opener, bottom)) break;
                if (opener.Kind == TokKind.Delim && opener.Active && opener.CanOpen
                    && opener.DelimChar == dc)
                {
                    // CommonMark "rule of three" - scoped to emphasis processors only,
                    // so non-emphasis pairs (e.g. ~~) aren't rejected before TryCreate runs.
                    bool oddMatch = processor.AppliesRuleOfThree
                        && (closer.CanOpen || opener.CanClose)
                        && (opener.Count + closer.Count) % 3 == 0
                        && !(opener.Count % 3 == 0 && closer.Count % 3 == 0);
                    if (!oddMatch)
                    {
                        var inner = Range(openerNode, closerNode, out height);
                        if (height >= maxHeight) break;

                        var candidateInner = ToNodes(inner);
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
                openerNode = openerNode.Previous;
            }

            if (!found)
            {
                openersBottom[key] = closerNode.Previous?.Value;
                if (!closer.CanOpen) closer.Active = false;
                closerNode = closerNode.Next;
                continue;
            }

            var op = openerNode!.Value;

            // Remove the inner tokens and splice in the wrapping node.
            while (openerNode.Next != closerNode) list.Remove(openerNode.Next!);

            op.Count -= used;
            closer.Count -= used;

            list.AddAfter(openerNode, new Tok { Kind = TokKind.Node, Node = node, Height = height + 1 });

            if (op.Count == 0)
            {
                list.Remove(openerNode);
            }
            if (closer.Count == 0)
            {
                var next = closerNode.Next;
                list.Remove(closerNode);
                closerNode = next;
            }
            // Re-evaluate from the opener neighbourhood to catch newly adjacent delimiters.
        }

        tokens.Clear();
        tokens.AddRange(list);
    }

    private static List<Tok> Range(LinkedListNode<Tok> opener, LinkedListNode<Tok> closer, out int height)
    {
        var range = new List<Tok>();
        height = 0;
        for (var current = opener.Next; current is not null && current != closer; current = current.Next)
        {
            range.Add(current.Value);
            height = Math.Max(height, current.Value.Kind == TokKind.Node ? current.Value.Height : 1);
        }
        return range;
    }
}
