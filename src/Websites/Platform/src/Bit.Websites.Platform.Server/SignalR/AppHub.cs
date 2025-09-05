using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using Bit.Websites.Platform.Client.Services.Contracts;
using Bit.Websites.Platform.Server.Services;
using Bit.Websites.Platform.Shared.Dtos.AiChat;
using Bit.Websites.Platform.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace Bit.Websites.Platform.Server.SignalR;

[AllowAnonymous]
public partial class AppHub : Hub
{
    [AutoInject] private IServiceProvider serviceProvider = default!;

    [AutoInject] private IConfiguration configuration = default!;

    public async IAsyncEnumerable<string> Chatbot(
        StartChatbotRequest request,
        IAsyncEnumerable<string> incomingMessages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Incoming user messages are received via `incomingMessages`.
        // We utilize `Channel` to read incoming messages and send responses using `ChatClient`.
        // While processing a user message, a new message may arrive.
        // To handle this, we cancel the ongoing message processing using `messageSpecificCancellationTokenSrc` and start processing the new message.

        Channel<string> channel = Channel.CreateUnbounded<string>(new() { SingleReader = true, SingleWriter = true });
        var chatClient = serviceProvider.CreateAsyncScope().ServiceProvider.GetRequiredService<IChatClient>();

        await using var mcpClient = await McpClientFactory.CreateAsync(new SseClientTransport(new()
        {
            Endpoint = new("https://mcp.deepwiki.com/mcp"),
            Name = "DeepWiki"
        }), cancellationToken: cancellationToken); // provides ask_question tool


        var mcpTools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);

        async Task ReadIncomingMessages()
        {
            List<ChatMessage> chatMessages = request.ChatMessagesHistory
                .Select(c => new ChatMessage(c.Role is AiChatMessageRole.Assistant ? ChatRole.Assistant : ChatRole.User, c.Content))
                .ToList();

            CancellationTokenSource? messageSpecificCancellationTokenSrc = null;
            try
            {
                await foreach (var incomingMessage in incomingMessages)
                {
                    if (messageSpecificCancellationTokenSrc is not null)
                        await messageSpecificCancellationTokenSrc.CancelAsync();

                    messageSpecificCancellationTokenSrc = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    _ = HandleIncomingMessage(incomingMessage, messageSpecificCancellationTokenSrc.Token);
                }
            }
            finally
            {
                messageSpecificCancellationTokenSrc?.Dispose();
                channel.Writer.Complete();
            }

            async Task HandleIncomingMessage(string incomingMessage, CancellationToken messageSpecificCancellationToken)
            {
                StringBuilder assistantResponse = new();
                try
                {
                    chatMessages.Add(new(ChatRole.User, incomingMessage));

                    ChatOptions chatOptions = new()
                    {
                        Tools = [.. mcpTools,
                                AIFunctionFactory.Create(async (string emailAddress, string conversationHistory) =>
                                {
                                    if (messageSpecificCancellationToken.IsCancellationRequested)
                                        return;

                                    await using var scope = serviceProvider.CreateAsyncScope();

                                    await scope.ServiceProvider.GetRequiredService<TelegramBotService>()
                                        .SendContactUsMessage(emailAddress, conversationHistory, messageSpecificCancellationToken);

                                }, name: "AskForSupport", description: "Saves the user's email address and the conversation history for future support. Parameters: emailAddress (string), conversationHistory (string)"),
                                AIFunctionFactory.Create(async (string emailAddress, string conversationHistory) =>
                                {
                                    if (messageSpecificCancellationToken.IsCancellationRequested)
                                        return;

                                    await using var scope = serviceProvider.CreateAsyncScope();

                                    await scope.ServiceProvider.GetRequiredService<TelegramBotService>()
                                        .SendBuyPackageMessage("Default", emailAddress, conversationHistory, messageSpecificCancellationToken);

                                }, name: "AskForSales", description: "Saves the user's email address and the conversation history for future susales. Parameters: emailAddress (string), conversationHistory (string)")
                        ]
                    };

                    configuration.GetRequiredSection("AppSettings:ChatOptions").Bind(chatOptions);

                    const string supportSystemPrompt = """
                        You are a helpful AI assistant for the bitplatform community. Your primary role is to assist users with their questions and needs related to bitplatform.

                        **RESPONSE FORMAT:**
                        - Always format your responses using Markdown syntax
                        - Use proper Markdown formatting for all content including headers, lists, code blocks, links, and emphasis
                        - Format URLs using Markdown link syntax: [Link Text](URL)
                        - Use code blocks with appropriate language tags for code examples: ```csharp or ```html or ```css etc.
                        - Use headers (##, ###) to organize information when providing detailed responses
                        - Use **bold** for important points and *italics* for emphasis when appropriate
                        - Use bullet points (-) or numbered lists (1.) to organize information clearly

                        Please follow these guidelines based on the user's intent:

                        ## 1. For Complaints or Issues:
                           - If a user complains about something, reports a problem, mentions bugs, issues, errors, or expresses dissatisfaction
                           - Ask the user to provide their email address
                           - Once you have their email, call the AskForSupport tool with their email and the conversation history
                           - Be empathetic and assure them that their issue will be addressed

                        ## 2. For Sales and Purchasing:
                           - If a user wants to buy something, purchase a license, upgrade their plan, or inquires about pricing/commercial offerings
                           - Ask the user to provide their email address
                           - Once you have their email, call the AskForSales tool with their email and the conversation history
                           - Be helpful and professional about their business needs

                        ## 3. For General Questions and Technical Support:
                           - For all other questions about bitplatform features, documentation, how-to guides, best practices, or technical questions
                           - Use the ask_question tool to search the bitfoundation/bitplatform repository documentation
                           - Provide comprehensive answers based on the official documentation
                           - Format code examples with proper syntax highlighting
                           - Include relevant documentation links using Markdown format: [Documentation Title](URL)

                        **Important Notes:**
                        - Always be polite, professional, and helpful
                        - If you're unsure about the user's intent, ask clarifying questions
                        - When asking for email addresses, explain why you need it (for support follow-up or sales contact)
                        - Provide accurate information based on the official bitplatform documentation
                        - If you cannot find the answer in the documentation, be honest about limitations
                        - When referencing external resources, always use proper Markdown link formatting
                        - Structure your responses with clear headings and organized content

                        **Remember:** Your goal is to provide excellent customer service while efficiently routing users to the appropriate support channels. All responses must be well-formatted using Markdown syntax for optimal readability.
                        """;

                    await foreach (var response in chatClient.GetStreamingResponseAsync([
                        new (ChatRole.System, supportSystemPrompt),
                            .. chatMessages,
                            new (ChatRole.User, incomingMessage)
                        ], options: chatOptions, cancellationToken: messageSpecificCancellationToken))
                    {
                        if (messageSpecificCancellationToken.IsCancellationRequested)
                            break;

                        var result = response.Text;
                        assistantResponse.Append(result);
                        await channel.Writer.WriteAsync(result, messageSpecificCancellationToken);
                    }

                    await channel.Writer.WriteAsync(SharedChatProcessMessages.MESSAGE_RPOCESS_SUCESS, cancellationToken);
                }
                catch (Exception exp)
                {
                    _ = HandleException(exp);
                    await channel.Writer.WriteAsync(SharedChatProcessMessages.MESSAGE_RPOCESS_ERROR, cancellationToken);
                }
                finally
                {
                    chatMessages.Add(new(ChatRole.Assistant, assistantResponse.ToString()));
                }
            }
        }

        _ = ReadIncomingMessages();


        await foreach (var str in channel.Reader.ReadAllAsync(cancellationToken).WithCancellation(cancellationToken))
        {
            yield return str;
        }
    }

    private async Task HandleException(Exception exp)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var serverExceptionHandler = scope.ServiceProvider.GetRequiredService<IExceptionHandler>();
        serverExceptionHandler.Handle(exp);
    }
}



