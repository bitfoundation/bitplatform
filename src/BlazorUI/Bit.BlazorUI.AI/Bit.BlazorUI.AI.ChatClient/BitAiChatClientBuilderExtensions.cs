using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BlazorUI;

/// <summary>
/// Registers the <see cref="IChatClient"/> transport with a <see cref="BitAiBuilder"/>.
/// </summary>
public static class BitAiChatClientBuilderExtensions
{
    /// <summary>
    /// Registers a <see cref="BitAiChatClientTransport"/>: each request goes to an <see cref="IChatClient"/> where the
    /// components run (a Blazor Server circuit, a Hybrid app).
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="chatClient">Gets the chat client; by default the one registered as a service.</param>
    /// <param name="options">The chat options every request is sent with, which win over what a request asks for.</param>
    /// <returns>The builder, for chaining.</returns>
    public static BitAiBuilder UseChatClient(
        this BitAiBuilder builder,
        Func<IServiceProvider, IChatClient>? chatClient = null,
        ChatOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseTransport(sp => new BitAiChatClientTransport(
            chatClient?.Invoke(sp) ?? sp.GetRequiredService<IChatClient>(),
            options));
    }
}
