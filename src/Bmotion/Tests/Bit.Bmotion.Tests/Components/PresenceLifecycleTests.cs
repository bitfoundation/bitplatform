using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Presence edge cases around an element's exit: an exit that no longer applies reports nothing,
/// an element that mounts into content already leaving still plays its exit (or the presence would
/// wait for it forever), a list reports each removed item exactly once, and re-entering after a
/// cancelled exit brings the properties only the exit touched back to where they were.
/// </summary>
[TestClass]
public class PresenceLifecycleTests
{
    private static RenderFragment Box(string id, BmTarget? exit = null, Action<IReadOnlyDictionary<string, string>>? onUpdate = null) => b =>
    {
        b.OpenComponent<Bmotion>(0);
        b.SetKey(id);
        b.AddComponentParameter(1, nameof(Bmotion.Id), id);
        b.AddComponentParameter(2, nameof(Bmotion.Initial), (BmTarget?)Bm.To(opacity: 0));
        b.AddComponentParameter(3, nameof(Bmotion.Animate), (BmTarget?)Bm.To(opacity: 1));
        if (exit is not null) b.AddComponentParameter(4, nameof(Bmotion.Exit), exit);
        if (onUpdate is not null)
        {
            b.AddComponentParameter(5, nameof(Bmotion.OnUpdate), onUpdate);
            b.AddComponentParameter(6, nameof(Bmotion.Transition), (BmTransition?)Bm.Tween(1, ease: BmEase.Linear));
        }
        b.AddComponentParameter(7, nameof(Bmotion.ChildContent), (RenderFragment)(leaf =>
        {
            leaf.OpenElement(0, "div");
            leaf.AddAttribute(1, "class", "box");
            leaf.CloseElement();
        }));
        b.CloseComponent();
    };

    private static RenderFragment Boxes(params string[] ids) => b =>
    {
        foreach (var id in ids) b.AddContent(0, Box(id, exit: Bm.To(opacity: 0)));
    };

    private static string? LastOpacityTarget(BmotionTestContext ctx, string id)
    {
        var play = ctx.Interop.Calls.LastOrDefault(c => c.Method == "playWaapiAnimation" && (string?)c.Args[0] == id);
        return play is null ? null : ((Dictionary<string, object>[])play.Args[2]!)[^1]["opacity"] as string;
    }

    private static BmotionElementDiagnostics State(BmotionTestContext ctx, string id)
        => ctx.Engine.GetDiagnostics().Single(d => d.Id == id);

    // ── An exit that no longer applies ────────────────────────────────────────

    [TestMethod]
    public async Task PlayExit_ForAnElementThatIsNotExiting_ReportsNothing()
    {
        using var ctx = new BmotionTestContext();
        // A presence context the test drives by hand, so the element and its context can disagree:
        // the context has started an exit the element hasn't been told about (it re-entered, or
        // its re-render hasn't reached it yet).
        var presence = new BmotionPresenceContext();
        int completions = 0;
        presence.AllExitsComplete += () => completions++;
        var cut = ctx.Render(b =>
        {
            b.OpenComponent<CascadingValue<BmotionPresenceContext>>(0);
            b.AddComponentParameter(1, "Value", presence);
            b.AddComponentParameter(2, "IsFixed", true);
            b.AddComponentParameter(3, "ChildContent", Box("box"));
            b.CloseComponent();
        });
        Assert.AreEqual(1, presence.ChildCount, "the element registers with its presence");

        presence.IsExiting = true;
        var element = cut.FindComponent<Bmotion>().Instance;
        await cut.InvokeAsync(element.PlayExitAsync);

        Assert.AreEqual(0, completions, "only an element that is itself exiting may report its exit complete");
    }

    // ── Mounting into content that is already leaving ─────────────────────────

    [TestMethod]
    public async Task ElementMountedIntoAnExitingPresence_PlaysItsExit_AndThePresenceCompletes()
    {
        using var ctx = new BmotionTestContext();
        int exits = 0;
        var cut = ctx.Render<BmotionAnimatePresence>(ps => ps
            .Add(p => p.IsPresent, true)
            .Add(p => p.OnExitComplete, EventCallback.Factory.Create(this, () => exits++))
            .Add(p => p.ChildContent, Boxes("a")));
        await ctx.FinishWaapiAsync();

        // The same render that starts the exit also adds "b": it mounts into content already leaving.
        cut.Render(ps => ps
            .Add(p => p.IsPresent, false)
            .Add(p => p.ChildContent, Boxes("a", "b")));

        Assert.AreEqual("0", LastOpacityTarget(ctx, "a"), "the existing element plays its exit");
        Assert.AreEqual("0", LastOpacityTarget(ctx, "b"), "the newcomer plays its exit once initialised");

        await ctx.FinishWaapiAsync();

        cut.WaitForAssertion(() => Assert.AreEqual(1, exits), TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("div.box").Count));
    }

    [TestMethod]
    public async Task StaleExitResumingDuringANewerExit_DoesNotCompleteIt()
    {
        using var ctx = new BmotionTestContext();
        int exits = 0;
        // The enter animates scale only and the exit opacity - a value the engine never held, so a
        // re-entry has nothing to restore and doesn't interrupt the first exit: it is still in flight
        // when the second exit starts and interrupts it.
        RenderFragment box = b =>
        {
            b.OpenComponent<Bmotion>(0);
            b.AddComponentParameter(1, nameof(Bmotion.Id), "box");
            b.AddComponentParameter(2, nameof(Bmotion.Initial), (BmTarget?)Bm.To(scale: 0.5));
            b.AddComponentParameter(3, nameof(Bmotion.Animate), (BmTarget?)Bm.To(scale: 1));
            b.AddComponentParameter(4, nameof(Bmotion.Exit), (BmTarget?)Bm.To(opacity: 0));
            b.AddComponentParameter(5, nameof(Bmotion.ChildContent), (RenderFragment)(leaf =>
            {
                leaf.OpenElement(0, "div");
                leaf.AddAttribute(1, "class", "box");
                leaf.CloseElement();
            }));
            b.CloseComponent();
        };
        var cut = ctx.Render<BmotionAnimatePresence>(ps => ps
            .Add(p => p.IsPresent, true)
            .Add(p => p.OnExitComplete, EventCallback.Factory.Create(this, () => exits++))
            .Add(p => p.ChildContent, box));
        await ctx.FinishWaapiAsync();

        cut.Render(ps => ps.Add(p => p.IsPresent, false)); // exit 1
        cut.Render(ps => ps.Add(p => p.IsPresent, true));  // re-entry: exit 1 keeps playing
        await cut.InvokeAsync(() => { });
        cut.Render(ps => ps.Add(p => p.IsPresent, false)); // exit 2 interrupts exit 1
        // Let exit 1's interrupted animation resume its PlayExitAsync.
        await cut.InvokeAsync(() => { });
        await cut.InvokeAsync(() => { });

        Assert.AreEqual(0, exits, "the stale exit must not complete the newer one");
        Assert.AreEqual(1, cut.FindAll("div.box").Count, "the element stays while exit 2 plays");

        // Exit 2 finishing is what completes the presence - once.
        await ctx.FinishWaapiAsync("box");
        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("div.box").Count), TimeSpan.FromSeconds(5));
        Assert.AreEqual(1, exits);
    }

    // ── Keyed lists ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task PresenceGroup_RemovingTwoItems_ReportsEachExitExactlyOnce()
    {
        using var ctx = new BmotionTestContext();
        int exits = 0;
        RenderFragment<string> item = id => Box($"item-{id}", exit: Bm.To(opacity: 0));
        var cut = ctx.Render<BmotionPresenceGroup<string>>(ps => ps
            .Add(p => p.Items, new[] { "a", "b", "c" })
            .Add(p => p.OnExitComplete, EventCallback.Factory.Create(this, () => exits++))
            .Add(p => p.ChildContent, item));
        await ctx.FinishWaapiAsync();

        cut.Render(ps => ps.Add(p => p.Items, new[] { "b" }));
        await ctx.FinishWaapiAsync("item-a");
        await ctx.FinishWaapiAsync("item-c");

        cut.WaitForAssertion(() => Assert.AreEqual(1, cut.FindAll("div.box").Count));
        // A second report per item would come from the removed children's disposal, which the
        // render above has already performed; flush the dispatcher so it would have landed.
        await cut.InvokeAsync(() => { });
        Assert.AreEqual(2, exits);
    }

    // ── Re-entering after a cancelled exit ────────────────────────────────────

    [TestMethod]
    public async Task ReEntryMidExit_AnimatesExitOnlyPropertiesBackToTheirPreExitValues()
    {
        using var ctx = new BmotionTestContext();
        // OnUpdate keeps every animation on the frame loop (in-process), so the exit is a driver
        // the test can advance part-way - a compositor animation would only report its endpoints.
        Action<IReadOnlyDictionary<string, string>> onUpdate = _ => { };
        var cut = ctx.Render<BmotionAnimatePresence>(ps => ps
            .Add(p => p.IsPresent, true)
            .Add(p => p.ChildContent, Box("box", exit: Bm.To(opacity: 0, x: -100), onUpdate: onUpdate)));
        Assert.IsFalse(ctx.Interop.WasCalled("playWaapiAnimation"));
        await ctx.RunFramesAsync(0, 1100);
        Assert.AreEqual(1.0, State(ctx, "box").NumericValues["opacity"], 1e-9);

        // Leave, and get roughly halfway out.
        cut.Render(ps => ps.Add(p => p.IsPresent, false));
        await ctx.RunFramesAsync(1116, 1616);
        double midExit = State(ctx, "box").Transforms["x"];
        Assert.IsTrue(midExit < -20 && midExit > -80, $"the exit should be part-way: x = {midExit}");

        // Change of mind: the enter replays opacity, and x - which only the exit touched - goes
        // back to the 0 it showed before the exit, instead of staying wherever the exit left it.
        cut.Render(ps => ps.Add(p => p.IsPresent, true));
        await ctx.RunFramesAsync(1632, 3000);

        var settled = State(ctx, "box");
        Assert.AreEqual(0, settled.Transforms["x"], 1e-6);
        Assert.AreEqual(1.0, settled.NumericValues["opacity"], 1e-9);
        Assert.AreEqual(1, cut.FindAll("div.box").Count);
    }
}
