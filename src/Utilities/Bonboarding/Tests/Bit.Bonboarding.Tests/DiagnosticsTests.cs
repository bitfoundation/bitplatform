using Bunit;
using Bit.Bonboarding.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bonboarding.Tests;

/// <summary>
/// What a tour tells the people who build it and measure it: why a step's target could not be pointed at, how the
/// user reached a step, how long a tour ran, and which events came from an author's preview rather than a user.
/// </summary>
[TestClass]
public class DiagnosticsTests
{
    private static BonboardingTourDefinition Targeted(string selector, BonboardingMissingTargetBehavior missing) => new()
    {
        Id = "targeted",
        MissingTarget = missing,
        Steps =
        [
            new BonboardingStepDefinition { Id = "one", Target = selector, Title = "One" },
            new BonboardingStepDefinition { Id = "two", Title = "Two" },
        ],
    };

    private static BonboardingEvent Single(BonboardingHarness harness, BonboardingEventKind kind)
        => harness.Events.Events.Single(e => e.Kind == kind);

    // ---- Why a target is missing -----------------------------------------------

    [TestMethod]
    public async Task A_Target_Nothing_Matches_Is_Reported_As_Not_Found()
    {
        await using var harness = new BonboardingHarness();

        var session = await harness.Service.StartAsync(Targeted("#absent", BonboardingMissingTargetBehavior.Wait));

        Assert.AreEqual(BonboardingTourStatus.Waiting, session.Status);
        Assert.AreEqual(BonboardingTargetIssue.NotFound, session.TargetIssue);
        Assert.AreEqual(BonboardingTargetIssue.NotFound, Single(harness, BonboardingEventKind.TargetMissing).TargetIssue);
    }

    [TestMethod]
    public async Task A_Target_That_Exists_But_Is_Not_Shown_Is_Reported_As_Hidden()
    {
        await using var harness = new BonboardingHarness();
        harness.Interop.HiddenSelectors.Add("#collapsed");

        var session = await harness.Service.StartAsync(Targeted("#collapsed", BonboardingMissingTargetBehavior.Center));

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status, "Centred, the step shows without its target.");
        Assert.AreEqual(BonboardingTargetIssue.Hidden, session.TargetIssue, "And it still says why its target is not pointed at.");
        Assert.AreEqual(BonboardingTargetIssue.Hidden, Single(harness, BonboardingEventKind.TargetMissing).TargetIssue);
    }

    [TestMethod]
    public async Task An_Invalid_Selector_Is_Reported_As_Such()
    {
        await using var harness = new BonboardingHarness();
        harness.Interop.InvalidSelectors.Add("#[broken");

        await harness.Service.StartAsync(Targeted("#[broken", BonboardingMissingTargetBehavior.Skip));

        Assert.AreEqual(BonboardingTargetIssue.InvalidSelector, Single(harness, BonboardingEventKind.TargetMissing).TargetIssue);
    }

    [TestMethod]
    public async Task The_Issue_Clears_When_The_Target_Turns_Up_And_On_The_Next_Step()
    {
        await using var harness = new BonboardingHarness();

        var session = await harness.Service.StartAsync(Targeted("#late", BonboardingMissingTargetBehavior.Wait));
        Assert.AreEqual(BonboardingTargetIssue.NotFound, session.TargetIssue);

        await harness.Interop.AppearAsync(session.Id, "#late");

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
        Assert.AreEqual(BonboardingTargetIssue.None, session.TargetIssue);
        Assert.AreEqual(BonboardingTargetIssue.None, Single(harness, BonboardingEventKind.TargetResolved).TargetIssue);

        await harness.Interop.LoseTargetAsync(session.Id, BonboardingTargetIssue.Hidden);
        Assert.AreEqual(BonboardingTargetIssue.Hidden, session.TargetIssue, "The browser says whether a lost target was removed or hidden.");

        await session.NextAsync();
        Assert.AreEqual("two", session.CurrentStep?.Id);
        Assert.AreEqual(BonboardingTargetIssue.None, session.TargetIssue, "A step without a target has nothing missing.");
    }

    [TestMethod]
    public async Task A_Lost_Target_Reported_Without_A_Reason_Counts_As_Not_Found()
    {
        await using var harness = new BonboardingHarness().WithElements("#here");

        var session = await harness.Service.StartAsync(Targeted("#here", BonboardingMissingTargetBehavior.Center));
        await harness.Interop.LoseTargetAsync(session.Id);

        Assert.AreEqual(BonboardingTargetIssue.NotFound, session.TargetIssue);
        Assert.AreEqual(BonboardingTargetIssue.NotFound, Single(harness, BonboardingEventKind.TargetMissing).TargetIssue);
    }

    [TestMethod]
    public void A_Geometry_Reads_Its_Issue_Defensively()
    {
        Assert.AreEqual(BonboardingTargetIssue.None, new BonboardingStepGeometry { Found = true, TargetIssue = "hidden" }.Issue);
        Assert.AreEqual(BonboardingTargetIssue.Hidden, new BonboardingStepGeometry { TargetIssue = "hidden" }.Issue);
        Assert.AreEqual(BonboardingTargetIssue.InvalidSelector, new BonboardingStepGeometry { TargetIssue = "hidden", InvalidSelector = "[" }.Issue);
        Assert.AreEqual(BonboardingTargetIssue.NotFound, new BonboardingStepGeometry().Issue, "An older browser layer that gives no reason.");
        Assert.AreEqual(BonboardingTargetIssue.NotFound, new BonboardingStepGeometry { TargetIssue = "something-new" }.Issue);
    }

    [TestMethod]
    public async Task The_Host_Says_Why_In_Its_Markup_While_The_Target_Is_Missing()
    {
        await using var ctx = new Bunit.BunitContext();
        ctx.Services.AddLogging();
        ctx.Services.AddBitBonboardingServices();
        var interop = new FakeInterop();
        interop.HiddenSelectors.Add("#collapsed");
        ctx.Services.AddSingleton<IBonboardingInterop>(interop);
        ctx.Services.AddBitBonboardingInMemoryStore();

        var host = ctx.Render<BonboardingRoot>();
        var service = ctx.Services.GetRequiredService<IBonboardingService>();

        var session = await host.InvokeAsync(() => service.StartAsync(Targeted("#collapsed", BonboardingMissingTargetBehavior.Wait)));
        host.WaitForAssertion(() => Assert.AreEqual("hidden", host.Find(".bit-bonboarding-root").GetAttribute("data-bonboarding-target-issue")));

        interop.HiddenSelectors.Clear();
        await host.InvokeAsync(() => interop.AppearAsync(session.Id, "#collapsed"));
        host.WaitForAssertion(() => Assert.IsNull(host.Find(".bit-bonboarding-root").GetAttribute("data-bonboarding-target-issue")));
    }

    // ---- Analytics ------------------------------------------------------------------

    [TestMethod]
    public async Task Step_Shown_Says_How_The_User_Got_There()
    {
        await using var harness = new BonboardingHarness();

        var tour = new BonboardingTourDefinition
        {
            Id = "reasons",
            Steps =
            [
                new BonboardingStepDefinition { Id = "a" },
                new BonboardingStepDefinition { Id = "b" },
                new BonboardingStepDefinition { Id = "c" },
            ],
        };

        var session = await harness.Service.StartAsync(tour);
        await session.NextAsync();
        await session.GoToAsync("c");
        await session.PreviousAsync();

        var shown = harness.Events.Events.Where(e => e.Kind == BonboardingEventKind.StepShown).Select(e => (e.Step!.Id, e.ChangeReason)).ToArray();

        CollectionAssert.AreEqual(
            new (string, BonboardingStepChangeReason?)[]
            {
                ("a", BonboardingStepChangeReason.Start),
                ("b", BonboardingStepChangeReason.Next),
                ("c", BonboardingStepChangeReason.Jump),
                ("b", BonboardingStepChangeReason.Previous),
            },
            shown);

        Assert.IsTrue(harness.Events.Events.Where(e => e.Kind != BonboardingEventKind.StepShown).All(e => e.ChangeReason is null));
    }

    [TestMethod]
    public async Task The_Event_That_Ends_A_Tour_Says_How_Long_It_Ran_By_The_Registered_Clock()
    {
        var clock = new ManualClock(new DateTimeOffset(2026, 11, 11, 9, 0, 0, TimeSpan.Zero));
        await using var harness = new BonboardingHarness(configureServices: services => services.AddSingleton<TimeProvider>(clock));

        var session = await harness.Service.StartAsync(Targeted("#unused", BonboardingMissingTargetBehavior.Center));
        clock.Now += TimeSpan.FromSeconds(30);
        await session.NextAsync();
        clock.Now += TimeSpan.FromSeconds(12);
        await session.CompleteAsync();

        var completed = Single(harness, BonboardingEventKind.TourCompleted);
        Assert.AreEqual(TimeSpan.FromSeconds(42), completed.Duration);
        Assert.AreEqual(clock.Now, completed.Timestamp, "Events are stamped by the registered clock.");

        var left = harness.Events.Events.Where(e => e.Kind == BonboardingEventKind.StepLeft).Select(e => e.Duration).ToArray();
        CollectionAssert.AreEqual(new TimeSpan?[] { TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(12) }, left);
    }

    [TestMethod]
    public async Task A_Queued_Start_That_Never_Ran_Reports_No_Duration()
    {
        await using var harness = new BonboardingHarness();

        await harness.Service.StartAsync(Targeted("#unused", BonboardingMissingTargetBehavior.Center));
        var queued = await harness.Service.StartAsync(
            new BonboardingTourDefinition { Id = "queued", Steps = [new BonboardingStepDefinition { Id = "q" }] },
            new BonboardingStartOptions { WhenBusy = BonboardingBusyBehavior.Queue });

        await queued.CancelAsync();

        var cancelled = harness.Events.Events.Single(e => e.Kind == BonboardingEventKind.TourCancelled && e.Tour.Id == "queued");
        Assert.IsNull(cancelled.Duration);
    }

    internal sealed class ManualClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
