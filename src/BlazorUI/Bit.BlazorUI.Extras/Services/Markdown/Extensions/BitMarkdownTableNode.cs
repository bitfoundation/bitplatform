namespace Bit.BlazorUI;

/// <summary>A GitHub-flavored pipe table.</summary>
public sealed class BitMarkdownViewerTableNode : BitMarkdownViewerMarkdownNode
{
    public List<List<BitMarkdownViewerMarkdownNode>> Header { get; } = new();
    public List<BitMarkdownViewerColumnAlignment> Alignments { get; } = new();
    public List<List<List<BitMarkdownViewerMarkdownNode>>> Rows { get; } = new();

    public override IEnumerable<IList<BitMarkdownViewerMarkdownNode>> ChildLists
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
