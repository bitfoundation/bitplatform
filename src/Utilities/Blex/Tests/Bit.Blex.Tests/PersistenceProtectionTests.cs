using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Blex.Tests;

/// <summary>A second persistent store, to show one store's protector failure does not hold the others back.</summary>
[BlexStoreAttribute(Name = "drafts", Persist = true)]
public partial class DraftsStore
{
    [BlexStateAttribute] private string _text = "";

    [BlexActionAttribute] private void OnWrite(string text) => Text = text;
}

/// <summary>
/// <see cref="BlexPersistenceOptions.Protector"/>: what reaches storage is protected, what comes back is checked, and a
/// failure never costs the user their data or writes it in the clear.
/// </summary>
[TestClass]
public class PersistenceProtectionTests
{
    /// <summary>A stand-in for real encryption: reversible, recognizably not plain JSON, and asynchronous like Web Crypto.</summary>
    private sealed class SealingProtector : IBlexStateProtector
    {
        private const string Seal = "sealed:";

        public bool Unavailable { get; set; }

        /// <summary>One store whose key cannot be reached right now.</summary>
        public string? UnavailableFor { get; set; }

        /// <summary>Times out (its own timeout, not the caller cancelling) instead of failing outright.</summary>
        public bool TimesOut { get; set; }

        public bool FailToProtect { get; set; }

        public async ValueTask<string> ProtectAsync(string storeName, string payload, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            if (FailToProtect)
                throw new InvalidOperationException("key service down");

            return Seal + Convert.ToBase64String(Encoding.UTF8.GetBytes(storeName + "|" + payload));
        }

        public async ValueTask<string?> UnprotectAsync(string storeName, string protectedPayload, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            if (TimesOut)
                throw new TaskCanceledException("key service timed out");

            if (Unavailable || storeName == UnavailableFor)
                throw new InvalidOperationException("key service down");

            if (!protectedPayload.StartsWith(Seal, StringComparison.Ordinal))
                return null;

            var text = Encoding.UTF8.GetString(Convert.FromBase64String(protectedPayload[Seal.Length..]));
            return text.StartsWith(storeName + "|", StringComparison.Ordinal) ? text[(storeName.Length + 1)..] : null;
        }
    }

    private sealed class Storage : IBlexStorage, IBlexExternalStorageChange
    {
        public Dictionary<string, string> Data { get; } = [];

        public event Action<string?>? ExternalChange;

        public ValueTask StartWatchingAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public void WriteFromAnotherTab(string key, string value)
        {
            Data[key] = value;
            ExternalChange?.Invoke(key);
        }

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

    private static (SettingsStore Store, BlexManager Manager, List<BlexError> Errors) App()
    {
        var errors = new List<BlexError>();
        var manager = new BlexManager { OnError = errors.Add };
        var store = new SettingsStore();
        manager.Register(store);
        return (store, manager, errors);
    }

    [TestMethod]
    public async Task What_is_stored_is_protected_and_comes_back_through_the_protector()
    {
        var storage = new Storage();
        var protector = new SealingProtector();

        var (store, manager, _) = App();
        await using (var persistor = new BlexStatePersistor(manager, storage, new() { Protector = protector }))
        {
            await persistor.StartAsync();
            store.SetTheme("dark");
            await persistor.FlushAsync();
        }

        StringAssert.StartsWith(storage.Data["blex:settings"], "sealed:");
        Assert.DoesNotContain("dark", storage.Data["blex:settings"], "nothing readable in storage");

        var (reloaded, nextManager, errors) = App();
        await using var next = new BlexStatePersistor(nextManager, storage, new() { Protector = protector });
        await next.StartAsync();

        Assert.AreEqual("dark", reloaded.Theme);
        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public async Task A_stored_value_the_protector_rejects_is_discarded_and_removed()
    {
        var storage = new Storage();
        storage.Data["blex:settings"] = """{"Theme":"dark"}""";   // in the clear: saved before protection, or forged

        var (store, manager, errors) = App();
        await using var persistor = new BlexStatePersistor(manager, storage, new() { Protector = new SealingProtector() });
        await persistor.StartAsync();

        Assert.AreEqual("light", store.Theme, "a value the protector cannot vouch for is never applied");
        Assert.IsFalse(storage.Data.ContainsKey("blex:settings"));
        Assert.HasCount(1, errors);
    }

    [TestMethod]
    public async Task A_protector_that_cannot_work_right_now_costs_no_data_and_does_not_stop_the_app()
    {
        var storage = new Storage();
        var protector = new SealingProtector();
        storage.Data["blex:settings"] = await protector.ProtectAsync("settings", """{"Theme":"dark"}""");
        storage.Data["blex:drafts"] = await protector.ProtectAsync("drafts", """{"Text":"hello"}""");
        protector.UnavailableFor = "settings";   // this key cannot be reached right now

        var (store, manager, errors) = App();
        var drafts = new DraftsStore();
        manager.Register(drafts);
        await using var persistor = new BlexStatePersistor(manager, storage, new() { Protector = protector });
        await persistor.StartAsync();

        Assert.AreEqual("hello", drafts.Text, "the other stores hydrate");
        Assert.AreEqual("light", store.Theme);
        Assert.HasCount(1, errors);

        var kept = storage.Data["blex:settings"];
        store.SetTheme("dim");
        await persistor.FlushAsync();
        Assert.AreEqual(kept, storage.Data["blex:settings"], "nothing overwrites the value that could not be read");

        protector.UnavailableFor = null;
        await persistor.RehydrateAsync();
        Assert.AreEqual("dark", store.Theme);

        store.SetTheme("dim");
        await persistor.FlushAsync();
        Assert.AreNotEqual(kept, storage.Data["blex:settings"], "saving resumes once it was read");
    }

    [TestMethod]
    public async Task A_protector_that_times_out_is_a_passing_failure_not_a_cancellation()
    {
        var storage = new Storage();
        var protector = new SealingProtector();
        storage.Data["blex:settings"] = await protector.ProtectAsync("settings", """{"Theme":"dark"}""");
        protector.TimesOut = true;   // its own timeout: nobody cancelled the hydration

        var (store, manager, errors) = App();
        await using var persistor = new BlexStatePersistor(manager, storage, new() { Protector = protector });
        await persistor.StartAsync();

        Assert.HasCount(1, errors, "reported, and the app starts");
        Assert.IsTrue(storage.Data.ContainsKey("blex:settings"));

        protector.TimesOut = false;
        await persistor.RehydrateAsync();
        Assert.AreEqual("dark", store.Theme);
    }

    [TestMethod]
    public async Task A_payload_that_fails_to_protect_is_never_written_in_the_clear()
    {
        var storage = new Storage();
        var protector = new SealingProtector();
        var (store, manager, errors) = App();
        await using var persistor = new BlexStatePersistor(manager, storage, new() { Protector = protector });
        await persistor.StartAsync();

        protector.FailToProtect = true;
        store.SetTheme("dark");
        await persistor.FlushAsync();

        Assert.IsFalse(storage.Data.ContainsKey("blex:settings"));
        Assert.HasCount(1, errors);

        protector.FailToProtect = false;
        store.SetTheme("dim");
        await persistor.FlushAsync();
        StringAssert.StartsWith(storage.Data["blex:settings"], "sealed:");
    }

    [TestMethod]
    public async Task A_protector_registered_in_DI_protects_what_is_stored()
    {
        var storage = new Storage();
        var services = new ServiceCollection();
        services.AddBitBlexServices(lifetime: ServiceLifetime.Singleton);
        services.AddBitBlexStore<SettingsStore>(ServiceLifetime.Singleton);
        services.AddSingleton<IBlexStorage>(storage);
        services.AddSingleton<IBlexStateProtector, SealingProtector>();   // may depend on other services
        services.AddBitBlexPersistence(lifetime: ServiceLifetime.Singleton);
        await using var provider = services.BuildServiceProvider();

        var persistor = provider.GetRequiredService<BlexStatePersistor>();
        await persistor.StartAsync();
        provider.GetRequiredService<SettingsStore>().SetTheme("dark");
        await persistor.FlushAsync();

        StringAssert.StartsWith(storage.Data["blex:settings"], "sealed:");
    }

    [TestMethod]
    public async Task Another_tabs_write_is_read_through_the_protector_too()
    {
        var storage = new Storage();
        var protector = new SealingProtector();
        var (store, manager, errors) = App();
        await using var persistor = new BlexStatePersistor(manager, storage, new() { Protector = protector, SyncAcrossTabs = true });
        await persistor.StartAsync();

        storage.WriteFromAnotherTab("blex:settings", await protector.ProtectAsync("settings", """{"Theme":"dark"}"""));
        await persistor.FlushAsync();
        Assert.AreEqual("dark", store.Theme);

        storage.WriteFromAnotherTab("blex:settings", """{"Theme":"forged"}""");
        await persistor.FlushAsync();
        Assert.AreEqual("dark", store.Theme, "rejected");
        Assert.IsTrue(storage.Data.ContainsKey("blex:settings"), "the other tab's value is left for it to judge");
        Assert.HasCount(1, errors);
    }
}
