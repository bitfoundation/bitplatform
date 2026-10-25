using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Children animated by an ancestor's variant cascade (no <c>Animate</c> of their own): they report
/// their animations like any other element, start from the initial variant they inherit rather
/// than from identity, and a child that unmounts stops holding a place in the stagger.
/// <para>
/// The variants animate opacity, which is compositor-eligible, so every play is a recorded
/// <c>playWaapiAnimation</c> call whose keyframes and timing the tests can read - and which stays
/// open until the test finishes it.
/// </para>
/// </summary>
[TestClass]
public class VariantChildLifecycleTests
{
    private static readonly BmVariants _states = new()
    {
        ["hidden"] = Bm.To(opacity: 0),
        ["visible"] = Bm.To(opacity: 1),
    };

    private sealed class Log
    {
        public readonly List<BmProps?> Started = new();
        public readonly List<BmProps?> Completed = new();
    }

    // The parent's own element, with one cascade-driven child Bmotion per id inside it.
    private RenderFragment Children(IEnumerable<string> ids, Log? log = null) => b =>
    {
        b.OpenElement(0, "div");
        foreach (var id in ids)
        {
            b.OpenComponent<Bmotion>(1);
            b.SetKey(id);
            b.AddComponentParameter(2, nameof(Bmotion.Id), id);
            if (log is not null)
            {
                b.AddComponentParameter(3, nameof(Bmotion.OnAnimationStart),
                    EventCallback.Factory.Create<BmProps?>(this, p => log.Started.Add(p)));
                b.AddComponentParameter(4, nameof(Bmotion.OnAnimationComplete),
                    EventCallback.Factory.Create<BmProps?>(this, p => { lock (log.Completed) log.Completed.Add(p); }));
            }
            b.AddComponentParameter(5, nameof(Bmotion.ChildContent), (RenderFragment)(leaf =>
            {
                leaf.OpenElement(0, "span");
                leaf.CloseElement();
            }));
            b.CloseComponent();
        }
        b.CloseElement();
    };

    private static IRenderedComponent<Bmotion> RenderParent(BmotionTestContext ctx, RenderFragment children, BmTransition? transition = null)
        => ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "parent")
            .Add(p => p.Variants, _states)
            .Add(p => p.InitialState, "hidden")
            .Add(p => p.State, "visible")
            .Add(p => p.Transition, transition)
            .Add(p => p.ChildContent, children));

    private static object?[][] PlaysFor(BmotionTestContext ctx, string id)
        => ctx.Interop.ArgsOf("playWaapiAnimation").Where(a => (string?)a[0] == id).ToArray();

    private static double DelayOf(object?[] play) => (double)((Dictionary<string, object?>)play[3]!)["delay"]!;

    private static string FirstFrameOpacity(object?[] play) => (string)((Dictionary<string, object>[])play[2]!)[0]["opacity"];

    // ── Lifecycle callbacks ───────────────────────────────────────────────────

    [TestMethod]
    public async Task CascadeDrivenChild_ReportsItsMountAnimation()
    {
        using var ctx = new BmotionTestContext();
        var log = new Log();
        var cut = RenderParent(ctx, Children(["child"], log));

        Assert.AreEqual(1, log.Started.Count, "the inherited enter must raise OnAnimationStart");
        Assert.AreEqual(1.0, log.Started[0]!.Opacity!.Last, "with the variant it plays");
        lock (log.Completed) Assert.AreEqual(0, log.Completed.Count, "nothing has finished yet");

        await ctx.FinishWaapiAsync("child");

        cut.WaitForAssertion(() => { lock (log.Completed) Assert.AreEqual(1, log.Completed.Count); });
        lock (log.Completed) Assert.AreEqual(1.0, log.Completed[0]!.Opacity!.Last);
    }

    [TestMethod]
    public async Task CascadeDrivenChild_ReportsTheAnimationOfALabelChange()
    {
        using var ctx = new BmotionTestContext();
        var log = new Log();
        var cut = RenderParent(ctx, Children(["child"], log));
        await ctx.FinishWaapiAsync();
        cut.WaitForAssertion(() => { lock (log.Completed) Assert.AreEqual(1, log.Completed.Count); });

        cut.Render(ps => ps.Add(p => p.State, "hidden"));

        Assert.AreEqual(2, log.Started.Count, "the label change must raise OnAnimationStart again");
        Assert.AreEqual(0.0, log.Started[1]!.Opacity!.Last);

        await ctx.FinishWaapiAsync("child");

        cut.WaitForAssertion(() => { lock (log.Completed) Assert.AreEqual(2, log.Completed.Count); });
        lock (log.Completed) Assert.AreEqual(0.0, log.Completed[1]!.Opacity!.Last);
    }

    // ── Inherited initial state ───────────────────────────────────────────────

    [TestMethod]
    public void CascadeDrivenChild_StartsFromTheInheritedInitialVariant()
    {
        using var ctx = new BmotionTestContext();
        RenderParent(ctx, Children(["child"]));

        // Seeded with the "hidden" variant the parent's InitialState names: the engine holds it
        // while the enter plays, and the enter leaves from it - not from opacity 1, which the
        // element never showed.
        Assert.AreEqual(0.0, ctx.Engine.GetDiagnostics().Single(d => d.Id == "child").NumericValues["opacity"], 1e-9);
        var play = PlaysFor(ctx, "child").Single();
        Assert.AreEqual("0", FirstFrameOpacity(play));
    }

    // ── Released stagger slots ────────────────────────────────────────────────

    [TestMethod]
    public void UnmountedChild_NoLongerHoldsAPlaceInTheStagger()
    {
        using var ctx = new BmotionTestContext();
        var transition = Bm.Tween(0.3, staggerChildren: 0.2);
        var cut = RenderParent(ctx, Children(["a", "b", "c"]), transition);

        CollectionAssert.AreEqual(new[] { 0.0, 200.0, 400.0 },
            new[] { "a", "b", "c" }.Select(id => Math.Round(DelayOf(PlaysFor(ctx, id).Single()), 6)).ToArray(),
            "the baseline cascade staggers every child");

        // "a" leaves the list, then the label changes: the cascade is only the children still there.
        cut.Render(ps => ps.Add(p => p.ChildContent, Children(["b", "c"])));
        cut.Render(ps => ps.Add(p => p.State, "hidden"));

        Assert.AreEqual(0.0, DelayOf(PlaysFor(ctx, "b")[^1]), 1e-6, "b is now first in the cascade");
        Assert.AreEqual(200.0, DelayOf(PlaysFor(ctx, "c")[^1]), 1e-6, "and c second");
    }
}
