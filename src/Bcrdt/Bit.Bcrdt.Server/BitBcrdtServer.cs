using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

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
}
