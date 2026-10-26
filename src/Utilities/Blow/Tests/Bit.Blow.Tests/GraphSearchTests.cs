namespace Bit.Blow.Tests;

/// <summary>Finding nodes and edges by what they say.</summary>
[TestClass]
public class GraphSearchTests
{
    private static readonly List<BlowNode> Nodes =
    [
        new() { Id = "n1", Label = "Résumé parser" },
        new() { Id = "n2", Label = "Parse" },
        new() { Id = "n3", Label = "Pre-parse cleanup" },
        new() { Id = "n4", Label = "Sum", Ports = [new BlowPort { Id = "a", Label = "Left input", DataType = "number" }] },
        new() { Id = "n5", Label = "Sparse matrix" },
        new() { Id = "parse", Label = "Unrelated", AriaLabel = "Parsing step" },
        new() { Id = "n6", Label = "Parser config" },
    ];

    private static string[] Ids(IEnumerable<BlowNode> nodes) => nodes.Select(n => n.Id).ToArray();

    [TestMethod]
    public void TheBestMatchesComeFirst()
    {
        // Equal to the query, starting with it, a word starting with it, then the query anywhere.
        // Ties keep the graph's order.
        CollectionAssert.AreEqual(new[] { "n2", "parse", "n6", "n1", "n3", "n5" }, Ids(BlowGraphSearch.FindNodes(Nodes, "parse")));
    }

    [TestMethod]
    public void CaseAndAccentsAreIgnored()
    {
        CollectionAssert.AreEqual(new[] { "n1" }, Ids(BlowGraphSearch.FindNodes(Nodes, "RESUME")));
    }

    [TestMethod]
    public void EveryWordHasToMatchSomewhere()
    {
        CollectionAssert.AreEqual(new[] { "n4" }, Ids(BlowGraphSearch.FindNodes(Nodes, "sum left")));
        Assert.IsEmpty(BlowGraphSearch.FindNodes(Nodes, "sum right"));
    }

    [TestMethod]
    public void PortsAndTextOfYourOwnAreSearched()
    {
        CollectionAssert.AreEqual(new[] { "n4" }, Ids(BlowGraphSearch.FindNodes(Nodes, "number")));

        var found = BlowGraphSearch.FindNodes(Nodes, "matrix ops", n => n.Id == "n5" ? "linear algebra ops" : null);
        CollectionAssert.AreEqual(new[] { "n5" }, Ids(found));
    }

    [TestMethod]
    public void AnEmptyQueryFindsNothing()
    {
        Assert.IsEmpty(BlowGraphSearch.FindNodes(Nodes, "   "));
        Assert.IsEmpty(BlowGraphSearch.FindNodes(Nodes, ""));
    }

    [TestMethod]
    public void TheQueryIsTextNotAPattern()
    {
        var nodes = new List<BlowNode> { new() { Id = "x", Label = "a.b" }, new() { Id = "y", Label = "axb" } };

        CollectionAssert.AreEqual(new[] { "x" }, Ids(BlowGraphSearch.FindNodes(nodes, "a.b")));
        Assert.IsEmpty(BlowGraphSearch.FindNodes(nodes, "(a+)+$"));
    }

    [TestMethod]
    public void EdgesAreFoundByTheNamesOfTheNodesTheyJoin()
    {
        var edges = new List<BlowEdge>
        {
            new() { Id = "e1", Source = "n2", Target = "n4" },
            new() { Id = "e2", Source = "n4", Target = "n7", Label = "total" },
        };
        var names = new Dictionary<string, string> { ["n2"] = "Parse", ["n4"] = "Sum", ["n7"] = "Report" };
        string[] Find(string query) => BlowGraphSearch.FindEdges(edges, query, id => names[id]).Select(e => e.Id).ToArray();

        CollectionAssert.AreEqual(new[] { "e1", "e2" }, Find("sum"));
        CollectionAssert.AreEqual(new[] { "e2" }, Find("total"));
        CollectionAssert.AreEqual(new[] { "e1" }, Find("parse sum"));

        // Without names, by the ids of the nodes.
        CollectionAssert.AreEqual(new[] { "e2" }, BlowGraphSearch.FindEdges(edges, "n7").Select(e => e.Id).ToArray());
    }
}
