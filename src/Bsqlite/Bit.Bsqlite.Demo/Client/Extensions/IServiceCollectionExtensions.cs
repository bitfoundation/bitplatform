using Bit.Bsqlite.Demo.Client;
using Bit.Bsqlite.Demo.Data;
using Bit.Bsqlite.Storage.CacheStorage;
using Bit.Bsqlite.Storage.InMemory;
using Bit.Bsqlite.Storage.IndexedDb;
using Bit.Bsqlite.Storage.Opfs;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The services shared by the WebAssembly client and the prerendering host. Both containers have to
/// register the same set: every page is instantiated once on the server for the prerender pass and again in
/// the browser, so anything a page injects must resolve in both places or prerendering fails with a
/// missing-service exception.
/// </summary>
/// <remarks>
/// Resolving them on the server is harmless: nothing here touches the browser until a page asks it to open
/// the database, and every page does that only once it runs interactively (see Components/SessionLifecycle.razor).
/// </remarks>
public static class IServiceCollectionExtensions
{
    /// <param name="baseAddress">
    /// The app's own origin, for the <see cref="HttpClient"/> a page can call the host with. The prerendering
    /// host passes nothing: a page never issues a request while it is being prerendered.
    /// </param>
    public static IServiceCollection AddDemoServices(this IServiceCollection services, string? baseAddress = null)
    {
        services.AddScoped(_ => baseAddress is null
            ? new HttpClient()
            : new HttpClient { BaseAddress = new Uri(baseAddress) });

        // One call registers selection, sticky bindings in localStorage, the worker transport, and the
        // shared database. Existing data outranks preference: a file created on IndexedDB stays there even
        // once OPFS becomes available, unless the application migrates it deliberately.
        services.AddBitBsqliteServices(sqlite => sqlite
            .AddStorageProvider<BsqliteOpfsStorageProvider>()
            .AddStorageProvider<BsqliteIndexedDbStorageProvider>()
            .AddStorageProvider<BsqliteCacheStorageProvider>()
            .AddStorageProvider<BsqliteInMemoryStorageProvider>()
            .UseStorage(s => s
                .Prefer(BsqliteOpfsStorageProvider.ProviderName)
                .Fallback(BsqliteIndexedDbStorageProvider.ProviderName)
                .Fallback(BsqliteCacheStorageProvider.ProviderName)
                .Fallback(BsqliteInMemoryStorageProvider.ProviderName)
                .AllowNonPersistentFallback())
            .AddDatabase(BrowserDatabase.DatabaseName));

        // Contexts come from a factory bound to that database. Migrations run once per session, then the
        // demo catalog is seeded if the database is empty.
        services.AddBitBsqliteDbContext<AppDbContext>(options =>
            options.OnFirstUse = (db, ct) => DemoData.SeedIfEmptyAsync(db, ct));

        // Scoped like the library's database it wraps: one for the app in the browser, one per request on the
        // prerendering host.
        services.AddScoped<BrowserDatabase>();

        return services;
    }
}
