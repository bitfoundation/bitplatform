namespace Bit.Bdnd.Tests.Core;

/// <summary>The index arithmetic of dropping into a list: before or after a target, in the same list or another.</summary>
[TestClass]
public class BdndListTests
{
    [TestMethod]
    [DataRow(3, null, 3)]
    [DataRow(3, BdndEdge.Top, 3)]
    [DataRow(3, BdndEdge.Left, 3)]
    [DataRow(3, BdndEdge.Bottom, 4)]
    [DataRow(3, BdndEdge.Right, 4)]
    public void TheInsertionIndexIsBeforeOrAfterTheTarget(int target, BdndEdge? edge, int expected)
        => Assert.AreEqual(expected, BdndList.InsertionIndex(target, edge));

    [TestMethod]
    [DataRow(3, BdndEdge.Left, 4)] // right to left: the left edge is after the item
    [DataRow(3, BdndEdge.Right, 3)]
    [DataRow(3, BdndEdge.Bottom, 3)] // column-reverse: the bottom edge is before it
    [DataRow(3, null, 3)]
    public void AReversedLayoutSwapsBeforeAndAfter(int target, BdndEdge? edge, int expected)
        => Assert.AreEqual(expected, BdndList.InsertionIndex(target, edge, reversed: true));

    [TestMethod]
    [DataRow(0, 2, BdndEdge.Top, 1)] // a: before c - between b and c
    [DataRow(0, 2, BdndEdge.Bottom, 2)] // a: after c
    [DataRow(3, 1, BdndEdge.Top, 1)] // d: before b
    [DataRow(3, 1, BdndEdge.Bottom, 2)] // d: after b
    [DataRow(1, 1, BdndEdge.Top, 1)] // onto itself
    [DataRow(1, 0, BdndEdge.Bottom, 1)] // after the one before it: where it is
    [DataRow(1, 2, BdndEdge.Top, 1)] // before the one after it: where it is
    public void TheReorderIndexAccountsForTheItemLeavingItsPlace(int start, int target, BdndEdge edge, int expected)
        => Assert.AreEqual(expected, BdndList.ReorderIndex(start, target, edge));

    [TestMethod]
    public void ReorderMovesAnItemWithinAList()
    {
        List<string> list = ["a", "b", "c", "d"];

        Assert.IsTrue(BdndList.Reorder(list, 0, 2));

        Assert.AreSequenceEqual(new[] { "b", "c", "a", "d" }, list);
    }

    [TestMethod]
    [DataRow(1, 1)]
    [DataRow(-1, 1)]
    [DataRow(1, 4)]
    [DataRow(4, 0)]
    public void ReorderOfNothingOrOutOfRangeChangesNothing(int start, int finish)
    {
        List<string> list = ["a", "b", "c", "d"];

        Assert.IsFalse(BdndList.Reorder(list, start, finish));

        Assert.AreSequenceEqual(new[] { "a", "b", "c", "d" }, list);
    }

    [TestMethod]
    public void MoveTakesAnItemToAnotherList()
    {
        List<string> todo = ["a", "b"];
        List<string> done = ["x", "y"];

        Assert.IsTrue(BdndList.Move(todo, 0, done, BdndList.InsertionIndex(0, BdndEdge.Bottom)));

        Assert.AreSequenceEqual(new[] { "b" }, todo);
        Assert.AreSequenceEqual(new[] { "x", "a", "y" }, done);
    }

    [TestMethod]
    public void MoveToTheEndOfAnotherList()
    {
        List<string> todo = ["a"];
        List<string> done = ["x"];

        Assert.IsTrue(BdndList.Move(todo, 0, done, done.Count));

        Assert.IsEmpty(todo);
        Assert.AreSequenceEqual(new[] { "x", "a" }, done);
    }

    [TestMethod]
    [DataRow(0, 3, new[] { "b", "c", "a", "d" })] // insertion index 3 = before d
    [DataRow(3, 0, new[] { "d", "a", "b", "c" })]
    [DataRow(0, 4, new[] { "b", "c", "d", "a" })] // the end
    public void MoveWithinTheSameListTakesAnInsertionIndexInTheListAsItIs(int from, int insertion, string[] expected)
    {
        List<string> list = ["a", "b", "c", "d"];

        Assert.IsTrue(BdndList.Move(list, from, list, insertion));

        Assert.AreSequenceEqual(expected, list);
    }

    [TestMethod]
    [DataRow(1, 1)] // before itself
    [DataRow(1, 2)] // after itself
    public void MoveWithinTheSameListToWhereTheItemIsChangesNothing(int from, int insertion)
    {
        List<string> list = ["a", "b", "c"];

        Assert.IsFalse(BdndList.Move(list, from, list, insertion));

        Assert.AreSequenceEqual(new[] { "a", "b", "c" }, list);
    }

    [TestMethod]
    [DataRow(-1, 0)]
    [DataRow(2, 0)]
    [DataRow(0, 3)]
    public void MoveOutOfRangeChangesNothing(int from, int insertion)
    {
        List<string> source = ["a", "b"];
        List<string> destination = ["x", "y"];

        Assert.IsFalse(BdndList.Move(source, from, destination, insertion));

        Assert.HasCount(2, source);
        Assert.HasCount(2, destination);
    }
}
