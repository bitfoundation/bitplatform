using Bit.Blex.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Bit.Blex.Tests;

/// <summary>A store that stamps its actions with the manager's clock, the way an app would.</summary>
[BlexStoreAttribute(Name = "stamps")]
public partial class StampStore
{
    [BlexStateAttribute] private DateTimeOffset _savedAt;

    [BlexActionAttribute]
    private void OnSave() => SavedAt = TimeProvider.GetUtcNow();
}

/// <summary>
/// Everything Blex times -- action timestamps, the debounce of generated effects, the persistence
/// debounce -- runs on <see cref="BlexManager.TimeProvider"/>, so a test advances a fake clock instead
/// of sleeping through real intervals.
/// </summary>
[TestClass]
public class TimeProviderTests
{
    private sealed class SignallingStorage : IBlexStorage
    {
        public Dictionary<string, string> Data { get; } = [];

        public TaskCompletionSource Written { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Data.TryGetValue(key, out var value) ? value : null);

        public ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            Data[key] = value;
            Written.TrySetResult();
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            Data.Remove(key);
            return ValueTask.CompletedTask;
        }
    }

    [TestMethod]
    public async Task A_debounced_effect_runs_when_the_clock_says_so()
    {
        var time = new FakeTimeProvider();
        using var harness = BlexTestHarness.For<EffectConcurrencyStore>();
        harness.TimeProvider = time;

        var first = harness.Store.TypeAhead("b");
        var second = harness.Store.TypeAhead("bl");

        // 250 ms of quiet is the store's gate; a millisecond short of it, nothing has run.
        time.Advance(TimeSpan.FromMilliseconds(249));
        Assert.AreEqual(0, harness.Store.DebouncedBodyRuns);

        time.Advance(TimeSpan.FromMilliseconds(1));
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual(1, harness.Store.DebouncedBodyRuns);
        Assert.AreEqual("bl", harness.Store.Last);
        CollectionAssert.AreEqual(new[] { "TypeAhead" }, harness.Log.Names.ToArray());
    }

    [TestMethod]
    public async Task A_debounced_save_is_written_when_the_clock_says_so()
    {
        var time = new FakeTimeProvider();
        var manager = new BlexManager { TimeProvider = time };
        var settings = new SettingsStore();
        manager.Register(settings);

        var storage = new SignallingStorage();
        var persistor = new BlexStatePersistor(manager, storage, new BlexPersistenceOptions { DebounceInterval = TimeSpan.FromMilliseconds(300) });
        await persistor.StartAsync();

        settings.SetTheme("dark");
        time.Advance(TimeSpan.FromMilliseconds(299));
        Assert.IsFalse(storage.Data.ContainsKey("blex:settings"));

        time.Advance(TimeSpan.FromMilliseconds(1));
        await storage.Written.Task.WaitAsync(TimeSpan.FromSeconds(5));

        StringAssert.Contains(storage.Data["blex:settings"], "dark");
    }

    [TestMethod]
    public async Task The_debounce_cap_is_measured_on_the_clock_too()
    {
        var time = new FakeTimeProvider();
        var manager = new BlexManager { TimeProvider = time };
        var settings = new SettingsStore();
        manager.Register(settings);

        var storage = new SignallingStorage();
        var persistor = new BlexStatePersistor(manager, storage, new BlexPersistenceOptions
        {
            DebounceInterval = TimeSpan.FromMilliseconds(300),
            DebounceMaxDelay = TimeSpan.FromMilliseconds(500),
        });
        await persistor.StartAsync();

        // A steady stream that never leaves 300 ms of quiet: only the cap gets a write out.
        settings.SetTheme("a");
        time.Advance(TimeSpan.FromMilliseconds(250));
        settings.SetTheme("b");
        time.Advance(TimeSpan.FromMilliseconds(250));
        Assert.IsFalse(storage.Data.ContainsKey("blex:settings"));

        settings.SetTheme("c");
        await storage.Written.Task.WaitAsync(TimeSpan.FromSeconds(5));

        StringAssert.Contains(storage.Data["blex:settings"], "\"c\"");
    }

    [TestMethod]
    public void Action_timestamps_come_from_the_clock()
    {
        var now = new DateTimeOffset(2030, 5, 17, 9, 30, 0, TimeSpan.Zero);
        var manager = new BlexManager { TimeProvider = new FakeTimeProvider(now) };
        var counter = new CounterStore();
        var failing = new FailingStore();
        manager.Register(counter);
        manager.Register(failing);

        var stamps = new List<DateTimeOffset>();
        using var sub = manager.Subscribe(ctx => stamps.Add(ctx.Timestamp));
        var failures = new List<DateTimeOffset>();
        manager.ActionFailed += ctx => failures.Add(ctx.Timestamp);

        counter.Increment();
        Assert.ThrowsExactly<InvalidOperationException>(failing.Boom);

        CollectionAssert.AreEqual(new[] { now }, stamps);
        CollectionAssert.AreEqual(new[] { now }, failures);
    }

    [TestMethod]
    public void A_store_reads_the_clock_of_the_manager_it_is_registered_with()
    {
        var now = new DateTimeOffset(2031, 1, 2, 3, 4, 5, TimeSpan.Zero);
        using var harness = BlexTestHarness.For<StampStore>();
        harness.TimeProvider = new FakeTimeProvider(now);

        harness.Store.Save();

        Assert.AreEqual(now, harness.Store.SavedAt);
    }

    [TestMethod]
    public void The_manager_takes_the_clock_from_the_options_then_the_container()
    {
        var registered = new FakeTimeProvider();
        var configured = new FakeTimeProvider();

        Assert.AreSame(TimeProvider.System, Resolve(services => services.AddBitBlexServices()));
        Assert.AreSame(registered, Resolve(services =>
        {
            services.AddSingleton<TimeProvider>(registered);
            services.AddBitBlexServices();
        }));
        Assert.AreSame(configured, Resolve(services =>
        {
            services.AddSingleton<TimeProvider>(registered);
            services.AddBitBlexServices(options => options.TimeProvider = configured);
        }));

        static TimeProvider Resolve(Action<IServiceCollection> configure)
        {
            var services = new ServiceCollection();
            configure(services);
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<BlexManager>().TimeProvider;
        }
    }

    [TestMethod]
    public void The_clock_cannot_be_unset()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new BlexManager().TimeProvider = null!);
}
