using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// Making a connection without a pointer: the connection key on a focused node starts it, focus aims it, Enter makes
/// it and Escape abandons it - with every step said out loud.
/// </summary>
[TestClass]
public class KeyboardConnectionTests : BunitContext
{
    public KeyboardConnectionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private BunitJSModuleInterop Module() => JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js");

    private IRenderedComponent<BlowCanvas> Render(List<BlowNode> nodes, List<BlowEdge> edges, BlowMeasuredNode[] measured,
        Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
    {
        Module().Setup<BlowMeasuredNode[]>("measureNodes", _ => true).SetResult(measured);
        return Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges);
            extra?.Invoke(p);
        });
    }

    private static BlowMeasuredNode Measured(string id, params BlowMeasuredHandle[] handles) => new(id, 150, 40, handles);

    private static BlowMeasuredHandle Out(string id = "", double x = 75) => new(id, "source", "bottom", x, 40);

    private static BlowMeasuredHandle In(string id = "", double x = 75) => new(id, "target", "top", x, 0);

    private static AngleSharp.Dom.IElement NodeElement(IRenderedComponent<BlowCanvas> cut, string id)
        => cut.Find($".bit-blow-node[data-id='{id}']");

    private static string Heard(IRenderedComponent<BlowCanvas> cut)
        => cut.Find(".bit-blow-a11y-live").TextContent.TrimEnd('​');

    private static Task Key(IRenderedComponent<BlowCanvas> cut, string nodeId, string key, bool ctrl = false)
        => NodeElement(cut, nodeId).KeyDownAsync(new KeyboardEventArgs { Key = key, CtrlKey = ctrl });

    private static List<BlowNode> AToB() =>
    [
        new() { Id = "a", Label = "Source" },
        new() { Id = "b", Label = "Sink", Position = new(0, 200) },
    ];

    [TestMethod]
    public async Task TheConnectionKeyFocusAndEnterMakeAConnection()
    {
        var edges = new List<BlowEdge>();
        var cut = Render(AToB(), edges, [Measured("a", Out()), Measured("b", In())]);

        await Key(cut, "a", "c");
        Assert.IsTrue(cut.Instance.IsConnecting);
        StringAssert.StartsWith(Heard(cut), "Connecting from Source.");
        Assert.HasCount(1, cut.FindAll(".bit-blow-connectionline"), "nothing shows the connection being made");

        await NodeElement(cut, "b").FocusAsync(new FocusEventArgs());
        Assert.AreEqual("Press Enter to connect to Sink.", Heard(cut));

        await Key(cut, "b", "Enter");

        var edge = edges.Single();
        Assert.AreEqual(("a", "b"), (edge.Source, edge.Target));
        Assert.AreEqual("Connected Source to Sink", Heard(cut));
        Assert.IsFalse(cut.Instance.IsConnecting);
        Assert.IsFalse(cut.Instance.GetNode("b")!.Selected, "Enter selected the node instead of ending the connection");
    }

    [TestMethod]
    public async Task EscapeAbandonsTheConnectionAndSaysSo()
    {
        var edges = new List<BlowEdge>();
        var cut = Render(AToB(), edges, [Measured("a", Out()), Measured("b", In())]);

        await Key(cut, "a", "c");
        await cut.Find(".bit-blow").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(cut.Instance.IsConnecting);
        Assert.IsEmpty(edges);
        Assert.AreEqual("Connection cancelled.", Heard(cut));
        Assert.IsEmpty(cut.FindAll(".bit-blow-connectionline"));
    }

    [TestMethod]
    public async Task ANodeTheConnectionCannotEndOnSaysWhyAndTheConnectionWaitsForAnother()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "n", Label = "Number", Ports = [new BlowPort { Id = "value", Type = BlowHandleType.Source, DataType = "number" }] },
            new() { Id = "t", Label = "Text", Position = new(0, 200), Ports = [new BlowPort { Id = "in", Type = BlowHandleType.Target, DataType = "text" }] },
            new() { Id = "s", Label = "Sum", Position = new(300, 200), Ports = [new BlowPort { Id = "in", Type = BlowHandleType.Target, DataType = "number" }] },
        };
        var edges = new List<BlowEdge>();
        var cut = Render(nodes, edges, [Measured("n", Out("value")), Measured("t", In("in")), Measured("s", In("in"))]);

        await Key(cut, "n", "c");
        await NodeElement(cut, "t").FocusAsync(new FocusEventArgs());
        Assert.AreEqual(BlowAriaLabels.Default.ConnectionRejected(BlowConnectionRejection.IncompatibleTypes), Heard(cut));

        await Key(cut, "t", "Enter");
        Assert.IsEmpty(edges);
        Assert.IsTrue(cut.Instance.IsConnecting, "a refused target ended the connection");

        await NodeElement(cut, "s").FocusAsync(new FocusEventArgs());
        await Key(cut, "s", "Enter");
        Assert.AreEqual(("n", "value", "s", "in"), (edges.Single().Source, edges.Single().SourceHandle, edges.Single().Target, edges.Single().TargetHandle));
    }

    [TestMethod]
    public async Task TheConnectionKeyStepsThroughTheSourcePortsAndTheTargetPorts()
    {
        var nodes = new List<BlowNode>
        {
            new()
            {
                Id = "split", Label = "Split",
                Ports = [new BlowPort { Id = "left", Type = BlowHandleType.Source, Label = "Left half" }, new BlowPort { Id = "right", Type = BlowHandleType.Source, Label = "Right half" }],
            },
            new()
            {
                Id = "join", Label = "Join", Position = new(0, 200),
                Ports = [new BlowPort { Id = "x", Type = BlowHandleType.Target, Label = "First" }, new BlowPort { Id = "y", Type = BlowHandleType.Target, Label = "Second" }],
            },
        };
        var edges = new List<BlowEdge>();
        var cut = Render(nodes, edges,
            [Measured("split", Out("left", 40), Out("right", 110)), Measured("join", In("x", 40), In("y", 110))]);

        await Key(cut, "split", "c");
        StringAssert.StartsWith(Heard(cut), "Connecting from Split, Left half.");
        await Key(cut, "split", "c");
        StringAssert.StartsWith(Heard(cut), "Connecting from Split, Right half.");

        await NodeElement(cut, "join").FocusAsync(new FocusEventArgs());
        Assert.AreEqual("Press Enter to connect to Join, First.", Heard(cut));
        await Key(cut, "join", "c");
        Assert.AreEqual("Press Enter to connect to Join, Second.", Heard(cut));

        await Key(cut, "join", "Enter");
        Assert.AreEqual(("right", "y"), (edges.Single().SourceHandle, edges.Single().TargetHandle));
    }

    [TestMethod]
    public async Task ANodeNoConnectionCanStartFromSaysSo()
    {
        var cut = Render(AToB(), [], [Measured("a", Out()), Measured("b", In())],
            p => p.Add(c => c.ConnectionMode, BlowConnectionMode.Strict));

        // A node that takes no connections at all.
        cut.Instance.GetNode("b")!.Connectable = false;
        cut.Render();
        await Key(cut, "b", "c");

        Assert.IsFalse(cut.Instance.IsConnecting);
        Assert.AreEqual("No connection can start from Sink.", Heard(cut));
    }

    [TestMethod]
    public async Task WithModifiersHeldOrTheKeyTurnedOffNothingStarts()
    {
        var cut = Render(AToB(), [], [Measured("a", Out()), Measured("b", In())]);
        await Key(cut, "a", "c", ctrl: true);
        Assert.IsFalse(cut.Instance.IsConnecting, "Ctrl+C started a connection");

        var off = Render(AToB(), [], [Measured("a", Out()), Measured("b", In())], p => p.Add(c => c.ConnectionKey, null));
        await Key(off, "a", "c");
        Assert.IsFalse(off.Instance.IsConnecting);
    }

    [TestMethod]
    public async Task AnotherKeyCanBeChosen()
    {
        var edges = new List<BlowEdge>();
        var cut = Render(AToB(), edges, [Measured("a", Out()), Measured("b", In())], p => p.Add(c => c.ConnectionKey, "k"));

        await Key(cut, "a", "c");
        Assert.IsFalse(cut.Instance.IsConnecting);
        await Key(cut, "a", "K");
        Assert.IsTrue(cut.Instance.IsConnecting);
        StringAssert.Contains(Heard(cut), "K to choose another port");
    }

    [TestMethod]
    public async Task FocusingTheSourceAgainTakesTheAimOff()
    {
        var cut = Render(AToB(), [], [Measured("a", Out()), Measured("b", In())]);

        await Key(cut, "a", "c");
        await NodeElement(cut, "b").FocusAsync(new FocusEventArgs());
        Assert.IsTrue(cut.Instance.IsValidConnectionTarget("b", null, BlowHandleType.Target));
        await NodeElement(cut, "a").FocusAsync(new FocusEventArgs());

        await Key(cut, "a", "Enter");
        Assert.IsTrue(cut.Instance.IsConnecting, "Enter on the source itself made or ended something");
        Assert.AreEqual(BlowAriaLabels.Default.ConnectionRejected(BlowConnectionRejection.NotConnectable), Heard(cut));
    }
}
