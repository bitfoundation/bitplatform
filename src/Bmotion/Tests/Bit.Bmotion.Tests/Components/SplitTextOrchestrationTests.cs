using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// How <see cref="BmotionSplitText"/> orchestrates its units: <c>OnComplete</c> rides on the unit
/// that finishes last - the one with the longest stagger delay, which is the first unit when the
/// stagger runs from the last - and without a <c>Transition</c> of its own every unit's staggered
/// transition is built on the surrounding <see cref="BmotionConfig"/>'s default, as a
/// <c>&lt;Bmotion&gt;</c> of its own would be.
/// </summary>
[TestClass]
public class SplitTextOrchestrationTests
{
    // The split units carry the engine ids, in reading order (the word groups of Chars mode don't).
    private static string[] UnitIds<T>(IRenderedComponent<T> cut) where T : IComponent => cut.FindAll("span[id]").Select(s => s.Id!).ToArray();

    private static Dictionary<string, object?> TimingOf(BmotionTestContext ctx, string id)
        => (Dictionary<string, object?>)ctx.Interop.ArgsOf("playWaapiAnimation").Single(a => (string?)a[0] == id)[3]!;

    [TestMethod]
    public async Task OnComplete_StaggeredFromTheLast_WaitsForTheFirstUnit()
    {
        using var ctx = new BmotionTestContext();
        int completions = 0;
        var cut = ctx.Render<BmotionSplitText>(ps => ps
            .Add(p => p.Text, "abcd")
            .Add(p => p.Initial, Bm.To(opacity: 0))
            .Add(p => p.Animate, Bm.To(opacity: 1))
            .Add(p => p.Stagger, Bm.Stagger(0.1, from: BmStaggerFrom.Last))
            .Add(p => p.OnComplete, EventCallback.Factory.Create(this, () => completions++)));
        var ids = UnitIds(cut);
        Assert.AreEqual(4, ids.Length);
        // The baseline: the cascade runs backwards, so "a" starts - and finishes - last.
        CollectionAssert.AreEqual(new[] { 300.0, 200.0, 100.0, 0.0 },
            ids.Select(id => Math.Round((double)TimingOf(ctx, id)["delay"]!, 6)).ToArray());

        // Every unit but the first is done: the text as a whole is not.
        foreach (var id in ids.Skip(1)) await ctx.FinishWaapiAsync(id);
        await cut.InvokeAsync(() => { });
        Assert.AreEqual(0, completions, "OnComplete must not fire while the last unit to finish is still playing");

        await ctx.FinishWaapiAsync(ids[0]);
        cut.WaitForAssertion(() => Assert.AreEqual(1, completions));
    }

    [TestMethod]
    public void OnComplete_IsAttachedToExactlyOneUnit_TheOneWithTheLongestDelay()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionSplitText>(ps => ps
            .Add(p => p.Text, "abcde")
            .Add(p => p.Animate, Bm.To(opacity: 1))
            .Add(p => p.Stagger, Bm.Stagger(0.1, from: BmStaggerFrom.Last))
            .Add(p => p.OnComplete, EventCallback.Factory.Create(this, () => { })));

        var reporting = cut.FindComponents<Bmotion>().Select(u => u.Instance.OnAnimationComplete.HasDelegate).ToArray();

        CollectionAssert.AreEqual(new[] { true, false, false, false, false }, reporting);
    }

    [TestMethod]
    public void WithoutATransition_UnitsBuildOnTheConfigsDefaultTransition()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionConfig>(ps => ps
            .Add(p => p.Transition, Bm.Tween(0.7))
            .Add(p => p.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<BmotionSplitText>(0);
                b.AddComponentParameter(1, nameof(BmotionSplitText.Text), "abc");
                b.AddComponentParameter(2, nameof(BmotionSplitText.Animate), (BmTarget?)Bm.To(opacity: 1));
                b.AddComponentParameter(3, nameof(BmotionSplitText.Stagger), Bm.Stagger(0.1));
                b.CloseComponent();
            })));

        var timings = UnitIds(cut).Select(id => TimingOf(ctx, id)).ToArray();

        // The config's 0.7s, with the stagger on top - not the engine's 0.3s default.
        CollectionAssert.AreEqual(new[] { 700.0, 700.0, 700.0 }, timings.Select(t => (double)t["duration"]!).ToArray());
        CollectionAssert.AreEqual(new[] { 0.0, 100.0, 200.0 }, timings.Select(t => Math.Round((double)t["delay"]!, 6)).ToArray());
    }
}
