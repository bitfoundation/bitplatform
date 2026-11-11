namespace Bit.Bdnd.Tests.Core;

/// <summary>Moving the nodes of a tree: never into themselves, with their subtrees, and back again.</summary>
[TestClass]
public class BdndTreeTests
{
    private sealed class Node(string name, params Node[] children)
    {
        public string Name { get; } = name;
        public List<Node> Children { get; } = [.. children];

        public override string ToString() => Name;
    }

    private static IList<Node> ChildrenOf(Node node) => node.Children;

    // root: docs (invoices, letters), photos (2025), music
    private static (Node Root, Node Docs, Node Invoices, Node Letters, Node Photos, Node Year, Node Music) Tree()
    {
        var invoices = new Node("invoices");
        var letters = new Node("letters");
        var docs = new Node("docs", invoices, letters);
        var year = new Node("2025");
        var photos = new Node("photos", year);
        var music = new Node("music");
        return (new Node("root", docs, photos, music), docs, invoices, letters, photos, year, music);
    }

    private static string Outline(Node node) => string.Join(", ", node.Children.Select(child =>
        child.Children.Count == 0 ? child.Name : $"{child.Name} ({Outline(child)})"));

    [TestMethod]
    public void ANodeContainsItselfAndItsDescendantsOnly()
    {
        var t = Tree();

        Assert.IsTrue(BdndTree.Contains(t.Docs, t.Docs, ChildrenOf));
        Assert.IsTrue(BdndTree.Contains(t.Docs, t.Letters, ChildrenOf));
        Assert.IsTrue(BdndTree.Contains(t.Root, t.Year, ChildrenOf));
        Assert.IsFalse(BdndTree.Contains(t.Docs, t.Photos, ChildrenOf));
        Assert.IsFalse(BdndTree.Contains(t.Letters, t.Docs, ChildrenOf));
    }

    [TestMethod]
    public void TheParentIsTheNodeWhoseChildrenHoldTheNode()
    {
        var t = Tree();

        Assert.AreSame(t.Docs, BdndTree.ParentOf(t.Root, t.Letters, ChildrenOf));
        Assert.AreSame(t.Root, BdndTree.ParentOf(t.Root, t.Music, ChildrenOf));
        Assert.IsNull(BdndTree.ParentOf(t.Root, t.Root, ChildrenOf));
        Assert.IsNull(BdndTree.ParentOf(t.Root, new Node("elsewhere"), ChildrenOf));
    }

    [TestMethod]
    public void ANodeMovesWithItsSubtreeIntoAnotherNode()
    {
        var t = Tree();

        var move = BdndTree.PlanMove(t.Root, t.Docs, t.Photos, 1, ChildrenOf);

        Assert.IsNotNull(move);
        Assert.IsTrue(move.Apply());
        Assert.AreEqual("photos (2025, docs (invoices, letters)), music", Outline(t.Root));

        Assert.IsTrue(move.Revert());
        Assert.AreEqual("docs (invoices, letters), photos (2025), music", Outline(t.Root));
    }

    [TestMethod]
    public void ANodeMovesAmongItsSiblings()
    {
        var t = Tree();

        Assert.IsTrue(BdndTree.PlanMove(t.Root, t.Music, t.Root, 0, ChildrenOf)!.Apply());

        Assert.AreEqual("music, docs (invoices, letters), photos (2025)", Outline(t.Root));
    }

    [TestMethod]
    public void ANodeIsNeverMovedIntoItself()
    {
        var t = Tree();

        Assert.IsNull(BdndTree.PlanMove(t.Root, t.Docs, t.Docs, 0, ChildrenOf));
        Assert.IsNull(BdndTree.PlanMove(t.Root, t.Docs, t.Letters, 0, ChildrenOf));
        Assert.AreEqual("docs (invoices, letters), photos (2025), music", Outline(t.Root));
    }

    [TestMethod]
    public void ANodeOutsideTheTreeOrAMoveThatChangesNothingIsNull()
    {
        var t = Tree();

        Assert.IsNull(BdndTree.PlanMove(t.Root, new Node("elsewhere"), t.Docs, 0, ChildrenOf));
        Assert.IsNull(BdndTree.PlanMove(t.Root, t.Root, t.Docs, 0, ChildrenOf));
        Assert.IsNull(BdndTree.PlanMove(t.Root, t.Invoices, t.Docs, 1, ChildrenOf)); // after itself
        Assert.IsNull(BdndTree.PlanMove(t.Root, t.Invoices, t.Docs, 5, ChildrenOf)); // out of range
    }

    [TestMethod]
    public void ADeepTreeIsWalkedWithoutRecursion()
    {
        var root = new Node("0");
        var deepest = root;
        for (var i = 1; i <= 100_000; i++)
        {
            var child = new Node(i.ToString(System.Globalization.CultureInfo.InvariantCulture));
            deepest.Children.Add(child);
            deepest = child;
        }

        Assert.IsTrue(BdndTree.Contains(root, deepest, ChildrenOf));
        Assert.IsNotNull(BdndTree.ParentOf(root, deepest, ChildrenOf));
        Assert.IsNull(BdndTree.PlanMove(root, root.Children[0], deepest, 0, ChildrenOf));
    }
}
