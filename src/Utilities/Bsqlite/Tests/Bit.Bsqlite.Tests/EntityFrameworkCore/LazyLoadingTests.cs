using Bit.Bsqlite.Data;
using Bit.Bsqlite.Interop;
using Bit.Bsqlite.Storage;
using Bit.Bsqlite.Storage.InMemory;
using Bit.Bsqlite.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Bsqlite.EntityFrameworkCore.Tests;

/// <summary>
/// <c>GetBitBsqliteServices</c>, which is what a WebAssembly app that lazy loads Bit.Bsqlite has instead of
/// <c>Program.cs</c>: the assembly is not there when the app's container is built, so the same registration is made
/// once it is, in a container of Bsqlite's own in front of the app's. Composed on desktop .NET with the in-process
/// transport; the app's container holds only what a Blazor host registers by itself, plus services of the app's own.
/// </summary>
[TestClass]
public sealed class LazyLoadingTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    /// <summary>The registration <c>Program.cs</c> would have made, moved where the pages can make it.</summary>
    private static void Configure(IServiceCollection services, Action<BsqliteBuilder>? configureSqlite = null)
    {
        services.AddBitBsqliteServices(sqlite =>
        {
            sqlite
                .AddStorageProvider<BsqliteInMemoryStorageProvider>()
                .UseStorage(s => s.Prefer(BsqliteInMemoryStorageProvider.ProviderName))
                .UseBindingStore<BsqliteInMemoryStorageBindingStore>()
                .AddDatabase("lazy.db");
            configureSqlite?.Invoke(sqlite);
        });

        // Desktop has no worker; the in-process engine stands in for it.
        services.AddSingleton<IBsqliteTransportFactory>(new InProcessTransportFactory());

        services.AddBitBsqliteDbContext<ProductContext>(options =>
        {
            options.Schema = BsqliteSchemaStrategy.EnsureCreated;
            options.Configure = (_, builder) => builder.ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });
    }

    /// <summary>An app's container without Bsqlite: the JavaScript runtime every Blazor host registers, and the app's own.</summary>
    private static ServiceProvider BuildApp(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(new UnusedJsRuntime());
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task Unregistered_TheServicesAreBuiltFromConfigure_OncePerConnection()
    {
        await using var app = BuildApp();
        var configured = 0;

        IServiceProvider Services() => app.GetBitBsqliteServices(services =>
        {
            configured++;
            Configure(services);
        });

        var factory = Services().GetRequiredService<IBsqliteDbContextFactory<ProductContext>>();
        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            db.Categories.Add(new Category { Name = "Tools" });
            await db.SaveChangesAsync(Ct);
        }

        // A second page asking gets the same services, and so the database the first one opened.
        var again = Services();
        Assert.AreSame(factory, again.GetRequiredService<IBsqliteDbContextFactory<ProductContext>>());
        Assert.AreSame(factory.Database, again.GetRequiredService<BsqliteDatabase>());
        Assert.AreSame(factory.Database, again.GetRequiredKeyedService<BsqliteDatabase>("lazy.db"));
        Assert.IsTrue(again.GetRequiredService<BsqliteDatabase>().IsOpen);
        Assert.AreEqual(1, configured);

        await using (var db = await again.GetRequiredService<IBsqliteDbContextFactory<ProductContext>>().CreateDbContextAsync(Ct))
        {
            Assert.AreEqual(1, await db.Categories.CountAsync(Ct));
        }

        // Nothing was added to the app's container.
        Assert.IsNull(app.GetService<BsqliteDatabase>());
    }

    /// <summary>
    /// A server prerendering the pages has a JavaScript runtime per circuit or request, and a user's services are
    /// never shared with another's.
    /// </summary>
    [TestMethod]
    public async Task EachConnection_GetsServicesOfItsOwn()
    {
        var services = new ServiceCollection();
        services.AddScoped<IJSRuntime, UnusedJsRuntime>();
        await using var server = services.BuildServiceProvider();
        await using var first = server.CreateAsyncScope();
        await using var second = server.CreateAsyncScope();

        var firstDatabase = first.ServiceProvider.GetBitBsqliteServices(s => Configure(s)).GetRequiredService<BsqliteDatabase>();
        var secondDatabase = second.ServiceProvider.GetBitBsqliteServices(s => Configure(s)).GetRequiredService<BsqliteDatabase>();

        Assert.AreNotSame(firstDatabase, secondDatabase);
        Assert.AreSame(firstDatabase, first.ServiceProvider.GetBitBsqliteServices(s => Configure(s)).GetRequiredService<BsqliteDatabase>());
    }

    /// <summary>
    /// A host that loads Bit.Bsqlite with the rest of the app - a Blazor Hybrid app sharing the pages, a server
    /// prerendering them - can register it as usual, and the pages that ask get what it registered.
    /// </summary>
    [TestMethod]
    public async Task Registered_TheApplicationsOwnAreUsed_AndConfigureDoesNotRun()
    {
        await using var app = BuildApp(services => Configure(services));

        var services = app.GetBitBsqliteServices(_ => Assert.Fail("configure ran although the app registered Bsqlite."));

        Assert.AreSame(app, services);
    }

    /// <summary>
    /// What the configure registers is built as if <c>Program.cs</c> had registered it: the app's services are there
    /// for its steps, for a context's constructor and for the app's own services registered next to Bsqlite's, and the
    /// provider handed back answers for them too.
    /// </summary>
    [TestMethod]
    public async Task WhatConfigureRegisters_ReachesTheApplicationsServices()
    {
        var tenant = new Tenant("contoso");
        await using var app = BuildApp(services => services.AddSingleton(tenant));
        Tenant? seenByStep = null;

        var services = app.GetBitBsqliteServices(services =>
        {
            Configure(services, sqlite => sqlite.BeforeFirstOpen((sp, _) =>
            {
                seenByStep = sp.GetRequiredService<Tenant>();
                return Task.CompletedTask;
            }));

            services.AddBitBsqliteDbContext<TenantContext>(options => options.Schema = BsqliteSchemaStrategy.EnsureCreated);
            services.AddScoped<TenantNotes>();
        });

        var notes = services.GetRequiredService<TenantNotes>();
        Assert.AreSame(tenant, notes.Tenant);
        Assert.AreSame(services.GetRequiredService<BsqliteDatabase>(), notes.Database);
        Assert.AreSame(tenant, services.GetRequiredService<Tenant>());

        await using var db = await services.GetRequiredService<IBsqliteDbContextFactory<TenantContext>>().CreateDbContextAsync(Ct);
        Assert.AreSame(tenant, db.Tenant);
        Assert.AreSame(tenant, seenByStep);
    }

    [TestMethod]
    public async Task ConfigureWithoutBsqlite_IsExplained_AndNotRemembered()
    {
        await using var app = BuildApp();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => app.GetBitBsqliteServices(services => services.AddScoped<TenantNotes>()));
        Assert.Contains("AddBitBsqliteServices", error.Message);

        // The next page asking, with the registration right, gets its services.
        Assert.IsNotNull(app.GetBitBsqliteServices(s => Configure(s)).GetRequiredService<BsqliteDatabase>());
    }

    [TestMethod]
    public async Task WithoutAJavaScriptRuntime_RegistrationIsAskedFor()
    {
        await using var app = new ServiceCollection().BuildServiceProvider();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => app.GetBitBsqliteServices(s => Configure(s)));

        Assert.Contains("AddBitBsqliteServices", error.Message);
        Assert.Contains("IJSRuntime", error.Message);
    }

    private sealed record Tenant(string Name);

    /// <summary>A service of the app's own, registered next to Bsqlite's, that takes from both containers.</summary>
    private sealed class TenantNotes(BsqliteDatabase database, Tenant tenant)
    {
        public BsqliteDatabase Database { get; } = database;

        public Tenant Tenant { get; } = tenant;
    }

    /// <summary>A context whose constructor takes a service of the app's.</summary>
    private sealed class TenantContext(DbContextOptions<TenantContext> options, Tenant tenant) : DbContext(options)
    {
        public Tenant Tenant { get; } = tenant;

        public DbSet<Category> Categories => Set<Category>();
    }

    private sealed class InProcessTransportFactory : IBsqliteTransportFactory
    {
        public IBsqliteTransport Create(IBsqliteStorageProvider provider) => new BsqliteInProcessTransport();
    }

    /// <summary>Stands for a connection: the in-memory backend and the in-process transport never call JavaScript.</summary>
    private sealed class UnusedJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => throw new InvalidOperationException($"Unexpected JavaScript call '{identifier}'.");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => throw new InvalidOperationException($"Unexpected JavaScript call '{identifier}'.");
    }
}
