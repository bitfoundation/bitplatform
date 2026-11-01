namespace Bit.BlazorUI;

/// <summary>
/// Completes reference links and images once the whole document is known. It collects every
/// <see cref="BitMarkdownLinkReferenceDefinitionNode"/> (removing it from the rendered tree),
/// then rewrites each <see cref="BitMarkdownLinkReferenceNode"/> into a real link or image -
/// or back into the literal text it was written as, when no definition matches.
/// </summary>
public sealed class BitMarkdownLinkReferenceAstProcessor : BitMarkdownAstProcessor
{
    private const int MaxExpandedUrlLength = 1 << 20;

    // Runs before every extension processor, so the flavors (task lists, autolinks, emoji,
    // auto identifiers) see a tree with no unresolved references left in it.
    public override int Order => 0;

    public override void Process(BitMarkdownDocumentNode document, BitMarkdownPipeline pipeline)
    {
        // Definitions are document-scoped wherever they were written, so they are collected
        // from the whole tree before anything is resolved; a definition is allowed to sit
        // after the reference that uses it. The first definition of a label wins.
        var definitions = new Dictionary<string, BitMarkdownLinkReferenceDefinitionNode>(StringComparer.Ordinal);
        BitMarkdownAstHelper.VisitChildLists(document, list =>
        {
            // Collected in document order so the first definition of a label wins, then
            // removed in one pass.
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is BitMarkdownLinkReferenceDefinitionNode def)
                    definitions.TryAdd(def.NormalizedLabel, def);
            }

            BitMarkdownAstHelper.RemoveAll<BitMarkdownLinkReferenceDefinitionNode>(list);
        });

        // Always run: a reference node can outlive its definition (a "[a]:" line the
        // pre-scan saw inside a code block, say), and it has to read as text either way.
        // The walk is a no-op for the documents - the overwhelming majority - that hold none.
        var imageUrls = new Dictionary<BitMarkdownLinkReferenceDefinitionNode, string>();
        int remaining = MaxExpandedUrlLength;
        BitMarkdownAstHelper.VisitChildLists(document, list => Resolve(list, definitions, imageUrls, ref remaining));

        if (definitions.Count == 0) return;

        // A reference nested inside another reference's label resolves after its host, which
        // would leave an <a> inside an <a>. Flattening once at the end guarantees a link's
        // content is never itself a link.
        foreach (var link in BitMarkdownAstHelper.Descendants(document).OfType<BitMarkdownLinkNode>())
        {
            Unnest(link.Children);
        }
    }

    // Replaces every link in the collection (at any depth) with its own children.
    private static void Unnest(IList<BitMarkdownNode> children)
    {
        // Rebuilt in one pass once a link is taken out, so a list of many links is not shifted
        // once per link. The nodes still to place sit on a stack, last on top.
        List<BitMarkdownNode>? rebuilt = null;
        var pending = new Stack<BitMarkdownNode>();
        for (int i = 0; i < children.Count; i++)
        {
            pending.Push(children[i]);
            while (pending.TryPop(out var node))
            {
                if (node is BitMarkdownLinkNode nested)
                {
                    rebuilt ??= new List<BitMarkdownNode>(children.Take(i));
                    // Placed in its stead, ahead of what follows: the content may hold links of its own.
                    for (int c = nested.Children.Count - 1; c >= 0; c--) pending.Push(nested.Children[c]);
                    continue;
                }

                foreach (var list in node.ChildLists) Unnest(list);
                rebuilt?.Add(node);
            }
        }

        if (rebuilt is not null) BitMarkdownAstHelper.ReplaceContents(children, rebuilt);
    }

    private static void Resolve(IList<BitMarkdownNode> list, Dictionary<string, BitMarkdownLinkReferenceDefinitionNode> definitions,
        Dictionary<BitMarkdownLinkReferenceDefinitionNode, string> imageUrls, ref int remaining)
    {
        // Rebuilt in one pass once a reference is put back as text, so a list of many of them is
        // not shifted once per reference. The nodes still to place sit on a stack, last on top.
        List<BitMarkdownNode>? rebuilt = null;
        var pending = new Stack<BitMarkdownNode>();
        for (int i = 0; i < list.Count; i++)
        {
            pending.Push(list[i]);
            while (pending.TryPop(out var node))
            {
                if (node is not BitMarkdownLinkReferenceNode reference)
                {
                    rebuilt?.Add(node);
                    continue;
                }

                if (definitions.TryGetValue(reference.Label, out var definition))
                {
                    var url = reference.IsImage ? ImageUrl(definition, imageUrls) : definition.Url;
                    if (url.Length <= remaining)
                    {
                        remaining -= url.Length;
                        // Until something has been put back, the node is list[i] itself.
                        if (rebuilt is null) list[i] = Build(reference, definition, url);
                        else rebuilt.Add(Build(reference, definition, url));
                        continue;
                    }
                }

                // No such definition: put the source text back, so "[not a link]" reads exactly
                // as it was written and a task marker like "[ ]" survives for the task-list
                // processor once the neighbouring text runs are merged again below. The children
                // go back ahead of what follows, since they may themselves hold references that
                // this pass has not visited yet.
                rebuilt ??= new List<BitMarkdownNode>(list.Take(i));
                pending.Push(new BitMarkdownTextNode(reference.RawSuffix));
                for (int c = reference.Children.Count - 1; c >= 0; c--) pending.Push(reference.Children[c]);
                pending.Push(new BitMarkdownTextNode(reference.RawPrefix));
            }
        }

        if (rebuilt is not null) BitMarkdownAstHelper.ReplaceContents(list, rebuilt);
    }

    private static string ImageUrl(BitMarkdownLinkReferenceDefinitionNode definition, Dictionary<BitMarkdownLinkReferenceDefinitionNode, string> imageUrls)
    {
        if (imageUrls.TryGetValue(definition, out var url) is false)
        {
            // Sanitized from the destination as written, not from the link-sanitized one: the
            // two sets of allowed schemes overlap but neither contains the other, so an
            // embedded "data:image/png" - a valid image source, never a valid link - would
            // already have been blanked by the first pass.
            url = BitMarkdownUrlSanitizer.Sanitize(definition.RawUrl, isImage: true);
            imageUrls[definition] = url;
        }
        return url;
    }

    private static BitMarkdownNode Build(BitMarkdownLinkReferenceNode reference, BitMarkdownLinkReferenceDefinitionNode definition, string url)
    {
        if (reference.IsImage)
        {
            return new BitMarkdownImageNode
            {
                Url = url,
                Title = definition.Title,
                Alt = BitMarkdownInlineHelpers.PlainText(reference.Children)
            };
        }

        var link = new BitMarkdownLinkNode { Url = url, Title = definition.Title };
        link.Children.AddRange(reference.Children);
        return link;
    }
}
