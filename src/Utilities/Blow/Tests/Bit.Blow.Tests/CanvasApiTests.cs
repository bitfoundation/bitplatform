using Bunit;
using Microsoft.AspNetCore.Components;
namespace Bit.Blow.Tests;

/// <summary>
/// Tests for the canvas surface added around markers, per-handle rules, error reporting,
/// localization and the uncontrolled starting viewport.
/// </summary>
[TestClass]
public class CanvasApiTests : BunitContext
{
    public CanvasApiTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<BlowCanvas> Render(
        List<BlowNode> nodes, List<BlowEdge>? edges = null, Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges ?? []);
            extra?.Invoke(p);
        });

    private static List<BlowNode> TwoNodes() =>
    [
        new() { Id = "a", Position = new(0, 0) },
        new() { Id = "b", Position = new(0, 200) },
    ];

    // ---- edge markers ----

    [TestMethod]
    public void EdgesSharingAMarkerStyleShareOneDefinition()
    {
        var edges = new List<BlowEdge>
        {
            new() { Id = "e1", Source = "a", Target = "b" },
            new() { Id = "e2", Source = "b", Target = "a" },
        };

        var cut = Render(TwoNodes(), edges);

        Assert.ContainsSingle(cut.FindAll("marker"));
    }

    [TestMethod]
    public void EdgesAskingForDifferentMarkersEachGetTheirOwnDefinition()
    {
        var edges = new List<BlowEdge>
        {
            new() { Id = "plain", Source = "a", Target = "b" },
            new() { Id = "red", Source = "b", Target = "a", MarkerColor = "#ff0000" },
            new() { Id = "big", Source = "a", Target = "b", MarkerWidth = 20 },
            new() { Id = "closed", Source = "b", Target = "a", MarkerType = BlowMarkerType.ArrowClosed },
        };

        var cut = Render(TwoNodes(), edges);

        Assert.AreEqual(4, cut.FindAll("marker").Count);
    }

    [TestMethod]
    public void AnEdgeReferencesTheMarkerItAskedFor()
    {
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", MarkerColor = "tomato" } };

        var cut = Render(TwoNodes(), edges);

        var markerId = cut.Find("marker").GetAttribute("id");
        var path = cut.Find(".bit-blow-edge-path");
        Assert.AreEqual($"url(#{markerId})", path.GetAttribute("marker-end"));
    }

    [TestMethod]
    public void AMarkerColorIsPaintedOnTheArrowhead()
    {
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", MarkerColor = "tomato" } };

        var cut = Render(TwoNodes(), edges);

        Assert.AreEqual("tomato", cut.Find("marker path").GetAttribute("stroke"));
    }

    [TestMethod]
    public void AnUncolouredMarkerInheritsTheEdgeStroke()
    {
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };

        var cut = Render(TwoNodes(), edges);

        Assert.AreEqual("context-stroke", cut.Find("marker path").GetAttribute("stroke"));
    }

    [TestMethod]
    public void MarkersAreOnlyDeclaredForEdgesThatShowThem()
    {
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", MarkerEnd = false } };

        var cut = Render(TwoNodes(), edges);

        Assert.IsEmpty(cut.FindAll("marker"));
    }

    [TestMethod]
    public void AClosedMarkerIsFilledAndAnOpenOneIsNot()
    {
        var open = Render(TwoNodes(), [new() { Id = "e", Source = "a", Target = "b" }]);
        var closed = Render(TwoNodes(),
            [new() { Id = "e", Source = "a", Target = "b", MarkerType = BlowMarkerType.ArrowClosed }]);

        Assert.AreEqual("none", open.Find("marker path").GetAttribute("fill"));
        Assert.AreEqual("context-stroke", closed.Find("marker path").GetAttribute("fill"));
    }

    [TestMethod]
    public void MarkerSpecKeysAreIdSafeForAwkwardColours()
    {
        var edges = new List<BlowEdge>
        {
            new() { Id = "e", Source = "a", Target = "b", MarkerColor = "rgba(1, 2, 3, 0.5)" },
        };

        var cut = Render(TwoNodes(), edges);

        var id = cut.Find("marker").GetAttribute("id")!;
        // An id must not carry characters that break a url(#...) reference.
        Assert.DoesNotContain('(', id);
        Assert.DoesNotContain(' ', id);
        Assert.DoesNotContain(',', id);
        Assert.DoesNotContain('#', id);
    }

    [TestMethod]
    public void CloningAnEdgeCarriesTheMarkerStyling()
    {
        var edge = new BlowEdge
        {
            Id = "e",
            Source = "a",
            Target = "b",
            MarkerColor = "teal",
            MarkerWidth = 11,
            MarkerHeight = 13,
        };

        var clone = edge.Clone("copy");

        Assert.AreEqual("teal", clone.MarkerColor);
        Assert.AreEqual(11, clone.MarkerWidth);
        Assert.AreEqual(13, clone.MarkerHeight);
    }

    [TestMethod]
    public void AMarkerDefaultsToASquareSizedByItsType()
    {
        var open = new BlowEdge { Id = "e", Source = "a", Target = "b" }.ResolveMarker();
        var closed = new BlowEdge { Id = "e", Source = "a", Target = "b", MarkerType = BlowMarkerType.ArrowClosed }
            .ResolveMarker();

        Assert.AreEqual(7, open.Width);
        Assert.AreEqual(7, open.Height);
        Assert.AreEqual(8, closed.Width);
        Assert.AreEqual(8, closed.Height);
    }

    [TestMethod]
    public void AMarkerHeightDefaultsToTheWidthItWasGiven()
    {
        var spec = new BlowEdge { Id = "e", Source = "a", Target = "b", MarkerWidth = 30 }.ResolveMarker();

        Assert.AreEqual(30, spec.Width);
        Assert.AreEqual(30, spec.Height);
    }

    // ---- uncontrolled default viewport ----

    [TestMethod]
    public void TheDefaultViewportIsAdoptedBeforeTheFirstPaint()
    {
        var cut = Render(TwoNodes(), extra: p => p.Add(c => c.DefaultViewport, new BlowViewport(40, 60, 1.5)));

        Assert.AreEqual(new BlowViewport(40, 60, 1.5), cut.Instance.GetViewport());
    }

    [TestMethod]
    public void TheDefaultViewportZoomIsClampedToTheConfiguredRange()
    {
        var cut = Render(TwoNodes(), extra: p =>
        {
            p.Add(c => c.MaxZoom, 2.0);
            p.Add(c => c.DefaultViewport, new BlowViewport(0, 0, 99));
        });

        Assert.AreEqual(2.0, cut.Instance.GetViewport().Zoom, 1e-6);
    }

    [TestMethod]
    public void AControlledViewportWinsOverTheDefaultOne()
    {
        var cut = Render(TwoNodes(), extra: p =>
        {
            p.Add(c => c.DefaultViewport, new BlowViewport(40, 60, 1.5));
            p.Add(c => c.Viewport, new BlowViewport(1, 2, 1));
        });

        Assert.AreEqual(new BlowViewport(1, 2, 1), cut.Instance.GetViewport());
    }

    [TestMethod]
    public async Task TheDefaultViewportIsNotReappliedOnLaterRenders()
    {
        var nodes = TwoNodes();
        var cut = Render(nodes, extra: p => p.Add(c => c.DefaultViewport, new BlowViewport(40, 60, 1)));

        await cut.InvokeAsync(() => cut.Instance.SetViewport(new BlowViewport(0, 0, 1)));
        cut.Render(p => p.Add(c => c.MinZoom, 0.1));

        Assert.AreEqual(new BlowViewport(0, 0, 1), cut.Instance.GetViewport());
    }

    // ---- error reporting ----

    [TestMethod]
    public void AnEdgePointingAtAMissingNodeIsReported()
    {
        var errors = new List<BlowError>();
        Render([new() { Id = "a" }], [new() { Id = "e", Source = "a", Target = "ghost" }],
            p => p.Add(c => c.OnError, EventCallback.Factory.Create<BlowError>(this, errors.Add)));

        Assert.Contains(e => e.Code == BlowErrorCode.MissingEdgeEndpoint, errors);
        Assert.Contains(e => e.Message.Contains("ghost"), errors);
    }

    [TestMethod]
    public void DuplicateNodeIdsAreReported()
    {
        var errors = new List<BlowError>();
        Render([new() { Id = "dup" }, new() { Id = "dup" }],
            extra: p => p.Add(c => c.OnError, EventCallback.Factory.Create<BlowError>(this, errors.Add)));

        Assert.Contains(e => e.Code == BlowErrorCode.DuplicateNodeId, errors);
    }

    [TestMethod]
    public void DuplicateEdgeIdsAreReported()
    {
        var errors = new List<BlowError>();
        var cut = Render(TwoNodes(),
            [new() { Id = "same", Source = "a", Target = "b" }, new() { Id = "same", Source = "b", Target = "a" }],
            p => p.Add(c => c.OnError, EventCallback.Factory.Create<BlowError>(this, errors.Add)));

        // The lookup is built lazily, so ask for an edge to force it.
        cut.Instance.GetEdge("same");
        cut.Render();

        Assert.Contains(e => e.Code == BlowErrorCode.DuplicateEdgeId, errors);
    }

    [TestMethod]
    public void ANodeNamingAMissingParentIsReported()
    {
        var errors = new List<BlowError>();
        Render([new() { Id = "orphan", ParentId = "nowhere" }],
            extra: p => p.Add(c => c.OnError, EventCallback.Factory.Create<BlowError>(this, errors.Add)));

        Assert.Contains(e => e.Code == BlowErrorCode.MissingParentNode, errors);
    }

    [TestMethod]
    public void APersistentProblemIsOnlyReportedOnce()
    {
        var errors = new List<BlowError>();
        var cut = Render([new() { Id = "a" }], [new() { Id = "e", Source = "a", Target = "ghost" }],
            p => p.Add(c => c.OnError, EventCallback.Factory.Create<BlowError>(this, errors.Add)));

        cut.Render();
        cut.Render();

        Assert.ContainsSingle(errors);
    }

    // ---- accessibility strings ----

    [TestMethod]
    public void AriaLabelsCanBeLocalized()
    {
        var labels = new BlowAriaLabels
        {
            NodeLabel = name => $"Knoten {name}",
            EdgeLabel = (s, t) => $"Kante {s} nach {t}",
        };

        var cut = Render(TwoNodes(), [new() { Id = "e", Source = "a", Target = "b" }],
            p => p.Add(c => c.AriaLabels, labels));

        Assert.AreEqual("Knoten Node a", cut.Find(".bit-blow-node").GetAttribute("aria-label"));
        Assert.AreEqual("Kante a nach b", cut.Find(".bit-blow-edge").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void SelectionAnnouncementsUseTheSuppliedStrings()
    {
        var labels = new BlowAriaLabels { NodeSelected = name => $"{name} ausgewählt" };
        var nodes = new List<BlowNode> { new() { Id = "a", Label = "Start" } };
        var cut = Render(nodes, extra: p => p.Add(c => c.AriaLabels, labels));

        cut.Find(".bit-blow-node").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });

        Assert.Contains("Start ausgewählt", cut.Find(".bit-blow-a11y-live").TextContent);
    }

    [TestMethod]
    public async Task RepeatingAnAnnouncementStillChangesTheLiveRegion()
    {
        var cut = Render([new() { Id = "a" }]);

        await cut.InvokeAsync(() => cut.Instance.Announce("moved"));
        var first = cut.Find(".bit-blow-a11y-live").TextContent;

        await cut.InvokeAsync(() => cut.Instance.Announce("moved"));
        var second = cut.Find(".bit-blow-a11y-live").TextContent;

        // The words are identical; the raw text is not, so the region is re-read.
        Assert.AreNotEqual(first, second);
        Assert.AreEqual("moved", second.Trim('​'));
    }

    // ---- splatted attributes ----

    [TestMethod]
    public void ExtraAttributesLandOnTheFlowRoot()
    {
        var cut = Render([new() { Id = "a" }], extra: p =>
        {
            p.AddUnmatched("id", "my-flow");
            p.AddUnmatched("data-testid", "canvas");
        });

        var root = cut.Find(".bit-blow");
        Assert.AreEqual("my-flow", root.GetAttribute("id"));
        Assert.AreEqual("canvas", root.GetAttribute("data-testid"));
    }

    // ---- nodes initialized ----

    [TestMethod]
    public void NodesAreNotInitializedUntilTheyHaveBeenMeasured()
    {
        // bUnit's loose interop hands back no measurements, which is the "not measured" path.
        var cut = Render(TwoNodes());

        Assert.IsFalse(cut.Instance.NodesInitialized);
    }

    [TestMethod]
    public void ANodeWithAnExplicitSizeIsStillNotInitializedUntilMeasured()
    {
        var cut = Render([new() { Id = "a", Width = 100, Height = 50 }]);

        Assert.IsFalse(cut.Instance.NodesInitialized);
    }

    [TestMethod]
    public void AnEmptyFlowIsNotReportedAsInitialized()
    {
        var cut = Render([]);

        Assert.IsFalse(cut.Instance.NodesInitialized);
    }

    // ---- edge lookup ----

    [TestMethod]
    public void GetEdgeFindsTheEdgeByIdAndReturnsNullOtherwise()
    {
        var edges = new List<BlowEdge> { new() { Id = "e1", Source = "a", Target = "b" } };
        var cut = Render(TwoNodes(), edges);

        Assert.AreSame(edges[0], cut.Instance.GetEdge("e1"));
        Assert.IsNull(cut.Instance.GetEdge("nope"));
    }

    [TestMethod]
    public async Task GetEdgeSeesEdgesAddedThroughTheApi()
    {
        var cut = Render(TwoNodes());

        await cut.InvokeAsync(() => cut.Instance.AddEdges(new BlowEdge { Id = "new", Source = "a", Target = "b" }));

        Assert.IsNotNull(cut.Instance.GetEdge("new"));
    }

    [TestMethod]
    public async Task GetEdgeStopsFindingADeletedEdge()
    {
        var edges = new List<BlowEdge> { new() { Id = "e1", Source = "a", Target = "b" } };
        var cut = Render(TwoNodes(), edges);

        await cut.InvokeAsync(() => cut.Instance.DeleteElementsAsync(edges: [edges[0]]));

        Assert.IsNull(cut.Instance.GetEdge("e1"));
    }

    // ---- per-handle rules ----

    [TestMethod]
    public async Task AHandleRuleCanRejectAConnectionTheCanvasWouldAllow()
    {
        var cut = Render(TwoNodes());

        await cut.InvokeAsync(() =>
        {
            var flow = (IBlowContext)cut.Instance;
            flow.RegisterHandle(
                BlowHandleRegistration.Key("b", BlowHandleType.Target, null),
                new BlowHandleRegistration { IsValidConnection = _ => false });
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom);
        });

        Assert.IsFalse(cut.Instance.IsValidConnectionTarget("b", null, BlowHandleType.Target));
    }

    [TestMethod]
    public async Task ARuleOnTheOriginatingHandleAlsoApplies()
    {
        var cut = Render(TwoNodes());

        await cut.InvokeAsync(() =>
        {
            var flow = (IBlowContext)cut.Instance;
            flow.RegisterHandle(
                BlowHandleRegistration.Key("a", BlowHandleType.Source, null),
                new BlowHandleRegistration { IsValidConnection = c => c.Target == "somewhere-else" });
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom);
        });

        Assert.IsFalse(cut.Instance.IsValidConnectionTarget("b", null, BlowHandleType.Target));
    }

    [TestMethod]
    public async Task AHandleRuleSeesTheNormalisedConnection()
    {
        BlowConnection? seen = null;
        var cut = Render(TwoNodes());

        await cut.InvokeAsync(() =>
        {
            var flow = (IBlowContext)cut.Instance;
            flow.RegisterHandle(
                BlowHandleRegistration.Key("b", BlowHandleType.Target, null),
                new BlowHandleRegistration { IsValidConnection = c => { seen = c; return true; } });
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom);
        });

        cut.Instance.IsValidConnectionTarget("b", null, BlowHandleType.Target);

        Assert.AreEqual("a", seen?.Source);
        Assert.AreEqual("b", seen?.Target);
    }

    [TestMethod]
    public async Task AHandleIsToldWhenADraggedConnectionLandsOnIt()
    {
        var connected = new List<BlowConnection>();
        var cut = Render(TwoNodes());

        await cut.InvokeAsync(() =>
        {
            var flow = (IBlowContext)cut.Instance;
            flow.RegisterHandle(
                BlowHandleRegistration.Key("b", BlowHandleType.Target, null),
                new BlowHandleRegistration
                {
                    OnConnect = EventCallback.Factory.Create<BlowConnection>(this, connected.Add),
                });
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom);
            cut.Instance.CompleteConnection("b", null, BlowHandleType.Target);
        });

        Assert.ContainsSingle(connected);
        Assert.AreEqual("a", connected[0].Source);
        Assert.AreEqual("b", connected[0].Target);
    }

    [TestMethod]
    public async Task BothEndsOfANewConnectionAreNotified()
    {
        var source = 0;
        var target = 0;
        var cut = Render(TwoNodes());

        await cut.InvokeAsync(() =>
        {
            var flow = (IBlowContext)cut.Instance;
            flow.RegisterHandle(BlowHandleRegistration.Key("a", BlowHandleType.Source, null), new BlowHandleRegistration
            {
                OnConnect = EventCallback.Factory.Create<BlowConnection>(this, () => source++),
            });
            flow.RegisterHandle(BlowHandleRegistration.Key("b", BlowHandleType.Target, null), new BlowHandleRegistration
            {
                OnConnect = EventCallback.Factory.Create<BlowConnection>(this, () => target++),
            });
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom);
            cut.Instance.CompleteConnection("b", null, BlowHandleType.Target);
        });

        Assert.AreEqual(1, source);
        Assert.AreEqual(1, target);
    }

    [TestMethod]
    public async Task AnUnregisteredHandleStopsBeingConsulted()
    {
        var cut = Render(TwoNodes());
        var key = BlowHandleRegistration.Key("b", BlowHandleType.Target, null);

        await cut.InvokeAsync(() =>
        {
            var flow = (IBlowContext)cut.Instance;
            flow.RegisterHandle(key, new BlowHandleRegistration { IsValidConnection = _ => false });
            flow.UnregisterHandle(key);
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom);
        });

        Assert.IsTrue(cut.Instance.IsValidConnectionTarget("b", null, BlowHandleType.Target));
    }

    [TestMethod]
    public void HandleRegistrationKeysDistinguishNodeTypeAndHandleId()
    {
        var keys = new[]
        {
            BlowHandleRegistration.Key("a", BlowHandleType.Source, null),
            BlowHandleRegistration.Key("a", BlowHandleType.Target, null),
            BlowHandleRegistration.Key("a", BlowHandleType.Source, "x"),
            BlowHandleRegistration.Key("b", BlowHandleType.Source, null),
        };

        Assert.AreEqual(keys.Length, keys.Distinct().Count());
    }

    [TestMethod]
    public void HandleRegistrationKeysDoNotCollideAcrossFieldBoundaries()
    {
        // Without separators "ab"+"" and "a"+"b" would produce the same key.
        Assert.AreNotEqual(BlowHandleRegistration.Key("ab", BlowHandleType.Source, null), BlowHandleRegistration.Key("a", BlowHandleType.Source, "b"));
    }
}
