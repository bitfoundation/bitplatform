using Bunit;

namespace Bit.Blow.Tests;

/// <summary>
/// The keyboard instructions a screen reader reads as the description of the flow, a node and an edge; the
/// localizable control labels; reduced motion; and the hit areas that keep a constant size on screen.
/// </summary>
[TestClass]
public class InstructionsAndMotionTests : BunitContext
{
    public InstructionsAndMotionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<BlowCanvas> Render(Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, [new BlowNode { Id = "a" }, new BlowNode { Id = "b", Position = new(0, 200) }]);
            p.Add(c => c.Edges, [new BlowEdge { Id = "e", Source = "a", Target = "b" }]);
            extra?.Invoke(p);
        });

    /// <summary>The accessible description, as a screen reader puts it together: every referenced element's text, in order.</summary>
    private static string DescriptionOf(IRenderedComponent<BlowCanvas> cut, AngleSharp.Dom.IElement element)
    {
        var ids = element.GetAttribute("aria-describedby");
        Assert.IsNotNull(ids, "the element is not described by anything");
        return string.Join(" ", ids.Split(' ').Select(id => cut.Find($"[id='{id}']").TextContent));
    }

    /// <summary>The keyboard instructions alone, among what the element is described by.</summary>
    private static string InstructionsOf(IRenderedComponent<BlowCanvas> cut, AngleSharp.Dom.IElement element)
    {
        var ids = element.GetAttribute("aria-describedby");
        Assert.IsNotNull(ids, "the element is not described by anything");
        return cut.Find($"[id='{ids.Split(' ').Single(id => id.Contains("-help-"))}']").TextContent;
    }

    // ---- keyboard instructions ----

    [TestMethod]
    public void ThePaneNodesAndEdgesAreDescribedByTheirKeyboardInstructions()
    {
        var cut = Render();
        var labels = BlowAriaLabels.Default;

        Assert.AreEqual(labels.PaneInstructions, DescriptionOf(cut, cut.Find(".bit-blow")));
        Assert.AreEqual($"{labels.NodeInstructions} {labels.KeyboardConnectionInstructions("C")}",
            InstructionsOf(cut, cut.Find(".bit-blow-node")));
        Assert.AreEqual(labels.EdgeInstructions, DescriptionOf(cut, cut.Find(".bit-blow-edge")));

        // A node's state comes first, then what can be done with it.
        Assert.AreEqual($"1 outgoing connection. {labels.NodeInstructions} {labels.KeyboardConnectionInstructions("C")}",
            DescriptionOf(cut, cut.Find(".bit-blow-node")));
    }

    [TestMethod]
    public void WithoutKeyboardConnectionsTheInstructionsDoNotOfferThem()
    {
        var cut = Render(p => p.Add(c => c.ConnectionKey, null));

        Assert.AreEqual(BlowAriaLabels.Default.NodeInstructions, InstructionsOf(cut, cut.Find(".bit-blow-node")));
    }

    [TestMethod]
    public void TheInstructionsAreHiddenFromTheReadingOrder()
    {
        var cut = Render();

        foreach (var id in cut.Find(".bit-blow-node").GetAttribute("aria-describedby")!.Split(' '))
            Assert.IsTrue(cut.Find($"[id='{id}']").HasAttribute("hidden"), $"{id} is in the reading order");
    }

    [TestMethod]
    public void TheInstructionsAreLocalizable()
    {
        var labels = new BlowAriaLabels
        {
            PaneInstructions = "Tabulator wechselt zwischen Knoten.",
            NodeInstructions = "Eingabe wählt den Knoten aus.",
            EdgeInstructions = "Eingabe wählt die Kante aus.",
            KeyboardConnectionInstructions = key => $"{key} verbindet.",
            NodeState = s => $"{s.Outgoing} ausgehend.",
        };
        var cut = Render(p => p.Add(c => c.AriaLabels, labels));

        Assert.AreEqual(labels.PaneInstructions, DescriptionOf(cut, cut.Find(".bit-blow")));
        Assert.AreEqual("1 ausgehend. Eingabe wählt den Knoten aus. C verbindet.", DescriptionOf(cut, cut.Find(".bit-blow-node")));
        Assert.AreEqual(labels.EdgeInstructions, DescriptionOf(cut, cut.Find(".bit-blow-edge")));
    }

    [TestMethod]
    public void AnEmptyInstructionLeavesTheElementUndescribed()
    {
        var labels = new BlowAriaLabels
        {
            PaneInstructions = "", NodeInstructions = "", EdgeInstructions = "", NodeState = _ => "", EdgeState = _ => "",
        };
        var cut = Render(p => p.Add(c => c.AriaLabels, labels));

        Assert.IsNull(cut.Find(".bit-blow").GetAttribute("aria-describedby"));
        Assert.IsNull(cut.Find(".bit-blow-node").GetAttribute("aria-describedby"));
        Assert.IsNull(cut.Find(".bit-blow-edge").GetAttribute("aria-describedby"));
        Assert.IsEmpty(cut.FindAll("[hidden]"));
    }

    [TestMethod]
    public void ADescriptionTheAppGivesThePaneIsKeptAlongsideTheInstructions()
    {
        var cut = Render(p => p.AddUnmatched("aria-describedby", "my-help"));

        var ids = cut.Find(".bit-blow").GetAttribute("aria-describedby")!.Split(' ');
        Assert.AreEqual("my-help", ids[0]);
        Assert.HasCount(2, ids);
    }

    [TestMethod]
    public void ElementsOutOfTheTabOrderAreNotDescribed()
    {
        var cut = Render(p => p
            .Add(c => c.NodesFocusable, false)
            .Add(c => c.EdgesFocusable, false));

        Assert.IsNull(cut.Find(".bit-blow-node").GetAttribute("aria-describedby"));
        Assert.IsNull(cut.Find(".bit-blow-edge").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void TwoFlowsOnOnePageDoNotShareDescriptionIds()
    {
        var first = Render();
        var second = Render();

        Assert.AreNotEqual(
            first.Find(".bit-blow-node").GetAttribute("aria-describedby"),
            second.Find(".bit-blow-node").GetAttribute("aria-describedby"));
    }

    // ---- control labels ----

    [TestMethod]
    public void TheControlButtonsCanBeRelabelled()
    {
        var cut = Render(p => p.AddChildContent<BlowControls>(c => c
            .Add(x => x.ZoomInLabel, "Vergrößern")
            .Add(x => x.ZoomOutLabel, "Verkleinern")
            .Add(x => x.FitViewLabel, "Einpassen")
            .Add(x => x.LockLabel, "Sperren")
            .Add(x => x.ShowInteractive, true)));

        var buttons = cut.FindAll(".bit-blow-controls-button");
        Assert.AreEqual("Vergrößern", buttons[0].GetAttribute("aria-label"));
        Assert.AreEqual("Vergrößern", buttons[0].GetAttribute("title"));
        Assert.AreEqual("Verkleinern", buttons[1].GetAttribute("aria-label"));
        Assert.AreEqual("Einpassen", buttons[2].GetAttribute("aria-label"));
        Assert.AreEqual("Sperren", buttons[3].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void TheLockButtonTooltipSaysWhatAClickWillDo()
    {
        var cut = Render(p => p.AddChildContent<BlowControls>(c => c
            .Add(x => x.ShowInteractive, true)
            .Add(x => x.Interactive, false)
            .Add(x => x.UnlockLabel, "Entsperren")));

        var lockButton = cut.FindAll(".bit-blow-controls-button")[^1];
        Assert.AreEqual("Entsperren", lockButton.GetAttribute("title"));
        Assert.AreEqual("Lock interaction", lockButton.GetAttribute("aria-label"));
    }

    // ---- reduced motion ----

    [TestMethod]
    public async Task WithReducedMotionATransitionJumpsStraightToItsDestination()
    {
        var cut = Render(p => p.Add(c => c.ReducedMotion, BlowReducedMotion.Always));

        var transition = cut.InvokeAsync(() => cut.Instance.SetViewportAsync(new BlowViewport(100, 50, 2), 10_000));

        // Not awaited first: a ten-second transition that has already arrived proves it never ran.
        Assert.AreEqual(new BlowViewport(100, 50, 2), cut.Instance.GetViewport());
        await transition;
    }

    [TestMethod]
    public void TheOperatingSystemSettingIsFollowedByDefault()
    {
        JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js").Setup<bool>("prefersReducedMotion").SetResult(true);

        var cut = Render();

        Assert.IsTrue(cut.Instance.ReducesMotion);
    }

    [TestMethod]
    public void TheAppCanOverruleTheOperatingSystem()
    {
        JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js").Setup<bool>("prefersReducedMotion").SetResult(true);

        var cut = Render(p => p.Add(c => c.ReducedMotion, BlowReducedMotion.Never));

        Assert.IsFalse(cut.Instance.ReducesMotion);
        Assert.Contains("bit-blow-allow-motion", cut.Find(".bit-blow").ClassName!);
    }

    [TestMethod]
    public void ForcedReducedMotionIsMarkedForTheStylesheet()
    {
        var cut = Render(p => p.Add(c => c.ReducedMotion, BlowReducedMotion.Always));

        Assert.Contains("bit-blow-reduce-motion", cut.Find(".bit-blow").ClassName!);
    }

    // ---- hit areas that keep their size on screen ----

    [TestMethod]
    public void TheZoomIsPublishedToTheStylesheet()
    {
        var cut = Render(p => p.Add(c => c.DefaultViewport, new BlowViewport(0, 0, 0.5)));

        Assert.Contains("--bit-blow-zoom:0.5", cut.Find(".bit-blow-viewport").GetAttribute("style")!);
    }

    [TestMethod]
    public void AnEdgeHitPathIsSizedInScreenPixels()
    {
        var cut = Render();

        var style = cut.Find(".bit-blow-edge-interaction").GetAttribute("style");
        Assert.AreEqual("stroke-width:calc(14px / var(--bit-blow-zoom, 1));", style);
    }

    [TestMethod]
    public void ACustomEdgeHitPathIsSizedInScreenPixelsToo()
    {
        var cut = Render<BlowBaseEdge>(p => p.Add(c => c.Path, "M0,0 L10,10").Add(c => c.InteractionWidth, 20));

        Assert.AreEqual("stroke-width:calc(20px / var(--bit-blow-zoom, 1));",
            cut.Find(".bit-blow-edge-interaction").GetAttribute("style"));
    }
}
