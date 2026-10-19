using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Tests for <see cref="BmotionPresenceSwitch{TItem}"/>: changing <c>Item</c> exits the old item's
/// subtree before (Wait) or while (Sync) the new one enters, and the outgoing subtree keeps
/// rendering the item it was created for.
/// </summary>
[TestClass]
public class PresenceSwitchTests
{
    // One page per item; the id carries the item so each page is addressable on its own.
    private static readonly RenderFragment<int> Page = item => b =>
    {
        b.OpenComponent<Bmotion>(0);
        b.AddComponentParameter(1, nameof(Bmotion.Id), $"page-{item}");
        b.AddComponentParameter(2, nameof(Bmotion.Initial), (BmTarget?)Bm.To(opacity: 0));
        b.AddComponentParameter(3, nameof(Bmotion.Animate), (BmTarget?)Bm.To(opacity: 1));
        b.AddComponentParameter(4, nameof(Bmotion.Exit), (BmTarget?)Bm.To(opacity: 0));
        b.AddComponentParameter(5, nameof(Bmotion.ChildContent), (RenderFragment)(leaf =>
        {
            leaf.OpenElement(0, "section");
            leaf.AddContent(1, $"Page {item}");
            leaf.CloseElement();
        }));
        b.CloseComponent();
    };

    private IRenderedComponent<BmotionPresenceSwitch<int>> RenderSwitch(
        BmotionTestContext ctx, BmPresenceMode mode, Action onExitComplete)
        => ctx.Render<BmotionPresenceSwitch<int>>(ps => ps
            .Add(p => p.Item, 1)
            .Add(p => p.Mode, mode)
            .Add(p => p.OnExitComplete, EventCallback.Factory.Create(this, onExitComplete))
            .Add(p => p.ChildContent, Page));

    private static string[] Pages(IRenderedComponent<BmotionPresenceSwitch<int>> cut)
        => cut.FindAll("section").Select(s => s.Id ?? "").ToArray();

    [TestMethod]
    public async Task WaitMode_ExitsTheOldItem_ThenEntersTheNewOne()
    {
        using var ctx = new BmotionTestContext();
        int exits = 0;
        var cut = RenderSwitch(ctx, BmPresenceMode.Wait, () => exits++);
        await ctx.FinishWaapiAsync("page-1");

        cut.Render(ps => ps.Add(p => p.Item, 2));

        // The old page is playing its exit; the new one hasn't mounted.
        CollectionAssert.AreEqual(new[] { "page-1" }, Pages(cut));
        Assert.IsFalse(ctx.Interop.Calls.Any(c => c.Method == "registerElement" && (string?)c.Args[0] == "page-2"));

        await ctx.FinishWaapiAsync("page-1");

        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "page-2" }, Pages(cut)));
        cut.WaitForAssertion(() => Assert.AreEqual(1, exits));
        Assert.AreEqual("Page 2", cut.Find("section").TextContent);
    }

    [TestMethod]
    public void SyncMode_EntersTheNewItemWhileTheOldOneExits()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderSwitch(ctx, BmPresenceMode.Sync, () => { });

        cut.Render(ps => ps.Add(p => p.Item, 2));

        // Both on screen at once - and the outgoing page still renders its own item, not the new one.
        CollectionAssert.AreEqual(new[] { "page-1", "page-2" }, Pages(cut));
        Assert.AreEqual("Page 1", cut.Find("#page-1").TextContent);
    }

    [TestMethod]
    public async Task SyncMode_RemovesTheOldItemOnceItsExitFinishes()
    {
        using var ctx = new BmotionTestContext();
        int exits = 0;
        var cut = RenderSwitch(ctx, BmPresenceMode.Sync, () => exits++);

        cut.Render(ps => ps.Add(p => p.Item, 2));
        await ctx.FinishWaapiAsync("page-1");

        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "page-2" }, Pages(cut)));
        cut.WaitForAssertion(() => Assert.AreEqual(1, exits));
    }

    [TestMethod]
    public void SettingTheSameItem_IsNotASwitch()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderSwitch(ctx, BmPresenceMode.Wait, () => { });
        int playsBefore = ctx.Interop.CountOf("playWaapiAnimation");

        cut.Render(ps => ps.Add(p => p.Item, 1));

        CollectionAssert.AreEqual(new[] { "page-1" }, Pages(cut));
        Assert.AreEqual(playsBefore, ctx.Interop.CountOf("playWaapiAnimation"), "nothing may exit or enter");
    }

    [TestMethod]
    public async Task WaitMode_RapidSwitches_EnterOnlyTheLatestItem()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderSwitch(ctx, BmPresenceMode.Wait, () => { });

        // 1 → 2 → 3 while page 1 is still exiting: the pending item is replaced, not queued.
        cut.Render(ps => ps.Add(p => p.Item, 2));
        cut.Render(ps => ps.Add(p => p.Item, 3));
        await ctx.FinishWaapiAsync("page-1");

        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "page-3" }, Pages(cut)));
        Assert.IsFalse(ctx.Interop.Calls.Any(c => c.Method == "registerElement" && (string?)c.Args[0] == "page-2"),
            "a superseded pending item must never mount");
    }

    [TestMethod]
    public async Task PopLayoutMode_PopsTheOutgoingItem()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderSwitch(ctx, BmPresenceMode.PopLayout, () => { });
        await ctx.FinishWaapiAsync("page-1");

        cut.Render(ps => ps.Add(p => p.Item, 2));

        Assert.IsTrue(ctx.Interop.Calls.Any(c => c.Method == "popLayout" && (string?)c.Args[0] == "page-1"));
        Assert.IsFalse(ctx.Interop.Calls.Any(c => c.Method == "popLayout" && (string?)c.Args[0] == "page-2"));
    }
}
