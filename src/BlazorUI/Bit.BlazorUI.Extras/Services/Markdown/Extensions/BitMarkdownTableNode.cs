namespace Bit.BlazorUI;

/// <summary>A GitHub-flavored pipe table.</summary>
public sealed class BitMarkdownTableNode : BitMarkdownNode
{
    public List<List<BitMarkdownNode>> Header { get; } = new();
    public List<BitMarkdownColumnAlignment> Alignments { get; } = new();
    public List<List<List<BitMarkdownNode>>> Rows { get; } = new();

    public override IEnumerable<IList<BitMarkdownNode>> ChildLists
    {
        get
        {
            foreach (var cell in Header) yield return cell;
            foreach (var row in Rows)
                foreach (var cell in row)
                    yield return cell;
        }
    }
}
