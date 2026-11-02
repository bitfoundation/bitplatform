using Bit.Bsqlite.EntityFrameworkCore;
using Bit.Bsqlite.Samples.Lazy.Data;
using Bit.Bsqlite.Storage.IndexedDb;
using Bit.Bsqlite.Storage.Opfs;

namespace Bit.Bsqlite.Samples.Lazy.Client;

// Called only from the pages the assemblies are loaded for - and from the server, which loads them with the app and so
// registers with Register too. Public for that: the router reads the app's public types as it starts, and a static class
// that names Bsqlite's types only in its methods loads before Bsqlite has.
public static class AppDatabase
{
    // What Program.cs would have registered, unchanged - and what the server registers.
    public static void Register(IServiceCollection services)
    {
        services.AddBitBsqliteServices(sqlite => sqlite
            .AddStorageProvider<BsqliteOpfsStorageProvider>()
            .AddStorageProvider<BsqliteIndexedDbStorageProvider>()
            .UseStorage(s => s
                .Prefer(BsqliteOpfsStorageProvider.ProviderName)
                .Fallback(BsqliteIndexedDbStorageProvider.ProviderName))
            .AddDatabase("app.db"));

        services.AddBitBsqliteDbContext<AppDbContext>();
    }

    public static IBsqliteDbContextFactory<AppDbContext> Contexts(IServiceProvider provider) => provider
        .GetBitBsqliteServices(Register)
        .GetRequiredService<IBsqliteDbContextFactory<AppDbContext>>();
}
