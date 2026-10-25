using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// What a validation shows on the graph and tells a screen reader, what a node's description says about it, taking
/// focus to an element from code, selecting from code, and steering the minimap from the keyboard.
/// </summary>
[TestClass]
public class ValidationMarksAndFocusTests : BunitContext
{
    public ValidationMarksAndFocusTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private BunitJSModuleInterop Module() => JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js");

    private IRenderedComponent<BlowCanvas> Render(List<BlowNode> nodes, List<BlowEdge>? edges = null,
        Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges ?? []);
            extra?.Invoke(p);
        });

    private static List<BlowNode> Graph() =>
    [
        new() { Id = "a", Label = "Read", Ports = [new BlowPort { Id = "out", Type = BlowHandleType.Source }] },
        new() { Id = "b", Label = "Write", Position = new(0, 200), Ports = [new BlowPort { Id = "in", Type = BlowHandleType.Target }] },
        new() { Id = "c", Label = "Log", Position = new(300, 200) },
    ];

    private static string DescriptionOf(IRenderedComponent<BlowCanvas> cut, string selector)
    {
        var ids = cut.Find(selector).GetAttribute("aria-describedby")!.Split(' ');
        return string.Join(" ", ids.Select(id => cut.Find($"[id='{id}']").TextContent));
    }

    // ---- validation marks ----

    [TestMethod]
    public void ErrorsAndWarningsMarkTheirElementsAndNotesDoNot()
    {
        var validation = new BlowValidationResult(
        [
            new("required", BlowValidationSeverity.Error, "Input in must be connected.") { NodeId = "b", PortId = "in", PortType = BlowHandleType.Target },
            new("isolated", BlowValidationSeverity.Warning, "Log is not connected.") { NodeId = "c" },
            new("note", BlowValidationSeverity.Info, "Read runs first.") { NodeId = "a" },
            new("edge", BlowValidationSeverity.Error, "The wire is broken.") { EdgeId = "a-b" },
        ]);
        var cut = Render(Graph(), [new BlowEdge { Id = "a-b", Source = "a", Target = "b", SourceHandle = "out", TargetHandle = "in" }],
            p => p.Add(c => c.Validation, validation));

        Assert.IsTrue(cut.Find(".bit-blow-node[data-id='b']").ClassList.Contains("bit-blow-error"));
        Assert.IsTrue(cut.Find(".bit-blow-node[data-id='c']").ClassList.Contains("bit-blow-warning"));
        var a = cut.Find(".bit-blow-node[data-id='a']");
        Assert.IsFalse(a.ClassList.Contains("bit-blow-error") || a.ClassList.Contains("bit-blow-warning"), "a note was marked");
        Assert.IsTrue(cut.Find(".bit-blow-edge").ClassList.Contains("bit-blow-error"));

        // The port the problem is at, not just its node.
        Assert.IsTrue(cut.Find(".bit-blow-node[data-id='b'] .bit-blow-handle[data-handle-id='in']").ClassList.Contains("bit-blow-handle-error"));
        Assert.IsFalse(cut.Find(".bit-blow-node[data-id='a'] .bit-blow-handle[data-handle-id='out']").ClassList.Contains("bit-blow-handle-error"));
    }

    [TestMethod]
    public void AScreenReaderHearsTheProblemsMostSeriousFirst()
    {
        var validation = new BlowValidationResult(
        [
            new("w", BlowValidationSeverity.Warning, "Slow.") { NodeId = "c" },
            new("e", BlowValidationSeverity.Error, "Broken.") { NodeId = "c" },
        ]);
        var cut = Render(Graph(), extra: p => p.Add(c => c.Validation, validation));

        StringAssert.StartsWith(DescriptionOf(cut, ".bit-blow-node[data-id='c']"), "Error: Broken. Warning: Slow. Not connected.");
    }

    [TestMethod]
    public void ANewValidationRedrawsTheMarks()
    {
        var cut = Render(Graph(), extra: p => p.Add(c => c.Validation, new BlowValidationResult(
            [new("e", BlowValidationSeverity.Error, "Broken.") { NodeId = "c" }])));
        Assert.IsTrue(cut.Find(".bit-blow-node[data-id='c']").ClassList.Contains("bit-blow-error"));

        cut.Render(p => p.Add(c => c.Validation, BlowValidationResult.Empty));

        Assert.IsFalse(cut.Find(".bit-blow-node[data-id='c']").ClassList.Contains("bit-blow-error"));
        Assert.DoesNotContain("Broken", DescriptionOf(cut, ".bit-blow-node[data-id='c']"));
    }

    [TestMethod]
    public void ANodesDescriptionSaysWhetherItIsSelectedAndHowItIsConnected()
    {
        var nodes = Graph();
        var cut = Render(nodes, [new BlowEdge { Id = "a-b", Source = "a", Target = "b" }, new BlowEdge { Id = "c-b", Source = "c", Target = "b" }]);

        StringAssert.StartsWith(DescriptionOf(cut, ".bit-blow-node[data-id='b']"), "2 incoming connections.");
        StringAssert.StartsWith(DescriptionOf(cut, ".bit-blow-node[data-id='a']"), "1 outgoing connection.");

        cut.Find(".bit-blow-node[data-id='b']").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        StringAssert.StartsWith(DescriptionOf(cut, ".bit-blow-node[data-id='b']"), "Selected. 2 incoming connections.");
    }

    [TestMethod]
    public void TheStateTextIsNotPartOfTheNodesOwnContent()
    {
        var cut = Render([new BlowNode { Id = "a", Label = "Read" }]);

        Assert.AreEqual("Read", cut.Find(".bit-blow-node").TextContent.Trim());
    }

    [TestMethod]
    public void AnEdgeIsCalledByTheNamesOfItsNodesWhenSelected()
    {
        var cut = Render(Graph(), [new BlowEdge { Id = "a-c", Source = "a", Target = "c" }]);

        cut.Find(".bit-blow-edge").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        StringAssert.Contains(cut.Find(".bit-blow-a11y-live").TextContent, "Edge Read to Log selected");
        StringAssert.StartsWith(DescriptionOf(cut, ".bit-blow-edge"), "Selected.");
    }

    // ---- focus and selection from code ----

    [TestMethod]
    public async Task FocusingANodeCentresItSelectsItIfAskedAndFocusesItsElement()
    {
        var module = Module();
        module.Setup<BlowDomRect?>("getRect", _ => true).SetResult(new BlowDomRect(0, 0, 800, 600));
        var focus = module.Setup<bool>("focusElement", _ => true);
        focus.SetResult(true);
        var nodes = Graph();
        nodes[2].Width = 100;
        nodes[2].Height = 40;
        var cut = Render(nodes);

        var focused = await cut.InvokeAsync(() => cut.Instance.FocusNodeAsync("c", select: true));

        Assert.IsTrue(focused);
        var call = focus.Invocations.Single();
        Assert.AreEqual("node", call.Arguments[1]);
        Assert.AreEqual("c", call.Arguments[2]);
        Assert.IsTrue(nodes[2].Selected);
        var centre = cut.Instance.GetViewport().FlowToScreen(new BlowXYPosition(350, 220));
        Assert.AreEqual(400, centre.X, 0.001);
        Assert.AreEqual(300, centre.Y, 0.001);
    }

    [TestMethod]
    public async Task NothingIsFocusedForAMissingOrHiddenNode()
    {
        var focus = Module().Setup<bool>("focusElement", _ => true);
        var nodes = Graph();
        nodes[0].Hidden = true;
        var cut = Render(nodes);

        Assert.IsFalse(await cut.InvokeAsync(() => cut.Instance.FocusNodeAsync("missing")));
        Assert.IsFalse(await cut.InvokeAsync(() => cut.Instance.FocusNodeAsync("a")));
        Assert.IsEmpty(focus.Invocations);
    }

    [TestMethod]
    public async Task FocusingAnEdgeFocusesItsElement()
    {
        var focus = Module().Setup<bool>("focusElement", _ => true);
        focus.SetResult(true);
        var cut = Render(Graph(), [new BlowEdge { Id = "a-b", Source = "a", Target = "b" }]);

        Assert.IsTrue(await cut.InvokeAsync(() => cut.Instance.FocusEdgeAsync("a-b", select: true)));
        Assert.AreEqual("edge", focus.Invocations.Single().Arguments[1]);
        Assert.IsTrue(cut.Instance.Edges[0].Selected);
    }

    [TestMethod]
    public async Task SelectingFromCodeReplacesOrAddsToTheSelectionAndSkipsWhatCannotBeSelected()
    {
        var nodes = Graph();
        nodes[2].Selectable = false;
        var changes = new List<BlowSelectionChange>();
        var cut = Render(nodes, extra: p => p.Add(c => c.OnSelectionChange, s => changes.Add(s)));

        await cut.InvokeAsync(() => cut.Instance.SelectAsync([nodes[0]]));
        await cut.InvokeAsync(() => cut.Instance.SelectAsync([nodes[1], nodes[2]], additive: true));

        CollectionAssert.AreEqual(new[] { true, true, false }, nodes.Select(n => n.Selected).ToArray());
        Assert.HasCount(2, changes);

        await cut.InvokeAsync(() => cut.Instance.SelectAsync([nodes[1]]));
        CollectionAssert.AreEqual(new[] { false, true, false }, nodes.Select(n => n.Selected).ToArray());
    }

    [TestMethod]
    public void TheCanvasSearchesOnlyWhatIsShown()
    {
        var nodes = Graph();
        nodes[1].Hidden = true;
        var cut = Render(nodes);

        Assert.IsEmpty(cut.Instance.FindNodes("write"));
        Assert.AreEqual("a", cut.Instance.FindNodes("read").Single().Id);
    }

    // ---- the minimap from the keyboard ----

    private IRenderedComponent<BlowCanvas> RenderWithMiniMap(bool pannable = true, bool zoomable = true)
    {
        Module().Setup<BlowDomRect?>("getRect", _ => true).SetResult(new BlowDomRect(0, 0, 800, 600));
        var nodes = Graph();
        nodes[0].Selected = true;
        return Render(nodes, extra: p => p.AddChildContent<BlowMiniMap>(m => m
            .Add(x => x.Pannable, pannable)
            .Add(x => x.Zoomable, zoomable)));
    }

    [TestMethod]
    public void ASteerableMinimapTakesFocusAndSaysHowToUseIt()
    {
        var cut = RenderWithMiniMap();
        var map = cut.Find(".bit-blow-minimap");

        Assert.AreEqual("0", map.GetAttribute("tabindex"));
        Assert.AreEqual("group", map.GetAttribute("role"));
        Assert.AreEqual("mini map", map.GetAttribute("aria-roledescription"));
        StringAssert.Contains(cut.Find($"[id='{map.GetAttribute("aria-describedby")}']").TextContent, "Arrow keys move the view");

        var still = RenderWithMiniMap(pannable: false, zoomable: false).Find(".bit-blow-minimap");
        Assert.IsNull(still.GetAttribute("tabindex"));
        Assert.AreEqual("img", still.GetAttribute("role"));
    }

    [TestMethod]
    public void ArrowsPanThePlusKeyZoomsAndTheSelectionStaysWhereItIs()
    {
        var cut = RenderWithMiniMap();
        var map = cut.Find(".bit-blow-minimap");
        var selected = cut.Instance.GetNode("a")!.Position;

        map.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.AreEqual(-80, cut.Instance.GetViewport().X, 0.001, "one press moves the view a tenth of the pane");
        map.KeyDown(new KeyboardEventArgs { Key = "ArrowDown", ShiftKey = true });
        Assert.AreEqual(-300, cut.Instance.GetViewport().Y, 0.001, "Shift moves it half the pane");
        Assert.AreEqual(selected, cut.Instance.GetNode("a")!.Position, "the arrows reached the pane and moved the selection");

        var zoom = cut.Instance.GetViewport().Zoom;
        cut.Find(".bit-blow-minimap").KeyDown(new KeyboardEventArgs { Key = "+" });
        Assert.IsGreaterThan(zoom, cut.Instance.GetViewport().Zoom);
    }
}
