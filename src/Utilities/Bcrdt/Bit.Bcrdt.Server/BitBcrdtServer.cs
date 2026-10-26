using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Bit.Bcrdt.Server;

/// <summary>Registers the collaboration hub with a server's dependency injection container.</summary>
public static class BitBcrdtServer
{
    /// <summary>
    /// The receive limit applied by default: comfortably above the handshake reply for a large
    /// document, and still finite so one client cannot make the server buffer without bound.
    /// </summary>
    public const long DefaultMaximumReceiveMessageSize = 16 * 1024 * 1024;

    /// <summary>
    /// Adds SignalR and sizes its message limit for <see cref="BcrdtHub"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SignalR's default <see cref="HubOptions.MaximumReceiveMessageSize"/> is 32 KB, and the JSON
    /// hub protocol base64-encodes binary payloads, so any message over roughly 24 KB of document
    /// data gets the sending client disconnected. A handshake reply carries a peer's whole missing
    /// state, so once a document grows past that size a newcomer can never be brought up to date,
    /// and every client that tries to answer it is dropped and reconnects, in a loop.
    /// </para>
    /// <para>
    /// Map the hub as usual afterwards: <c>app.MapHub&lt;BcrdtHub&gt;("/hub/collaboration")</c>.
    /// </para>
    /// </remarks>
    /// <param name="services">The container to register with.</param>
    /// <param name="maximumReceiveMessageSize">The largest message, in bytes, a client may send.</param>
    /// <returns>The SignalR builder, for further configuration such as a backplane.</returns>
    [RequiresUnreferencedCode("SignalR does not currently support trimming or native AOT.")]
    public static ISignalRServerBuilder AddBitBcrdtHub(
        this IServiceCollection services,
        long maximumReceiveMessageSize = DefaultMaximumReceiveMessageSize) =>
        services.AddBitBcrdtHub<BcrdtHub>(maximumReceiveMessageSize);

    /// <summary>
    /// Adds SignalR and sizes its message limit for a hub derived from <see cref="BcrdtHub"/>.
    /// </summary>
    /// <typeparam name="THub">The hub type the application maps.</typeparam>
    /// <param name="services">The container to register with.</param>
    /// <param name="maximumReceiveMessageSize">The largest message, in bytes, a client may send.</param>
    /// <returns>The SignalR builder, for further configuration such as a backplane.</returns>
    [RequiresUnreferencedCode("SignalR does not currently support trimming or native AOT.")]
    public static ISignalRServerBuilder AddBitBcrdtHub<THub>(
        this IServiceCollection services,
        long maximumReceiveMessageSize = DefaultMaximumReceiveMessageSize)
        where THub : BcrdtHub
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumReceiveMessageSize, 1);

        ISignalRServerBuilder builder = services.AddSignalR();
        builder.AddHubOptions<THub>(options => options.MaximumReceiveMessageSize = maximumReceiveMessageSize);
        return builder;
    }

    /// <summary>
    /// Registers a <see cref="BcrdtDocumentHost"/>, so that <see cref="BcrdtHub"/> keeps a persisted
    /// replica of every document it carries.
    /// </summary>
    /// <remarks>
    /// The host is a singleton, disposed - and its documents flushed to the store - when the application
    /// stops. The WebSocket endpoint does not pick it up on its own, because its rooms are scoped to the
    /// mapping; pass it in <see cref="BcrdtWebSocketEndpointOptions.DocumentHost"/>, resolved with
    /// <c>app.Services.GetRequiredService&lt;BcrdtDocumentHost&gt;()</c>.
    /// </remarks>
    /// <param name="services">The container to register with.</param>
    /// <param name="options">
    /// Builds the host's settings - typically giving it a store - from the container. Omitted, documents
    /// are kept in process memory only.
    /// </param>
    /// <returns>The container, for chaining.</returns>
    public static IServiceCollection AddBitBcrdtDocumentHost(
        this IServiceCollection services,
        Func<IServiceProvider, BcrdtDocumentHostOptions>? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(provider => new BcrdtDocumentHost(
            options?.Invoke(provider),
            provider.GetService<ILogger<BcrdtDocumentHost>>()));
        return services;
    }

    /// <summary>
    /// Registers a <see cref="BcrdtPresenceCircuitHandler"/>, so that a Blazor Server user whose browser connection
    /// drops stops being listed as present at once, rather than when their circuit expires.
    /// </summary>
    /// <remarks>
    /// Scoped, as Blazor expects every <see cref="CircuitHandler"/> to be: each circuit resolves its own, beside its
    /// own <see cref="IBcrdtCollaborationService"/>. Registering it more than once adds it once.
    /// </remarks>
    /// <param name="services">The container to register with.</param>
    /// <returns>The container, for chaining.</returns>
    public static IServiceCollection AddBitBcrdtCircuitPresence(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, BcrdtPresenceCircuitHandler>());
        return services;
    }
}
