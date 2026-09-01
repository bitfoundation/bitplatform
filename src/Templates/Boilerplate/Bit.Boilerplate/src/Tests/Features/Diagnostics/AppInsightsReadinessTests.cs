//+:cnd:noEmit
using Microsoft.JSInterop;
using BlazorApplicationInsights.Models;
using Boilerplate.Client.Core.Infrastructure.Services;
using Microsoft.Extensions.Time.Testing;

namespace Boilerplate.Tests.Features.Diagnostics;

/// <summary>
/// <see cref="AppInsightsJsSdkService"/> waits for the Application Insights JS SDK to arrive from its CDN before it
/// forwards anything. That wait used to be a one-shot: a launch that started offline, or a CDN slower than the
/// timeout, faulted the gate permanently and every later Track call rethrew that first timeout - so telemetry was
/// dead for the rest of the app session even after the SDK loaded. Nothing surfaces that, which is exactly why it
/// gets a test.
/// <para>
/// The clock is a <see cref="FakeTimeProvider"/>, so these do not spend the real 15 second timeout.
/// </para>
/// </summary>
[TestClass]
public class AppInsightsReadinessTests
{
    [TestMethod]
    public async Task Telemetry_Should_Recover_AfterTheSdkArrivesLate()
    {
        var (sut, jsRuntime, timeProvider) = CreateSut();
        jsRuntime.FilesAreLoaded = false;

        var timedOut = AdvanceUntilCompleted(sut.TrackTrace(new TraceTelemetry { Message = "before" }), timeProvider);

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await timedOut);

        jsRuntime.FilesAreLoaded = true;

        // The point of the whole fix: the next call retries instead of replaying the first failure. Asserting that
        // the record actually reached the SDK matters - a gate that latches "ready" without ever loading the SDK
        // would let this call return quietly while dropping the telemetry.
        await sut.TrackTrace(new TraceTelemetry { Message = "after" });

        Assert.AreEqual(1, jsRuntime.ForwardedTelemetryCalls);
    }

    /// <summary>
    /// The telemetry initializer is added once at startup, fire and forget, before the SDK is guaranteed to be
    /// there. If that attempt is lost, every record for the rest of the session ships without the app's own
    /// context. It must be re-applied by whichever call finds the SDK loaded.
    /// </summary>
    [TestMethod]
    public async Task TelemetryInitializer_Should_BeApplied_WhenTheSdkArrivesAfterItWasAdded()
    {
        var (sut, jsRuntime, timeProvider) = CreateSut();
        jsRuntime.FilesAreLoaded = false;

        // Fire and forget, exactly as AppClientCoordinator calls it. It must not throw, even though it cannot work yet.
        await AdvanceUntilCompleted(sut.AddTelemetryInitializer(new TelemetryItem { Tags = [] }), timeProvider);

        Assert.AreEqual(0, jsRuntime.AddTelemetryInitializerCalls);

        jsRuntime.FilesAreLoaded = true;

        await sut.TrackTrace(new TraceTelemetry { Message = "after" });

        Assert.AreEqual(1, jsRuntime.AddTelemetryInitializerCalls, "The stored initializer must be applied once the SDK is available.");
    }

    /// <summary>
    /// The other ordering, and the one that is easy to lose: readiness is established by whichever call gets there
    /// first, which is routinely a Track call carrying no initializer - the app's own initializer is added fire and
    /// forget from AppClientCoordinator and can arrive second. A readiness gate that short-circuits on "already
    /// ready" drops it, and every later record then ships without the app's tags with nothing to show for it.
    /// </summary>
    [TestMethod]
    public async Task TelemetryInitializer_Should_BeApplied_WhenReadinessWasAlreadyEstablishedByATrackCall()
    {
        var (sut, jsRuntime, _) = CreateSut();

        await sut.TrackTrace(new TraceTelemetry { Message = "establishes readiness" });

        Assert.AreEqual(0, jsRuntime.AddTelemetryInitializerCalls);

        await sut.AddTelemetryInitializer(new TelemetryItem { Tags = [] });

        Assert.AreEqual(1, jsRuntime.AddTelemetryInitializerCalls, "An initializer added after readiness must still reach the SDK.");
    }

    /// <summary>
    /// Readiness is cached once it succeeds. Without that, every telemetry call would re-probe the SDK over JS
    /// interop - three round trips per record, per user, which on Blazor Server is three network hops each.
    /// </summary>
    [TestMethod]
    public async Task Readiness_Should_BeEstablishedOnce_WhenTheSdkIsAlreadyThere()
    {
        var (sut, jsRuntime, _) = CreateSut();

        await sut.AddTelemetryInitializer(new TelemetryItem { Tags = [] });
        await sut.TrackTrace(new TraceTelemetry { Message = "a" });
        await sut.TrackTrace(new TraceTelemetry { Message = "b" });

        Assert.AreEqual(1, jsRuntime.ReadinessProbes, "A successful readiness check must not be repeated per call.");
        Assert.AreEqual(1, jsRuntime.AddTelemetryInitializerCalls, "The initializer must reach the SDK exactly once.");
    }

    /// <summary>
    /// The service polls on the injected clock, so nothing here waits in real time. Stepping rather than jumping
    /// lets each poll's continuation run before the next tick.
    /// </summary>
    private static async Task AdvanceUntilCompleted(Task task, FakeTimeProvider timeProvider)
    {
        // Each poll schedules its next delay from a continuation, so the clock has to be stepped from another task
        // that lets those continuations run - jumping straight to the deadline would outrun them.
        var advancer = Task.Run(async () =>
        {
            while (task.IsCompleted is false)
            {
                timeProvider.Advance(TimeSpan.FromMilliseconds(250));
                await Task.Delay(1);
            }
        });

        await task.ContinueWith(_ => { }, TaskScheduler.Default) // Settled - the caller decides what to assert about how.
                  .WaitAsync(TimeSpan.FromSeconds(30));

        await advancer;
        await task;
    }

    private static (AppInsightsJsSdkService sut, FakeAppInsightsJsRuntime jsRuntime, FakeTimeProvider timeProvider) CreateSut()
    {
        var jsRuntime = new FakeAppInsightsJsRuntime();
        var timeProvider = new FakeTimeProvider();

        // Only UpdateCfg reads it, and these tests are about readiness, so the refusing default is fine.
        var consentService = new ConsentService(new PubSubService(new ServiceCollection().BuildServiceProvider()), new FakeStorageService());

        return (new AppInsightsJsSdkService(jsRuntime, timeProvider, consentService), jsRuntime, timeProvider);
    }

    /// <summary>An <see cref="IStorageService"/> that keeps the values in a dictionary, which is all a consent decision needs.</summary>
    private sealed class FakeStorageService : IStorageService
    {
        private readonly Dictionary<string, string?> items = [];

        public ValueTask SetItem(string key, string? value, bool persistent = true) { items[key] = value; return ValueTask.CompletedTask; }
        public ValueTask<string?> GetItem(string key) => ValueTask.FromResult(items.GetValueOrDefault(key));
        public ValueTask<bool> IsPersistent(string key) => ValueTask.FromResult(true);
        public ValueTask RemoveItem(string key) { items.Remove(key); return ValueTask.CompletedTask; }
        public ValueTask Clear() { items.Clear(); return ValueTask.CompletedTask; }
    }

    /// <summary>
    /// Stands in for the Application Insights JS snippet: <see cref="FilesAreLoaded"/> is what the service polls for.
    /// </summary>
    private sealed class FakeAppInsightsJsRuntime : IJSRuntime
    {
        public bool FilesAreLoaded { get; set; } = true;
        public int AddTelemetryInitializerCalls { get; private set; }
        public int ForwardedTelemetryCalls { get; private set; }

        /// <summary>
        /// How many times the service has probed for the SDK. One probe is three `hasOwnProperty` calls, so it is
        /// counted on the first of them - the `&amp;&amp;` chain in the service short-circuits, and only this one is
        /// guaranteed to run on every probe.
        /// </summary>
        public int ReadinessProbes { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier.EndsWith("hasOwnProperty", StringComparison.Ordinal))
            {
                if (identifier is "window.hasOwnProperty" && args?.FirstOrDefault() as string is "appInsights")
                {
                    ReadinessProbes++;
                }

                return ValueTask.FromResult((TValue)(object)FilesAreLoaded);
            }

            if (identifier.Contains("addTelemetryInitializer", StringComparison.OrdinalIgnoreCase))
            {
                AddTelemetryInitializerCalls++;
            }
            else
            {
                ForwardedTelemetryCalls++;
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);
    }
}
