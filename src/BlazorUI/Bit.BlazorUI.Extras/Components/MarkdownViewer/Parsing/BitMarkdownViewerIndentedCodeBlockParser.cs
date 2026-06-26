using System.Text;

namespace Bit.BlazorUI;

/// <summary>Parses indented (4-space) code blocks.</summary>
public sealed class BitMarkdownViewerIndentedCodeBlockParser : BitMarkdownViewerBlockParser
{
    public override int Order => 50;

    public override bool TryParse(BitMarkdownViewerBlockProcessor state, List<BitMarkdownViewerMarkdownNode> output)
    {
        var lines = state.Lines;
        var first = lines[state.Line];
        if (BitMarkdownViewerBlockProcessor.GetIndent(first) < 4) return false;
        // A line that is blank after its indentation must not open a code block.
        if (BitMarkdownViewerBlockProcessor.IsBlank(first)) return false;

        var sb = new StringBuilder();
        int i = state.Line;
        int lastNonBlank = state.Line;
        while (i < lines.Count)
        {
            string l = lines[i];
            if (BitMarkdownViewerBlockProcessor.IsBlank(l)) { sb.AppendLine(string.Empty); i++; continue; }
            if (BitMarkdownViewerBlockProcessor.GetIndent(l) < 4) break;
            sb.AppendLine(BitMarkdownViewerBlockProcessor.StripIndent(l, 4));
            lastNonBlank = i;
            i++;
        }

        output.Add(new BitMarkdownViewerCodeBlockNode
        {
            Content = BitMarkdownViewerBlockProcessor.TrimTrailingNewline(sb.ToString()).TrimEnd('\r', '\n')
        });
        state.Line = lastNonBlank + 1;
        return true;
    }
}
