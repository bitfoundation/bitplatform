using Bit.Bsqlite.Data;
using Bit.Bsqlite.Interop;
using Bit.Bsqlite.Storage;
using Bit.Bsqlite.Storage.InMemory;
using Bit.Bsqlite.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

    /// <summary>The one method an app passes to every call, as its <c>AppDatabase</c> does.</summary>
    private static void Register(IServiceCollection services) => Configure(services);

    private static void RegisterWithNotes(IServiceCollection services)
    {
        Configure(services);
        services.AddScoped<TenantNotes>();
    }

    /// <summary>
    /// A WebAssembly app's container without Bsqlite: the JavaScript runtime every Blazor WebAssembly host registers, and
    /// the app's own.
    /// </summary>
    private static App BuildApp(Action<IServiceCollection>? configure = null, bool validateScopes = false)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(new WebAssemblyJsRuntime());
        configure?.Invoke(services);
        return new(services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = validateScopes }));
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

    /// <summary>Tests run several WebAssembly runtimes in one process, and each one has services of its own.</summary>
    [TestMethod]
    public async Task EachWebAssemblyRuntime_GetsServicesOfItsOwn()
    {
        await using var first = BuildApp();
        await using var second = BuildApp();

        var firstDatabase = first.GetBitBsqliteServices(Register).GetRequiredService<BsqliteDatabase>();
        var secondDatabase = second.GetBitBsqliteServices(Register).GetRequiredService<BsqliteDatabase>();

        Assert.AreNotSame(firstDatabase, secondDatabase);
        Assert.AreSame(firstDatabase, first.GetBitBsqliteServices(Register).GetRequiredService<BsqliteDatabase>());
    }

    [TestMethod]
    public async Task ConcurrentFirstCalls_RunConfigureOnce()
    {
        await using var app = BuildApp();
        var configured = 0;

        void Count(IServiceCollection services)
        {
            Interlocked.Increment(ref configured);
            Configure(services);
        }

        var services = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => app.GetBitBsqliteServices(Count), Ct)));

        Assert.AreEqual(1, configured);
        Assert.IsTrue(services.All(provider => ReferenceEquals(provider, services[0])));
    }

    /// <summary>
    /// The services are built once, so a second method would get services that lack what it registers - the context a
    /// second feature added, say - depending on which page happened to ask first.
    /// </summary>
    [TestMethod]
    public async Task AnotherConfigure_IsRefused()
    {
        await using var app = BuildApp();
        app.GetBitBsqliteServices(Register);

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => app.GetBitBsqliteServices(RegisterWithNotes));

        Assert.Contains(nameof(Register), error.Message);
        Assert.Contains(nameof(RegisterWithNotes), error.Message);
    }

    /// <summary>
    /// A Blazor Hybrid web view or a server circuit reaches the browser over a connection, and ends long before its process:
    /// services built for it would keep its databases open. Bit.Bsqlite is loaded with the app there, which registers them.
    /// </summary>
    [TestMethod]
    public async Task OutsideWebAssembly_RegistrationIsAskedFor()
    {
        var services = new ServiceCollection();
        services.AddScoped<IJSRuntime, RemoteJsRuntime>();
        await using var server = services.BuildServiceProvider();
        await using var circuit = server.CreateAsyncScope();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => circuit.ServiceProvider.GetBitBsqliteServices(Register));

        Assert.Contains("AddBitBsqliteServices", error.Message);
        Assert.Contains("RendererInfo.IsInteractive", error.Message);
    }

    /// <summary>
    /// A host that loads Bit.Bsqlite with the rest of the app - a Blazor Hybrid app sharing the pages, a server
    /// prerendering them - registers it as usual, and the pages that ask get what it registered, without anything being
    /// built to find out.
    /// </summary>
    [TestMethod]
    public async Task Registered_TheApplicationsOwnAreUsed()
    {
        await using var app = BuildApp(services =>
        {
            Register(services);
            services.AddScoped<IBsqliteSessionFactory>(_ => throw new AssertFailedException("The session factory was built to tell whether it is registered."));
        });

        var services = app.GetBitBsqliteServices(Register);

        Assert.AreSame(app, services);
    }

    /// <summary>
    /// What configure registers besides Bsqlite's services has to be in a registering app's container too, which the
    /// pages get instead: a Hybrid app that called AddBitBsqliteServices but left the context to the shared method is
    /// told so, rather than failing on whichever page asks for the context.
    /// </summary>
    [TestMethod]
    public async Task Registered_WithoutWhatConfigureRegisters_IsExplained()
    {
        await using var app = BuildApp(services =>
        {
            services.AddBitBsqliteServices(sqlite => sqlite
                .AddStorageProvider<BsqliteInMemoryStorageProvider>()
                .UseStorage(s => s.Prefer(BsqliteInMemoryStorageProvider.ProviderName))
                .UseBindingStore<BsqliteInMemoryStorageBindingStore>()
                .AddDatabase("lazy.db"));
            services.AddSingleton<IBsqliteTransportFactory>(new InProcessTransportFactory());
        });

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => app.GetBitBsqliteServices(Register));

        Assert.Contains("IBsqliteDbContextFactory<ProductContext>", error.Message);
        Assert.DoesNotContain("BsqliteDatabase", error.Message);
    }

    [TestMethod]
    public async Task Registered_WithoutAKeyedServiceConfigureRegisters_IsExplained()
    {
        await using var app = BuildApp(Register);

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => app.GetBitBsqliteServices(services =>
        {
            Register(services);
            services.AddKeyedScoped<KeyedNotes>("main");
        }));

        Assert.Contains("KeyedNotes with the key 'main'", error.Message);
    }

    [TestMethod]
    public async Task Registered_WithoutAnOpenGenericConfigureRegisters_IsExplained()
    {
        await using var app = BuildApp(Register);

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => app.GetBitBsqliteServices(services =>
        {
            Register(services);
            services.AddScoped(typeof(Repository<>));
        }));

        Assert.Contains("Repository<>", error.Message);
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

    /// <summary>
    /// The app's services come from a scope of its own that lasts as long as the services do - not from whatever the
    /// first caller passed: a scope it disposes, as an OwningComponentBase's is, or the root, which Development's scope
    /// validation refuses scoped services from.
    /// </summary>
    [TestMethod]
    public async Task TheApplicationsServices_ComeFromAScopeThatLastsAsLongAsThey()
    {
        await using var app = BuildApp(services => services.AddScoped(_ => new Tenant("contoso")), validateScopes: true);

        IServiceProvider services;
        await using (var page = app.CreateAsyncScope())
        {
            services = page.ServiceProvider.GetBitBsqliteServices(RegisterWithNotes);
        }

        var notes = services.GetRequiredService<TenantNotes>();
        Assert.AreEqual("contoso", notes.Tenant.Name);
        Assert.AreSame(notes, app.GetBitBsqliteServices(RegisterWithNotes).GetRequiredService<TenantNotes>());
    }

    /// <summary>
    /// A scope created through the services is a scope of both containers: what it builds takes that scope's services -
    /// the app's included - and disposing it disposes the app's scope with it.
    /// </summary>
    [TestMethod]
    public async Task AScopeOfTheServices_IsAScopeOfTheApplicationsToo()
    {
        await using var app = BuildApp(services => services.AddScoped<AppSession>());
        var services = app.GetBitBsqliteServices(services =>
        {
            Configure(services);
            services.AddScoped<SessionNotes>();
        });

        AppSession session;
        await using (var scope = services.CreateAsyncScope())
        {
            var notes = scope.ServiceProvider.GetRequiredService<SessionNotes>();
            session = scope.ServiceProvider.GetRequiredService<AppSession>();

            Assert.AreSame(session, notes.Session);
            Assert.AreSame(scope.ServiceProvider, notes.Services);
            Assert.AreNotSame(services.GetRequiredService<SessionNotes>(), notes);
            Assert.AreNotSame(services.GetRequiredService<AppSession>(), session);
            Assert.AreSame(app.GetRequiredService<IJSRuntime>(), scope.ServiceProvider.GetRequiredService<IJSRuntime>());
        }

        Assert.IsTrue(session.Disposed);
        Assert.IsFalse(services.GetRequiredService<AppSession>().Disposed);
    }

    [TestMethod]
    public async Task AKeyedService_GetsTheKeyItIsAskedFor()
    {
        await using var app = BuildApp();
        var services = app.GetBitBsqliteServices(services =>
        {
            Configure(services);
            services.AddKeyedScoped<KeyedNotes>("main");
            services.AddKeyedScoped<KeyedNotes>(KeyedService.AnyKey);
        });

        var main = services.GetRequiredKeyedService<KeyedNotes>("main");
        var other = services.GetRequiredKeyedService<KeyedNotes>("other");

        Assert.AreEqual("main", main.Key);
        Assert.AreEqual("other", other.Key);
        Assert.AreSame(services.GetRequiredService<BsqliteDatabase>(), main.Database);
    }

    /// <summary>An open generic of the app's own, registered next to Bsqlite's, takes from both containers as well.</summary>
    [TestMethod]
    public async Task AnOpenGenericOfConfigures_ReachesTheApplicationsServices()
    {
        await using var app = BuildApp(services => services.AddLogging());
        var services = app.GetBitBsqliteServices(services =>
        {
            Configure(services);
            services.AddScoped(typeof(Repository<>));
        });

        var repository = services.GetRequiredService<Repository<Category>>();

        Assert.IsNotNull(repository.Logger);
        Assert.AreSame(services.GetRequiredService<BsqliteDatabase>(), repository.Database);
        Assert.AreSame(repository, services.GetRequiredService<Repository<Category>>());
    }

    [TestMethod]
    public async Task AKeyedOpenGenericOfConfigures_ReachesTheApplicationsServices()
    {
        await using var app = BuildApp(services => services.AddLogging());
        var services = app.GetBitBsqliteServices(services =>
        {
            Configure(services);
            services.AddKeyedScoped(typeof(Repository<>), "main");
        });

        var repository = services.GetRequiredKeyedService<Repository<Category>>("main");

        Assert.IsNotNull(repository.Logger);
        Assert.AreSame(repository, services.GetRequiredKeyedService<Repository<Category>>("main"));
    }

    /// <summary>
    /// Every closing of an open generic of configure's in an IEnumerable takes from both containers too, next to the
    /// closed services configure registers, in the order it registers them.
    /// </summary>
    [TestMethod]
    public async Task AnEnumerableOfAnOpenGenericOfConfigures_ReachesTheApplicationsServices()
    {
        await using var app = BuildApp(services => services.AddLogging());
        var services = app.GetBitBsqliteServices(services =>
        {
            Configure(services);
            services.AddScoped(typeof(Repository<>));
            services.AddScoped(_ => new Repository<Category>(null!, null!));
        });

        var repositories = services.GetServices<Repository<Category>>().ToArray();

        Assert.HasCount(2, repositories);
        Assert.IsNotNull(repositories[0].Logger);
        Assert.IsNull(repositories[1].Logger);
        Assert.AreSame(repositories[1], services.GetRequiredService<Repository<Category>>());
    }

    /// <summary>
    /// A constructor that leads back to itself is refused, as the container would refuse it, rather than overflow the
    /// stack - here through closings of open generics, which the provider builds without the container.
    /// </summary>
    [TestMethod]
    public async Task ACircularDependency_IsRefused()
    {
        await using var app = BuildApp();
        var services = app.GetBitBsqliteServices(services =>
        {
            Configure(services);
            services.AddScoped(typeof(Chicken<>));
            services.AddScoped(typeof(Egg<>));
        });

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => services.GetRequiredService<Chicken<Category>>());

        Assert.Contains("circular dependency", error.Message);
        Assert.Contains($"{typeof(Chicken<Category>)} -> {typeof(Egg<Category>)} -> {typeof(Chicken<Category>)}", error.Message);
    }

    [TestMethod]
    public async Task DisposingAScope_DisposesTheApplicationsScope_EvenWhenAServiceOfItThrows()
    {
        await using var app = BuildApp(services => services.AddScoped<AppSession>());
        var services = app.GetBitBsqliteServices(services =>
        {
            Configure(services);
            services.AddScoped(typeof(Fragile<>));
        });

        var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<Fragile<Category>>();
        var session = scope.ServiceProvider.GetRequiredService<AppSession>();

        Assert.ThrowsExactly<InvalidOperationException>(scope.Dispose);
        Assert.IsTrue(session.Disposed);
    }

    /// <summary>Not every container can tell what it has; one that cannot is asked by resolving.</summary>
    [TestMethod]
    public async Task AnApplicationContainerThatCannotTellWhatItHas_IsAskedByResolving()
    {
        await using var app = BuildApp(services => services.AddSingleton(new Tenant("contoso")));

        var services = new Untelling(app).GetBitBsqliteServices(RegisterWithNotes);

        Assert.AreEqual("contoso", services.GetRequiredService<TenantNotes>().Tenant.Name);
    }

    /// <summary>
    /// Logging, options and what builds on them are open generics the app's container has too; a second set of them would
    /// answer for the app's, losing its logging and its options.
    /// </summary>
    [TestMethod]
    public async Task ConfigureThatWouldHideTheApplicationsLogging_IsExplained()
    {
        await using var app = BuildApp(services => services.AddLogging());

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => app.GetBitBsqliteServices(services =>
        {
            Configure(services);
            services.AddLogging();
        }));

        Assert.Contains("ILogger<>", error.Message);
        Assert.Contains("IOptions<>", error.Message);
    }

    [TestMethod]
    public async Task ConfigureWithoutBsqlite_IsExplained_AndNotRemembered()
    {
        await using var app = BuildApp();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => app.GetBitBsqliteServices(services => services.AddScoped<TenantNotes>()));
        Assert.Contains("AddBitBsqliteServices", error.Message);

        // The next page asking, with the registration right, gets its services.
        Assert.IsNotNull(app.GetBitBsqliteServices(Register).GetRequiredService<BsqliteDatabase>());
    }

    [TestMethod]
    public async Task WithoutAJavaScriptRuntime_RegistrationIsAskedFor()
    {
        await using var app = new ServiceCollection().BuildServiceProvider();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => app.GetBitBsqliteServices(Register));

        Assert.Contains("AddBitBsqliteServices", error.Message);
        Assert.Contains("WebAssembly", error.Message);
    }

    /// <summary>
    /// A WebAssembly app's services go with its tab, so only tests release them: the databases close, and the next call
    /// builds them anew.
    /// </summary>
    [TestMethod]
    public async Task Released_TheDatabasesClose_AndTheNextCallBuildsAnew()
    {
        await using var app = BuildApp();
        var configured = 0;

        void Count(IServiceCollection services)
        {
            configured++;
            Configure(services);
        }

        var database = app.GetBitBsqliteServices(Count).GetRequiredService<BsqliteDatabase>();
        await database.OpenAsync(Ct);

        await BsqliteOwnServices.ReleaseAsync(app.GetRequiredService<IJSRuntime>());

        Assert.IsFalse(database.IsOpen);
        Assert.AreNotSame(database, app.GetBitBsqliteServices(Count).GetRequiredService<BsqliteDatabase>());
        Assert.AreEqual(2, configured);
    }

    /// <summary>
    /// The app's container, which releases what <c>GetBitBsqliteServices</c> built for its runtime when it is disposed,
    /// as closing the tab would.
    /// </summary>
    private sealed class App(ServiceProvider services) : IServiceProvider, IAsyncDisposable
    {
        public object? GetService(Type serviceType) => services.GetService(serviceType);

        public async ValueTask DisposeAsync()
        {
            await BsqliteOwnServices.ReleaseAsync(services.GetRequiredService<IJSRuntime>());
            await services.DisposeAsync();
        }
    }

    private sealed record Tenant(string Name);

    /// <summary>A service of the app's own, registered next to Bsqlite's, that takes from both containers.</summary>
    private sealed class TenantNotes(BsqliteDatabase database, Tenant tenant)
    {
        public BsqliteDatabase Database { get; } = database;

        public Tenant Tenant { get; } = tenant;
    }

    /// <summary>A scoped service of the app's.</summary>
    private sealed class AppSession : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    private sealed class SessionNotes(AppSession session, IServiceProvider services)
    {
        public AppSession Session { get; } = session;

        public IServiceProvider Services { get; } = services;
    }

    private sealed class KeyedNotes(BsqliteDatabase database, [ServiceKey] string key)
    {
        public BsqliteDatabase Database { get; } = database;

        public string Key { get; } = key;
    }

    private sealed class Repository<T>(BsqliteDatabase database, ILogger<Repository<T>> logger)
    {
        public BsqliteDatabase Database { get; } = database;

        public ILogger<Repository<T>> Logger { get; } = logger;
    }

    private sealed class Chicken<T>(Egg<T> egg)
    {
        public Egg<T> Egg { get; } = egg;
    }

    private sealed class Egg<T>(Chicken<T> chicken)
    {
        public Chicken<T> Chicken { get; } = chicken;
    }

    private sealed class Fragile<T> : IDisposable
    {
        public void Dispose() => throw new InvalidOperationException($"{typeof(T).Name} broke.");
    }

    /// <summary>An application container without <see cref="IServiceProviderIsService"/>, nor its scopes.</summary>
    private sealed class Untelling(IServiceProvider services) : IServiceProvider, IServiceScopeFactory
    {
        public object? GetService(Type serviceType)
            => serviceType == typeof(IServiceProviderIsService) || serviceType == typeof(IServiceProviderIsKeyedService) ? null
                : serviceType == typeof(IServiceScopeFactory) ? this
                : services.GetService(serviceType);

        public IServiceScope CreateScope() => new Scope(services.CreateScope());

        private sealed class Scope(IServiceScope scope) : IServiceScope
        {
            public IServiceProvider ServiceProvider { get; } = new Untelling(scope.ServiceProvider);

            public void Dispose() => scope.Dispose();
        }
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

    /// <summary>
    /// WebAssembly's runtime, the one that is in-process; the in-memory backend and the in-process transport never call
    /// JavaScript.
    /// </summary>
    private sealed class WebAssemblyJsRuntime : IJSInProcessRuntime
    {
        public TResult Invoke<TResult>(string identifier, params object?[]? args)
            => throw new InvalidOperationException($"Unexpected JavaScript call '{identifier}'.");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => throw new InvalidOperationException($"Unexpected JavaScript call '{identifier}'.");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => throw new InvalidOperationException($"Unexpected JavaScript call '{identifier}'.");
    }

    /// <summary>A runtime that reaches the browser over a connection: a server circuit's, or a Blazor Hybrid web view's.</summary>
    private sealed class RemoteJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => throw new InvalidOperationException($"Unexpected JavaScript call '{identifier}'.");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => throw new InvalidOperationException($"Unexpected JavaScript call '{identifier}'.");
    }
}
