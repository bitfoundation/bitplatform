using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bit.Blex;
using Bit.Blex.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// Exercises Bit.Blex.Maui end-to-end without a device: a fake <see cref="IPreferences"/> stands in
/// for the platform store, and the startup initializer is driven the way MauiApp.Build() would.
/// </summary>
[TestClass]
public class MauiIntegrationTests
{
    private sealed class FakePreferences : IPreferences
    {
        public Dictionary<string, object?> Data { get; } = new();

        public bool ContainsKey(string key, string? sharedName = null) => Data.ContainsKey(key);

        public void Remove(string key, string? sharedName = null) => Data.Remove(key);

        public void Clear(string? sharedName = null) => Data.Clear();

        public void Set<T>(string key, T value, string? sharedName = null) => Data[key] = value;

        public T Get<T>(string key, T defaultValue, string? sharedName = null)
            => Data.TryGetValue(key, out var value) ? (T)value! : defaultValue;
    }

    private static ServiceProvider BuildProvider(FakePreferences preferences, bool history = false)
    {
        var services = new ServiceCollection();
        services.AddBitBlexServices();
        services.AddBitBlexStore<SettingsStore>();
        if (history)
            services.AddBitBlexHistory();
        services.AddScoped<IBlexStorage>(_ => new BlexPreferencesStorage(preferences));
        services.AddBitBlexPreferencesPersistence();
        return services.BuildServiceProvider();
    }

    private static void RunInitializers(ServiceProvider provider)
    {
        foreach (var initializer in provider.GetServices<IMauiInitializeService>())
            initializer.Initialize(provider);
    }

    [TestMethod]
    public void MauiRegistrations_AreValidUnderScopeValidation()
    {
        // A MAUI app has no scopes: everything resolves from the root provider, and the startup
        // initializer has to do exactly that. Registering Blex as scoped makes it illegal the
        // moment the container is built with ValidateScopes, so the MAUI helpers use singletons.
        var services = new ServiceCollection();
        services.AddBitBlexServices(lifetime: ServiceLifetime.Singleton);
        services.AddBitBlexStore<SettingsStore>(ServiceLifetime.Singleton);
        services.AddBitBlexHistory(lifetime: ServiceLifetime.Singleton);
        services.AddScoped(_ => new FakePreferences());
        services.AddSingleton<IBlexStorage>(new BlexPreferencesStorage(new FakePreferences()));
        services.AddBitBlexPreferencesPersistence(lifetime: ServiceLifetime.Singleton);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        RunInitializers(provider); // must not throw
        Assert.AreEqual("light", provider.GetRequiredService<SettingsStore>().Theme);

        // Singleton registration also means the host may tear the container down synchronously,
        // which ServiceProvider rejects for an IAsyncDisposable-only service.
        provider.Dispose();
    }

    [TestMethod]
    public void SingletonPersistor_SurvivesSynchronousContainerDisposal()
    {
        var preferences = new FakePreferences();
        var services = new ServiceCollection();
        services.AddBitBlexServices(lifetime: ServiceLifetime.Singleton);
        services.AddBitBlexStore<SettingsStore>(ServiceLifetime.Singleton);
        services.AddSingleton<IBlexStorage>(new BlexPreferencesStorage(preferences));
        services.AddBitBlexPreferencesPersistence(lifetime: ServiceLifetime.Singleton);

        var provider = services.BuildServiceProvider();
        RunInitializers(provider);
        provider.GetRequiredService<SettingsStore>().SetTheme("blue");

        provider.Dispose(); // must not throw...

        // ...and must have handed the pending state to storage on the way out.
        Assert.Contains("blue", (string)preferences.Data["blex:settings"]!);
    }

    [TestMethod]
    public void ScopedRegistrations_UnderScopeValidation_ReportRatherThanCrashStartup()
    {
        // The legacy (core, scoped) wiring is still rejected by ValidateScopes. Startup must
        // survive it with an actionable report instead of taking the app down.
        var errors = new List<BlexError>();
        var services = new ServiceCollection();
        services.AddBitBlexServices(options => options.OnError = errors.Add, ServiceLifetime.Singleton);
        services.AddBitBlexStore<SettingsStore>(); // scoped: the mismatch under test
        services.AddBitBlexMauiInitializer();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        RunInitializers(provider); // must not throw

        Assert.Contains(e => e.Source == "startup", errors);
    }

    [TestMethod]
    public async Task PreferencesStorage_RoundTrips()
    {
        var storage = new BlexPreferencesStorage(new FakePreferences());

        Assert.IsNull(await storage.GetAsync("k"));

        await storage.SetAsync("k", "v");
        Assert.AreEqual("v", await storage.GetAsync("k"));

        await storage.RemoveAsync("k");
        Assert.IsNull(await storage.GetAsync("k"));
    }

    [TestMethod]
    public async Task Initializer_RegistersStores_AndHydratesPersistedState()
    {
        var preferences = new FakePreferences();
        preferences.Data["blex:settings"] = "{\"Theme\":\"dark\",\"FontSize\":20}";

        await using var provider = BuildProvider(preferences);
        RunInitializers(provider);

        var settings = provider.GetRequiredService<SettingsStore>();
        Assert.AreEqual("dark", settings.Theme);
        Assert.AreEqual(20, settings.FontSize);
    }

    [TestMethod]
    public async Task Actions_AfterInitialization_ArePersistedToPreferences()
    {
        var preferences = new FakePreferences();
        await using var provider = BuildProvider(preferences);
        RunInitializers(provider);

        provider.GetRequiredService<SettingsStore>().SetTheme("blue");
        await provider.GetRequiredService<BlexStatePersistor>().FlushAsync();

        Assert.Contains("blue", (string)preferences.Data["blex:settings"]!);
    }

    [TestMethod]
    public async Task Initializer_StartsHistory_AfterHydration()
    {
        var preferences = new FakePreferences();
        preferences.Data["blex:settings"] = "{\"Theme\":\"dark\",\"FontSize\":12}";

        await using var provider = BuildProvider(preferences, history: true);
        RunInitializers(provider);

        var settings = provider.GetRequiredService<SettingsStore>();
        var historian = provider.GetRequiredService<BlexHistory>();
        Assert.IsFalse(historian.CanUndo);

        settings.SetTheme("light");
        Assert.IsTrue(historian.CanUndo);

        historian.Undo();
        // The undo baseline is the hydrated state, not the store's compiled-in default.
        Assert.AreEqual("dark", settings.Theme);
    }

    [TestMethod]
    public void Initializer_IsRegisteredOnce_AcrossRepeatedCalls()
    {
        var services = new ServiceCollection();
        services.AddBitBlexServices();
        services.AddBitBlexPreferencesPersistence();
        services.AddBitBlexPreferencesPersistence();
        services.AddBitBlexMauiInitializer();

        using var provider = services.BuildServiceProvider();
        Assert.ContainsSingle(provider.GetServices<IMauiInitializeService>());
    }

    [TestMethod]
    public async Task Initializer_UnreadableStorage_DoesNotThrow_AndReportsError()
    {
        var preferences = new FakePreferences();
        // A non-string value makes BlexPreferencesStorage's Get<string?> cast throw.
        preferences.Data["blex:settings"] = 42;

        var errors = new List<BlexError>();
        var services = new ServiceCollection();
        services.AddBitBlexServices(options => options.OnError = errors.Add);
        services.AddBitBlexStore<SettingsStore>();
        services.AddScoped<IBlexStorage>(_ => new BlexPreferencesStorage(preferences));
        services.AddBitBlexPreferencesPersistence();

        await using var provider = services.BuildServiceProvider();
        RunInitializers(provider); // must not throw: the app still starts with default state

        Assert.AreEqual("light", provider.GetRequiredService<SettingsStore>().Theme);
        Assert.Contains(e => e.Source == "persistence", errors);
    }
}
