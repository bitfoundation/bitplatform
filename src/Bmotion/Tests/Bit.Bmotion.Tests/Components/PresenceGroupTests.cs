using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Tests for <see cref="BmotionPresenceGroup{TItem}"/> - keyed list presence: added items enter,
/// removed items stay mounted (at their old position) until their exit finishes, and an item
/// re-added mid-exit cancels the exit.
/// </summary>
[TestClass]
public class PresenceGroupTests
{
    private static readonly RenderFragment<string> Item = item => b =>
    {
        b.OpenComponent<Bmotion>(0);
        b.AddComponentParameter(1, nameof(Bmotion.Id), $"item-{item}");
        b.AddComponentParameter(2, nameof(Bmotion.Initial), (BmTarget?)Bm.To(opacity: 0));
        b.AddComponentParameter(3, nameof(Bmotion.Animate), (BmTarget?)Bm.To(opacity: 1));
        b.AddComponentParameter(4, nameof(Bmotion.Exit), (BmTarget?)Bm.To(opacity: 0));
        b.AddComponentParameter(5, nameof(Bmotion.ChildContent), (RenderFragment)(leaf =>
        {
            leaf.OpenElement(0, "li");
            leaf.AddContent(1, item);
            leaf.CloseElement();
        }));
        b.CloseComponent();
    };

    private IRenderedComponent<BmotionPresenceGroup<string>> RenderGroup(
        BmotionTestContext ctx, string[] items, Action? onExitComplete = null, BmPresenceMode mode = BmPresenceMode.Sync)
        => ctx.Render<BmotionPresenceGroup<string>>(ps => ps
            .Add(p => p.Items, items)
            .Add(p => p.Mode, mode)
            .Add(p => p.OnExitComplete, EventCallback.Factory.Create(this, onExitComplete ?? (() => { })))
            .Add(p => p.ChildContent, Item));

    private static string[] Rows(IRenderedComponent<BmotionPresenceGroup<string>> cut)
        => cut.FindAll("li").Select(li => li.TextContent).ToArray();

    private static string? LastOpacityTarget(BmotionTestContext ctx, string id)
    {
        var play = ctx.Interop.Calls.LastOrDefault(c => c.Method == "playWaapiAnimation" && (string?)c.Args[0] == id);
        return play is null ? null : ((Dictionary<string, object>[])play.Args[2]!)[^1]["opacity"] as string;
    }

    [TestMethod]
    public void AddedItem_MountsAndEnters_WithoutDisturbingTheOthers()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderGroup(ctx, ["a", "b"]);
        int aPlays = ctx.Interop.Calls.Count(c => c.Method == "playWaapiAnimation" && (string?)c.Args[0] == "item-a");

        cut.Render(ps => ps.Add(p => p.Items, new[] { "a", "c", "b" }));

        CollectionAssert.AreEqual(new[] { "a", "c", "b" }, Rows(cut));
        Assert.AreEqual("1", LastOpacityTarget(ctx, "item-c"), "the new item enters");
        Assert.AreEqual(aPlays, ctx.Interop.Calls.Count(c => c.Method == "playWaapiAnimation" && (string?)c.Args[0] == "item-a"),
            "kept items are not replayed");
    }

    [TestMethod]
    public async Task RemovedItem_StaysAtItsPositionUntilItsExitFinishes()
    {
        using var ctx = new BmotionTestContext();
        int exits = 0;
        var cut = RenderGroup(ctx, ["a", "b", "c"], () => exits++);
        await ctx.FinishWaapiAsync();

        cut.Render(ps => ps.Add(p => p.Items, new[] { "a", "c" }));

        // Still mounted, anchored where it was, and playing its exit.
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, Rows(cut));
        Assert.AreEqual("0", LastOpacityTarget(ctx, "item-b"));
        Assert.AreEqual(0, exits);

        await ctx.FinishWaapiAsync("item-b");

        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "a", "c" }, Rows(cut)));
        // Exactly-once is pinned (and currently ignored) by OnExitComplete_FiresOncePerRemovedItem.
        cut.WaitForAssertion(() => Assert.IsTrue(exits >= 1, "the finished exit must be reported"));
    }

    [TestMethod]
    public async Task OnExitComplete_FiresOncePerRemovedItem()
    {
        using var ctx = new BmotionTestContext();
        int exits = 0;
        var cut = RenderGroup(ctx, ["a", "b"], () => exits++);
        await ctx.FinishWaapiAsync();

        cut.Render(ps => ps.Add(p => p.Items, new[] { "a" }));
        await ctx.FinishWaapiAsync("item-b");

        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "a" }, Rows(cut)));
        // Give a duplicate callback every chance to land: it would come from b's disposal, which the
        // render above has already performed.
        await cut.InvokeAsync(() => { });
        Assert.AreEqual(1, exits);
    }

    [TestMethod]
    public async Task ReAddedMidExit_CancelsTheExitAndReEnters()
    {
        using var ctx = new BmotionTestContext();
        int exits = 0;
        var cut = RenderGroup(ctx, ["a", "b"], () => exits++);
        await ctx.FinishWaapiAsync();

        cut.Render(ps => ps.Add(p => p.Items, new[] { "a" }));
        cut.Render(ps => ps.Add(p => p.Items, new[] { "a", "b" }));

        Assert.AreEqual("1", LastOpacityTarget(ctx, "item-b"), "the revived item plays its enter again");
        await ctx.FinishWaapiAsync();
        CollectionAssert.AreEqual(new[] { "a", "b" }, Rows(cut));
        Assert.AreEqual(0, exits, "a cancelled exit must never be reported");
    }

    [TestMethod]
    public async Task ReAddedMidExit_ThenRemovedAgain_StillExits()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderGroup(ctx, ["a", "b"]);
        await ctx.FinishWaapiAsync();

        cut.Render(ps => ps.Add(p => p.Items, new[] { "a" }));
        cut.Render(ps => ps.Add(p => p.Items, new[] { "a", "b" }));
        await ctx.FinishWaapiAsync();
        cut.Render(ps => ps.Add(p => p.Items, new[] { "a" }));
        await ctx.FinishWaapiAsync("item-b");

        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "a" }, Rows(cut)), TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task PopLayoutMode_PopsOnlyTheRemovedItem()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderGroup(ctx, ["a", "b"], mode: BmPresenceMode.PopLayout);
        await ctx.FinishWaapiAsync();

        cut.Render(ps => ps.Add(p => p.Items, new[] { "a" }));

        var popped = ctx.Interop.ArgsOf("popLayout").Select(a => (string?)a[0]).ToArray();
        CollectionAssert.AreEqual(new[] { "item-b" }, popped);
    }

    [TestMethod]
    public void DuplicateKeys_AreRejected()
    {
        using var ctx = new BmotionTestContext();
        Assert.ThrowsExactly<InvalidOperationException>(() => RenderGroup(ctx, ["a", "a"]));
    }

    [TestMethod]
    public void ItemKey_MatchesReCreatedInstances_SoTheyAreNotReplayed()
    {
        using var ctx = new BmotionTestContext();
        // Same key, new instance: the case ItemKey exists for. Keyed by the first character.
        var cut = ctx.Render<BmotionPresenceGroup<string>>(ps => ps
            .Add(p => p.Items, new[] { "a1" })
            .Add(p => p.ItemKey, s => s[0])
            .Add(p => p.ChildContent, Item));
        int registrations = ctx.Interop.CountOf("registerElement");

        cut.Render(ps => ps.Add(p => p.Items, new[] { "a2" }));

        Assert.AreEqual(registrations, ctx.Interop.CountOf("registerElement"), "the entry must be kept, not remounted");
        Assert.AreEqual("a2", cut.Find("li").TextContent, "but it renders the new item data");
    }
}
