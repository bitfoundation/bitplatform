using BenchmarkDotNet.Attributes;

namespace Bit.Bdnd.Benchmarks;

/// <summary>
/// The arithmetic of a drop: working a move of a selection out (<c>BdndList.PlanMove</c>), making it and taking it
/// back, and refusing a tree node's own subtree (<see cref="BdndTree"/>). Each runs once per drop - or, for the tree,
/// once per target the drag passes over - so the budget is "imperceptible", well under a frame.
/// </summary>
[MemoryDiagnoser]
public class ListBenchmarks
{
    private sealed class Node
    {
        public List<Node> Children { get; } = [];
    }

    private List<int> _list = null!;
    private int[] _selection = null!;
    private Node _root = null!;
    private Node _deepest = null!;

    /// <summary>Items of the list, and nodes of the tree.</summary>
    [Params(1_000, 10_000)]
    public int Items { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _list = [.. Enumerable.Range(0, Items)];
        // a selection of one item in a hundred, spread over the list
        _selection = [.. Enumerable.Range(0, Items / 100).Select(i => i * 100 + 7)];

        // a tree ten wide at every level, filled breadth first; the last node is the deepest
        _root = new Node();
        _deepest = _root;
        var level = new Queue<Node>([_root]);
        for (var count = 1; count < Items;)
        {
            var parent = level.Dequeue();
            for (var c = 0; c < 10 && count < Items; c++, count++)
            {
                var child = new Node();
                parent.Children.Add(child);
                level.Enqueue(child);
                _deepest = child;
            }
        }
    }

    /// <summary>A selection of one item in a hundred moved to the start of the list, and taken back.</summary>
    [Benchmark]
    public bool MoveSelectionAndUndo()
    {
        var move = BdndList.PlanMove(_list, _selection, _list, 0)!;
        return move.Apply() && move.Revert();
    }

    /// <summary>One item moved from the end of the list to its start, and taken back.</summary>
    [Benchmark]
    public bool MoveOneAndUndo()
    {
        var move = BdndList.PlanMove(_list, _list.Count - 1, _list, 0)!;
        return move.Apply() && move.Revert();
    }

    /// <summary>
    /// Whether the deepest node is inside the root - the whole tree walked, as a drop target's
    /// <c>CanDropPredicate</c> does for the root dragged over a leaf.
    /// </summary>
    [Benchmark]
    public bool TreeContains() => BdndTree.Contains(_root, _deepest, node => node.Children);
}
