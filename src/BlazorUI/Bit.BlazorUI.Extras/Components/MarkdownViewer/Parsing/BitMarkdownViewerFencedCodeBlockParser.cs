using System.Text;

namespace Bit.BlazorUI;

/// <summary>Parses fenced code blocks (<c>```</c> / <c>~~~</c>).</summary>
public sealed class BitMarkdownViewerFencedCodeBlockParser : BitMarkdownViewerBlockParser
{
    public override int Order => 10;

    public override bool CanInterruptParagraph(BitMarkdownViewerBlockProcessor state, int lineIndex)
        => BitMarkdownViewerBlockGrammar.Fence().IsMatch(state.Lines[lineIndex]);

    public override bool TryParse(BitMarkdownViewerBlockProcessor state, List<BitMarkdownViewerMarkdownNode> output)
    {
        var lines = state.Lines;
        var fence = BitMarkdownViewerBlockGrammar.Fence().Match(lines[state.Line]);
        if (!fence.Success) return false;

        string marker = fence.Groups["fence"].Value;
        char fenceChar = marker[0];
        int fenceLen = marker.Length;
        string info = fence.Groups["info"].Value.Trim();
        int indent = BitMarkdownViewerBlockProcessor.GetIndent(lines[state.Line]);

        var sb = new StringBuilder();
        int i = state.Line + 1;
        while (i < lines.Count)
        {
            string l = lines[i];
            var close = BitMarkdownViewerBlockGrammar.FenceClose().Match(l);
            if (close.Success && close.Groups[1].Value[0] == fenceChar
                && close.Groups[1].Value.Length >= fenceLen)
            {
                i++;
                break;
            }
            sb.Append(BitMarkdownViewerBlockProcessor.StripIndent(l, indent)).Append('\n');
            i++;
        }

        output.Add(new BitMarkdownViewerCodeBlockNode
        {
            Info = string.IsNullOrEmpty(info) ? null : info,
            Content = BitMarkdownViewerBlockProcessor.TrimTrailingNewline(sb.ToString())
        });
        state.Line = i;
        return true;
    }
}
