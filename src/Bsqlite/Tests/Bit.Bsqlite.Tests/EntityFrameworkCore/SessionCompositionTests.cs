using Bit.Bsqlite.Data;
using Bit.Bsqlite.Interop;
using Bit.Bsqlite.Storage;
using Bit.Bsqlite.Storage.InMemory;
using Bit.Bsqlite.Testing;
using Microsoft.EntityFrameworkCore;

namespace Bit.Bsqlite.EntityFrameworkCore.Tests;

/// <summary>
/// The composition <c>UseBitBsqlite</c> is documented as: resolve and open through
/// <see cref="BsqliteSessionFactory"/>, then hand the session's connection to EF.
/// </summary>
[TestClass]
public sealed class SessionCompositionTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task SessionFactory_ThenUseBitBsqlite_MigratesAndQueries()
    {
        var provider = new BsqliteInMemoryStorageProvider();
        var store = new BsqliteInMemoryStorageBindingStore();
        var factory = new BsqliteSessionFactory(
            new BsqliteStorageProviderResolver([provider], store),
            new InProcessTransportFactory(),
            BsqliteStorageSelectionBuilder.Create(s => s.Prefer(BsqliteInMemoryStorageProvider.ProviderName)),
            store);

        await using var session = await factory.OpenAsync("app.db", Ct);
        await using var ctx = new ProductContext(new DbContextOptionsBuilder<ProductContext>()
            .UseBitBsqlite(session.Connection)
            .Options);

        await ctx.Database.MigrateAsync(Ct);

        ctx.Categories.Add(new Category { Name = "Tools" });
        ctx.Products.Add(new Product { Name = "Widget", Price = 4.50m, CategoryId = 1 });
        await ctx.SaveChangesAsync(Ct);
        ctx.ChangeTracker.Clear();

        var loaded = await ctx.Products.Include(p => p.Category).SingleAsync(Ct);
        Assert.AreEqual("Widget", loaded.Name);
        Assert.AreEqual("Tools", loaded.Category!.Name);
        Assert.AreEqual(BsqliteInMemoryStorageProvider.ProviderName, await store.GetProviderNameAsync("app.db", Ct));
    }

    private sealed class InProcessTransportFactory : IBsqliteTransportFactory
    {
        public IBsqliteTransport Create(IBsqliteStorageProvider provider)
            => new BsqliteInProcessTransport();
    }
}
