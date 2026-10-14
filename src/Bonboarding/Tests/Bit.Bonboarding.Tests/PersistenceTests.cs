using Bit.Bonboarding.Tests.Infrastructure;

namespace Bit.Bonboarding.Tests;

[TestClass]
public class PersistenceTests
{
    private static BonboardingTourDefinition Tour(int version = 1) => new()
    {
        Id = "persisted",
        Version = version,
        Steps =
        [
            new BonboardingStepDefinition { Id = "a", Target = BonboardingStepTarget.Css("#a") },
            new BonboardingStepDefinition { Id = "b", Target = BonboardingStepTarget.Css("#b") },
            new BonboardingStepDefinition { Id = "c", Target = BonboardingStepTarget.Css("#c") },
        ],
    };

    [TestMethod]
    public async Task Progress_Is_Written_On_Every_Step()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var session = await harness.Service.StartAsync(Tour());
        await session.NextAsync();

        var record = await harness.Service.GetRecordAsync("persisted");

        Assert.IsNotNull(record);
        Assert.AreEqual("b", record!.CurrentStepId);
        Assert.AreEqual(1, record.CurrentIndex);
        Assert.IsFalse(record.Completed);
        CollectionAssert.AreEqual(new[] { "a", "b" }, record.VisitedStepIds.ToArray());
    }

    [TestMethod]
    public async Task Completion_Is_Recorded()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var session = await harness.Service.StartAsync(Tour());
        await session.CompleteAsync();

        Assert.IsTrue(await harness.Service.IsCompletedAsync("persisted"));
        Assert.IsTrue(await harness.Service.IsClosedAsync("persisted"));
        Assert.IsNotNull((await harness.Service.GetRecordAsync("persisted"))!.CompletedAt);
    }

    [TestMethod]
    public async Task Dismissal_Closes_The_Tour_Without_Completing_It()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var session = await harness.Service.StartAsync(Tour());
        await session.DismissAsync();

        Assert.IsFalse(await harness.Service.IsCompletedAsync("persisted"));
        Assert.IsTrue(await harness.Service.IsClosedAsync("persisted"));
    }

    [TestMethod]
    public async Task An_Interrupted_Tour_Resumes_Where_The_User_Left_Off()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var first = await harness.Service.StartAsync(Tour());
        await first.NextAsync();

        // Interrupted rather than refused: a page reload, a lost circuit, or another tour.
        await first.CancelAsync();

        var resumed = await harness.Service.StartAsync(Tour(), new BonboardingStartOptions { Force = true });

        Assert.AreEqual("b", resumed.CurrentStep!.Id);
        Assert.Contains(BonboardingEventKind.TourResumed, harness.Events.Kinds);
    }

    [TestMethod]
    public async Task Cancelling_Does_Not_Count_As_The_User_Refusing_The_Tour()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var session = await harness.Service.StartAsync(Tour());
        await session.CancelAsync();

        Assert.AreEqual(BonboardingTourStatus.Cancelled, session.Status);
        Assert.IsFalse(await harness.Service.IsClosedAsync("persisted"));

        // So StartOnce still offers it next time.
        Assert.IsNotNull(await harness.Service.StartOnceAsync(Tour()));
    }

    [TestMethod]
    public async Task Dismissing_Does_Count_As_The_User_Refusing_The_Tour()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var session = await harness.Service.StartAsync(Tour());
        await session.DismissAsync();

        Assert.IsTrue(await harness.Service.IsClosedAsync("persisted"));
        Assert.IsNull(await harness.Service.StartOnceAsync(Tour()));
    }

    [TestMethod]
    public async Task Resume_Can_Be_Turned_Off()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var first = await harness.Service.StartAsync(Tour());
        await first.NextAsync();
        await first.CancelAsync();

        var restarted = await harness.Service.StartAsync(
            Tour(), new BonboardingStartOptions { Resume = false, Force = true });

        Assert.AreEqual("a", restarted.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task A_Bumped_Version_Discards_Stale_Progress()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var first = await harness.Service.StartAsync(Tour());
        await first.NextAsync();
        await first.CancelAsync();

        var next = await harness.Service.StartAsync(Tour(version: 2), new BonboardingStartOptions { Force = true });

        Assert.AreEqual("a", next.CurrentStep!.Id);
    }

    [TestMethod]
    public async Task StartOnce_Skips_A_Tour_The_User_Has_Finished_With()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var first = await harness.Service.StartOnceAsync(Tour());
        Assert.IsNotNull(first);
        await first!.CompleteAsync();

        Assert.IsNull(await harness.Service.StartOnceAsync(Tour()));
    }

    [TestMethod]
    public async Task Restart_Clears_Progress_And_Begins_Again()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var first = await harness.Service.StartAsync(Tour());
        await first.CompleteAsync();

        var restarted = await harness.Service.RestartAsync(Tour());

        Assert.AreEqual("a", restarted.CurrentStep!.Id);
        Assert.IsFalse(await harness.Service.IsCompletedAsync("persisted"));
    }

    [TestMethod]
    public async Task Reset_Makes_A_Finished_Tour_Startable_Again()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var session = await harness.Service.StartAsync(Tour());
        await session.CompleteAsync();

        await harness.Service.ResetAsync("persisted");

        Assert.IsNull(await harness.Service.GetRecordAsync("persisted"));
        Assert.IsNotNull(await harness.Service.StartOnceAsync(Tour()));
    }

    [TestMethod]
    public async Task Persistence_Can_Be_Disabled_Per_Tour()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        var tour = Tour();
        tour.Persist = false;

        var session = await harness.Service.StartAsync(tour);
        await session.CompleteAsync();

        Assert.IsNull(await harness.Service.GetRecordAsync("persisted"));
    }

    [TestMethod]
    public async Task Persistence_Can_Be_Disabled_Globally()
    {
        await using var harness = new BonboardingHarness(options => options.Persist = false);
        harness.WithElements("#a", "#b", "#c");

        var session = await harness.Service.StartAsync(Tour());
        await session.CompleteAsync();

        Assert.IsNull(await harness.Service.GetRecordAsync("persisted"));
    }

    [TestMethod]
    public async Task Resuming_Onto_A_Deleted_Step_Falls_Back_To_The_Index()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b", "#c");

        await harness.Store.SetAsync(new BonboardingRecord
        {
            TourId = "persisted",
            Version = 1,
            CurrentStepId = "removed-long-ago",
            CurrentIndex = 2,
            VisitedStepIds = ["a", "removed-long-ago"],
        });

        var session = await harness.Service.StartAsync(Tour(), new BonboardingStartOptions { Force = true });

        Assert.AreEqual("c", session.CurrentStep!.Id);
        // The history keeps only steps that still exist, so Back cannot land on a ghost.
        Assert.DoesNotContain("removed-long-ago", session.History);
    }

    [TestMethod]
    public async Task The_Browser_Store_Round_Trips_Through_Storage()
    {
        await using var harness = new BonboardingHarness();
        var store = new BonboardingBrowserStore(
            harness.Interop,
            Microsoft.Extensions.Options.Options.Create(new BonboardingOptions()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<BonboardingBrowserStore>.Instance);

        await store.SetAsync(new BonboardingRecord { TourId = "t1", CurrentStepId = "s2", CurrentIndex = 1 });

        Assert.ContainsSingle(harness.Interop.Storage);
        Assert.IsTrue(harness.Interop.Storage.ContainsKey("bit-bonboarding:t1"));

        var reloaded = await store.GetAsync("t1");
        Assert.AreEqual("s2", reloaded!.CurrentStepId);

        await store.ClearAsync();
        Assert.IsEmpty(harness.Interop.Storage);
    }

    [TestMethod]
    public async Task The_Browser_Store_Discards_Unreadable_Values()
    {
        await using var harness = new BonboardingHarness();
        harness.Interop.Storage["bit-bonboarding:broken"] = "{not json";

        var store = new BonboardingBrowserStore(
            harness.Interop,
            Microsoft.Extensions.Options.Options.Create(new BonboardingOptions()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<BonboardingBrowserStore>.Instance);

        Assert.IsNull(await store.GetAsync("broken"));
        Assert.IsEmpty(harness.Interop.Storage);
    }
}
