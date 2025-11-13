//+:cnd:noEmit
using System.ComponentModel;
using Boilerplate.Server.Api.SignalR;
using Boilerplate.Shared.Dtos.Chatbot;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// MCP tools for chatbot interactions
/// </summary>
[McpServerToolType]
public partial class AppMcpService
{
    /// <summary>
    /// Sends a message to the chatbot and returns the response
    /// </summary>
    [McpServerTool(Name = "bit_mcp")]
    public async Task<string> StartChat(
        [Required, Description("The user's new message to send to the chatbot")] string newMessage,
        [Description("Optional: Previous summarized conversation history")] List<AiChatMessage>? conversationHistory = null,
        [Description("Optional: User's culture LCID (e.g., 1033 for en-US)")] int? cultureLcid = null,
        [Description("Optional: Device information string (e.g., Google Chrome on Windows)")] string? deviceInfo = null,
        [Description("Optional: SignalR connection id")] string? signalRConnectionId = null, // If the mcp caller somehow knows the user's SignalR connection id, it can passes it to use features AppChatbot provides such as navigating the user to a specific page.
        McpServer server = default!,
        RequestContext<CallToolRequestParams> context = default!,
        AppChatbot chatBot = default!,
        IHttpContextAccessor httpContextAccessor = default!,
        CancellationToken cancellationToken = default)
    {
        // Start the chatbot session
        await chatBot.Start(
            conversationHistory ?? [],
            cultureLcid,
            deviceInfo,
            signalRConnectionId,
            cancellationToken);

        Uri? serverApiAddress = null;
        if (httpContextAccessor.HttpContext is not null)
        {
            var request = httpContextAccessor.HttpContext.Request;
            serverApiAddress = new Uri($"{request.Scheme}://{request.Host}");
        }

        // Process the message and collect the response
        var responseTask = chatBot.ProcessMessageAsync(
            generateFollowUpSuggestions: false,
            newMessage,
            serverApiAddress,
            cancellationToken);

        var response = new System.Text.StringBuilder();

        // Read all responses from the channel
        await foreach (var chunk in chatBot.GetStreamingChannel().ReadAllAsync(cancellationToken))
        {
            if (chunk == SharedPubSubMessages.MESSAGE_RPOCESS_SUCCESS)
            {
                // Message processed successfully
                break;
            }
            else if (chunk == SharedPubSubMessages.MESSAGE_RPOCESS_ERROR)
            {
                return "Error processing message. Please try again.";
            }
            else
            {
                // Append the response chunk
                response.Append(chunk);
            }
        }

        await responseTask;

        return response.ToString();
    }
}
