using Bit.Bonboarding.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bonboarding.Tests;

/// <summary>
/// Who a start is for decides what may hold it back: an automatic start gives way to a snooze and to the
/// application's global rule, a start the user asks for does not, and an author's preview leaves the user's progress
/// exactly as it was.
/// </summary>
[TestClass]
public class StartPolicyTests
{
    private static BonboardingTourDefinition Tour(string id = "tips", Func<BonboardingTourContext, ValueTask<bool>>? canStart = null) => new()
    {
        Id = id,
        CanStart = canStart,
        Steps =
        [
            new BonboardingStepDefinition { Id = $"{id}-1", Title = "One" },
            new BonboardingStepDefinition { Id = $"{id}-2", Title = "Two" },
        ],
    };

    private static readonly BonboardingStartOptions Automatic = new() { Automatic = true };

    // ---- The global gate for automatic starts ----------------------------------------

    [TestMethod]
    public async Task CanAutoStart_Holds_Back_Automatic_Starts_Only()
    {
        var muted = true;
        await using var harness = new BonboardingHarness(options => options.CanAutoStart = _ => ValueTask.FromResult(!muted));

        Assert.IsNull(await harness.Service.StartOnceAsync(Tour()), "StartOnceAsync is automatic.");

        var refused = await Assert.ThrowsExactlyAsync<BonboardingException>(() => harness.Service.StartAsync(Tour(), Automatic));
        StringAssert.Contains(refused.Message, "CanAutoStart");

        var asked = await harness.Service.StartAsync(Tour());
        Assert.AreEqual(BonboardingTourStatus.Running, asked.Status, "A start the user asks for is not the gate's business.");
        await asked.CancelAsync();

        muted = false;
        Assert.IsNotNull(await harness.Service.StartOnceAsync(Tour()));
    }

    [TestMethod]
    public async Task CanAutoStart_Is_Asked_After_The_Tours_Own_CanStart_And_Is_Bypassed_By_Force()
    {
        var asked = new List<string>();
        await using var harness = new BonboardingHarness(options => options.CanAutoStart = ctx =>
        {
            asked.Add($"global:{ctx.Tour.Id}");
            return ValueTask.FromResult(false);
        });

        var tour = Tour(canStart: _ =>
        {
            asked.Add("tour");
            return ValueTask.FromResult(true);
        });

        Assert.IsNull(await harness.Service.StartOnceAsync(tour));
        CollectionAssert.AreEqual(new[] { "tour", "global:tips" }, asked);

        asked.Clear();
        var forced = await harness.Service.StartOnceAsync(tour, new BonboardingStartOptions { Force = true });
        Assert.IsNotNull(forced);
        Assert.IsEmpty(asked, "A forced start asks nobody.");
    }

    [TestMethod]
    public async Task A_Queued_Automatic_Start_Asks_The_Gate_Again_When_Its_Turn_Comes()
    {
        var muted = false;
        await using var harness = new BonboardingHarness(options => options.CanAutoStart = _ => ValueTask.FromResult(!muted));

        var running = await harness.Service.StartAsync(Tour("running"));
        var queued = await harness.Service.StartAsync(Tour("queued"), new BonboardingStartOptions { WhenBusy = BonboardingBusyBehavior.Queue, Automatic = true });
        Assert.AreEqual(BonboardingTourStatus.Idle, queued.Status);

        // The user turned tips off while the queued tour waited.
        muted = true;
        await running.CompleteAsync();

        Assert.AreEqual(BonboardingTourStatus.Cancelled, queued.Status);
        Assert.IsNull(harness.Service.Active);
    }

    [TestMethod]
    public async Task An_Automatic_Start_Passes_Over_A_Snoozed_Tour_And_One_The_User_Asks_For_Does_Not()
    {
        await using var harness = new BonboardingHarness();

        var session = await harness.Service.StartAsync(Tour());
        await session.SnoozeAsync(TimeSpan.FromHours(1));

        var refused = await Assert.ThrowsExactlyAsync<BonboardingException>(() => harness.Service.StartAsync(Tour(), Automatic));
        StringAssert.Contains(refused.Message, "snoozed");

        var asked = await harness.Service.StartAsync(Tour());
        Assert.AreEqual("tips-1", asked.CurrentStep?.Id);
    }

    [TestMethod]
    [DataRow(false, DisplayName = "Muted")]
    [DataRow(true, DisplayName = "Allowed")]
    public async Task A_Declarative_AutoStart_Always_Honours_The_Gate(bool allowed)
    {
        await using var ctx = new Bunit.BunitContext();
        ctx.Services.AddLogging();
        ctx.Services.AddBitBonboardingServices(options => options.CanAutoStart = _ => ValueTask.FromResult(allowed));
        var interop = new FakeInterop();
        interop.ExistingSelectors.Add("#b");
        interop.ExistingSelectors.Add("#c");
        ctx.Services.AddSingleton<IBonboardingInterop>(interop);
        ctx.Services.AddBitBonboardingInMemoryStore();

        ctx.Render<ToggleableTourHarness>(parameters => parameters.Add(p => p.AutoStart, BonboardingAutoStart.Always));

        var service = ctx.Services.GetRequiredService<IBonboardingService>();
        Assert.AreEqual(allowed, service.Active is not null, "Nobody asked for the tour, so the application's rule decides.");
    }

    // ---- Previews ------------------------------------------------------------------------

    [TestMethod]
    public async Task A_Preview_Neither_Reads_Nor_Writes_The_Users_Progress()
    {
        await using var harness = new BonboardingHarness();

        // The user is half way through.
        var real = await harness.Service.StartAsync(Tour());
        await real.NextAsync();
        await real.CancelAsync();
        var before = await harness.Store.GetAsync("tips");

        var preview = await harness.Service.StartAsync(Tour(), new BonboardingStartOptions { Preview = true });
        Assert.IsTrue(preview.IsPreview);
        Assert.AreEqual("tips-1", preview.CurrentStep?.Id, "A preview starts at the top, not where the user is.");

        await preview.NextAsync();
        await preview.CompleteAsync();

        Assert.AreEqual(before, await harness.Store.GetAsync("tips"), "The user's record is exactly as it was.");
        Assert.IsFalse(await harness.Service.IsCompletedAsync("tips"));
    }

    [TestMethod]
    public async Task A_Preview_Runs_Whatever_CanStart_Says_And_However_The_User_Left_The_Tour()
    {
        await using var harness = new BonboardingHarness(options => options.CanAutoStart = _ => ValueTask.FromResult(false));
        var tour = Tour(canStart: _ => ValueTask.FromResult(false));

        await harness.Store.SetAsync(new BonboardingRecord { TourId = "tips", Dismissed = true });

        var preview = await harness.Service.StartOnceAsync(tour, new BonboardingStartOptions { Preview = true });

        Assert.IsNotNull(preview);
        Assert.AreEqual(BonboardingTourStatus.Running, preview.Status);
    }

    [TestMethod]
    public async Task A_Previews_Events_Are_Marked_So_Analytics_Can_Leave_Them_Out()
    {
        await using var harness = new BonboardingHarness();

        var preview = await harness.Service.StartAsync(Tour(), new BonboardingStartOptions { Preview = true });
        await preview.CompleteAsync();
        var real = await harness.Service.StartAsync(Tour("real"));
        await real.CompleteAsync();

        Assert.IsTrue(harness.Events.Events.Where(e => e.Tour.Id == "tips").All(e => e.IsPreview));
        Assert.IsTrue(harness.Events.Events.Where(e => e.Tour.Id == "real").All(e => !e.IsPreview));
    }

    [TestMethod]
    public async Task A_Preview_Is_Not_Stopped_By_The_User_Finishing_The_Tour_Elsewhere()
    {
        var store = new ObservableInMemoryStore();
        await using var harness = new BonboardingHarness(configureServices: services => services.AddSingleton<IBonboardingStore>(store));

        var preview = await harness.Service.StartAsync(Tour(), new BonboardingStartOptions { Preview = true });

        // Another tab: the user dismissed the tour there.
        await store.SetAsync(new BonboardingRecord { TourId = "tips", Dismissed = true });
        store.RaiseChanged("tips");
        await Task.Delay(50);

        Assert.AreEqual(BonboardingTourStatus.Running, preview.Status);
    }

    private sealed class ObservableInMemoryStore : IBonboardingStore, IBonboardingStoreEvents
    {
        private readonly BonboardingInMemoryStore _inner = new();

        public event EventHandler<BonboardingStoreChangedEventArgs>? Changed;

        public void RaiseChanged(string? tourId) => Changed?.Invoke(this, new BonboardingStoreChangedEventArgs(tourId));

        public ValueTask<BonboardingRecord?> GetAsync(string tourId, CancellationToken cancellationToken = default) => _inner.GetAsync(tourId, cancellationToken);

        public ValueTask SetAsync(BonboardingRecord record, CancellationToken cancellationToken = default) => _inner.SetAsync(record, cancellationToken);

        public ValueTask RemoveAsync(string tourId, CancellationToken cancellationToken = default) => _inner.RemoveAsync(tourId, cancellationToken);

        public ValueTask ClearAsync(CancellationToken cancellationToken = default) => _inner.ClearAsync(cancellationToken);
    }
}
