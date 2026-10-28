using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// A <c>LayoutId</c> handed over while its previous holder is still on screen on its way out - a
/// <see cref="BmotionPresenceSwitch{TItem}"/> in Sync mode, the old item exiting as the new one enters: the new one
/// grows out of where the old one is, and the two crossfade (motion.dev's <c>layoutCrossfade</c>).
/// </summary>
[TestClass]
public class LayoutCrossfadeTests
{
    // Item 1 is a small card on the left, item 2 a wide one on the right, both holding the LayoutId "card".
    private static RenderFragment<int> Card(bool crossfade = true) => item => b =>
    {
        b.OpenComponent<Bmotion>(0);
        b.AddComponentParameter(1, nameof(Bmotion.Id), $"card-{item}");
        b.AddComponentParameter(2, nameof(Bmotion.LayoutId), "card");
        b.AddComponentParameter(3, nameof(Bmotion.Exit), (BmTarget?)Bm.To(opacity: 0));
        b.AddComponentParameter(4, nameof(Bmotion.Transition), (BmTransition?)Bm.Tween(0.4));
        b.AddComponentParameter(5, nameof(Bmotion.LayoutCrossfade), crossfade);
        b.AddComponentParameter(6, nameof(Bmotion.ChildContent), (RenderFragment)(leaf =>
        {
            leaf.OpenElement(0, "section");
            leaf.AddContent(1, $"Card {item}");
            leaf.CloseElement();
        }));
        b.CloseComponent();
    };

    private static BmotionBoundingRect? Rects(string id) => id switch
    {
        "card-1" => new BmotionBoundingRect { Left = 0, Top = 0, Width = 50, Height = 40 },
        "card-2" => new BmotionBoundingRect { Left = 200, Top = 0, Width = 100, Height = 40 },
        _ => null,
    };

    private static IRenderedComponent<BmotionPresenceSwitch<int>> RenderSwitch(BmotionTestContext ctx, BmPresenceMode mode, bool crossfade = true)
    {
        ctx.Interop.BoundingRectProvider = Rects;
        return ctx.Render<BmotionPresenceSwitch<int>>(ps => ps
            .Add(p => p.Item, 1)
            .Add(p => p.Mode, mode)
            .Add(p => p.ChildContent, Card(crossfade)));
    }

    [TestMethod]
    public void TheNewHolder_GrowsOutOfTheOneLeaving_AndTheTwoCrossfade()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderSwitch(ctx, BmPresenceMode.Sync);

        cut.Render(ps => ps.Add(p => p.Item, 2));

        var flip = ctx.Interop.ArgsOf("playWaapiFlip").Single();
        Assert.AreEqual("card-2", flip[0]);
        Assert.AreEqual(-200, (double)flip[1]!, 1e-9, "it starts where the leaving card is");
        Assert.AreEqual(0.5, (double)flip[3]!, 1e-9, "and at its width (50 / 100)");

        var fade = ctx.Interop.ArgsOf("playLayoutCrossfade").Single();
        Assert.AreEqual("card-2", fade[0], "the new holder fades in");
        Assert.AreEqual("card-1", fade[1], "the one leaving fades out");
        Assert.AreEqual(200, (double)fade[2]!, 1e-9, "travelling onto the new card's box");
        Assert.AreEqual(0, (double)fade[3]!, 1e-9);
        Assert.AreEqual(2, (double)fade[4]!, 1e-9, "and growing to its width (100 / 50)");
        Assert.AreEqual(1, (double)fade[5]!, 1e-9);
        Assert.AreEqual((double)flip[5]!, (double)fade[6]!, 1e-9, "over the same time as the layout animation");
        Assert.AreEqual((string)flip[6]!, (string)fade[7]!, "and on the same curve");
    }

    [TestMethod]
    public void TheNewHolder_RendersHidden_UntilItsCrossfadeTakesOver()
    {
        // On Blazor Server the crossfade starts round trips after the first paint: until then the new card must not
        // show at its new place at full strength.
        using var ctx = new BmotionTestContext();
        var cut = RenderSwitch(ctx, BmPresenceMode.Sync);
        StringAssert.DoesNotMatch(cut.Find("#card-1").GetAttribute("style") ?? "", new System.Text.RegularExpressions.Regex("filter"), "nothing to crossfade with");

        cut.Render(ps => ps.Add(p => p.Item, 2));
        StringAssert.Contains(cut.Find("#card-2").GetAttribute("style"), "filter:opacity(0)");

        // The crossfade clears it in the browser; a later render doesn't bring it back.
        cut.Render(ps => ps.Add(p => p.Item, 2));
        StringAssert.DoesNotMatch(cut.Find("#card-2").GetAttribute("style") ?? "", new System.Text.RegularExpressions.Regex("filter"));
    }

    [TestMethod]
    public async Task IfTheLeavingCardHasGoneBeforeTheCrossfadeCanStart_TheNewOneIsShownAnyway()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderSwitch(ctx, BmPresenceMode.Sync);
        var measuring = new TaskCompletionSource();
        ctx.Interop.BoundingRectGate = measuring.Task;

        cut.Render(ps => ps.Add(p => p.Item, 2));
        // Card 1's exit ends while card 2 is still measuring itself (a round trip on Blazor Server).
        await ctx.FinishWaapiAsync("card-1");
        cut.WaitForAssertion(() => Assert.AreEqual(1, cut.FindAll("section").Count));
        measuring.SetResult();

        cut.WaitForAssertion(() => Assert.AreEqual("card-2", (string?)ctx.Interop.ArgsOf("cancelLayoutCrossfade").LastOrDefault()?[0], "its hidden start is cleared"));
        Assert.IsFalse(ctx.Interop.WasCalled("playLayoutCrossfade"));
    }

    [TestMethod]
    public async Task WaitMode_TheOldCardHasGoneFirst_SoTheNewOneOnlyFlips()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderSwitch(ctx, BmPresenceMode.Wait);

        cut.Render(ps => ps.Add(p => p.Item, 2));
        await ctx.FinishWaapiAsync("card-1"); // its exit

        cut.WaitForAssertion(() => Assert.AreEqual(1, ctx.Interop.CountOf("playWaapiFlip")));
        Assert.AreEqual(-200, (double)ctx.Interop.ArgsOf("playWaapiFlip").Single()[1]!, 1e-9, "from where the old card was last");
        Assert.IsFalse(ctx.Interop.WasCalled("playLayoutCrossfade"));
        StringAssert.DoesNotMatch(cut.Find("#card-2").GetAttribute("style") ?? "", new System.Text.RegularExpressions.Regex("filter"), "and isn't hidden");
    }

    [TestMethod]
    public void LayoutCrossfadeFalse_OnlyFlips()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderSwitch(ctx, BmPresenceMode.Sync, crossfade: false);

        cut.Render(ps => ps.Add(p => p.Item, 2));

        Assert.AreEqual(1, ctx.Interop.CountOf("playWaapiFlip"));
        Assert.IsFalse(ctx.Interop.WasCalled("playLayoutCrossfade"));
    }

    [TestMethod]
    public async Task TheLeavingCard_NoLongerRecordsWhereItIs()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderSwitch(ctx, BmPresenceMode.Sync);
        cut.Render(ps => ps.Add(p => p.Item, 2));
        cut.Render(ps => ps.Add(p => p.Item, 2)); // re-rendered while card 1 is on its way out
        await ctx.FinishWaapiAsync("card-1");
        cut.WaitForAssertion(() => Assert.AreEqual(1, cut.FindAll("section").Count));

        // A third card takes the id over: it grows out of card 2, the holder - not card 1, which had left.
        ctx.Interop.BoundingRectProvider = id => id == "card-3" ? new BmotionBoundingRect { Left = 400, Top = 0, Width = 100, Height = 40 } : Rects(id);
        cut.Render(ps => ps.Add(p => p.Item, 3));

        var flip = ctx.Interop.ArgsOf("playWaapiFlip").Last();
        Assert.AreEqual("card-3", flip[0]);
        Assert.AreEqual(-200, (double)flip[1]!, 1e-9, "from card 2's box (200 - 400)");
    }

    // Card 1 inside a presence, card 2 beside it: hiding the first while showing the second hands the id over
    // with card 1 still on its way out; showing the first again brings it back before it has gone.
    private sealed class CrossfadeHost : ComponentBase
    {
        [Parameter] public bool First { get; set; } = true;
        [Parameter] public bool Second { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<BmotionAnimatePresence>(0);
            builder.AddComponentParameter(1, nameof(BmotionAnimatePresence.IsPresent), First);
            builder.AddComponentParameter(2, nameof(BmotionAnimatePresence.ChildContent), Card()(1));
            builder.CloseComponent();
            if (Second) builder.AddContent(3, Card()(2));
        }
    }

    [TestMethod]
    public void ComingBackBeforeItHasGone_TakesTheLeavingCardOffItsCrossfade()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.BoundingRectProvider = Rects;
        var cut = ctx.Render<CrossfadeHost>();
        cut.Render(ps => ps.Add(p => p.First, false).Add(p => p.Second, true));
        Assert.AreEqual("card-1", (string?)ctx.Interop.ArgsOf("playLayoutCrossfade").Single()[1]);
        Assert.IsFalse(ctx.Interop.WasCalled("cancelLayoutCrossfade"));

        cut.Render(ps => ps.Add(p => p.First, true));

        cut.WaitForAssertion(() => Assert.AreEqual("card-1", (string?)ctx.Interop.ArgsOf("cancelLayoutCrossfade").SingleOrDefault()?[0]));
    }
}
