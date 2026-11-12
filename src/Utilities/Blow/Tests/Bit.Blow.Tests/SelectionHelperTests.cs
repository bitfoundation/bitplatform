namespace Bit.Blow.Tests;

/// <summary>The sets "select upstream", "select downstream", "select members" and "invert selection" pick.</summary>
[TestClass]
public class SelectionHelperTests
{
    private static readonly List<BlowNode> Nodes =
    [
        new() { Id = "a" }, new() { Id = "b" }, new() { Id = "c" }, new() { Id = "d" }, new() { Id = "x" },
        new() { Id = "g" }, new() { Id = "g1", ParentId = "g" }, new() { Id = "g2", ParentId = "g1" },
        new() { Id = "locked", Selectable = false },
    ];

    private static readonly List<BlowEdge> Edges =
    [
        new() { Id = "ab", Source = "a", Target = "b" }, new() { Id = "bc", Source = "b", Target = "c" },
        new() { Id = "cd", Source = "c", Target = "d" }, new() { Id = "ca", Source = "c", Target = "a" },
    ];

    private static string[] Ids(IEnumerable<BlowNode> nodes) => [.. nodes.Select(n => n.Id).Order()];

    [TestMethod]
    public void UpstreamAndDownstreamFollowTheEdges()
    {
        var graph = new BlowGraphIndex(Nodes, Edges);

        CollectionAssert.AreEqual(new[] { "c", "d" }, Ids(BlowSelection.Downstream(graph, ["c"], includeStart: true).Where(n => n.Id is "c" or "d")));
        CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, Ids(BlowSelection.Downstream(graph, ["b"])));
        CollectionAssert.AreEqual(new[] { "d" }, Ids(BlowSelection.Upstream(graph, ["d"]).Where(n => n.Id == "d")));
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, Ids(BlowSelection.Upstream(graph, ["d"], includeStart: false)));
        CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, Ids(BlowSelection.Connected(graph, ["d"])));
        Assert.AreEqual(0, BlowSelection.Downstream(graph, ["nope"]).Count);
    }

    [TestMethod]
    public void EdgesAmongAndTouchingANodeSet()
    {
        var graph = new BlowGraphIndex(Nodes, Edges);

        CollectionAssert.AreEquivalent(new[] { "ab" }, BlowSelection.EdgesAmong(graph, ["a", "b"]).Select(e => e.Id).ToArray());
        CollectionAssert.AreEquivalent(new[] { "ab", "ca" }, BlowSelection.EdgesTouching(graph, ["a"]).Select(e => e.Id).ToArray());
    }

    [TestMethod]
    public void MembersAndInverse()
    {
        CollectionAssert.AreEqual(new[] { "g1", "g2" }, Ids(BlowSelection.Members(Nodes, ["g"])));
        CollectionAssert.AreEqual(new[] { "g1" }, Ids(BlowSelection.Members(Nodes, ["g"], deep: false)));
        CollectionAssert.AreEqual(new[] { "c", "d", "g", "g1", "g2", "x" }, Ids(BlowSelection.Invert(Nodes, Nodes.Take(2))));
    }
}
