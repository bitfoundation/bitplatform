namespace Bit.Blex.Tests;

/// <summary>
/// <see cref="BlexManager.Transaction(Action)"/>: actions of several stores as one unit -- all of them stand, or none.
/// </summary>
[TestClass]
public class TransactionTests
{
    private sealed class MemoryStorage : IBlexStorage
    {
        public Dictionary<string, string> Data { get; } = [];

        public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
            => new(Data.TryGetValue(key, out var value) ? value : null);

        public ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            Data[key] = value;
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            Data.Remove(key);
            return ValueTask.CompletedTask;
        }
    }

    private static (BlexManager Manager, CounterStore Counter, ProfileStore Profile, SettingsStore Settings) App()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        var profile = new ProfileStore();
        var settings = new SettingsStore();
        manager.Register(counter);
        manager.Register(profile);
        manager.Register(settings);
        return (manager, counter, profile, settings);
    }

    [TestMethod]
    public void A_throw_puts_back_every_store_the_body_changed_and_only_those()
    {
        var (manager, counter, profile, settings) = App();
        counter.Increment();
        var restored = new List<string>();
        manager.StoreRestored += store => restored.Add(store.Name);

        Assert.ThrowsExactly<InvalidOperationException>(() => manager.Transaction(() =>
        {
            counter.Increment();
            profile.Age = 30;
            throw new InvalidOperationException("payment declined");
        }));

        Assert.AreEqual(1, counter.Count);
        Assert.AreEqual(0, profile.Age);
        Assert.AreEqual("light", settings.Theme);
        CollectionAssert.AreEquivalent(new[] { "counter", "profile" }, restored, "the store the body left alone is not restored");
    }

    [TestMethod]
    public void A_body_that_completes_keeps_its_changes_and_returns_its_value()
    {
        var (manager, counter, profile, _) = App();
        var restored = 0;
        manager.StoreRestored += _ => restored++;

        var total = manager.Transaction(() =>
        {
            counter.Increment();
            profile.Age = 30;
            return counter.Count + profile.Age;
        });

        Assert.AreEqual(31, total);
        Assert.AreEqual((1, 30), (counter.Count, profile.Age));
        Assert.AreEqual(0, restored);
    }

    [TestMethod]
    public void Subscriptions_land_on_the_restored_values()
    {
        var (manager, counter, profile, _) = App();
        var seen = new List<(int, int)>();
        using var subscription = manager.Subscribe(() => (counter.Count, profile.Age), seen.Add);

        Assert.ThrowsExactly<InvalidOperationException>(() => manager.Transaction(() =>
        {
            counter.Increment();
            profile.Age = 30;
            throw new InvalidOperationException();
        }));

        Assert.AreEqual((0, 0), seen[^1]);
    }

    [TestMethod]
    public async Task Persistence_writes_the_restored_state_back()
    {
        var (manager, _, _, settings) = App();
        var storage = new MemoryStorage();
        await using var persistor = new BlexStatePersistor(manager, storage);
        await persistor.StartAsync();

        Assert.ThrowsExactly<InvalidOperationException>(() => manager.Transaction(() =>
        {
            settings.SetTheme("dark");   // recorded, and saved
            throw new InvalidOperationException();
        }));
        await persistor.FlushAsync();

        StringAssert.Contains(storage.Data["blex:settings"], "light", "a reload must not resurrect the half-done change");
    }

    [TestMethod]
    public void Undo_never_lands_on_what_a_failed_transaction_took_back()
    {
        var (manager, counter, profile, _) = App();
        var history = new BlexHistory(manager);
        history.Start();
        counter.Increment();   // a step before the transaction

        Assert.ThrowsExactly<InvalidOperationException>(() => manager.Transaction(() =>
        {
            counter.Increment();
            counter.Increment();
            throw new InvalidOperationException();
        }));
        Assert.AreEqual(1, counter.Count);

        while (history.CanUndo)
        {
            history.Undo();
            Assert.IsLessThanOrEqualTo(1, counter.Count, "no step holds a count the transaction took back");
        }

        Assert.AreEqual(0, counter.Count);
    }

    [TestMethod]
    public void An_inner_transaction_that_fails_is_taken_back_while_the_outer_one_goes_on()
    {
        var (manager, counter, profile, _) = App();

        manager.Transaction(() =>
        {
            counter.Increment();
            try
            {
                manager.Transaction(() =>
                {
                    profile.Age = 30;
                    throw new InvalidOperationException();
                });
            }
            catch (InvalidOperationException)
            {
            }

            counter.Increment();
        });

        Assert.AreEqual((2, 0), (counter.Count, profile.Age));
    }
}
