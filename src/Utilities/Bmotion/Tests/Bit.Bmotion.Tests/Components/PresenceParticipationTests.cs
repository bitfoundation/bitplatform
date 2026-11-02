using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// A component of the app's own takes part in a presence's exits (motion.dev's <c>usePresence</c>): the presence keeps
/// the content mounted until the component reports its exit over, alongside any <see cref="Bmotion"/> in it; a
/// re-entry cancels the exit; a registration that is disposed holds nothing up.
/// </summary>
[TestClass]
public class PresenceParticipationTests
{
    // Plays an exit of its own - the test decides when it is over.
    private sealed class OwnExit : ComponentBase, IDisposable
    {
        [CascadingParameter] public BmotionPresenceContext? Presence { get; set; }
        [Parameter] public TaskCompletionSource ExitDone { get; set; } = default!;
        [Parameter] public Action? OnLeaving { get; set; }

        public BmPresenceRegistration? Registration { get; private set; }
        private bool _leaving;

        protected override void OnInitialized() => Registration = Presence?.Register();

        protected override async Task OnParametersSetAsync()
        {
            if (Registration is not { IsPresent: false } || _leaving) return;
            _leaving = true;
            OnLeaving?.Invoke();
            await ExitDone.Task;
            _leaving = false;
            Registration.SafeToRemove();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "own-exit");
            builder.CloseElement();
        }

        public void Dispose() => Registration?.Dispose();
    }

    private static RenderFragment Content(TaskCompletionSource exitDone, Action? onLeaving = null, bool withBmotion = false) => b =>
    {
        b.OpenComponent<OwnExit>(0);
        b.AddComponentParameter(1, nameof(OwnExit.ExitDone), exitDone);
        b.AddComponentParameter(2, nameof(OwnExit.OnLeaving), onLeaving);
        b.CloseComponent();
        if (!withBmotion) return;
        b.OpenComponent<Bmotion>(3);
        b.AddComponentParameter(4, nameof(Bmotion.Id), "box");
        b.AddComponentParameter(5, nameof(Bmotion.Animate), (BmTarget?)Bm.To(opacity: 1));
        b.AddComponentParameter(6, nameof(Bmotion.Exit), (BmTarget?)Bm.To(opacity: 0));
        b.AddComponentParameter(7, nameof(Bmotion.ChildContent), (RenderFragment)(leaf =>
        {
            leaf.OpenElement(0, "div");
            leaf.AddAttribute(1, "class", "box");
            leaf.CloseElement();
        }));
        b.CloseComponent();
    };

    private IRenderedComponent<BmotionAnimatePresence> Render(BmotionTestContext ctx, RenderFragment content, Action onExitComplete,
        BmPresenceMode mode = BmPresenceMode.Sync)
        => ctx.Render<BmotionAnimatePresence>(ps => ps
            .Add(p => p.IsPresent, true)
            .Add(p => p.Mode, mode)
            .Add(p => p.OnExitComplete, EventCallback.Factory.Create(this, onExitComplete))
            .Add(p => p.ChildContent, content));

    [TestMethod]
    public async Task AComponentOfItsOwn_KeepsTheContentMounted_UntilItsExitIsOver()
    {
        using var ctx = new BmotionTestContext();
        var exitDone = new TaskCompletionSource();
        int leaving = 0, exits = 0;
        var cut = Render(ctx, Content(exitDone, () => leaving++), () => exits++);

        cut.Render(ps => ps.Add(p => p.IsPresent, false));

        Assert.AreEqual(1, leaving, "the component sees it is no longer present, and starts its exit once");
        Assert.AreEqual(1, cut.FindAll("div.own-exit").Count, "mounted while its exit plays");
        Assert.AreEqual(0, exits);

        await cut.InvokeAsync(exitDone.SetResult);

        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("div.own-exit").Count));
        cut.WaitForAssertion(() => Assert.AreEqual(1, exits));
    }

    [TestMethod]
    public async Task ItWaitsForBothTheComponent_AndTheBmotionNextToIt()
    {
        using var ctx = new BmotionTestContext();
        var exitDone = new TaskCompletionSource();
        int exits = 0;
        var cut = Render(ctx, Content(exitDone, withBmotion: true), () => exits++);
        await ctx.FinishWaapiAsync("box");

        cut.Render(ps => ps.Add(p => p.IsPresent, false));
        await ctx.FinishWaapiAsync("box"); // the Bmotion's exit is over
        Assert.AreEqual(1, cut.FindAll("div.own-exit").Count, "the component's exit is still playing");
        Assert.AreEqual(0, exits);

        await cut.InvokeAsync(exitDone.SetResult);
        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("div.box").Count));
        cut.WaitForAssertion(() => Assert.AreEqual(1, exits));
    }

    [TestMethod]
    public async Task AReEntry_CancelsTheExit_AndALateSafeToRemoveCompletesNothing()
    {
        using var ctx = new BmotionTestContext();
        var exitDone = new TaskCompletionSource();
        int exits = 0;
        var cut = Render(ctx, Content(exitDone), () => exits++);

        cut.Render(ps => ps.Add(p => p.IsPresent, false));
        cut.Render(ps => ps.Add(p => p.IsPresent, true));
        await cut.InvokeAsync(exitDone.SetResult); // the overtaken exit reports in

        Assert.AreEqual(1, cut.FindAll("div.own-exit").Count, "back, so it stays");
        Assert.AreEqual(0, exits);
        var registration = cut.FindComponent<OwnExit>().Instance.Registration!;
        Assert.IsTrue(registration.IsPresent);
        Assert.IsFalse(registration.IsExitComplete);
    }

    [TestMethod]
    public void ADisposedRegistration_HoldsNothingUp()
    {
        using var ctx = new BmotionTestContext();
        int exits = 0;
        var cut = Render(ctx, Content(new TaskCompletionSource()), () => exits++);

        cut.Render(ps => ps.Add(p => p.IsPresent, false));
        cut.InvokeAsync(() => cut.FindComponent<OwnExit>().Instance.Registration!.Dispose());

        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindAll("div.own-exit").Count));
        cut.WaitForAssertion(() => Assert.AreEqual(1, exits));
    }

    [TestMethod]
    public async Task WaitMode_HoldsTheNextEnter_UntilTheComponentsExitIsOver()
    {
        using var ctx = new BmotionTestContext();
        var exitDone = new TaskCompletionSource();
        var cut = Render(ctx, Content(exitDone), () => { }, BmPresenceMode.Wait);

        cut.Render(ps => ps.Add(p => p.IsPresent, false));
        cut.Render(ps => ps.Add(p => p.IsPresent, true));
        var registration = cut.FindComponent<OwnExit>().Instance.Registration!;
        Assert.IsFalse(registration.IsPresent, "Wait: still on its way out");

        await cut.InvokeAsync(exitDone.SetResult);
        cut.WaitForAssertion(() => Assert.IsTrue(cut.FindComponent<OwnExit>().Instance.Registration!.IsPresent));
    }
}
