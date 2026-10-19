using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Blow.Tests;

/// <summary>
/// Tests for edges that come back to the node they left. Every ordinary path algorithm
/// degenerates on them - both anchors sit on one node, so the "route between two nodes" the
/// algorithm exists to compute does not exist - and the result is a line laid over the node.
/// </summary>
[TestClass]
public class SelfLoopTests : BunitContext
{
    public SelfLoopTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>Samples a cubic bezier "M s C c1 c2 t" at t, from the four control points.</summary>
    private static BlowXYPosition Sample(
        BlowXYPosition s, BlowXYPosition c1, BlowXYPosition c2, BlowXYPosition t, double u)
    {
        var v = 1 - u;
        var a = v * v * v;
        var b = 3 * v * v * u;
        var c = 3 * v * u * u;
        var d = u * u * u;
        return new BlowXYPosition(
            a * s.X + b * c1.X + c * c2.X + d * t.X,
            a * s.Y + b * c1.Y + c * c2.Y + d * t.Y);
    }

    private static (BlowXYPosition S, BlowXYPosition C1, BlowXYPosition C2, BlowXYPosition T) BlowControls(string path)
    {
        // "M{sx},{sy} C{c1x},{c1y} {c2x},{c2y} {tx},{ty}"
        var numbers = System.Text.RegularExpressions.Regex
            .Matches(path, @"-?\d+(\.\d+)?")
            .Select(m => double.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();

        Assert.AreEqual(8, numbers.Length);
        return (new(numbers[0], numbers[1]), new(numbers[2], numbers[3]),
                new(numbers[4], numbers[5]), new(numbers[6], numbers[7]));
    }

    /// <summary>The furthest the curve strays from the straight line joining its endpoints.</summary>
    private static double MaxBulge(string path)
    {
        var (s, c1, c2, t) = BlowControls(path);
        var worst = 0.0;
        for (var i = 1; i < 20; i++)
        {
            var p = Sample(s, c1, c2, t, i / 20.0);
            var chord = Sample(s, s, t, t, i / 20.0);
            worst = Math.Max(worst, Math.Sqrt(
                Math.Pow(p.X - chord.X, 2) + Math.Pow(p.Y - chord.Y, 2)));
        }
        return worst;
    }

    // ---- the geometry itself ----

    [TestMethod]
    public void ALoopBetweenOppositeSidesSwingsClearOfTheNode()
    {
        // Right handle back round to the left handle, both at the node's mid-height: the plain
        // bezier for this puts all four control points on one horizontal line.
        var loop = BlowEdgePath.Loop(150, 20, BlowPosition.Right, 0, 20, BlowPosition.Left, radius: 60);

        Assert.IsTrue(MaxBulge(loop.Path) > 40, "the loop stayed flat against the node");
    }

    [TestMethod]
    public void ALoopBetweenTopAndBottomAlsoSwingsClear()
    {
        var loop = BlowEdgePath.Loop(75, 40, BlowPosition.Bottom, 75, 0, BlowPosition.Top, radius: 60);

        Assert.IsTrue(MaxBulge(loop.Path) > 40, "the loop stayed flat against the node");
    }

    [TestMethod]
    public void ALoopBetweenTwoHandlesOnTheSameSideBulgesOutFromIt()
    {
        // Two handles on the right at different heights need no sideways kick: pushing both
        // controls straight out already makes a D.
        var loop = BlowEdgePath.Loop(150, 10, BlowPosition.Right, 150, 60, BlowPosition.Right, radius: 60);

        var (_, c1, c2, _) = BlowControls(loop.Path);
        Assert.IsTrue(c1.X > 150 && c2.X > 150, "the loop did not stand off the node");
        Assert.IsTrue(MaxBulge(loop.Path) > 20);
    }

    [TestMethod]
    public void TwoHandlesMeasuredAtTheSamePointStillProduceAVisibleLoop()
    {
        // The degenerate-of-the-degenerate: nothing at all distinguishes the two ends.
        var loop = BlowEdgePath.Loop(100, 100, BlowPosition.Right, 100, 100, BlowPosition.Right, radius: 50);

        Assert.IsTrue(MaxBulge(loop.Path) > 20, "a zero-length loop collapsed to a point");
    }

    [TestMethod]
    public void ALargerRadiusStandsTheLoopFurtherOff()
    {
        var small = MaxBulge(BlowEdgePath.Loop(150, 20, BlowPosition.Right, 0, 20, BlowPosition.Left, radius: 30).Path);
        var large = MaxBulge(BlowEdgePath.Loop(150, 20, BlowPosition.Right, 0, 20, BlowPosition.Left, radius: 90).Path);

        Assert.IsTrue(large > small * 2);
    }

    [TestMethod]
    public void TheLoopStartsAndEndsExactlyOnItsHandles()
    {
        var loop = BlowEdgePath.Loop(150, 20, BlowPosition.Right, 0, 20, BlowPosition.Left);
        var (s, _, _, t) = BlowControls(loop.Path);

        Assert.AreEqual(new BlowXYPosition(150, 20), s);
        Assert.AreEqual(new BlowXYPosition(0, 20), t);
    }

    [TestMethod]
    public void TheLabelSitsOnTheLoopRatherThanInsideTheNode()
    {
        var loop = BlowEdgePath.Loop(150, 20, BlowPosition.Right, 0, 20, BlowPosition.Left, radius: 60);
        var (s, c1, c2, t) = BlowControls(loop.Path);
        var midpoint = Sample(s, c1, c2, t, 0.5);

        Assert.AreEqual(midpoint.X, loop.LabelX, 1e-6);
        Assert.AreEqual(midpoint.Y, loop.LabelY, 1e-6);
    }

    [TestMethod]
    public void TheLoopPathIsWrittenWithInvariantNumbers()
    {
        var original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var loop = BlowEdgePath.Loop(1.5, 2.5, BlowPosition.Right, 3.5, 4.5, BlowPosition.Left, radius: 10.5);

            Assert.DoesNotContain(";", loop.Path);
            Assert.Contains("1.5", loop.Path);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = original;
        }
    }

    // ---- the canvas choosing it ----

    private IRenderedComponent<BlowCanvas> Render(
        List<BlowNode> nodes, List<BlowEdge> edges,
        Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges);
            extra?.Invoke(p);
        });

    [TestMethod]
    public void AnEdgeBackToItsOwnNodeIsDrawnAsALoop()
    {
        List<BlowNode> nodes = [new() { Id = "a", Position = new(0, 0), Width = 150, Height = 40 }];
        List<BlowEdge> edges = [new() { Id = "self", Source = "a", Target = "a" }];

        var cut = Render(nodes, edges);
        var path = cut.Find(".bit-blow-edge-path").GetAttribute("d")!;

        Assert.IsTrue(MaxBulge(path) > 30, $"the self-edge was drawn flat: {path}");
    }

    [TestMethod]
    public void ASelfEdgeLoopsWhateverPathTypeItAsksFor()
    {
        // A straight self-edge would be a zero-length line; a step one would fold flat. Both have
        // to be looped, or the edge is simply invisible.
        foreach (var type in new[] { BlowEdgeType.Straight, BlowEdgeType.Step, BlowEdgeType.SmoothStep, BlowEdgeType.SimpleBezier })
        {
            List<BlowNode> nodes = [new() { Id = "a", Position = new(0, 0), Width = 150, Height = 40 }];
            List<BlowEdge> edges = [new() { Id = "self", Source = "a", Target = "a", Type = type }];

            var cut = Render(nodes, edges);
            var path = cut.Find(".bit-blow-edge-path").GetAttribute("d")!;

            Assert.IsTrue(MaxBulge(path) > 30, $"{type} self-edge was drawn flat: {path}");
        }
    }

    [TestMethod]
    public void AnOrdinaryEdgeIsUnaffected()
    {
        List<BlowNode> nodes =
        [
            new() { Id = "a", Position = new(0, 0), Width = 150, Height = 40 },
            new() { Id = "b", Position = new(0, 200), Width = 150, Height = 40 },
        ];
        List<BlowEdge> edges = [new() { Id = "e", Source = "a", Target = "b", Type = BlowEdgeType.Straight }];

        var cut = Render(nodes, edges);
        var path = cut.Find(".bit-blow-edge-path").GetAttribute("d")!;

        Assert.StartsWith("M ", path);
        Assert.Contains(" L ", path);
    }

    [TestMethod]
    public void LoopRadiusIsTunablePerEdge()
    {
        List<BlowNode> Nodes() => [new() { Id = "a", Position = new(0, 0), Width = 150, Height = 40 }];

        var tight = Render(Nodes(),
            [new() { Id = "self", Source = "a", Target = "a", PathOptions = new() { LoopRadius = 20 } }]);
        var wide = Render(Nodes(),
            [new() { Id = "self", Source = "a", Target = "a", PathOptions = new() { LoopRadius = 120 } }]);

        var tightBulge = MaxBulge(tight.Find(".bit-blow-edge-path").GetAttribute("d")!);
        var wideBulge = MaxBulge(wide.Find(".bit-blow-edge-path").GetAttribute("d")!);

        Assert.IsTrue(wideBulge > tightBulge * 2);
    }

    [TestMethod]
    public void ChangingTheLoopRadiusInvalidatesTheCachedGeometry()
    {
        var edge = new BlowEdge { Id = "self", Source = "a", Target = "a", PathOptions = new() { LoopRadius = 20 } };
        List<BlowNode> nodes = [new() { Id = "a", Position = new(0, 0), Width = 150, Height = 40 }];

        var cut = Render(nodes, [edge]);
        var before = cut.Find(".bit-blow-edge-path").GetAttribute("d")!;

        edge.PathOptions!.LoopRadius = 120;
        cut.Render();

        Assert.AreNotEqual(before, cut.Find(".bit-blow-edge-path").GetAttribute("d")!);
    }
}
