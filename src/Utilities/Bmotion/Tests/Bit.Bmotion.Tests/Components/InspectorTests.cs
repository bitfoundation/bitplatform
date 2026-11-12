using Bit.Bmotion.Tests.TestInfra;
using Bunit;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Tests for <see cref="BmotionInspector"/>, the debug overlay that renders the engine's
/// diagnostics. It polls on a real timer, so the assertions wait (bounded) for its next refresh.
/// </summary>
[TestClass]
public class InspectorTests
{
    private static readonly TimeSpan Refresh = TimeSpan.FromSeconds(5);

    [TestMethod]
    public void BeforeItsFirstPoll_ShowsTheEmptyState()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionInspector>(ps => ps.Add(p => p.RefreshMs, 60_000));

        Assert.AreEqual("No elements registered.", cut.Find(".bm-insp-empty").TextContent);
        Assert.Contains("0 el", cut.Find(".bm-insp-count").TextContent);
    }

    [TestMethod]
    public async Task RendersARowPerRegisteredElement_WithItsLiveState()
    {
        using var ctx = new BmotionTestContext();
        // Without Web Animations the colour tween runs on the frame loop, so it shows up as an active driver.
        ctx.Interop.WebAnimationsSupported = false;
        ctx.Engine.RegisterElement("idle", new Dictionary<string, object?> { ["x"] = 10.0, ["opacity"] = 0.5 });
        ctx.Engine.RegisterElement("busy", new Dictionary<string, object?> { ["backgroundColor"] = "#000000" });
        await ctx.Engine.AnimateToAsync("busy", new Dictionary<string, object?> { ["backgroundColor"] = "#ff0000" },
            Bm.Tween(10).ToConfig());

        var cut = ctx.Render<BmotionInspector>(ps => ps.Add(p => p.RefreshMs, 50));

        cut.WaitForAssertion(() => Assert.HasCount(2, cut.FindAll(".bm-insp-el")), Refresh);

        var ids = cut.FindAll(".bm-insp-id").Select(e => e.TextContent).ToArray();
        Assert.AreSequenceEqual(new[] { "idle", "busy" }, ids, Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
        Assert.Contains("2 el · 1 active", cut.Find(".bm-insp-count").TextContent);

        var busy = cut.FindAll(".bm-insp-el").Single(e => e.QuerySelector(".bm-insp-id")!.TextContent == "busy");
        Assert.IsTrue(busy.ClassList.Contains("live"));
        Assert.AreEqual("1 driver", busy.QuerySelector(".bm-insp-badge")!.TextContent);
        Assert.AreEqual("backgroundColor", busy.QuerySelector(".bm-insp-props")!.TextContent);

        var idle = cut.FindAll(".bm-insp-el").Single(e => e.QuerySelector(".bm-insp-id")!.TextContent == "idle");
        Assert.IsFalse(idle.ClassList.Contains("live"));
        var chips = idle.QuerySelectorAll(".bm-insp-chip").Select(c => c.TextContent.Trim()).ToArray();
        Assert.AreSequenceEqual(new[] { "x 10", "opacity 0.5" }, chips, Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
    }

    [TestMethod]
    public async Task ABrowserPlayedAnimation_ShowsAsLive_WithWhatTheBrowserAnimates()
    {
        using var ctx = new BmotionTestContext();
        ctx.Engine.RegisterElement("busy", new Dictionary<string, object?> { ["backgroundColor"] = "#000000" });
        await ctx.Engine.AnimateToAsync("busy", new Dictionary<string, object?> { ["backgroundColor"] = "#ff0000" },
            Bm.Tween(10).ToConfig());

        var cut = ctx.Render<BmotionInspector>(ps => ps.Add(p => p.RefreshMs, 50));

        cut.WaitForAssertion(() => Assert.Contains("1 el · 1 active", cut.Find(".bm-insp-count").TextContent), Refresh);
        var busy = cut.Find(".bm-insp-el");
        Assert.IsTrue(busy.ClassList.Contains("live"));
        var badge = busy.QuerySelector(".bm-insp-badge")!;
        Assert.AreEqual("1 in browser", badge.TextContent);
        Assert.AreEqual("backgroundColor", badge.GetAttribute("title"));
    }

    [TestMethod]
    public void TheBar_TogglesThePanelBody()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionInspector>(ps => ps.Add(p => p.StartOpen, false).Add(p => p.RefreshMs, 60_000));

        Assert.IsEmpty(cut.FindAll(".bm-insp-body"), "StartOpen=false starts collapsed");
        Assert.IsTrue(cut.Find(".bm-inspector").ClassList.Contains("closed"));

        cut.Find(".bm-insp-bar").Click();

        Assert.ContainsSingle(cut.FindAll(".bm-insp-body"));
        Assert.IsTrue(cut.Find(".bm-inspector").ClassList.Contains("open"));
    }
}
