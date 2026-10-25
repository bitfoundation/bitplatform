using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Bcrdt.SignalR;

/// <summary>Registers the SignalR transport with a dependency injection container.</summary>
public static class BitBcrdtSignalR
{
    /// <summary>
    /// Registers a <see cref="BcrdtSignalRTransportFactory"/> as the application's transport source.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A singleton, because the factory holds only configuration; the per-document state lives in
    /// the transports it hands out, whose lifetime the collaboration service owns.
    /// </para>
    /// <para>
    /// Registered with <c>TryAdd</c> so that a test host or a hosted-server scenario can register a
    /// different transport first and have this call defer to it, rather than silently replacing the
    /// in-memory transport a test had deliberately installed.
    /// </para>
    /// </remarks>
    /// <param name="services">The container to register with.</param>
    /// <param name="hubUrl">
    /// Where the hub is mapped, as an absolute URL. The .NET SignalR client cannot resolve a relative
    /// one; in Blazor WebAssembly, resolve it against <c>HostEnvironment.BaseAddress</c>.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="hubUrl"/> is not an absolute URL.</exception>
    /// <param name="configure">Applied to each connection's builder, for access tokens or a binary hub protocol.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddBitBcrdtSignalRTransport(
        this IServiceCollection services,
        string hubUrl,
        Action<IHubConnectionBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(hubUrl);
        BcrdtSignalRTransport.RequireAbsolute(hubUrl);

        services.TryAddSingleton<IBcrdtTransportFactory>(_ => new BcrdtSignalRTransportFactory(hubUrl, configure));
        return services;
    }
}
