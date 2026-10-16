using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Tests for <see cref="BmotionAnimatePresence"/>: an exiting subtree stays mounted until its
/// <c>Exit</c> animations finish, the three <see cref="BmPresenceMode"/>s sequence exit and re-entry
/// differently, and a re-entry mid-exit cancels the exit instead of stranding the element.
/// <para>
/// Opacity enter/exit targets are compositor-eligible, so each one is a Web Animation the fake
/// browser holds open until the test finishes it - which makes "the exit is still playing" a state
/// the test controls rather than a race.
/// </para>
/// </summary>
[TestClass]
public class AnimatePresenceTests
{
    private static RenderFragment Box(string id, bool withExit = true) => b =>
    {
        b.OpenComponent<Bmotion>(0);
        b.AddComponentParameter(1, nameof(Bmotion.Id), id);
        b.AddComponentParameter(2, nameof(Bmotion.Initial), (BmTarget?)Bm.To(opacity: 0));
        b.AddComponentParameter(3, nameof(Bmotion.Animate), (BmTarget?)Bm.To(opacity: 1));
        if (withExit) b.AddComponentParameter(4, nameof(Bmotion.Exit), (BmTarget?)Bm.To(opacity: 0));
        b.AddComponentParameter(5, nameof(Bmotion.ChildContent), (RenderFragment)(leaf =>
        {
            leaf.OpenElement(0, "div");
            leaf.AddAttribute(1, "class", "box");
            leaf.CloseElement();
        }));
        b.CloseComponent();
    };

    private IRenderedComponent<BmotionAnimatePresence> RenderPresence(
        BmotionTestContext ctx, BmPresenceMode mode, Action onExitComplete, bool isPresent = true, bool withExit = true)
        => ctx.Render<BmotionAnimatePresence>(ps => ps
            .Add(p => p.IsPresent, isPresent)
            .Add(p => p.Mode, mode)
            .Add(p => p.OnExitComplete, EventCallback.Factory.Create(this, onExitComplete))
            .Add(p => p.ChildContent, Box("box", withExit)));

    // The opacity the most recent compositor animation for the element is heading to.
    private static string? LastOpacityTarget(BmotionTestContext ctx, string id)
    {
        var play = ctx.Interop.Calls.LastOrDefault(c => c.Method == "playWaapiAnimation" && (string?)c.Args[0] == id);
        return play is null ? null : ((Dictionary<string, object>[])play.Args[2]!)[^1]["opacity"] as string;
    }

    private static int IndexOfLast(BmotionTestContext ctx, string method, string id)
    {
        for (int i = ctx.Interop.Calls.Count - 1; i >= 0; i--)
            if (ctx.Interop.Calls[i].Method == method && (string?)ctx.Interop.Calls[i].Args[0] == id) return i;
        return -1;
    }

    [TestMethod]
    public async Task Exit_KeepsTheElementUntilItsExitFinishes_ThenRemovesIt()
    {
        using var ctx = new BmotionTestContext();
        int exits = 0;
        var cut = RenderPresence(ctx, BmPresenceMode.Sync, () => exits++);
        await ctx.FinishWaapiAsync("box"); // the enter animation has played

        cut.Render(ps => ps.Add(p => p.IsPresent, false));

        // Mid-exit: still in the DOM, playing its Exit target.
        Assert.AreEqual(1, cut.FindAll("div.box").Count, "an exiting element must stay mounted while its exit plays");
        Assert.AreEqual("0", LastOpacityTarget(ctx, "box"));
        Assert.AreEqual(0, exits);

        await ctx.FinishWaapiAsync("box");

        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("div.box").Count));
        cut.WaitForAssertion(() => Assert.AreEqual(1, exits, "OnExitComplete fires once per completed exit"));
        Assert.IsTrue(ctx.Interop.WasCalled("unregisterElement"), "the removed element must be torn down");
    }

    [TestMethod]
    public void InitiallyAbsent_RendersNothing_UntilPresent()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderPresence(ctx, BmPresenceMode.Sync, () => { }, isPresent: false);

        Assert.AreEqual(0, cut.FindAll("div.box").Count);
        Assert.IsFalse(ctx.Interop.WasCalled("registerElement"), "nothing may mount while absent");

        cut.Render(ps => ps.Add(p => p.IsPresent, true));

        Assert.AreEqual(1, cut.FindAll("div.box").Count);
        Assert.AreEqual("1", LastOpacityTarget(ctx, "box"), "mounting plays the enter animation");
    }

    [TestMethod]
    public void ChildWithoutAnExitTarget_IsRemovedWithoutWaiting()
    {
        using var ctx = new BmotionTestContext();
        int exits = 0;
        var cut = RenderPresence(ctx, BmPresenceMode.Sync, () => exits++, withExit: false);

        cut.Render(ps => ps.Add(p => p.IsPresent, false));

        // Nothing to play, so the exit is complete at once.
        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("div.box").Count));
        cut.WaitForAssertion(() => Assert.AreEqual(1, exits));
    }

    [TestMethod]
    public async Task PopLayout_PopsTheExitingElement_BeforeItsExitPlays()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderPresence(ctx, BmPresenceMode.PopLayout, () => { });
        await ctx.FinishWaapiAsync("box");
        int playsBefore = ctx.Interop.CountOf("playWaapiAnimation");

        cut.Render(ps => ps.Add(p => p.IsPresent, false));

        int pop = IndexOfLast(ctx, "popLayout", "box");
        Assert.IsTrue(pop >= 0, "PopLayout mode must pop the exiting element out of the flow");
        Assert.IsTrue(ctx.Interop.CountOf("playWaapiAnimation") > playsBefore);
        Assert.IsTrue(pop < IndexOfLast(ctx, "playWaapiAnimation", "box"), "it pops first, then plays the exit");
    }

    [TestMethod]
    public void SyncMode_DoesNotPopLayout()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderPresence(ctx, BmPresenceMode.Sync, () => { });

        cut.Render(ps => ps.Add(p => p.IsPresent, false));

        Assert.IsFalse(ctx.Interop.WasCalled("popLayout"));
    }

    [TestMethod]
    public async Task PopLayout_ReEnterMidExit_UnpopsAndReplaysTheEnter()
    {
        using var ctx = new BmotionTestContext();
        int exits = 0;
        var cut = RenderPresence(ctx, BmPresenceMode.PopLayout, () => exits++);
        await ctx.FinishWaapiAsync("box");

        cut.Render(ps => ps.Add(p => p.IsPresent, false));
        cut.Render(ps => ps.Add(p => p.IsPresent, true));

        // Put back into the flow, after it was popped out.
        int unpop = IndexOfLast(ctx, "unpopLayout", "box");
        Assert.IsTrue(unpop > IndexOfLast(ctx, "popLayout", "box"), "a cancelled popLayout exit must restore the element");
        Assert.AreEqual("1", LastOpacityTarget(ctx, "box"), "the enter animation replays");
        Assert.AreEqual(1, cut.FindAll("div.box").Count);

        // Even once every animation has run its course, the cancelled exit never completes.
        await ctx.FinishWaapiAsync();
        Assert.AreEqual(1, cut.FindAll("div.box").Count);
        Assert.AreEqual(0, exits);
    }

    [TestMethod]
    public async Task SyncMode_ReEnterMidExit_KeepsTheElement_AndNeverReportsTheExit()
    {
        using var ctx = new BmotionTestContext();
        int exits = 0;
        var cut = RenderPresence(ctx, BmPresenceMode.Sync, () => exits++);
        await ctx.FinishWaapiAsync("box");

        cut.Render(ps => ps.Add(p => p.IsPresent, false));
        cut.Render(ps => ps.Add(p => p.IsPresent, true));

        Assert.AreEqual("1", LastOpacityTarget(ctx, "box"), "Sync re-enters straight away");
        await ctx.FinishWaapiAsync();
        Assert.AreEqual(1, cut.FindAll("div.box").Count);
        Assert.AreEqual(0, exits);
    }

    [TestMethod]
    public async Task ReEnterMidExit_ThenExitAgain_StillCompletesAndRemoves()
    {
        using var ctx = new BmotionTestContext();
        int exits = 0;
        var cut = RenderPresence(ctx, BmPresenceMode.Sync, () => exits++);
        await ctx.FinishWaapiAsync("box");

        // Leave, change your mind, then leave for real.
        cut.Render(ps => ps.Add(p => p.IsPresent, false));
        cut.Render(ps => ps.Add(p => p.IsPresent, true));
        await ctx.FinishWaapiAsync("box");
        cut.Render(ps => ps.Add(p => p.IsPresent, false));
        await ctx.FinishWaapiAsync("box");

        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("div.box").Count),
            TimeSpan.FromSeconds(5));
        Assert.AreEqual(1, exits);
    }

    [TestMethod]
    public async Task WaitMode_ReEnterMidExit_HoldsTheEnterUntilTheExitFinishes()
    {
        using var ctx = new BmotionTestContext();
        int exits = 0;
        var cut = RenderPresence(ctx, BmPresenceMode.Wait, () => exits++);
        await ctx.FinishWaapiAsync("box");

        cut.Render(ps => ps.Add(p => p.IsPresent, false));
        cut.Render(ps => ps.Add(p => p.IsPresent, true));

        // Wait: the exit keeps playing; the enter is deferred.
        Assert.AreEqual("0", LastOpacityTarget(ctx, "box"), "the enter must not interrupt the exit in Wait mode");
        Assert.IsFalse(ctx.Interop.WasCalled("unpopLayout"));

        await ctx.FinishWaapiAsync("box");

        // The exit completed, then the content entered again - and stayed.
        cut.WaitForAssertion(() => Assert.AreEqual(1, exits));
        cut.WaitForAssertion(() => Assert.AreEqual("1", LastOpacityTarget(ctx, "box")));
        Assert.AreEqual(1, cut.FindAll("div.box").Count);
    }
}
