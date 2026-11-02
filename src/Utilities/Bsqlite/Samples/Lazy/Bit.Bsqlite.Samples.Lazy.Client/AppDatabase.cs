using Bit.Bsqlite.EntityFrameworkCore;
using Bit.Bsqlite.Samples.Lazy.Data;
using Bit.Bsqlite.Storage.IndexedDb;
using Bit.Bsqlite.Storage.Opfs;

namespace Bit.Bsqlite.Samples.Lazy.Client;

// Internal, and called only from the pages the assemblies are loaded for - and from the server, which loads them with the
// app.
internal static class AppDatabase
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
