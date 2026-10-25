using Bit.Bonboarding.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bonboarding.Tests;

/// <summary>
/// Tours competing for the screen, users asking to be reminded later, and edited tours meeting old progress: the
/// rules that keep guidance from interrupting people, or from coming back when it should not.
/// </summary>
[TestClass]
public class OrchestrationTests
{
    private static BonboardingTourDefinition Tour(string id, int priority = 0, int version = 1) => new()
    {
        Id = id,
        Priority = priority,
        Version = version,
        Steps =
        [
            new BonboardingStepDefinition { Id = $"{id}-1", Title = "One" },
            new BonboardingStepDefinition { Id = $"{id}-2", Title = "Two" },
        ],
    };

    private static readonly BonboardingStartOptions Queue = new() { WhenBusy = BonboardingBusyBehavior.Queue };

    // ---- Competing tours ------------------------------------------------------

    [TestMethod]
    public async Task A_Queued_Start_Waits_For_The_Running_Tour_To_End()
    {
        await using var harness = new BonboardingHarness();

        var running = await harness.Service.StartAsync(Tour("running"));
        var waiting = await harness.Service.StartAsync(Tour("waiting"), Queue);

        Assert.AreEqual(BonboardingTourStatus.Running, running.Status, "The tour the user is taking is not interrupted.");
        Assert.AreEqual(BonboardingTourStatus.Idle, waiting.Status);
        Assert.AreSame(running, harness.Service.Active);
        Assert.Contains(BonboardingEventKind.TourQueued, harness.Events.Kinds);

        await running.CompleteAsync();

        Assert.AreEqual(BonboardingTourStatus.Running, waiting.Status);
        Assert.AreSame(waiting, harness.Service.Active);
    }

    [TestMethod]
    public async Task Queued_Tours_Start_By_Priority_Then_In_Order()
    {
        await using var harness = new BonboardingHarness();

        var running = await harness.Service.StartAsync(Tour("running"));
        var low = await harness.Service.StartAsync(Tour("low"), Queue);
        var first = await harness.Service.StartAsync(Tour("first", priority: 5), Queue);
        var second = await harness.Service.StartAsync(Tour("second", priority: 5), Queue);

        await running.DismissAsync();
        Assert.AreEqual(BonboardingTourStatus.Running, first.Status);
        Assert.AreEqual(BonboardingTourStatus.Idle, second.Status);
        Assert.AreEqual(BonboardingTourStatus.Idle, low.Status);

        await first.CompleteAsync();
        Assert.AreEqual(BonboardingTourStatus.Running, second.Status, "Equal priorities start in the order they were queued.");

        await second.CompleteAsync();
        Assert.AreEqual(BonboardingTourStatus.Running, low.Status);
    }

    [TestMethod]
    public async Task A_Start_Asked_To_Skip_Does_Not_Start()
    {
        await using var harness = new BonboardingHarness();
        await harness.Service.StartAsync(Tour("running"));

        var skip = new BonboardingStartOptions { WhenBusy = BonboardingBusyBehavior.Skip };

        Assert.IsNull(await harness.Service.StartOnceAsync(Tour("polite"), skip));
        await Assert.ThrowsExactlyAsync<BonboardingException>(() => harness.Service.StartAsync(Tour("polite"), skip));
        Assert.HasCount(1, harness.Service.Sessions);
    }

    [TestMethod]
    public async Task Replace_Is_Still_The_Default()
    {
        await using var harness = new BonboardingHarness();

        var first = await harness.Service.StartAsync(Tour("first"));
        var second = await harness.Service.StartAsync(Tour("second"));

        Assert.AreEqual(BonboardingTourStatus.Cancelled, first.Status);
        Assert.AreEqual(BonboardingTourStatus.Running, second.Status);
    }

    [TestMethod]
    public async Task A_Queued_Once_Start_Asks_Again_When_Its_Turn_Comes()
    {
        await using var harness = new BonboardingHarness();

        var running = await harness.Service.StartAsync(Tour("running"));
        var waiting = await harness.Service.StartOnceAsync(Tour("waiting"), Queue);

        // Finished on another device while it waited.
        await harness.Store.SetAsync(new BonboardingRecord { TourId = "waiting", Completed = true });

        await running.CompleteAsync();

        Assert.AreEqual(BonboardingTourStatus.Cancelled, waiting!.Status);
        Assert.IsNull(harness.Service.Active);
    }

    [TestMethod]
    public async Task A_Queued_Start_Whose_CanStart_Now_Says_No_Is_Cancelled()
    {
        await using var harness = new BonboardingHarness();

        var allowed = true;
        var gated = Tour("gated");
        gated.CanStart = _ => ValueTask.FromResult(allowed);

        var running = await harness.Service.StartAsync(Tour("running"));
        var waiting = await harness.Service.StartAsync(gated, Queue);

        allowed = false;
        await running.CompleteAsync();

        Assert.AreEqual(BonboardingTourStatus.Cancelled, waiting.Status);
    }

    [TestMethod]
    public async Task A_Cancelled_Queued_Start_Never_Runs()
    {
        await using var harness = new BonboardingHarness();

        var running = await harness.Service.StartAsync(Tour("running"));
        var waiting = await harness.Service.StartAsync(Tour("waiting"), Queue);

        await waiting.CancelAsync();
        await running.CompleteAsync();

        Assert.AreEqual(BonboardingTourStatus.Cancelled, waiting.Status);
        Assert.IsEmpty(harness.Service.Sessions);
    }

    [TestMethod]
    public async Task Stopping_Everything_Lets_No_Queued_Tour_Slip_In()
    {
        await using var harness = new BonboardingHarness();

        var running = await harness.Service.StartAsync(Tour("running"));
        var waiting = await harness.Service.StartAsync(Tour("waiting"), Queue);

        await harness.Service.StopAllAsync();

        Assert.AreEqual(BonboardingTourStatus.Cancelled, running.Status);
        Assert.AreEqual(BonboardingTourStatus.Cancelled, waiting.Status);
        Assert.IsFalse(harness.Events.Events.Any(e => e.Kind == BonboardingEventKind.TourStarted && e.Tour.Id == "waiting"));
    }

    [TestMethod]
    public async Task A_Queued_Start_Of_The_Running_Tour_Replaces_It()
    {
        await using var harness = new BonboardingHarness();

        var first = await harness.Service.StartAsync(Tour("same"));
        var again = await harness.Service.StartAsync(Tour("same"), Queue);

        // The same tour starting again is a restart, not a conflict to wait out.
        Assert.AreEqual(BonboardingTourStatus.Cancelled, first.Status);
        Assert.AreEqual(BonboardingTourStatus.Running, again.Status);
    }

    private static BonboardingTourDefinition MultiPageTour() => new()
    {
        Id = "multi-page",
        Steps =
        [
            new BonboardingStepDefinition { Id = "here", Title = "Here" },
            new BonboardingStepDefinition { Id = "there", Title = "There", Route = "settings" },
        ],
    };

    [TestMethod]
    public async Task An_Automatic_Start_Replaces_A_Tour_The_User_Walked_Away_From()
    {
        await using var harness = new BonboardingHarness();

        var left = await harness.Service.StartAsync(MultiPageTour());
        await left.NextAsync();

        // The user followed a link elsewhere: nothing of the tour is on screen, and they may never go back.
        harness.Navigation!.SimulateUserNavigation("dashboard");
        Assert.AreEqual(BonboardingWaitReason.Route, left.WaitReason);

        var page = await harness.Service.StartAsync(Tour("page"), Queue);

        Assert.AreEqual(BonboardingTourStatus.Running, page.Status, "It did not wait for a tour that may never come back.");
        Assert.AreSame(page, harness.Service.Active);
        Assert.AreEqual(BonboardingTourStatus.Cancelled, left.Status);
        Assert.AreEqual("there", (await harness.Service.GetRecordAsync("multi-page"))!.CurrentStepId, "The tour left behind keeps its place.");
    }

    [TestMethod]
    public async Task An_Automatic_Start_Still_Waits_For_A_Tour_On_Its_Way_To_Its_Page()
    {
        await using var harness = new BonboardingHarness();

        // A router renders the new page, and the page starts its own tour, before the tour that navigated there hears
        // of the navigation: its handler was added later.
        Task<IBonboardingTourSession>? pageStart = null;
        harness.Navigation!.LocationChanged += (_, _) => pageStart ??= harness.Service.StartAsync(Tour("page"), Queue);

        var taking = await harness.Service.StartAsync(MultiPageTour());
        await taking.NextAsync();
        var page = await pageStart!;

        Assert.AreEqual(BonboardingTourStatus.Running, taking.Status);
        Assert.AreEqual("there", taking.CurrentStep!.Id);
        Assert.AreEqual(BonboardingTourStatus.Idle, page.Status, "Queued behind the tour the user is taking.");
    }

    [TestMethod]
    public async Task A_Start_Cancelled_Before_It_Got_Going_Holds_Up_Nothing()
    {
        await using var harness = new BonboardingHarness();

        await Assert.ThrowsAsync<OperationCanceledException>(() => harness.Service.StartAsync(Tour("cancelled"), cancellationToken: new CancellationToken(true)));

        Assert.IsEmpty(harness.Service.Sessions, "Nothing is left starting.");

        var next = await harness.Service.StartAsync(Tour("next"), Queue);
        Assert.AreEqual(BonboardingTourStatus.Running, next.Status, "Nothing is left in the way of a queued start.");
    }

    [TestMethod]
    public async Task Restarting_A_Tour_That_Cannot_Start_Keeps_The_Users_Progress()
    {
        await using var harness = new BonboardingHarness();

        var session = await harness.Service.StartAsync(Tour("edited"));
        await session.NextAsync();
        await session.CancelAsync();

        // The tour was edited into one that is not valid: two steps share an id.
        var broken = Tour("edited");
        broken.Steps[1].Id = broken.Steps[0].Id;

        await Assert.ThrowsExactlyAsync<BonboardingException>(() => harness.Service.RestartAsync(broken));

        Assert.AreEqual("edited-2", (await harness.Service.GetRecordAsync("edited"))?.CurrentStepId);
    }

    // ---- Snooze --------------------------------------------------------------------

    private static (BonboardingHarness Harness, ManualClock Clock) SnoozeHarness()
    {
        var clock = new ManualClock(new DateTimeOffset(2026, 10, 25, 9, 0, 0, TimeSpan.Zero));
        var harness = new BonboardingHarness(configureServices: services => services.AddSingleton<TimeProvider>(clock));
        return (harness, clock);
    }

    [TestMethod]
    public async Task A_Snoozed_Tour_Is_Passed_Over_By_Automatic_Starts_Until_The_Snooze_Ends()
    {
        var (harness, clock) = SnoozeHarness();
        await using var _ = harness;

        var session = await harness.Service.StartAsync(Tour("later"));
        await session.NextAsync();
        await session.SnoozeAsync(TimeSpan.FromHours(4));

        Assert.AreEqual(BonboardingTourStatus.Snoozed, session.Status);
        Assert.AreEqual(BonboardingTourEndReason.Snoozed, session.EndReason);
        Assert.Contains(BonboardingEventKind.TourSnoozed, harness.Events.Kinds);

        var record = await harness.Service.GetRecordAsync("later");
        Assert.AreEqual(clock.Now.AddHours(4), record!.SnoozedUntil);
        Assert.IsFalse(record.IsClosed, "A snooze is not a refusal.");

        clock.Now = clock.Now.AddHours(3);
        Assert.IsNull(await harness.Service.StartOnceAsync(Tour("later")));

        clock.Now = clock.Now.AddHours(2);
        var resumed = await harness.Service.StartOnceAsync(Tour("later"));

        Assert.IsNotNull(resumed);
        Assert.AreEqual("later-2", resumed.CurrentStep!.Id, "The tour resumes where it was snoozed.");
        Assert.IsNull((await harness.Service.GetRecordAsync("later"))!.SnoozedUntil, "Running again ends the snooze.");
    }

    [TestMethod]
    public async Task A_Start_The_User_Asks_For_Ignores_A_Snooze()
    {
        var (harness, _) = SnoozeHarness();
        await using var __ = harness;

        var session = await harness.Service.StartAsync(Tour("later"));
        await session.SnoozeAsync(TimeSpan.FromDays(3));

        var asked = await harness.Service.StartAsync(Tour("later"));

        Assert.AreEqual(BonboardingTourStatus.Running, asked.Status);
    }

    [TestMethod]
    public async Task The_Remind_Me_Later_Action_Snoozes_For_A_Day()
    {
        var (harness, clock) = SnoozeHarness();
        await using var _ = harness;

        var tour = Tour("later");
        var remind = BonboardingStepAction.RemindMeLater();
        tour.Steps[0].Actions = [remind];

        var session = await harness.Service.StartAsync(tour);
        await session.InvokeActionAsync(remind);

        Assert.AreEqual(BonboardingTourStatus.Snoozed, session.Status);
        Assert.AreEqual(clock.Now.AddDays(1), (await harness.Service.GetRecordAsync("later"))!.SnoozedUntil);
    }

    [TestMethod]
    public void A_Snooze_Must_Last_A_While()
    {
        var session = new BonboardingTourSession(Tour("x"), new BonboardingOptions(), new FakeInterop(), new BonboardingInMemoryStore(),
            new BonboardingDefaultLocalizer(Microsoft.Extensions.Options.Options.Create(new BonboardingOptions())), null,
            new ServiceCollection().BuildServiceProvider(), Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance, _ => ValueTask.CompletedTask);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => session.SnoozeAsync(TimeSpan.Zero));
    }

    // ---- Version migration ----------------------------------------------------------

    [TestMethod]
    public async Task A_Version_Bump_Discards_Progress_Unless_The_Tour_Migrates_It()
    {
        await using var harness = new BonboardingHarness();

        var v1 = await harness.Service.StartAsync(Tour("edited", version: 1));
        await v1.CompleteAsync();

        // Only the wording changed: keep the completion, so users who finished it are not shown it again.
        var reworded = Tour("edited", version: 2);
        reworded.MigrateRecord = record => record;

        Assert.IsNull(await harness.Service.StartOnceAsync(reworded));

        var rewritten = Tour("edited", version: 3);
        Assert.IsNotNull(await harness.Service.StartOnceAsync(rewritten), "Without a migration a bump means the tour is new.");
    }

    [TestMethod]
    public async Task A_Migration_Can_Map_Renamed_Steps()
    {
        await using var harness = new BonboardingHarness();

        var v1 = await harness.Service.StartAsync(Tour("renamed", version: 1));
        await v1.NextAsync();
        await v1.CancelAsync();

        var v2 = new BonboardingTourDefinition
        {
            Id = "renamed",
            Version = 2,
            Steps = [new BonboardingStepDefinition { Id = "intro" }, new BonboardingStepDefinition { Id = "details" }],
            MigrateRecord = record => record with
            {
                CurrentStepId = record.CurrentStepId == "renamed-2" ? "details" : "intro",
                VisitedStepIds = ["intro"],
            },
        };

        var resumed = await harness.Service.StartAsync(v2);

        Assert.AreEqual("details", resumed.CurrentStep!.Id);
        Assert.AreEqual(2, (await harness.Service.GetRecordAsync("renamed"))!.Version);
    }

    [TestMethod]
    public async Task A_Migration_That_Throws_Is_A_Discarded_Record()
    {
        await using var harness = new BonboardingHarness();

        var v1 = await harness.Service.StartAsync(Tour("broken", version: 1));
        await v1.NextAsync();
        await v1.CancelAsync();

        var v2 = Tour("broken", version: 2);
        v2.MigrateRecord = _ => throw new InvalidOperationException("Bad migration.");

        var started = await harness.Service.StartAsync(v2);

        Assert.AreEqual(BonboardingTourStatus.Running, started.Status);
        Assert.AreEqual("broken-1", started.CurrentStep!.Id);
    }

    private sealed class ManualClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
