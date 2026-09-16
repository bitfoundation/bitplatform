namespace Bit.BlazorUI;

/// <summary>
/// Wires footnotes together once the whole document is parsed: every definition is lifted
/// out of the flow into a single footnotes section at the end, numbered in order of first
/// reference, and every reference is given its number and back-link occurrence. A reference
/// with no definition degrades to the text it was written as, and a definition nobody cites
/// is dropped.
/// </summary>
public sealed class BitMarkdownFootnoteAstProcessor : BitMarkdownAstProcessor
{
    // After the core reference resolver (0) and before the rest of the flavors.
    public override int Order => 10;

    public override void Process(BitMarkdownDocumentNode document, BitMarkdownPipeline pipeline)
    {
        var definitions = new Dictionary<string, BitMarkdownFootnoteDefinitionNode>(StringComparer.Ordinal);

        BitMarkdownAstHelper.VisitChildLists(document, list =>
        {
            // Collected in document order so the first definition of a label wins (matching
            // how link references behave), then removed back to front.
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is BitMarkdownFootnoteDefinitionNode definition)
                    definitions.TryAdd(definition.Label, definition);
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] is BitMarkdownFootnoteDefinitionNode) list.RemoveAt(i);
            }
        });

        if (definitions.Count > 0)
        {
            var section = new BitMarkdownFootnotesNode();
            // The section joins the document before numbering so that a footnote citing
            // another footnote is picked up by the same sweep that numbered the first.
            document.Children.Add(section);

            bool discovered;
            do
            {
                discovered = false;
                foreach (var reference in BitMarkdownAstHelper.Descendants(document).OfType<BitMarkdownFootnoteReferenceNode>().ToList())
                {
                    if (reference.Number != 0) continue;
                    if (definitions.TryGetValue(reference.Label, out var definition) is false) continue;

                    if (definition.Number == 0)
                    {
                        // Numbering follows the order references appear, which is what
                        // readers expect from the printed markers.
                        definition.Number = section.Children.Count + 1;
                        section.Children.Add(definition);
                        discovered = true;
                    }

                    definition.ReferenceCount++;
                    reference.Number = definition.Number;
                    reference.Occurrence = definition.ReferenceCount;
                }
            }
            while (discovered);

            // A document whose only footnote syntax was an uncited definition gets no section.
            if (section.Children.Count == 0)
            {
                document.Children.Remove(section);
            }
        }

        DropUnresolvedReferences(document, definitions);
    }

    // A "[^x]" that never resolved (its definition was dropped, or the pre-scan matched a
    // "[^x]:" line inside a code block) reads as the plain text it was written as.
    private static void DropUnresolvedReferences(
        BitMarkdownDocumentNode document, Dictionary<string, BitMarkdownFootnoteDefinitionNode> definitions)
    {
        BitMarkdownAstHelper.VisitChildLists(document, list =>
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is not BitMarkdownFootnoteReferenceNode reference) continue;
                if (reference.Number > 0 && definitions.ContainsKey(reference.Label)) continue;
                list[i] = new BitMarkdownTextNode(reference.RawText);
            }
        });
    }
}
