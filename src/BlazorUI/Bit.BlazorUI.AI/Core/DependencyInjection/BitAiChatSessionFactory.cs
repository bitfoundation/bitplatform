using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI;

/// <summary>
/// Creates <see cref="BitAiChatSession"/> instances with the registered transport, tools, and policies.
/// </summary>
/// <remarks>
/// Inject this into a component rather than constructing a session by hand, so a session picks up
/// everything the host configured without the component having to know what that was.
/// </remarks>
public sealed class BitAiChatSessionFactory
{
    private readonly IServiceProvider _services;
    private readonly IBitAiTransport _transport;
    private readonly IBitAiToolRegistry _tools;
    private readonly IBitAiToolApprovalPolicy _approvalPolicy;
    private readonly BitAiChatSessionOptions _options;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>Creates a factory.</summary>
    /// <param name="services">The scoped service provider handed to tools.</param>
    /// <param name="transport">The registered transport.</param>
    /// <param name="tools">The registered tools.</param>
    /// <param name="approvalPolicy">The registered approval policy.</param>
    /// <param name="options">The configured session defaults.</param>
    /// <param name="loggerFactory">Creates loggers for the sessions.</param>
    public BitAiChatSessionFactory(
        IServiceProvider services,
        IBitAiTransport transport,
        IBitAiToolRegistry tools,
        IBitAiToolApprovalPolicy approvalPolicy,
        BitAiChatSessionOptions options,
        ILoggerFactory loggerFactory)
    {
        _services = services;
        _transport = transport;
        _tools = tools;
        _approvalPolicy = approvalPolicy;
        _options = options;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates a session for a conversation.
    /// </summary>
    /// <param name="conversation">The conversation to drive, or <see langword="null"/> for a new one.</param>
    /// <param name="options">Overrides for this session only.</param>
    /// <returns>The session.</returns>
    public BitAiChatSession Create(BitAiConversation? conversation = null, BitAiChatSessionOptions? options = null) => new(
        _transport,
        conversation,
        options ?? _options,
        _tools,
        _approvalPolicy,
        _services,
        _loggerFactory.CreateLogger<BitAiChatSession>());
}
