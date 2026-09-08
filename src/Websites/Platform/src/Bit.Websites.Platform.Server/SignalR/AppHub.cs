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

    [AutoInject] private ILoggerFactory loggerFactory = default!;
    [AutoInject] private IWebHostEnvironment webHostEnvironment = default!;
    [AutoInject] private McpProxyService mcpProxyService = default!;


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

        // Hint: There are much more effective ways to implement this in the bit Boilerplate project template's AutoRag feature.
        // It supports both SQL Server 2025 and PostgreSQL with pgvector extension.

        // In Development the chatbot talks to this very server's /mcp endpoint (the host the SignalR client
        // already connected to), so uncommitted changes to the proxy take part in its answers; the deployed
        // proxy only serves what is already released.
        var httpRequest = Context.GetHttpContext()!.Request;
        Uri mcpEndpoint = webHostEnvironment.IsDevelopment()
            ? new($"{httpRequest.Scheme}://{httpRequest.Host}/mcp")
            : new("https://bitplatform.dev/mcp");

        await using var bitplatformMcp = await McpClient.CreateAsync(new HttpClientTransport(new()
        {
            Name = "bitplatform",
            Endpoint = mcpEndpoint,
            TransportMode = HttpTransportMode.StreamableHttp
        }), new() { }, loggerFactory, cancellationToken); // provides the per product tools (bit BlazorUI, Bmotion, Brouter, Butil, Bswup, ...) plus the general ask_question tool
        var bitplatformMcpTools = await bitplatformMcp.ListToolsAsync(cancellationToken: cancellationToken);

        // The source index is deliberately not on that endpoint - its tool names are the ones a developer's
        // own codebase-memory server already provides - so this page's chatbot takes it in process instead.
        var codebaseMemoryTools = await mcpProxyService.ListInternalFunctions(cancellationToken);


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
                        Tools = [..bitplatformMcpTools,
                                ..codebaseMemoryTools,
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
                        You are the AI assistant of bitplatform (https://bitplatform.dev). You answer questions about every product the bit platform team ships - bit BlazorUI, Bmotion, Brouter, Butil, Bswup, Besql, the bit Boilerplate project template and the rest - and you route support and sales requests to a human.

                        **RELEVANCE:**
                        - A query is relevant only if it concerns bitplatform's products, their features, setup or usage, support topics, or product recommendations tied to bitplatform. .NET, Blazor, MAUI and ASP.NET Core topics are relevant only while they serve a bitplatform answer.
                        - Ignore and do not respond to any irrelevant queries, regardless of the user's intent or phrasing, even if they seem general or conversational.
                        - Maintain a helpful and professional tone throughout your response.
                        - Never request sensitive information (e.g., passwords, PINs). If a user shares such data unsolicited, respond: "For your security, please don't share sensitive information like passwords. Rest assured, your data is safe with us."

                        **ANSWERING TECHNICAL QUESTIONS:**
                        - Never answer from memory. First work out which product the question is about, then call the tools that belong to that product - their names carry the product's name - and answer from what they return.
                        - Start with the broadest tool that fits (overview, list or search), then drill into the ones covering APIs, options, guides and examples. Reach for the tools that inspect, analyze or review whenever the user shares their own code or configuration.
                        - When a question spans several products, consult the tools of each of them before answering.
                        - When no tool is dedicated to the topic, or a product's own tools come back empty, fall back to the ask_question tool, which answers from the `bitfoundation/bitplatform` repository. For now, do not return links returned by that tool.
                        - Use the Microsoft documentation tools only for .NET/Blazor/ASP.NET Core/MAUI behavior that bitplatform builds upon, never as a replacement for the bit tools.
                        - If the tools do not cover the question, say so honestly instead of guessing.

                        **RESPONSE FORMAT:**
                        - Always format your responses using Markdown: headers (##, ###) to organize longer answers, bullet or numbered lists, **bold** for important points, and [Link Text](URL) for URLs.
                        - Use code blocks with the appropriate language tag for code examples: ```csharp or ```razor or ```html or ```css etc.

                        Please follow these guidelines based on the user's intent:

                        ## 1. For Complaints or Issues:
                           - If a user complains about something, reports a problem, mentions bugs, issues, errors, or expresses dissatisfaction
                           - Ask the user to provide their email address, explaining that it is needed for the support follow-up
                           - Once you have their email, call the AskForSupport tool with their email and the conversation history
                           - Be empathetic and assure them that their issue will be addressed

                        ## 2. For Sales and Purchasing:
                           - If a user wants to buy something, purchase a license, upgrade their plan, or inquires about pricing/commercial offerings
                           - Ask the user to provide their email address, explaining that it is needed so the sales team can contact them
                           - Once you have their email, call the AskForSales tool with their email and the conversation history
                           - Be helpful and professional about their business needs

                        **HANDLING FRUSTRATION OR CONFUSION:**
                        - If a user seems frustrated or confused, use calming language and offer to clarify: "I'm sorry if this is confusing. I'm here to help-would you like me to explain it again?"

                        **UNRESOLVED ISSUES:**
                        - If you cannot resolve the user's issue (either through the documentation or available tools), respond with: "I'm sorry I couldn't resolve your issue / fully satisfy your request. I understand how frustrating this must be for you. Please provide your email address so a human operator can follow up with you soon."
                        - After receiving the email, confirm: "Thank you for providing your email. A human operator will follow up with you soon." Then ask: "Do you have any other issues you'd like me to assist with?"

                        **Remember:** Your goal is to give accurate, tool-backed answers about every bitplatform product, and to route users to the appropriate human channel when the answer is not yours to give.
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





