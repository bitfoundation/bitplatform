namespace Bit.Bdnd.Tests.Core;

/// <summary>
/// Moves worked out before they are made: one item or a selection, within a list or to another, applied, taken back
/// and made again - and refused once the lists no longer hold the items where the move expects them.
/// </summary>
[TestClass]
public class BdndListMoveTests
{
    [TestMethod]
    public void APlanChangesNothingUntilItIsApplied()
    {
        List<string> list = ["a", "b", "c", "d"];

        var move = BdndList.PlanMove(list, 0, list, 3);

        Assert.IsNotNull(move);
        Assert.IsFalse(move.IsApplied);
        Assert.AreSequenceEqual(new[] { "a", "b", "c", "d" }, list);
        Assert.AreEqual(2, move.FinishIndex);
        Assert.AreEqual(3, move.InsertionIndex);
        Assert.IsTrue(move.IsReorder);
        Assert.AreSequenceEqual(new[] { "a" }, move.Items);
    }

    [TestMethod]
    [DataRow(0, 3, new[] { "b", "c", "a", "d" })]
    [DataRow(3, 0, new[] { "d", "a", "b", "c" })]
    [DataRow(0, 4, new[] { "b", "c", "d", "a" })]
    [DataRow(2, 1, new[] { "a", "c", "b", "d" })]
    public void AMoveOfOneItemDoesWhatMoveDoes(int from, int insertion, string[] expected)
    {
        List<string> planned = ["a", "b", "c", "d"];
        List<string> moved = ["a", "b", "c", "d"];

        Assert.IsTrue(BdndList.PlanMove(planned, from, planned, insertion)!.Apply());
        Assert.IsTrue(BdndList.Move(moved, from, moved, insertion));

        Assert.AreSequenceEqual(expected, planned);
        Assert.AreSequenceEqual(moved, planned);
    }

    [TestMethod]
    [DataRow(new[] { 3, 1 }, 0, new[] { "b", "d", "a", "c", "e", "f" }, 0)] // to the start, in list order
    [DataRow(new[] { 0, 2 }, 6, new[] { "b", "d", "e", "f", "a", "c" }, 4)] // to the end
    [DataRow(new[] { 0, 4 }, 3, new[] { "b", "c", "a", "e", "d", "f" }, 2)] // between: one came from before
    [DataRow(new[] { 1, 2, 4 }, 2, new[] { "a", "b", "c", "e", "d", "f" }, 1)] // dropped on one of its own
    [DataRow(new[] { 0, 5 }, 3, new[] { "b", "c", "a", "f", "d", "e" }, 2)]
    public void ASelectionMovesTogetherKeepingItsOrder(int[] indexes, int insertion, string[] expected, int finish)
    {
        List<string> list = ["a", "b", "c", "d", "e", "f"];

        var move = BdndList.PlanMove(list, indexes, list, insertion)!;

        Assert.AreEqual(finish, move.FinishIndex);
        Assert.IsTrue(move.Apply());
        Assert.AreSequenceEqual(expected, list);
    }

    [TestMethod]
    public void ASelectionMovesToAnotherList()
    {
        List<string> todo = ["a", "b", "c", "d"];
        List<string> done = ["x", "y"];

        var move = BdndList.PlanMove(todo, [2, 0], done, 1)!;

        Assert.IsFalse(move.IsReorder);
        Assert.AreEqual(1, move.FinishIndex);
        Assert.AreSequenceEqual(new[] { 0, 2 }, move.SourceIndexes);
        Assert.IsTrue(move.Apply());
        Assert.AreSequenceEqual(new[] { "b", "d" }, todo);
        Assert.AreSequenceEqual(new[] { "x", "a", "c", "y" }, done);
    }

    [TestMethod]
    [DataRow(new[] { 1 }, 1)] // before itself
    [DataRow(new[] { 1 }, 2)] // after itself
    [DataRow(new[] { 1, 2 }, 1)] // a block before itself
    [DataRow(new[] { 1, 2 }, 3)] // a block after itself
    [DataRow(new[] { 2, 1 }, 2)] // a block dropped inside itself
    public void APlanThatWouldChangeNothingIsNull(int[] indexes, int insertion)
    {
        List<string> list = ["a", "b", "c", "d"];

        Assert.IsNull(BdndList.PlanMove(list, indexes, list, insertion));
    }

    [TestMethod]
    public void ASelectionWithAGapDroppedInsideItselfClosesTheGap()
    {
        List<string> list = ["a", "b", "c", "d"];

        Assert.IsTrue(BdndList.PlanMove(list, [0, 2], list, 1)!.Apply());

        Assert.AreSequenceEqual(new[] { "a", "c", "b", "d" }, list);
    }

    [TestMethod]
    [DataRow(new int[0], 0)]
    [DataRow(new[] { -1 }, 0)]
    [DataRow(new[] { 4 }, 0)]
    [DataRow(new[] { 0 }, -1)]
    [DataRow(new[] { 0 }, 3)]
    public void APlanOutOfRangeIsNull(int[] indexes, int insertion)
    {
        List<string> source = ["a", "b", "c", "d"];
        List<string> destination = ["x", "y"];

        Assert.IsNull(BdndList.PlanMove(source, indexes, destination, insertion));
    }

    [TestMethod]
    public void RepeatedIndexesCountOnce()
    {
        List<string> list = ["a", "b", "c", "d"];

        var move = BdndList.PlanMove(list, [0, 0, 1], list, 4)!;

        Assert.HasCount(2, move.Items);
        Assert.IsTrue(move.Apply());
        Assert.AreSequenceEqual(new[] { "c", "d", "a", "b" }, list);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void RevertPutsBothListsBackAndApplyMakesTheMoveAgain(bool sameList)
    {
        List<string> source = ["a", "b", "c", "d", "e"];
        List<string> destination = sameList ? source : ["x", "y", "z"];
        string[] sourceBefore = [.. source];
        string[] destinationBefore = [.. destination];

        var move = BdndList.PlanMove(source, [4, 1, 2], destination, sameList ? 0 : 2)!;
        Assert.IsTrue(move.Apply());
        string[] sourceAfter = [.. source];
        string[] destinationAfter = [.. destination];

        Assert.IsTrue(move.Revert());
        Assert.IsFalse(move.IsApplied);
        Assert.AreSequenceEqual(sourceBefore, source);
        Assert.AreSequenceEqual(destinationBefore, destination);

        Assert.IsTrue(move.Apply());
        Assert.AreSequenceEqual(sourceAfter, source);
        Assert.AreSequenceEqual(destinationAfter, destination);
    }

    [TestMethod]
    public void AMoveIsMadeOnceAndTakenBackOnce()
    {
        List<string> list = ["a", "b", "c"];
        var move = BdndList.PlanMove(list, 0, list, 3)!;

        Assert.IsFalse(move.Revert());
        Assert.IsTrue(move.Apply());
        Assert.IsFalse(move.Apply());
        Assert.AreSequenceEqual(new[] { "b", "c", "a" }, list);
        Assert.IsTrue(move.Revert());
        Assert.IsFalse(move.Revert());
        Assert.AreSequenceEqual(new[] { "a", "b", "c" }, list);
    }

    [TestMethod]
    public void AMoveWhoseItemsAreNoLongerWhereItExpectsThemChangesNothing()
    {
        List<string> list = ["a", "b", "c"];
        var stale = BdndList.PlanMove(list, 0, list, 3)!;
        list.Insert(0, "new");

        Assert.IsFalse(stale.Apply());
        Assert.AreSequenceEqual(new[] { "new", "a", "b", "c" }, list);

        var move = BdndList.PlanMove(list, 1, list, 4)!;
        Assert.IsTrue(move.Apply());
        list.RemoveAt(list.Count - 1);

        Assert.IsFalse(move.Revert());
        Assert.AreSequenceEqual(new[] { "new", "b", "c" }, list);
    }

    [TestMethod]
    public void UndoingMovesInReverseOrderRestoresTheLists()
    {
        List<string> todo = ["a", "b", "c", "d"];
        List<string> done = ["x"];
        var history = new Stack<BdndListMove<string>>();

        void Do(BdndListMove<string>? move)
        {
            Assert.IsNotNull(move);
            Assert.IsTrue(move.Apply());
            history.Push(move);
        }

        Do(BdndList.PlanMove(todo, [1, 3], done, 0));
        Do(BdndList.PlanMove(done, 2, todo, 0));
        Do(BdndList.PlanMove(todo, 0, todo, 3));

        while (history.TryPop(out var move)) Assert.IsTrue(move.Revert());

        Assert.AreSequenceEqual(new[] { "a", "b", "c", "d" }, todo);
        Assert.AreSequenceEqual(new[] { "x" }, done);
    }

    [TestMethod]
    public void AMoveRefusedBecauseAListShrankLeavesBothListsAsTheyAre()
    {
        // the items are still where the move expects them, but the destination lost the place it goes to
        List<string> source = ["a", "b", "c", "d"];
        List<string> destination = ["x", "y", "z"];
        var move = BdndList.PlanMove(source, 0, destination, 3)!;
        destination.RemoveAt(2);

        Assert.IsFalse(move.Apply());
        Assert.AreSequenceEqual(new[] { "a", "b", "c", "d" }, source);
        Assert.AreSequenceEqual(new[] { "x", "y" }, destination);

        // and the source lost the place the items go back to
        var back = BdndList.PlanMove(source, 3, destination, 0)!;
        Assert.IsTrue(back.Apply());
        source.Clear();

        Assert.IsFalse(back.Revert());
        Assert.IsEmpty(source);
        Assert.AreSequenceEqual(new[] { "d", "x", "y" }, destination);
    }

    [TestMethod]
    public void AReorderRefusedBecauseTheListShrankLeavesItAsItIs()
    {
        List<string> list = ["a", "b", "c", "d"];
        var move = BdndList.PlanMove(list, 0, list, 4)!;
        list.RemoveAt(3);

        Assert.IsFalse(move.Apply());
        Assert.AreSequenceEqual(new[] { "a", "b", "c" }, list);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AListThatIsNotAListOfTMovesAndRevertsTheSame(bool sameList)
    {
        // a List<T> is moved in one pass; any other IList<T> one item at a time - with the same outcome
        string[] start = ["a", "b", "c", "d", "e", "f", "g"];
        List<string> fast = [.. start];
        var slow = new System.Collections.ObjectModel.Collection<string>([.. start]);
        List<string> fastOther = ["x", "y"];
        var slowOther = new System.Collections.ObjectModel.Collection<string>(["x", "y"]);

        var fastMove = BdndList.PlanMove(fast, [6, 1, 3], sameList ? fast : fastOther, sameList ? 5 : 1)!;
        var slowMove = BdndList.PlanMove(slow, [6, 1, 3], sameList ? slow : slowOther, sameList ? 5 : 1)!;

        Assert.IsTrue(fastMove.Apply());
        Assert.IsTrue(slowMove.Apply());
        Assert.AreSequenceEqual(slow, fast);
        Assert.AreSequenceEqual(slowOther, fastOther);
        Assert.AreSequenceEqual(sameList ? new[] { "a", "c", "e", "b", "d", "g", "f" } : new[] { "a", "c", "e", "f" }, fast);

        Assert.IsTrue(fastMove.Revert());
        Assert.IsTrue(slowMove.Revert());
        Assert.AreSequenceEqual(start, fast);
        Assert.AreSequenceEqual(start, slow);
        Assert.AreSequenceEqual(new[] { "x", "y" }, fastOther);
        Assert.AreSequenceEqual(new[] { "x", "y" }, slowOther);
    }

    [TestMethod]
    public void ASelectionAtTheStartAndTheEndOfALongListGoesBackWhereItWas()
    {
        List<int> list = [.. Enumerable.Range(0, 1000)];
        int[] selection = [0, 1, 500, 998, 999];

        var move = BdndList.PlanMove(list, selection, list, 250)!;
        Assert.IsTrue(move.Apply());
        Assert.AreSequenceEqual(new[] { 0, 1, 500, 998, 999 }, list.Skip(move.FinishIndex).Take(5));
        Assert.HasCount(1000, list);

        Assert.IsTrue(move.Revert());
        Assert.AreSequenceEqual(Enumerable.Range(0, 1000), list);
    }
}
