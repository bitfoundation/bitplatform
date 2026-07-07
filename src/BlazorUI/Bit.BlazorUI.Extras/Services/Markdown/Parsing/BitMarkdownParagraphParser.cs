namespace Bit.BlazorUI;

/// <summary>The fallback parser: gathers a paragraph and detects setext headings.</summary>
public sealed class BitMarkdownViewerParagraphParser : BitMarkdownViewerBlockParser
{
    public override int Order => 1000;

    public override bool TryParse(BitMarkdownViewerBlockProcessor state, List<BitMarkdownViewerMarkdownNode> output)
    {
        var lines = state.Lines;
        var buffer = new List<string>();
        int i = state.Line;

        while (i < lines.Count)
        {
            string l = lines[i];
            if (BitMarkdownViewerBlockProcessor.IsBlank(l)) break;

            if (buffer.Count > 0)
            {
                var setext = BitMarkdownViewerBlockGrammar.Setext().Match(l);
                if (setext.Success)
                {
                    int level = setext.Groups[1].Value[0] == '=' ? 1 : 2;
                    var heading = new BitMarkdownViewerHeadingNode { Level = level };
                    heading.Inlines.AddRange(state.ParseInlines(string.Join('\n', buffer).Trim()));
                    output.Add(heading);
                    state.Line = i + 1;
                    return true;
                }

                if (state.StartsBlock(i)) break;
            }

            // Keep trailing spaces so two-space hard breaks survive.
            buffer.Add(l.TrimStart());
            i++;
        }

        if (buffer.Count > 0)
        {
            var para = new BitMarkdownViewerParagraphNode();
            para.Inlines.AddRange(state.ParseInlines(string.Join('\n', buffer)));
            output.Add(para);
        }
        state.Line = i;
        return true;
    }
}
