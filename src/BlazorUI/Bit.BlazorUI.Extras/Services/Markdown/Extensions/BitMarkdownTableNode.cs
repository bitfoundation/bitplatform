namespace Bit.BlazorUI;

/// <summary>A GitHub-flavored pipe table.</summary>
public sealed class BitMarkdownTableNode : BitMarkdownNode
{
    public List<List<BitMarkdownNode>> Header { get; } = new();
    /// <summary>
    /// The alignment each column carries, in column order, or <see langword="null"/> where the delimiter row
    /// asked for none. A pipe table names its alignment physically, so only <see cref="BitTextAlign.Left"/>,
    /// <see cref="BitTextAlign.Center"/> and <see cref="BitTextAlign.Right"/> are ever parsed out of one.
    /// </summary>
    public List<BitTextAlign?> Alignments { get; } = new();
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
