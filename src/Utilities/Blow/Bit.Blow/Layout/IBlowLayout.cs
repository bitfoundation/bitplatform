namespace Bit.Blow;

/// <summary>
/// A way of arranging nodes: something <c>BlowCanvas.LayoutAsync</c> can apply to the whole graph or to a selection.
/// Every built-in layout's options are one - <see cref="BlowLayeredLayoutOptions"/>, <see cref="BlowTreeLayoutOptions"/>,
/// <see cref="BlowRadialLayoutOptions"/>, <see cref="BlowForceLayoutOptions"/>, <see cref="BlowGridLayoutOptions"/>,
/// <see cref="BlowCircleLayoutOptions"/> - and an application's own layout, or an adapter to an external engine,
/// implements it the same way.
/// </summary>
public interface IBlowLayout
{
    /// <summary>
    /// Sets the <see cref="BlowNode.Position"/> of every node in <paramref name="nodes"/>, and nothing else of
    /// importance. The nodes all share one parent, so their positions are in one coordinate space;
    /// <paramref name="edges"/> are the edges among them.
    /// </summary>
    void Arrange(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges);
}
