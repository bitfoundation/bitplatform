namespace Bit.BlazorUI;

/// <summary>
/// Completes reference links and images once the whole document is known. It collects every
/// <see cref="BitMarkdownLinkReferenceDefinitionNode"/> (removing it from the rendered tree),
/// then rewrites each <see cref="BitMarkdownLinkReferenceNode"/> into a real link or image -
/// or back into the literal text it was written as, when no definition matches.
/// </summary>
public sealed class BitMarkdownLinkReferenceAstProcessor : BitMarkdownAstProcessor
{
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
            // removed back to front so the indexes stay valid.
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is BitMarkdownLinkReferenceDefinitionNode def)
                    definitions.TryAdd(def.NormalizedLabel, def);
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] is BitMarkdownLinkReferenceDefinitionNode) list.RemoveAt(i);
            }
        });

        // Always run: a reference node can outlive its definition (a "[a]:" line the
        // pre-scan saw inside a code block, say), and it has to read as text either way.
        // The walk is a no-op for the documents - the overwhelming majority - that hold none.
        BitMarkdownAstHelper.VisitChildLists(document, list => Resolve(list, definitions));

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
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is not BitMarkdownLinkNode nested)
            {
                foreach (var list in children[i].ChildLists) Unnest(list);
                continue;
            }

            children.RemoveAt(i);
            int at = i;
            foreach (var child in nested.Children) children.Insert(at++, child);
            // Deliberately not advancing: the spliced-in content may hold links of its own.
            i--;
        }
    }

    private static void Resolve(IList<BitMarkdownNode> list, Dictionary<string, BitMarkdownLinkReferenceDefinitionNode> definitions)
    {
        int i = 0;
        while (i < list.Count)
        {
            if (list[i] is not BitMarkdownLinkReferenceNode reference)
            {
                i++;
                continue;
            }

            if (definitions.TryGetValue(reference.Label, out var definition))
            {
                list[i] = Build(reference, definition);
                i++;
                continue;
            }

            // No such definition: put the source text back, so "[not a link]" reads exactly
            // as it was written and a task marker like "[ ]" survives for the task-list
            // processor once the neighbouring text runs are merged again below.
            list.RemoveAt(i);
            int at = i;
            list.Insert(at++, new BitMarkdownTextNode(reference.RawPrefix));
            foreach (var child in reference.Children) list.Insert(at++, child);
            list.Insert(at, new BitMarkdownTextNode(reference.RawSuffix));
            // Deliberately not advancing: the spliced-in children may themselves hold
            // references that this pass has not visited yet.
        }
    }

    private static BitMarkdownNode Build(BitMarkdownLinkReferenceNode reference, BitMarkdownLinkReferenceDefinitionNode definition)
    {
        if (reference.IsImage)
        {
            return new BitMarkdownImageNode
            {
                // The definition's destination was sanitized as a link; an image is only
                // allowed a narrower set of schemes, so it is sanitized again here.
                Url = BitMarkdownUrlSanitizer.Sanitize(definition.Url, isImage: true),
                Title = definition.Title,
                Alt = BitMarkdownInlineHelpers.PlainText(reference.Children)
            };
        }

        var link = new BitMarkdownLinkNode { Url = definition.Url, Title = definition.Title };
        link.Children.AddRange(reference.Children);
        return link;
    }
}
