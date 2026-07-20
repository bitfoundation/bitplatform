using System.Text;

namespace Bit.BlazorUI;

/// <summary>Parses fenced code blocks (<c>```</c> / <c>~~~</c>).</summary>
public sealed class BitMarkdownFencedCodeBlockParser : BitMarkdownBlockParser
{
    public override int Order => 10;

    public override bool CanInterruptParagraph(BitMarkdownBlockProcessor state, int lineIndex)
        => BitMarkdownBlockGrammar.Fence().IsMatch(state.Lines[lineIndex]);

    public override bool TryParse(BitMarkdownBlockProcessor state, List<BitMarkdownNode> output)
    {
        var lines = state.Lines;
        var fence = BitMarkdownBlockGrammar.Fence().Match(lines[state.Line]);
        if (!fence.Success) return false;

        string marker = fence.Groups["fence"].Value;
        char fenceChar = marker[0];
        int fenceLen = marker.Length;
        string info = fence.Groups["info"].Value.Trim();
        int indent = BitMarkdownBlockProcessor.GetIndent(lines[state.Line]);

        var sb = new StringBuilder();
        int i = state.Line + 1;
        while (i < lines.Count)
        {
            string l = lines[i];
            var close = BitMarkdownBlockGrammar.FenceClose().Match(l);
            if (close.Success && close.Groups[1].Value[0] == fenceChar
                && close.Groups[1].Value.Length >= fenceLen)
            {
                i++;
                break;
            }
            sb.Append(BitMarkdownBlockProcessor.StripIndent(l, indent)).Append('\n');
            i++;
        }

        output.Add(new BitMarkdownCodeBlockNode
        {
            Info = string.IsNullOrEmpty(info) ? null : info,
            Content = BitMarkdownBlockProcessor.TrimTrailingNewline(sb.ToString())
        });
        state.Line = i;
        return true;
    }
}
