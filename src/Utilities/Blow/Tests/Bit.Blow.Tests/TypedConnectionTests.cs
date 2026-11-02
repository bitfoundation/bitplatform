using Bunit;

namespace Bit.Blow.Tests;

/// <summary>
/// The canvas end of the connection rules: declared ports drawn and enforced, rejection reasons reported and
/// announced, cycles prevented, limits shown on the handles, asynchronous confirmation, and parent cycles reported.
/// </summary>
[TestClass]
public class TypedConnectionTests : BunitContext
{
    public TypedConnectionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<BlowCanvas> Render(List<BlowNode> nodes, List<BlowEdge>? edges = null,
        Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges ?? []);
            extra?.Invoke(p);
        });

    private static BlowNode Typed(string id, string? input = null, string? output = null, int? maxOut = null) => new()
    {
        Id = id,
        Ports =
        [
            new BlowPort { Id = "in", Type = BlowHandleType.Target, DataType = input, Label = $"{id} in" },
            new BlowPort { Id = "out", Type = BlowHandleType.Source, DataType = output, MaxConnections = maxOut },
        ],
    };

    private static Task Connect(IRenderedComponent<BlowCanvas> cut, string from, string? fromHandle, string to, string? toHandle)
        => cut.InvokeAsync(() =>
        {
            cut.Instance.StartConnection(from, fromHandle, BlowHandleType.Source, BlowPosition.Right);
            cut.Instance.CompleteConnection(to, toHandle, BlowHandleType.Target);
        });

    // ---- declared ports on the built-in nodes ----

    [TestMethod]
    public void ABuiltInNodeDrawsOneHandlePerDeclaredPort()
    {
        var node = new BlowNode
        {
            Id = "n",
            Ports =
            [
                new BlowPort { Id = "a", Type = BlowHandleType.Target, Position = BlowPosition.Left },
                new BlowPort { Id = "b", Type = BlowHandleType.Target, Position = BlowPosition.Left },
                new BlowPort { Id = "r", Type = BlowHandleType.Source, Position = BlowPosition.Right, DataType = "number", Label = "Result" },
            ],
        };

        var cut = Render([node]);

        var handles = cut.FindAll(".bit-blow-handle");
        Assert.HasCount(3, handles);
        Assert.AreEqual("top:33.33%", handles[0].GetAttribute("style"));
        Assert.AreEqual("top:66.67%", handles[1].GetAttribute("style"));
        Assert.IsNull(handles[2].GetAttribute("style"), "a lone port on a side keeps the stylesheet's centring");
        Assert.AreEqual("number", handles[2].GetAttribute("data-port-type"));
        Assert.AreEqual("Result", handles[2].GetAttribute("title"));
        Assert.Contains("bit-blow-handle-right", handles[2].ClassName!);
    }

    [TestMethod]
    public void APortWithoutASideUsesTheNodesHandleSides()
    {
        var node = new BlowNode
        {
            Id = "n",
            SourcePosition = BlowPosition.Right,
            Ports = [new BlowPort { Type = BlowHandleType.Source }],
        };

        var cut = Render([node]);

        Assert.Contains("bit-blow-handle-right", cut.Find(".bit-blow-handle").ClassName!);
    }

    [TestMethod]
    public void ATemplateHandlePicksUpItsDeclaredPort()
    {
        var node = new BlowNode { Id = "n", Ports = [new BlowPort { Id = "x", Type = BlowHandleType.Target, DataType = "image" }] };

        var cut = Render([node], extra: p => p.Add(c => c.NodeTemplate, n => b =>
        {
            b.OpenComponent<BlowHandle>(0);
            b.AddAttribute(1, nameof(BlowHandle.Type), BlowHandleType.Target);
            b.AddAttribute(2, nameof(BlowHandle.Id), "x");
            b.CloseComponent();
        }));

        Assert.AreEqual("image", cut.Find(".bit-blow-handle").GetAttribute("data-port-type"));
    }

    // ---- enforcement while connecting ----

    [TestMethod]
    public async Task IncompatiblePortsDoNotConnectAndSayWhy()
    {
        var ends = new List<BlowConnectEndInfo>();
        var edges = new List<BlowEdge>();
        var cut = Render([Typed("a", output: "number"), Typed("b", input: "text")], edges,
            p => p.Add(c => c.OnConnectEnd, e => ends.Add(e)));

        await Connect(cut, "a", "out", "b", "in");

        Assert.IsEmpty(edges);
        Assert.AreEqual(BlowConnectionRejection.IncompatibleTypes, ends.Single().Rejection);
        Assert.IsFalse(ends.Single().IsValid);
        Assert.AreEqual(BlowAriaLabels.Default.ConnectionRejected(BlowConnectionRejection.IncompatibleTypes),
            cut.Find(".bit-blow-a11y-live").TextContent);
    }

    [TestMethod]
    public async Task CompatiblePortsConnectAndTheConnectionIsAnnounced()
    {
        var ends = new List<BlowConnectEndInfo>();
        var edges = new List<BlowEdge>();
        var cut = Render([Typed("a", output: "number"), Typed("b", input: "number")], edges,
            p => p.Add(c => c.OnConnectEnd, e => ends.Add(e)));

        await Connect(cut, "a", "out", "b", "in");

        Assert.HasCount(1, edges);
        Assert.AreEqual(BlowConnectionRejection.None, ends.Single().Rejection);
        Assert.AreEqual("Connected a to b", cut.Find(".bit-blow-a11y-live").TextContent);
    }

    [TestMethod]
    public async Task ACustomTypeRuleIsUsedByTheCanvas()
    {
        var edges = new List<BlowEdge>();
        var cut = Render([Typed("a", output: "int"), Typed("b", input: "float")], edges,
            p => p.Add(c => c.ConnectionTypeCompatibility, (s, t) => s == t || (s == "int" && t == "float")));

        await Connect(cut, "a", "out", "b", "in");

        Assert.HasCount(1, edges);
    }

    [TestMethod]
    public async Task WithCyclesPreventedTheConnectionClosingOneIsRefused()
    {
        var edges = new List<BlowEdge>
        {
            new() { Id = "1", Source = "a", Target = "b" },
            new() { Id = "2", Source = "b", Target = "c" },
        };
        var ends = new List<BlowConnectEndInfo>();
        var cut = Render([new() { Id = "a" }, new() { Id = "b" }, new() { Id = "c" }], edges, p => p
            .Add(c => c.PreventCycles, true)
            .Add(c => c.OnConnectEnd, e => ends.Add(e)));

        await Connect(cut, "c", null, "a", null);

        Assert.HasCount(2, edges);
        Assert.AreEqual(BlowConnectionRejection.Cycle, ends.Single().Rejection);
    }

    [TestMethod]
    public async Task CheckConnectionReportsTheReasonWhileDragging()
    {
        var cut = Render([Typed("a", output: "x"), Typed("b", input: "y"), Typed("c", input: "x")]);

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", "out", BlowHandleType.Source, BlowPosition.Right));

        Assert.AreEqual(BlowConnectionRejection.IncompatibleTypes, cut.Instance.CheckConnection("b", "in", BlowHandleType.Target).Rejection);
        Assert.IsTrue(cut.Instance.CheckConnection("c", "in", BlowHandleType.Target).IsValid);
        Assert.AreEqual(BlowConnectionRejection.SameHandleType, cut.Instance.CheckConnection("c", "out", BlowHandleType.Source).Rejection);
    }

    [TestMethod]
    public void OutsideAGestureEveryHandleIsRefused()
    {
        var cut = Render([new() { Id = "a" }, new() { Id = "b" }]);

        Assert.AreEqual(BlowConnectionRejection.NotConnectable, cut.Instance.CheckConnection("b", null, BlowHandleType.Target).Rejection);
    }

    [TestMethod]
    public async Task AHandleRuleRefusalIsReportedAsRejectedByRule()
    {
        var ends = new List<BlowConnectEndInfo>();
        var cut = Render([new() { Id = "a" }, new() { Id = "b" }], [], p => p
            .Add(c => c.OnConnectEnd, e => ends.Add(e))
            .Add(c => c.NodeTemplate, n => b =>
            {
                b.OpenComponent<BlowHandle>(0);
                b.AddAttribute(1, nameof(BlowHandle.Type), BlowHandleType.Target);
                b.AddAttribute(2, nameof(BlowHandle.IsValidConnection), (Func<BlowConnection, bool>)(_ => false));
                b.CloseComponent();
            }));

        await Connect(cut, "a", null, "b", null);

        Assert.AreEqual(BlowConnectionRejection.RejectedByRule, ends.Single().Rejection);
    }

    // ---- limits ----

    [TestMethod]
    public async Task AFullOutputNeitherStartsNorShowsItselfConnectable()
    {
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", SourceHandle = "out", Target = "b", TargetHandle = "in" } };
        var cut = Render([Typed("a", maxOut: 1), Typed("b"), Typed("c")], edges);

        var output = cut.Find(".bit-blow-node[data-id='a'] .bit-blow-handle.source");
        Assert.DoesNotContain("connectable", output.ClassName!.Split(' '));

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", "out", BlowHandleType.Source, BlowPosition.Right));
        Assert.IsFalse(cut.Instance.IsConnecting, "a full output started a connection");
    }

    [TestMethod]
    public async Task AFreedOutputBecomesConnectableAgain()
    {
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", SourceHandle = "out", Target = "b", TargetHandle = "in" } };
        var cut = Render([Typed("a", maxOut: 1), Typed("b")], edges);

        await cut.InvokeAsync(() => cut.Instance.DeleteElementsAsync(edges: [edges[0]]));

        var output = cut.Find(".bit-blow-node[data-id='a'] .bit-blow-handle.source");
        Assert.Contains("connectable", output.ClassName!.Split(' '));
    }

    [TestMethod]
    public async Task AHandleParameterLimitAppliesWithoutDeclaringAPort()
    {
        var edges = new List<BlowEdge>();
        var ends = new List<BlowConnectEndInfo>();
        var cut = Render([new() { Id = "a" }, new() { Id = "b" }, new() { Id = "c" }], edges, p => p
            .Add(c => c.OnConnectEnd, e => ends.Add(e))
            .Add(c => c.NodeTemplate, n => b =>
            {
                b.OpenComponent<BlowHandle>(0);
                b.AddAttribute(1, nameof(BlowHandle.Type), BlowHandleType.Target);
                b.AddAttribute(2, nameof(BlowHandle.MaxConnections), (int?)1);
                b.CloseComponent();
                b.OpenComponent<BlowHandle>(3);
                b.AddAttribute(4, nameof(BlowHandle.Type), BlowHandleType.Source);
                b.CloseComponent();
            }));

        await Connect(cut, "a", null, "c", null);
        await Connect(cut, "b", null, "c", null);

        Assert.HasCount(1, edges);
        Assert.AreEqual(BlowConnectionRejection.TargetFull, ends[^1].Rejection);
    }

    // ---- asynchronous confirmation ----

    [TestMethod]
    public async Task OnBeforeConnectDecidesWhetherTheEdgeIsAdded()
    {
        var edges = new List<BlowEdge>();
        var ends = new List<BlowConnectEndInfo>();
        var answer = false;
        var cut = Render([new() { Id = "a" }, new() { Id = "b" }, new() { Id = "c" }], edges, p => p
            .Add(c => c.OnConnectEnd, e => ends.Add(e))
            .Add(c => c.OnBeforeConnect, _ => Task.FromResult(answer)));

        await Connect(cut, "a", null, "b", null);
        Assert.IsEmpty(edges);
        Assert.AreEqual(BlowConnectionRejection.RejectedByRule, ends[^1].Rejection);

        answer = true;
        await Connect(cut, "a", null, "c", null);
        Assert.HasCount(1, edges);
        Assert.IsTrue(ends[^1].IsValid);
    }

    [TestMethod]
    public async Task AConnectionThatBecameInvalidDuringTheConfirmationIsDropped()
    {
        var edges = new List<BlowEdge>();
        var ends = new List<BlowConnectEndInfo>();
        var confirmation = new TaskCompletionSource<bool>();
        var cut = Render([new() { Id = "a" }, new() { Id = "b" }], edges, p => p
            .Add(c => c.OnConnectEnd, e => ends.Add(e))
            .Add(c => c.OnBeforeConnect, _ => confirmation.Task));

        await Connect(cut, "a", null, "b", null);
        Assert.IsFalse(cut.Instance.IsConnecting, "the gesture should end while the confirmation runs");
        Assert.IsEmpty(ends, "the gesture's end was reported before its outcome was known");

        // Meanwhile the very same connection is made another way.
        edges.Add(new BlowEdge { Id = "made-elsewhere", Source = "a", Target = "b" });
        await cut.InvokeAsync(() => confirmation.SetResult(true));

        Assert.HasCount(1, edges);
        Assert.AreEqual(BlowConnectionRejection.Duplicate, ends.Single().Rejection);
    }

    [TestMethod]
    public async Task AConfirmationArrivingMidwayThroughTheNextGestureDoesNotLeakIntoIt()
    {
        var edges = new List<BlowEdge>();
        var ends = new List<BlowConnectEndInfo>();
        var confirmation = new TaskCompletionSource<bool>();
        var cut = Render([new() { Id = "a" }, new() { Id = "b" }, new() { Id = "c" }], edges, p => p
            .Add(c => c.OnConnectEnd, e => ends.Add(e))
            .Add(c => c.OnBeforeConnect, _ => confirmation.Task));

        await Connect(cut, "a", null, "b", null);

        // The user is already dragging the next connection when the first one is confirmed...
        await cut.InvokeAsync(() => cut.Instance.StartConnection("c", null, BlowHandleType.Source, BlowPosition.Bottom));
        await cut.InvokeAsync(() => confirmation.SetResult(true));
        // ...and drops it on a handle that refuses it.
        await cut.InvokeAsync(() => cut.Instance.CompleteConnection("c", null, BlowHandleType.Target));

        Assert.HasCount(1, edges);
        Assert.HasCount(2, ends);
        Assert.IsTrue(ends[0].IsValid, "the confirmed connection");
        Assert.IsFalse(ends[1].IsValid, "the second gesture was reported as connected");
        Assert.AreEqual(BlowConnectionRejection.SelfConnection, ends[1].Rejection);
    }

    [TestMethod]
    public async Task AFailingConfirmationSurfacesItsException()
    {
        var cut = Render([new() { Id = "a" }, new() { Id = "b" }], [], p => p
            .Add(c => c.OnBeforeConnect, async _ =>
            {
                await Task.Yield();
                throw new InvalidOperationException("server said no");
            }));

        await Connect(cut, "a", null, "b", null);

        // The canvas could not await it - the pointer release that started it has long returned - so it hands the
        // exception to the renderer, where an error boundary or the app's error UI sees it, rather than losing it.
        var thrown = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsInstanceOfType<InvalidOperationException>(thrown);
        Assert.AreEqual("server said no", thrown.Message);
    }

    // ---- parent cycles ----

    [TestMethod]
    public void AParentCycleIsReportedAndBrokenInsteadOfFlingingTheNodes()
    {
        var errors = new List<BlowError>();
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", ParentId = "b", Position = new(10, 10) },
            new() { Id = "b", ParentId = "a", Position = new(20, 20) },
        };

        var cut = Render(nodes, extra: p => p.Add(c => c.OnError, e => errors.Add(e)));

        Assert.AreEqual(BlowErrorCode.ParentCycle, errors.Single().Code);
        // The cycle is cut at b, which becomes a root; a stays b's child.
        Assert.AreEqual(new BlowXYPosition(20, 20), nodes[1].AbsolutePosition);
        Assert.AreEqual(new BlowXYPosition(30, 30), nodes[0].AbsolutePosition);
    }

    [TestMethod]
    public void ANodeThatIsItsOwnParentIsReported()
    {
        var errors = new List<BlowError>();

        Render([new() { Id = "a", ParentId = "a" }], extra: p => p.Add(c => c.OnError, e => errors.Add(e)));

        Assert.AreEqual(BlowErrorCode.ParentCycle, errors.Single().Code);
    }
}
