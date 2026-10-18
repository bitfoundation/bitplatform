using System.Diagnostics.CodeAnalysis;
using Bit.Bsqlite;
using Bit.Bsqlite.Interop;
using Bit.Bsqlite.Storage;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Collects the choices <c>AddBitBsqliteServices</c> turns into registrations.</summary>
public sealed class BsqliteBuilder
{
    private readonly List<string> _onOpenSql = [];
    private readonly List<BsqliteJsModule> _extensions = [];
    private BsqliteStorageSelection? _selection;
    private string? _hostModuleUrl;
    private string? _workerUrl;
    private bool _enableForeignKeys = true;
    private bool _bindingStoreRegistered;

    internal BsqliteBuilder(IServiceCollection services) => Services = services;

    /// <summary>The service collection being configured.</summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Registers a storage provider, scoped like the rest of the stack. Its constructor may take
    /// <see cref="IJSRuntime"/>.
    /// </summary>
    public BsqliteBuilder AddStorageProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>()
        where TProvider : class, IBsqliteStorageProvider
    {
        Services.AddScoped<IBsqliteStorageProvider, TProvider>();
        return this;
    }

    /// <summary>Registers a storage provider built by <paramref name="factory"/>, once per scope.</summary>
    public BsqliteBuilder AddStorageProvider(Func<IServiceProvider, IBsqliteStorageProvider> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        Services.AddScoped(factory);
        return this;
    }

    /// <summary>
    /// Registers an already-built storage provider. Being one instance, it is shared by every scope, so it
    /// must not hold on to a scoped service such as a server circuit's <see cref="IJSRuntime"/>.
    /// </summary>
    public BsqliteBuilder AddStorageProvider(IBsqliteStorageProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        Services.AddSingleton(provider);
        return this;
    }

    /// <summary>
    /// Sets the preference order among the registered providers. Required: selection never guesses.
    /// </summary>
    public BsqliteBuilder UseStorage(Action<BsqliteStorageSelectionBuilder> configure)
    {
        _selection = BsqliteStorageSelectionBuilder.Create(configure);
        return this;
    }

    /// <summary>Uses an already-built selection.</summary>
    public BsqliteBuilder UseStorage(BsqliteStorageSelection selection)
    {
        _selection = selection ?? throw new ArgumentNullException(nameof(selection));
        return this;
    }

    /// <summary>
    /// Replaces the sticky-binding store. The default remembers bindings in <c>localStorage</c>,
    /// which is what production wants; tests use <see cref="BsqliteInMemoryStorageBindingStore"/>.
    /// </summary>
    public BsqliteBuilder UseBindingStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStore>()
        where TStore : class, IBsqliteStorageBindingStore
    {
        Services.AddScoped<IBsqliteStorageBindingStore, TStore>();
        _bindingStoreRegistered = true;
        return this;
    }

    /// <summary>
    /// Replaces the sticky-binding store with <paramref name="store"/>, one instance shared by every scope.
    /// </summary>
    public BsqliteBuilder UseBindingStore(IBsqliteStorageBindingStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        Services.AddSingleton(store);
        _bindingStoreRegistered = true;
        return this;
    }

    /// <summary>
    /// Registers a <see cref="BsqliteDatabase"/> for <paramref name="databaseName"/>. Call once per
    /// database the application opens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each database is a keyed service under its name, so with several registered, inject the one
    /// wanted with <c>[FromKeyedServices("cache.db")]</c> and bind a context to it with
    /// <see cref="Bit.Bsqlite.EntityFrameworkCore.BsqliteDbContextOptions{TContext}.DatabaseName"/>. An unkeyed
    /// <see cref="BsqliteDatabase"/> resolves to the one registered last, which is the application's
    /// one shared database when there is only one.
    /// </para>
    /// <para>
    /// Scoped: one session for the whole of a WebAssembly application, and one per circuit on a server.
    /// </para>
    /// </remarks>
    public BsqliteBuilder AddDatabase(string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        if (Services.Any(service => service.ServiceType == typeof(BsqliteDatabaseRegistration)
            && service.ImplementationInstance is BsqliteDatabaseRegistration { DatabaseName: var registered }
            && string.Equals(registered, databaseName, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"The database '{databaseName}' is already registered. Each database is one shared "
                + "session, so it is added once.");
        }

        Services.AddSingleton(new BsqliteDatabaseRegistration(databaseName));
        Services.AddKeyedScoped(databaseName, (sp, _) => new BsqliteDatabase(
            sp.GetRequiredService<IBsqliteSessionFactory>(),
            databaseName));
        Services.AddScoped(sp => sp.GetRequiredKeyedService<BsqliteDatabase>(databaseName));
        return this;
    }

    /// <summary>
    /// Whether every connection runs <c>PRAGMA foreign_keys = ON</c> on open. On by default, as
    /// with Microsoft.Data.Sqlite.
    /// </summary>
    public BsqliteBuilder EnableForeignKeys(bool enable = true)
    {
        _enableForeignKeys = enable;
        return this;
    }

    /// <summary>Adds a statement to run on every open, after the provider's own configuration.</summary>
    public BsqliteBuilder OnOpen(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        _onOpenSql.Add(sql);
        return this;
    }

    /// <summary>
    /// Adds a JavaScript module that registers user-defined functions, collations, or virtual
    /// tables on every open database. See <see cref="BsqliteWorkerTransportOptions.ExtensionModules"/>.
    /// </summary>
    public BsqliteBuilder AddExtensionModule(string moduleUrl, string registerExport = "register")
    {
        _extensions.Add(new BsqliteJsModule(moduleUrl, registerExport));
        return this;
    }

    /// <summary>Overrides where the worker host module and worker script are served from.</summary>
    public BsqliteBuilder UseWorker(string? hostModuleUrl = null, string? workerUrl = null)
    {
        _hostModuleUrl = hostModuleUrl;
        _workerUrl = workerUrl;
        return this;
    }

    internal void Complete()
    {
        if (_selection is null)
        {
            throw new InvalidOperationException(
                "AddBitBsqliteServices needs a storage preference: call UseStorage(s => s.Prefer(\"opfs\")...).");
        }

        if (!_bindingStoreRegistered)
        {
            Services.TryAddScoped<IBsqliteStorageBindingStore>(sp =>
                new BsqliteLocalStorageBindingStore(sp.GetRequiredService<IJSRuntime>()));
        }

        var selection = _selection;
        var defaults = new BsqliteWorkerTransportDefaults
        {
            HostModuleUrl = _hostModuleUrl ?? BsqliteWorkerTransport.VersionedHostModuleUrl,
            WorkerUrl = _workerUrl,
            EnableForeignKeys = _enableForeignKeys,
            OnOpenSql = [.. _onOpenSql],
            ExtensionModules = [.. _extensions],
        };

        Services.TryAddSingleton(selection);
        Services.TryAddSingleton(defaults);
        Services.TryAddScoped<IBsqliteTransportFactory>(sp =>
            new BsqliteWorkerTransportFactory(sp.GetRequiredService<IJSRuntime>(), defaults));
        Services.TryAddSingleton<BsqliteStorageMigrator>();
        Services.TryAddScoped(sp => new BsqliteStorageProviderResolver(
            sp.GetServices<IBsqliteStorageProvider>(),
            sp.GetRequiredService<IBsqliteStorageBindingStore>(),
            sp.GetService<ILogger<BsqliteStorageProviderResolver>>()));
        Services.TryAddScoped<IBsqliteSessionFactory>(sp => new BsqliteSessionFactory(
            sp.GetRequiredService<BsqliteStorageProviderResolver>(),
            sp.GetRequiredService<IBsqliteTransportFactory>(),
            sp.GetRequiredService<BsqliteStorageSelection>(),
            sp.GetRequiredService<IBsqliteStorageBindingStore>(),
            sp.GetRequiredService<BsqliteStorageMigrator>(),
            sp.GetService<ILogger<BsqliteSessionFactory>>()));
        Services.TryAddScoped(sp => (BsqliteSessionFactory)sp.GetRequiredService<IBsqliteSessionFactory>());
    }
}

/// <summary>
/// One per <see cref="BsqliteBuilder.AddDatabase"/> call, so the registered names can be listed
/// without opening - or even constructing - any of the databases.
/// </summary>
internal sealed record BsqliteDatabaseRegistration(string DatabaseName);
