using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Blow.Tests;

/// <summary>
/// Tests for what the canvas actually renders: the minimap's view box, the reconnect grab
/// areas, the connection line's styling hooks, default arrowhead colouring and toolbar anchoring.
/// </summary>
[TestClass]
public class PresentationTests : BunitContext
{
    public PresentationTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static List<BlowNode> TwoNodes() =>
    [
        new() { Id = "a", Position = new(0, 0), Width = 100, Height = 40 },
        new() { Id = "b", Position = new(0, 300), Width = 100, Height = 40 },
    ];

    // ---- minimap view box ----

    [TestMethod]
    [DataRow(200, 150)]
    [DataRow(120, 240)]
    [DataRow(100, 100)]
    public void TheMinimapViewBoxMatchesTheMinimapsOwnShape(int width, int height)
    {
        // A view box of a different aspect ratio would be letterboxed by the SVG, wasting half
        // the minimap and silently shifting every pointer position mapped back into flow space.
        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, TwoNodes());
            p.Add(c => c.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<BlowMiniMap>(0);
                b.AddAttribute(1, nameof(BlowMiniMap.Width), width);
                b.AddAttribute(2, nameof(BlowMiniMap.Height), height);
                b.CloseComponent();
            }));
        });

        var parts = cut.Find(".bit-blow-minimap").GetAttribute("viewBox")!.Split(' ');
        var boxWidth = double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
        var boxHeight = double.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);

        Assert.AreEqual((double)width / height, boxWidth / boxHeight, 1e-6);
    }

    [TestMethod]
    public void TheMinimapViewBoxStillContainsEveryNode()
    {
        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, TwoNodes());
            p.Add(c => c.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<BlowMiniMap>(0);
                b.AddAttribute(1, nameof(BlowMiniMap.Width), 200);
                b.AddAttribute(2, nameof(BlowMiniMap.Height), 150);
                b.CloseComponent();
            }));
        });

        var parts = cut.Find(".bit-blow-minimap").GetAttribute("viewBox")!
            .Split(' ')
            .Select(v => double.Parse(v, System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();

        var box = new BlowRect(parts[0], parts[1], parts[2], parts[3]);

        // Widening for the aspect ratio must only ever add room, never crop the graph out.
        Assert.IsTrue(box.Left <= 0 && box.Top <= 0);
        Assert.IsTrue(box.Right >= 100 && box.Bottom >= 340);
    }

    // ---- edge reconnect grab area ----

    [TestMethod]
    public void ASelectedEdgeGetsAGrabAreaLargerThanItsVisibleDot()
    {
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", Selected = true } };

        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, TwoNodes());
            p.Add(c => c.Edges, edges);
            p.Add(c => c.ReconnectRadius, 16);
        });

        var dot = Radius(cut.Find(".bit-blow-edge-reconnect"));
        var hit = Radius(cut.Find(".bit-blow-edge-reconnect-hit"));

        Assert.AreEqual(4, dot, 1e-6);
        Assert.AreEqual(16, hit, 1e-6);
    }

    [TestMethod]
    public void TheGrabAreaNeverShrinksBelowTheVisibleDot()
    {
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", Selected = true } };

        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, TwoNodes());
            p.Add(c => c.Edges, edges);
            p.Add(c => c.ReconnectRadius, 0);
        });

        Assert.AreEqual(4, Radius(cut.Find(".bit-blow-edge-reconnect-hit")), 1e-6);
    }

    [TestMethod]
    public void TheGrabAreaKeepsAConstantScreenSizeAsTheFlowZooms()
    {
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", Selected = true } };

        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, TwoNodes());
            p.Add(c => c.Edges, edges);
            p.Add(c => c.ReconnectRadius, 20);
        });

        cut.InvokeAsync(() => cut.Instance.SetViewport(new BlowViewport(0, 0, 2)));
        cut.Render();

        // The anchors live in the transformed layer, so a screen radius of 20 at 2x zoom is 10
        // flow units.
        Assert.AreEqual(10, Radius(cut.Find(".bit-blow-edge-reconnect-hit")), 1e-6);
    }

    [TestMethod]
    public void AnUnselectedEdgeHasNoGrabAreaAtAll()
    {
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };

        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, TwoNodes());
            p.Add(c => c.Edges, edges);
        });

        Assert.IsEmpty(cut.FindAll(".bit-blow-edge-reconnect-hit"));
    }

    // ---- connection line styling ----

    [TestMethod]
    public void TheConnectionLineTakesTheStyleAndClassItWasGiven()
    {
        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, TwoNodes());
            p.Add(c => c.ConnectionLineStyle, "stroke:tomato;stroke-dasharray:5 5");
            p.Add(c => c.ConnectionLineClass, "my-line");
        });

        cut.InvokeAsync(() =>
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 100, 100));
        cut.Find(".bit-blow").PointerMove(new Microsoft.AspNetCore.Components.Web.PointerEventArgs
        {
            ClientX = 200,
            ClientY = 200,
        });

        var line = cut.Find(".bit-blow-connectionline");
        Assert.AreEqual("stroke:tomato;stroke-dasharray:5 5", line.GetAttribute("style"));
        Assert.Contains("my-line", line.GetAttribute("class") ?? string.Empty);
    }

    // ---- default marker colour ----

    [TestMethod]
    public void DefaultMarkerColorPaintsEveryArrowheadThatDoesNotNameItsOwn()
    {
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };

        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, TwoNodes());
            p.Add(c => c.Edges, edges);
            p.Add(c => c.DefaultMarkerColor, "#334155");
        });

        Assert.AreEqual("#334155", cut.Find("marker path").GetAttribute("stroke"));
    }

    [TestMethod]
    public void AnEdgesOwnMarkerColorStillWinsOverTheDefault()
    {
        var edges = new List<BlowEdge>
        {
            new() { Id = "plain", Source = "a", Target = "b" },
            new() { Id = "own", Source = "b", Target = "a", MarkerColor = "tomato" },
        };

        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, TwoNodes());
            p.Add(c => c.Edges, edges);
            p.Add(c => c.DefaultMarkerColor, "#334155");
        });

        var strokes = cut.FindAll("marker path").Select(m => m.GetAttribute("stroke")).ToList();
        Assert.Contains("#334155", strokes);
        Assert.Contains("tomato", strokes);
    }

    [TestMethod]
    public void WithNoDefaultMarkerColorArrowheadsStillInheritTheEdgeStroke()
    {
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };

        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, TwoNodes());
            p.Add(c => c.Edges, edges);
        });

        Assert.AreEqual("context-stroke", cut.Find("marker path").GetAttribute("stroke"));
    }

    // ---- node toolbar alignment ----

    [TestMethod]
    [DataRow(BlowToolbarAlign.Center, "left:50%", "translate(-50%")]
    [DataRow(BlowToolbarAlign.Start, "left:0", "translate(0")]
    [DataRow(BlowToolbarAlign.End, "left:100%", "translate(-100%")]
    public void ATopToolbarAnchorsAlongTheEdgeAccordingToItsAlignment(
        BlowToolbarAlign align, string expectedAnchor, string expectedShift)
    {
        var node = new BlowNode { Id = "a", Position = new(0, 0), Selected = true };

        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, [node]);
            p.Add(c => c.NodeTemplate, (RenderFragment<BlowNode>)(n => b =>
            {
                b.OpenComponent<BlowNodeToolbar>(0);
                b.AddAttribute(1, nameof(BlowNodeToolbar.Position), BlowPosition.Top);
                b.AddAttribute(2, nameof(BlowNodeToolbar.Align), align);
                b.CloseComponent();
            }));
        });

        var style = cut.Find(".bit-blow-node-toolbar").GetAttribute("style")!;
        Assert.Contains(expectedAnchor, style);
        Assert.Contains(expectedShift, style);
    }

    [TestMethod]
    public void ASideToolbarAlignsAlongTheVerticalEdgeInstead()
    {
        var node = new BlowNode { Id = "a", Position = new(0, 0), Selected = true };

        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, [node]);
            p.Add(c => c.NodeTemplate, (RenderFragment<BlowNode>)(n => b =>
            {
                b.OpenComponent<BlowNodeToolbar>(0);
                b.AddAttribute(1, nameof(BlowNodeToolbar.Position), BlowPosition.Right);
                b.AddAttribute(2, nameof(BlowNodeToolbar.Align), BlowToolbarAlign.Start);
                b.CloseComponent();
            }));
        });

        var style = cut.Find(".bit-blow-node-toolbar").GetAttribute("style")!;
        Assert.Contains("top:0", style);
        Assert.Contains("left:100%", style);
    }

    private static double Radius(AngleSharp.Dom.IElement element) =>
        double.Parse(element.GetAttribute("r")!, System.Globalization.CultureInfo.InvariantCulture);
}
