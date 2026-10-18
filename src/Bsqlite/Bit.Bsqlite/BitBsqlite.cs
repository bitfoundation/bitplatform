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
/// </remarks>
public static partial class BitBsqlite
{
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
}
