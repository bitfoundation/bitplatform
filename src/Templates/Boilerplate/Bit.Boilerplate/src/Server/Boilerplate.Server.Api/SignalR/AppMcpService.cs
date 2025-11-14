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
    /// Due the current stateless design of the AppMcpService, the following method accepts all necessary parameters to start a chat that would process
    /// only one new message from the user. The method returns the chatbot's response as a string.
    /// The new message can be anything from a question to a command supported by the AppChatbot service.
    /// </summary>
    [McpServerTool(Name = "bit_mcp")]
    public async Task<string> Mcp(
        StartMcpChatRequest request,
        McpServer server = default!,
        RequestContext<CallToolRequestParams> context = default!,
        AppChatbot chatBot = default!,
        IHttpContextAccessor httpContextAccessor = default!,
        CancellationToken cancellationToken = default)
    {
        // Start the chatbot session
        await chatBot.StartChat(
            new()
            {
                ChatMessagesHistory = request.ConversationHistory,
                CultureId = request.CultureLcid,
                DeviceInfo = request.DeviceInfo,
                TimeZoneId = request.TimeZoneId,
                ServerApiAddress = httpContextAccessor.HttpContext?.Request?.GetBaseUrl(),
            },
            request.SignalRConnectionId,
            cancellationToken);

        // Process the message and collect the response
        var responseTask = chatBot.ProcessNewMessage(
            generateFollowUpSuggestions: false,
            request.NewMessage,
            httpContextAccessor.HttpContext?.Request?.GetBaseUrl(),
            cancellationToken);

        var response = new System.Text.StringBuilder();

        // Read all responses from the channel
        await foreach (var chunk in chatBot.GetStreamingChannel().ReadAllAsync(cancellationToken))
        {
            if (chunk == SharedAppMessages.MESSAGE_RPOCESS_SUCCESS)
            {
                // Message processed successfully
                break;
            }
            else if (chunk == SharedAppMessages.MESSAGE_RPOCESS_ERROR)
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

public class StartMcpChatRequest
{
    [Required, Description("The user's new message to send")]
    public string NewMessage { get; set; } = string.Empty;

    [Description("Optional: Previous conversation history")]
    public List<AiChatMessage> ConversationHistory { get; set; } = [];

    [Description("Optional: User's culture LCID (e.g., 1033 for en-US)")]
    public int? CultureLcid { get; set; }

    [Description("Optional: Device information string (e.g., Google Chrome on Windows)")]
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// If the mcp caller somehow knows the user's SignalR connection id, it can passes it to use features AppChatbot provides such as navigating the user to a specific page.
    /// </summary>
    [Description("Optional: SignalR connection id")]
    public string? SignalRConnectionId { get; set; }

    [Description("Optional: User's time zone")]
    public string? TimeZoneId { get; set; }
}
