using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

[TestClass]
public class StateStoreTests
{
    public sealed class CounterState : IBevStateProvider
    {
        public int Count { get; private set; }

        public List<string> Log { get; } = [];

        public string Password { get; set; } = "secret";

        public string Name => "CounterState";

        public event Action<BevStateChangeInfo>? Changed;

        public object? GetSnapshot() => this;

        public void Increment()
        {
            Count++;
            Log.Add("inc");
            Changed?.Invoke(new BevStateChangeInfo("Increment", Count.ToString()));
        }
    }

    private sealed class ConcurrentState : IBevStateProvider
    {
        private int _count;

        public string Name => "ConcurrentState";

        public event Action<BevStateChangeInfo>? Changed;

        public object GetSnapshot() => new { Count = Volatile.Read(ref _count) };

        public void Increment()
        {
            var count = Interlocked.Increment(ref _count);
            Changed?.Invoke(new BevStateChangeInfo("Increment", count.ToString()));
        }
    }

    private sealed class ObservableState
    {
        private Action? _changed;

        public int SubscriberCount { get; private set; }

        public int Count { get; private set; }

        public event Action Changed
        {
            add
            {
                _changed += value;
                SubscriberCount++;
            }
            remove
            {
                _changed -= value;
                SubscriberCount--;
            }
        }

        public IDisposable Subscribe(Action callback)
        {
            Changed += callback;
            return new CallbackDisposable(() =>
            {
                Changed -= callback;
            });
        }

        public void Increment()
        {
            Count++;
            _changed?.Invoke();
        }
    }

    private sealed class CallbackDisposable(Action callback) : IDisposable
    {
        private Action? _callback = callback;

        public void Dispose() => Interlocked.Exchange(ref _callback, null)?.Invoke();
    }

    [TestMethod]
    public void Changes_produce_diffs_history_and_timeline_events()
    {
        var (session, scope) = TestHelpers.CreateSession(services: s => s.AddBevToolsStateProvider<CounterState>());
        using (scope)
        {
            var state = scope.ServiceProvider.GetRequiredService<CounterState>();
            var entry = Assert.ContainsSingle(session.State.Entries);
            Assert.AreEqual("DI", entry.Origin);

            state.Increment();
            state.Increment();

            Assert.AreEqual(2, entry.ChangeCount);
            var changes = entry.Changes;
            Assert.AreEqual(2, changes.Length);
            var last = changes[1];
            Assert.AreEqual("Increment", last.Action);
            Assert.Contains(c => c.Path == "Count" && c.Before == "1" && c.After == "2", last.Changes);
            Assert.Contains(c => c.Path == "Log[1]" && c.Kind == "added", last.Changes);
            Assert.AreEqual(BevRedactor.RedactedValue, last.Snapshot["Password"]);

            var evt = session.Timeline.Find(last.TimelineEventId)!;
            Assert.AreEqual(BevToolsEventKind.StateChange, evt.Kind);
            Assert.AreEqual("CounterState: Increment", evt.Title);
            Assert.Contains("Count: 1 → 2", evt.Detail!);

            session.ClearAll();
            Assert.AreEqual(0, entry.ChangeCount);
            Assert.IsEmpty(entry.Changes);
        }
    }

    [TestMethod]
    public void Runtime_registration_can_be_undone()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var state = new CounterState();
            var registration = session.StateProviders.Register(state);
            Assert.ContainsSingle(session.StateProviders.Providers);
            registration.Dispose();
            Assert.IsEmpty(session.StateProviders.Providers);
            state.Increment();
            Assert.DoesNotContain(e => e.Kind == BevToolsEventKind.StateChange, session.Timeline.Snapshot());
        }
    }

    [TestMethod]
    public void Adapter_registration_exposes_existing_services()
    {
        var (session, scope) = TestHelpers.CreateSession(services: s =>
        {
            s.AddScoped<CounterState>();
            s.AddBevToolsStateProvider<CounterState>("Adapted", c => new { c.Count }, (c, notify) => c.Changed += _ => notify());
        });
        using (scope)
        {
            var entry = Assert.ContainsSingle(session.State.Entries);
            Assert.AreEqual("Adapted", entry.Name);
            scope.ServiceProvider.GetRequiredService<CounterState>().Increment();
            Assert.AreEqual(1, entry.ChangeCount);
            Assert.Contains(c => c.Path == "Count" && c.After == "1", entry.Changes[0].Changes);
        }
    }

    [TestMethod]
    public void Disposable_adapter_subscription_is_released_with_the_scope()
    {
        var observed = new ObservableState();
        var (session, scope) = TestHelpers.CreateSession(services: services =>
        {
            services.AddSingleton(observed);
            services.AddBevToolsStateProviderWithSubscription<ObservableState>(
                "Observed",
                state => new { state.Count },
                (state, notify) => state.Subscribe(notify));
        });

        Assert.ContainsSingle(session.State.Entries);
        Assert.AreEqual(1, observed.SubscriberCount);
        observed.Increment();
        Assert.AreEqual(1, session.State.Entries[0].ChangeCount);

        scope.Dispose();
        Assert.AreEqual(0, observed.SubscriberCount);
    }

    [TestMethod]
    public void Action_adapter_for_a_singleton_subscribes_only_once_across_scopes()
    {
        var observed = new ObservableState();
        var services = new ServiceCollection();
        services.AddSingleton(observed);
        services.AddBevToolsStateProvider<ObservableState>("Observed", state => new { state.Count }, (state, notify) => state.Changed += notify);
        services.AddBitBevToolsServices(options => options.Enabled = true);
        using var provider = services.BuildServiceProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        firstScope.ServiceProvider.GetRequiredService<BevToolsSession>().EnsureActivated();
        secondScope.ServiceProvider.GetRequiredService<BevToolsSession>().EnsureActivated();

        Assert.AreEqual(1, observed.SubscriberCount);
    }

    [TestMethod]
    public void Concurrent_provider_notifications_do_not_lose_change_counts()
    {
        var state = new ConcurrentState();
        var (session, scope) = TestHelpers.CreateSession(services: services => services.AddSingleton<IBevStateProvider>(state));
        using (scope)
        {
            Parallel.For(0, 100, _ => state.Increment());

            var entry = Assert.ContainsSingle(session.State.Entries);
            Assert.AreEqual(100, entry.ChangeCount);
        }
    }
}
