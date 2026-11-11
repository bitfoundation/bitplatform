using Microsoft.AspNetCore.SignalR.Client;

namespace Bit.BlazorUI;

/// <summary>
/// Registers the SignalR transport with a <see cref="BitAiBuilder"/>.
/// </summary>
public static class BitAiSignalRBuilderExtensions
{
    /// <summary>
    /// Registers a <see cref="BitAiSignalRTransport"/>: each reply is streamed from a <c>BitAiHub</c>
    /// (Bit.BlazorUI.AI.Server) over a SignalR connection.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="connection">Gets the connection to the hub.</param>
    /// <param name="options">The hub method names and the serializer settings.</param>
    /// <returns>The builder, for chaining.</returns>
    public static BitAiBuilder UseSignalRTransport(
        this BitAiBuilder builder,
        Func<IServiceProvider, HubConnection> connection,
        BitAiSignalRTransportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(connection);
        return builder.UseTransport(sp => new BitAiSignalRTransport(connection(sp), options));
    }
}
