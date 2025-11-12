//+:cnd:noEmit
using System.ComponentModel;
using ModelContextProtocol.Server;
using Boilerplate.Server.Api.SignalR;
using Boilerplate.Shared.Dtos.Chatbot;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// MCP tools for chatbot interactions
/// </summary>
[McpServerToolType]
public class AppMcpService
{
    /// <summary>
    /// Sends a message to the chatbot and returns the response
    /// </summary>
    [McpServerTool(Name = "contextual_chat_bot")]
    [Description(@"A multi-purpose AI assistant that provides intelligent support for the application.
Capabilities include: answering questions about app features and functionality, searching and recommending products from the database, handling user complaints and feedback, providing contextual help based on user's culture and device, and maintaining conversation history for natural multi-turn interactions.
The assistant is context-aware and can access real-time application data to provide accurate, personalized responses.")]
    public static async Task<string> ChatWithBot(
        [Required, Description("The user's message to send to the chatbot")] string message,
        [Description("Optional: Previous conversation history")] List<AiChatMessage>? convesationHistory = null,
        [Description("Optional: User's culture LCID (e.g., 1033 for en-US)")] int? cultureLcid = null,
        [Description("Optional: Device information string (e.g., Google Chrome on Windows)")] string? deviceInfo = null,
        IServiceProvider serviceProvider = default!,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var chatbotService = scope.ServiceProvider.GetRequiredService<ChatbotService>();
        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

        // Start the chatbot session
        await chatbotService.Start(
            convesationHistory ?? [],
            cultureLcid,
            deviceInfo,
            cancellationToken);

        // Get server API address
        Uri? serverApiAddress = null;
        if (httpContextAccessor.HttpContext is not null)
        {
            var request = httpContextAccessor.HttpContext.Request;
            serverApiAddress = new Uri($"{request.Scheme}://{request.Host}");
        }

        // Process the message and collect the response
        var responseTask = chatbotService.ProcessMessageAsync(
            message,
            serverApiAddress,
            cancellationToken);

        var response = new System.Text.StringBuilder();
        var followUpSuggestions = string.Empty;

        // Read all responses from the channel
        await foreach (var chunk in chatbotService.GetStreamingChannel().ReadAllAsync(cancellationToken))
        {
            if (chunk == SharedChatProcessMessages.MESSAGE_RPOCESS_SUCESS)
            {
                // Message processed successfully
                break;
            }
            else if (chunk == SharedChatProcessMessages.MESSAGE_RPOCESS_ERROR)
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
