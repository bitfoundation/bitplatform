using Bit.Bonboarding.Tests.Infrastructure;
using Microsoft.JSInterop;

namespace Bit.Bonboarding.Tests;

/// <summary>
/// The failure modes that a real deployment actually hits: a Blazor Server circuit dropping
/// mid-tour, a page unloading, storage being unavailable, and the scope being torn down while a
/// tour is running. None of them should surface as an error to the application.
/// </summary>
[TestClass]
public class ResilienceTests
{
    private static BonboardingTourDefinition Tour() => new()
    {
        Id = "resilient",
        Steps =
        [
            new BonboardingStepDefinition { Id = "a", Target = BonboardingStepTarget.Css("#a"), Title = "A" },
            new BonboardingStepDefinition { Id = "b", Target = BonboardingStepTarget.Css("#b"), Title = "B" },
        ],
    };

    public static IEnumerable<object[]> TransientFailures =>
    [
        [(Func<Exception>)(() => new JSDisconnectedException("the circuit is gone"))],
        [(Func<Exception>)(() => new JSException("the page is unloading"))],
        [(Func<Exception>)(() => new ObjectDisposedException("module"))],
        // Blazor throws this from interop during prerendering.
        [(Func<Exception>)(() => new InvalidOperationException("JavaScript interop calls cannot be issued during prerendering"))],
        [(Func<Exception>)(() => new OperationCanceledException())],
    ];

    [TestMethod]
    [DynamicData(nameof(TransientFailures))]
    public async Task A_Browser_Failure_During_Activation_Does_Not_Fail_The_Tour(Func<Exception> failure)
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");
        harness.Interop.Failure = failure;

        var session = await harness.Service.StartAsync(Tour());

        // The step is shown without measurement rather than the tour dying.
        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
        Assert.AreEqual("a", session.CurrentStep!.Id);
        Assert.IsNull(session.Error);
    }

    [TestMethod]
    [DynamicData(nameof(TransientFailures))]
    public async Task The_Circuit_Can_Drop_Mid_Tour(Func<Exception> failure)
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var session = await harness.Service.StartAsync(Tour());
        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);

        harness.Interop.Failure = failure;

        await session.NextAsync();
        await session.RefreshAsync();

        Assert.AreEqual("b", session.CurrentStep!.Id);
        Assert.IsNull(session.Error);
    }

    [TestMethod]
    [DynamicData(nameof(TransientFailures))]
    public async Task Ending_A_Tour_Survives_A_Dead_Circuit(Func<Exception> failure)
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var session = await harness.Service.StartAsync(Tour());
        harness.Interop.Failure = failure;

        await session.CompleteAsync();

        Assert.AreEqual(BonboardingTourStatus.Completed, session.Status);
        Assert.IsNull(session.Error);
    }

    [TestMethod]
    public async Task A_Store_That_Is_Unavailable_Does_Not_Break_A_Tour()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        // The browser store falls back to memory when interop is unavailable.
        var store = new BonboardingBrowserStore(
            harness.Interop,
            Microsoft.Extensions.Options.Options.Create(new BonboardingOptions()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<BonboardingBrowserStore>.Instance);

        harness.Interop.Failure = () => new JSDisconnectedException("the circuit is gone");

        await store.SetAsync(new BonboardingRecord { TourId = "resilient", CurrentStepId = "a" });

        Assert.IsNotNull(await store.GetAsync("resilient"));
        Assert.AreEqual(0, harness.Interop.Storage.Count);
    }

    [TestMethod]
    public async Task Disposing_The_Scope_While_A_Tour_Runs_Is_Clean()
    {
        var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var session = await harness.Service.StartAsync(Tour());
        Assert.IsTrue(session.IsActive);

        await harness.DisposeAsync();

        // Commands issued after disposal are ignored rather than throwing.
        await session.NextAsync();
        await session.DismissAsync();
    }

    [TestMethod]
    public async Task A_Session_Can_Be_Disposed_Twice()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var session = (BonboardingTourSession)await harness.Service.StartAsync(Tour());

        await session.DisposeAsync();
        await session.DisposeAsync();
        session.Dispose();
    }

    [TestMethod]
    public async Task A_Prerendered_Pass_Leaves_No_Browser_State_Behind()
    {
        await using var harness = new BonboardingHarness();
        harness.Interop.IsAvailable = false;

        var session = await harness.Service.StartAsync(Tour());
        await session.CompleteAsync();

        Assert.IsEmpty(harness.Interop.AttachedSessions);
        Assert.IsEmpty(harness.Interop.Activations);
        Assert.AreEqual(BonboardingTourStatus.Completed, session.Status);
    }

    [TestMethod]
    public async Task An_Interleaved_Storm_Of_Commands_Leaves_A_Consistent_State()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a", "#b");

        var session = await harness.Service.StartAsync(Tour());

        // Navigation, geometry pushes and a refresh all racing, as they do on a resizing page.
        await Task.WhenAll(
            session.NextAsync(),
            session.PreviousAsync(),
            harness.Interop.PushGeometryAsync(session.Id),
            session.RefreshAsync(),
            harness.Interop.PushGeometryAsync(session.Id));

        Assert.IsTrue(session.Status is BonboardingTourStatus.Running or BonboardingTourStatus.Completed);
        Assert.IsNull(session.Error);
    }

    [TestMethod]
    public async Task Out_Of_Order_Measurements_Are_Discarded()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var session = (BonboardingTourSession)await harness.Service.StartAsync(Tour());
        await harness.Interop.PushGeometryAsync(session.Id);

        var current = session.Placement;

        // A frame from before the current one must not move the popover backwards.
        await session.OnGeometryChangedAsync(new BonboardingStepGeometry
        {
            Found = true,
            Target = new BonboardingRect(900, 900, 10, 10),
            Viewport = new BonboardingRect(0, 0, 1024, 768),
            Popover = new BonboardingSize(320, 200),
            TargetVisible = true,
            Sequence = 0,
        });

        Assert.AreEqual(current, session.Placement);
    }

    [TestMethod]
    public async Task A_Frame_That_Overtakes_The_Activation_Reply_Is_Kept()
    {
        await using var harness = new BonboardingHarness();
        harness.WithElements("#a");

        var moved = new BonboardingRect(400, 300, 80, 30);

        // The observers report a newer frame before the reply to the activation arrives.
        harness.Interop.DuringActivation = async activation =>
        {
            harness.Interop.TargetRect = moved;
            await harness.Interop.PushGeometryAsync(activation.SessionId);
        };

        var session = await harness.Service.StartAsync(Tour());

        Assert.AreEqual(moved, session.TargetRect);
    }

    [TestMethod]
    public void Only_Routine_Interop_Failures_Are_Logged_Quietly()
    {
        const Microsoft.Extensions.Logging.LogLevel Debug = Microsoft.Extensions.Logging.LogLevel.Debug;
        const Microsoft.Extensions.Logging.LogLevel Warning = Microsoft.Extensions.Logging.LogLevel.Warning;

        Assert.AreEqual(Debug, BonboardingInteropFailure.LevelFor(new JSDisconnectedException("the circuit is gone")));
        Assert.AreEqual(Debug, BonboardingInteropFailure.LevelFor(new OperationCanceledException()));
        Assert.AreEqual(Debug, BonboardingInteropFailure.LevelFor(new ObjectDisposedException("module")));
        Assert.AreEqual(Debug, BonboardingInteropFailure.LevelFor(new InvalidOperationException(
            "JavaScript interop calls cannot be issued at this time. This is because the component is being statically rendered.")));

        // Still survivable, but a real problem someone should see.
        Assert.AreEqual(Warning, BonboardingInteropFailure.LevelFor(new JSException("SyntaxError: not a valid selector")));
        Assert.AreEqual(Warning, BonboardingInteropFailure.LevelFor(new InvalidOperationException("the custom store is misconfigured")));
    }
}
