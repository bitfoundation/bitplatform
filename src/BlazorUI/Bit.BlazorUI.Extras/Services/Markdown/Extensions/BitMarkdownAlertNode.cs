namespace Bit.BlazorUI;

/// <summary>
/// A GitHub alert: a block quote whose first line is one of <c>[!NOTE]</c>, <c>[!TIP]</c>,
/// <c>[!IMPORTANT]</c>, <c>[!WARNING]</c> or <c>[!CAUTION]</c>.
/// </summary>
public sealed class BitMarkdownAlertNode : BitMarkdownNode
{
    /// <summary>Which of the five alert kinds this is.</summary>
    public BitMarkdownAlertKind Kind { get; init; }

    /// <summary>The alert's content, i.e. the block quote without its marker line.</summary>
    public List<BitMarkdownNode> Children { get; } = new();

    public override IList<BitMarkdownNode> ChildNodes => Children;
}
