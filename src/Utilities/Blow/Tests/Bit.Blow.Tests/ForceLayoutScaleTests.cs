namespace Bit.Blow.Tests;

/// <summary>
/// The force layout on graphs past <see cref="BlowLayout.ExactRepulsionLimit"/> nodes, where far-off groups of nodes
/// push as one (Barnes-Hut): how close that comes to the exact push, and that the layout keeps its promises - the same
/// every time, nothing overlapping, clusters apart.
/// </summary>
[TestClass]
public class ForceLayoutScaleTests
{
    private static BlowNode Box(string id) => new() { Id = id, Width = 100, Height = 40 };

    private static BlowXYPosition Centre(BlowNode node) => new(node.Position.X + 50, node.Position.Y + 20);

    private static void AssertNoOverlap(IReadOnlyList<BlowNode> nodes)
    {
        var sorted = nodes.OrderBy(n => n.Position.X).ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            for (var j = i + 1; j < sorted.Count && sorted[j].Position.X < sorted[i].Position.X + 100; j++)
            {
                var a = sorted[i].GetRect();
                var b = sorted[j].GetRect();
                var overlaps = a.Left < b.Right - 1e-6 && b.Left < a.Right - 1e-6 && a.Top < b.Bottom - 1e-6 && b.Top < a.Bottom - 1e-6;
                Assert.IsFalse(overlaps, $"{sorted[i].Id} overlaps {sorted[j].Id}");
            }
        }
    }

    /// <summary><paramref name="clusters"/> rings of <paramref name="size"/> nodes, each with chords, and nothing between them.</summary>
    private static (List<BlowNode> Nodes, List<BlowEdge> Edges) Clusters(int clusters, int size)
    {
        var nodes = new List<BlowNode>();
        var edges = new List<BlowEdge>();
        for (var c = 0; c < clusters; c++)
        {
            for (var i = 0; i < size; i++) nodes.Add(Box($"c{c}n{i}"));
            for (var i = 0; i < size; i++)
            {
                edges.Add(new BlowEdge { Id = $"c{c}r{i}", Source = $"c{c}n{i}", Target = $"c{c}n{(i + 1) % size}" });
                edges.Add(new BlowEdge { Id = $"c{c}x{i}", Source = $"c{c}n{i}", Target = $"c{c}n{(i + size / 3) % size}" });
            }
        }
        return (nodes, edges);
    }

    [TestMethod]
    public void TheApproximatePushIsCloseToTheExactOne()
    {
        // A thousand points scattered over a large square, some bunched, as a layout's are mid-way.
        var random = new Random(42);
        const int n = 1000;
        var x = new double[n];
        var y = new double[n];
        for (var i = 0; i < n; i++)
        {
            var bunch = i % 4 == 0;
            x[i] = bunch ? 2000 + random.NextDouble() * 300 : random.NextDouble() * 6000;
            y[i] = bunch ? 1000 + random.NextDouble() * 300 : random.NextDouble() * 6000;
        }

        const double k2 = 180 * 180;
        var tree = new BlowLayout.RepulsionTree(n);
        tree.Build(x, y);

        var errors = new double[n];
        for (var i = 0; i < n; i++)
        {
            double ex = 0, ey = 0;
            for (var j = 0; j < n; j++)
            {
                if (j == i) continue;
                var (fx, fy) = BlowLayout.Repulsion(x, y, i, j, k2);
                ex += fx;
                ey += fy;
            }
            var (ax, ay) = tree.Repel(i, x, y, k2, 0.9 * 0.9);
            errors[i] = Math.Sqrt((ax - ex) * (ax - ex) + (ay - ey) * (ay - ey)) / Math.Sqrt(ex * ex + ey * ey);
        }

        Array.Sort(errors);
        Assert.IsLessThan(0.02, errors[n / 2], $"median error {errors[n / 2]:P2}");
        Assert.IsLessThan(0.10, errors[n * 95 / 100], $"95th percentile error {errors[n * 95 / 100]:P2}");

        // With θ = 0 nothing is taken as a group: every push is the exact one, give or take the order of the sums.
        for (var i = 0; i < n; i += 97)
        {
            double ex = 0, ey = 0;
            for (var j = 0; j < n; j++)
            {
                if (j == i) continue;
                var (fx, fy) = BlowLayout.Repulsion(x, y, i, j, k2);
                ex += fx;
                ey += fy;
            }
            var (ax, ay) = tree.Repel(i, x, y, k2, 0);
            Assert.AreEqual(ex, ax, Math.Abs(ex) * 1e-9 + 1e-9);
            Assert.AreEqual(ey, ay, Math.Abs(ey) * 1e-9 + 1e-9);
        }
    }

    /// <summary>A cluster size that makes four clusters a graph large enough to be laid out with the quadtree.</summary>
    private static readonly int Quarter = BlowLayout.ExactRepulsionLimit / 4 + 10;

    [TestMethod]
    public void ALargeLayoutIsTheSameEveryTimeAndLeavesNothingOverlapping()
    {
        var (nodes, edges) = Clusters(4, Quarter);
        var copy = nodes.Select(n => n.Clone()).ToList();

        BlowLayout.Force(nodes, edges, new BlowForceLayoutOptions { Iterations = 120 });
        BlowLayout.Force(copy, edges, new BlowForceLayoutOptions { Iterations = 120 });

        Assert.IsTrue(nodes.All(n => double.IsFinite(n.Position.X) && double.IsFinite(n.Position.Y)), "a position is not a number");
        CollectionAssert.AreEqual(nodes.Select(n => n.Position).ToList(), copy.Select(n => n.Position).ToList());
        AssertNoOverlap(nodes);
    }

    [TestMethod]
    public void ALargeLayoutSeparatesItsClustersAsWellAsAnExactOne()
    {
        // Four lattices, each a dense patch of edges, with nothing between them.
        var side = (int)Math.Ceiling(Math.Sqrt(Quarter));
        var size = side * side;
        var nodes = new List<BlowNode>();
        var edges = new List<BlowEdge>();
        for (var c = 0; c < 4; c++)
        {
            for (var i = 0; i < size; i++) nodes.Add(Box($"c{c}n{i}"));
            for (var i = 0; i < size; i++)
            {
                if ((i + 1) % side != 0) edges.Add(new BlowEdge { Id = $"c{c}h{i}", Source = $"c{c}n{i}", Target = $"c{c}n{i + 1}" });
                if (i + side < size) edges.Add(new BlowEdge { Id = $"c{c}v{i}", Source = $"c{c}n{i}", Target = $"c{c}n{i + side}" });
            }
        }
        var exact = nodes.Select(n => n.Clone()).ToList();

        BlowLayout.Force(nodes, edges);
        BlowLayout.Force(exact, edges, new BlowForceLayoutOptions { Theta = 0 });

        // How far apart two nodes of one lattice are on average, against two of different lattices.
        double Ratio(List<BlowNode> laid)
        {
            double within = 0, across = 0;
            int w = 0, a = 0;
            for (var i = 0; i < laid.Count; i += 3)
            {
                for (var j = i + 1; j < laid.Count; j += 5)
                {
                    var (p, q) = (Centre(laid[i]), Centre(laid[j]));
                    var d = Math.Sqrt((p.X - q.X) * (p.X - q.X) + (p.Y - q.Y) * (p.Y - q.Y));
                    if (i / size == j / size) { within += d; w++; }
                    else { across += d; a++; }
                }
            }
            return within / w / (across / a);
        }

        var approximate = Ratio(nodes);
        var reference = Ratio(exact);
        Assert.IsLessThan(0.5, approximate, $"the lattices are not apart: {approximate:F3}");
        Assert.IsLessThanOrEqualTo(1.15 * reference, approximate, $"apart less than exactly: exact {reference:F3}, approximate {approximate:F3}");
    }

    [TestMethod]
    public void NodesPiledOnOnePointArePartedWhenStartingFromWhereTheyAre()
    {
        // Every node at the same place: the quadtree keeps them in one cell, and they still come apart.
        var count = 4 * Quarter;
        var nodes = Enumerable.Range(0, count).Select(i => Box($"n{i}")).ToList();
        var edges = Enumerable.Range(1, count - 1).Select(i => new BlowEdge { Id = $"e{i}", Source = $"n{(i - 1) / 2}", Target = $"n{i}" }).ToList();

        BlowLayout.Force(nodes, edges, new BlowForceLayoutOptions { StartFromCurrentPositions = true, Iterations = 100 });

        Assert.IsTrue(nodes.All(n => double.IsFinite(n.Position.X) && double.IsFinite(n.Position.Y)), "a position is not a number");
        AssertNoOverlap(nodes);
        Assert.IsGreaterThan(nodes.Count / 2, nodes.Select(n => n.Position).Distinct().Count(), "the pile did not come apart");
    }

    [TestMethod]
    [DataRow(0.0)]
    [DataRow(double.NaN)]
    [DataRow(-1.0)]
    public void WithNoThetaEveryPairIsPushedApartExactly(double theta)
    {
        // Laid out exactly whatever the size: the same as a layout no larger than the limit would be.
        var (nodes, edges) = Clusters(2, 2 * Quarter);
        var exact = nodes.Select(n => n.Clone()).ToList();

        BlowLayout.Force(nodes, edges, new BlowForceLayoutOptions { Iterations = 40, Theta = theta });
        BlowLayout.Force(exact, edges, new BlowForceLayoutOptions { Iterations = 40, Theta = 0 });

        CollectionAssert.AreEqual(exact.Select(n => n.Position).ToList(), nodes.Select(n => n.Position).ToList());
        AssertNoOverlap(nodes);
    }
}
