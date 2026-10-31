using System.Runtime.CompilerServices;
using Bit.Bsqlite;
using Bit.Bsqlite.Interop;
using Microsoft.JSInterop;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers bit Bsqlite with dependency injection.</summary>
/// <remarks>
/// <para>
/// Everything that reaches the browser, or holds a database session, is registered as scoped, which is
/// correct for every hosting model: in WebAssembly a scope lives for the whole application, so there is one
/// session per database exactly as the worker design wants; on a server one scope exists per circuit, so a
/// user's database - which lives in that user's browser - is never reached through another user's
/// <c>IJSRuntime</c>.
/// </para>
/// <para>
/// In a Blazor Web App, register this in the client project, and in the server project as well when the
/// pages that use it are prerendered there: resolving the services is harmless on the server, and only
/// opening a database needs the browser.
/// </para>
/// <para>
/// A Blazor Hybrid app registers the same thing: there .NET runs natively, so the file system backend is
/// added and ranked ahead of the browser ones, and databases are SQLite files on the device opened by the
/// native engine in-process - a scope per web view, like a tab.
/// </para>
/// </remarks>
public static partial class BitBsqlite
{
    // One container per browser connection for applications that never called AddBitBsqliteServices. Keyed weakly, so
    // a request prerendered on a server takes its container with it.
    private static readonly ConditionalWeakTable<IJSRuntime, IServiceProvider> _ownServices = new();

    /// <summary>
    /// Registers the session factory, storage selection, sticky bindings, and the worker transport.
    /// Storage providers are added on the builder.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddBitBsqliteServices(sqlite => sqlite
    ///     .AddStorageProvider&lt;BsqliteOpfsStorageProvider&gt;()
    ///     .AddStorageProvider&lt;BsqliteIndexedDbStorageProvider&gt;()
    ///     .AddStorageProvider&lt;BsqliteInMemoryStorageProvider&gt;()
    ///     .UseStorage(s => s.Prefer("opfs").Fallback("indexeddb").Fallback("in-memory").AllowNonPersistentFallback())
    ///     .AddDatabase("app.db"));
    /// </code>
    /// </example>
    public static IServiceCollection AddBitBsqliteServices(
        this IServiceCollection services,
        Action<BsqliteBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new BsqliteBuilder(services);
        configure(builder);
        builder.Complete();
        return services;
    }

    /// <summary>
    /// The services to resolve Bsqlite's from - <see cref="BsqliteDatabase"/>, the context factories, and the
    /// application's own services that take them: <paramref name="services"/> itself when the application registered
    /// them with <see cref="AddBitBsqliteServices"/>, otherwise ones built from <paramref name="configure"/> the first
    /// time they are asked for, once per browser connection - the whole application on WebAssembly. Inject them
    /// wherever they are registered; this is for an application that lazy loads Bit.Bsqlite on WebAssembly and so
    /// cannot register them in <c>Program.cs</c>, which runs before the assembly is there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <paramref name="configure"/> makes the calls <c>Program.cs</c> would: <see cref="AddBitBsqliteServices"/>,
    /// <see cref="AddBitBsqliteDbContext{TContext}"/> for each context, and the registrations of the application's own
    /// services that take Bsqlite's. They go into a container of their own, so they can be made once Bit.Bsqlite has
    /// loaded; what they register still reaches every service of the application's container, which is what the
    /// returned provider answers with for whatever they leave out - <c>LazyAssemblyLoader</c>, logging, the services a
    /// context's constructor takes.
    /// </para>
    /// <para>
    /// The services are built once per <see cref="IJSRuntime"/> of <paramref name="services"/>, so every call of a
    /// connection gets the same ones - the same database, opened once - and <paramref name="configure"/> runs only for
    /// the first. Pass the <see cref="IServiceProvider"/> a component injects. Keep the configuration in one method of
    /// the application's own rather than repeating it at each call, and keep that method out of anything that runs
    /// before the assembly has loaded.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Called from the pages Bit.Bsqlite is loaded for, never from Program.cs.
    /// internal static class AppDatabase
    /// {
    ///     public static IBsqliteDbContextFactory&lt;AppDbContext&gt; Contexts(IServiceProvider provider) => provider
    ///         .GetBitBsqliteServices(services =>
    ///         {
    ///             services.AddBitBsqliteServices(sqlite => sqlite
    ///                 .AddStorageProvider&lt;BsqliteOpfsStorageProvider&gt;()
    ///                 .UseStorage(s => s.Prefer(BsqliteOpfsStorageProvider.ProviderName))
    ///                 .AddDatabase("app.db"));
    ///
    ///             services.AddBitBsqliteDbContext&lt;AppDbContext&gt;();
    ///         })
    ///         .GetRequiredService&lt;IBsqliteDbContextFactory&lt;AppDbContext&gt;&gt;();
    /// }
    ///
    /// // A page:
    /// @inject IServiceProvider Services
    ///
    /// await using var db = await AppDatabase.Contexts(Services).CreateDbContextAsync();
    /// </code>
    /// </example>
    /// <param name="services">The services of the component asking, as injected through <see cref="IServiceProvider"/>.</param>
    /// <param name="configure">Registers Bsqlite's services, as <c>Program.cs</c> would have.</param>
    public static IServiceProvider GetBitBsqliteServices(this IServiceProvider services, Action<IServiceCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        if (services.GetService<IBsqliteSessionFactory>() is not null)
        {
            return services;
        }

        var js = services.GetService<IJSRuntime>() ?? throw new InvalidOperationException(
            "Bit.Bsqlite's services are not registered: call builder.Services.AddBitBsqliteServices(...) in Program.cs. "
            + "Without them GetBitBsqliteServices builds them for each browser connection, which needs the IJSRuntime of "
            + "a Blazor app, and this container has none.");

        return _ownServices.GetValue(js, _ => BsqliteOwnServices.Build(services, configure));
    }
}
