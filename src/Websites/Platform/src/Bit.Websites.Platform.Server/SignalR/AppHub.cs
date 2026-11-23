using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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
    private const int maxToolsCount = 128;

    /// <summary>
    /// The bit libraries whose documentation site serves an MCP server of its own, at
    /// https://{name in lowercase}.bitplatform.dev/mcp. The chatbot answers from their tools.
    /// </summary>
    private static readonly string[] librariesWithMcpServers =
    [
        "Bmotion", "Bswup", "Brouter", "Bmage", "Bink", "Balette", "BbScanner", "Bdnd", "BlazeDb",
        "Blow", "Borm", "Bcrdt", "Bonboarding", "Bortable", "Blex", "Bsqlite", "Belemetry", "Bheadless"
    ];

    [AutoInject] private IServiceProvider serviceProvider = default!;
    [AutoInject] private IConfiguration configuration = default!;

    [AutoInject] private ILoggerFactory loggerFactory = default!;
    [AutoInject] private ILogger<AppHub> logger = default!;


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

        // Every library's own MCP server, connected to in parallel. One that is down or slow only takes its own
        // tools out of this chat.
        await using var libraryMcpServers = await ConnectToLibraryMcpServers(cancellationToken);
        var libraryTools = (await Task.WhenAll(libraryMcpServers.Clients.Select(client => ListTools(client, cancellationToken))))
            .SelectMany(tools => tools)
            .OrderBy(tool => GetChatbotPriority(tool.Name))
            .ToArray();


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

                    AITool[] chatbotTools = [
                        AIFunctionFactory.Create(async (string emailAddress, string conversationHistory) =>
                        {
                            if (messageSpecificCancellationToken.IsCancellationRequested)
                                return;

                            await using var scope = serviceProvider.CreateAsyncScope();

                            await scope.ServiceProvider.GetRequiredService<TelegramBotService>()
                                .SendContactUsMessage(emailAddress, conversationHistory, messageSpecificCancellationToken);

                        }, name: "AskForSupport", description: "Saves the user's email address and the conversation history for future support. Parameters: emailAddress (string), conversationHistory (string)")
                    ];

                    ChatOptions chatOptions = new()
                    {
                        Tools = [.. libraryTools.Take(maxToolsCount - chatbotTools.Length), .. chatbotTools]
                    };

                    configuration.GetRequiredSection("AppSettings:ChatOptions").Bind(chatOptions);

                    var supportSystemPrompt = $"""
                        You are the AI assistant of bitplatform (https://bitplatform.dev). You answer questions about the open-source libraries the bit platform team ships - bit BlazorUI, bit Butil, bit {string.Join(", bit ", librariesWithMcpServers)} and the build tools (Minifier, BlazorES2019, Code Analyzers and Source Generators) - and you route support requests to a human.

                        **RELEVANCE:**
                        - A query is relevant only if it concerns bitplatform's products, their features, setup or usage, support topics, or product recommendations tied to bitplatform. .NET, Blazor, MAUI and ASP.NET Core topics are relevant only while they serve a bitplatform answer.
                        - Ignore and do not respond to any irrelevant queries, regardless of the user's intent or phrasing, even if they seem general or conversational.
                        - Maintain a helpful and professional tone throughout your response.
                        - Never request sensitive information (e.g., passwords, PINs). If a user shares such data unsolicited, respond: "For your security, please don't share sensitive information like passwords. Rest assured, your data is safe with us."

                        **ANSWERING TECHNICAL QUESTIONS:**
                        - Never answer from memory. First work out which library the question is about, then call the tools that belong to that library - their names carry the library's name - and answer from what they return.
                        - Start with the broadest tool that fits (overview, list or search), then drill into the ones covering APIs, options, guides and examples. Reach for the tools that inspect, analyze or review whenever the user shares their own code or configuration.
                        - When a question spans several libraries, consult the tools of each of them before answering.
                        - You have no tools for bit BlazorUI and bit Butil. Point the user to their documentation at https://blazorui.bitplatform.dev and https://butil.bitplatform.dev instead of answering from memory.
                        - Each library you have tools for also has its MCP server at https://<name in lowercase>.bitplatform.dev/mcp, such as https://bmotion.bitplatform.dev/mcp, which the user's own coding agent can use.
                        - bit Boilerplate, the project template, and bit CLI are part of bit Fabric (https://bitfabric.co), which builds whole apps on these libraries. For questions about them, point the user to https://bitfabric.co.
                        - If the tools do not cover the question, say so honestly instead of guessing.

                        **RESPONSE FORMAT:**
                        - Always format your responses using Markdown: headers (##, ###) to organize longer answers, bullet or numbered lists, **bold** for important points, and [Link Text](URL) for URLs.
                        - Use code blocks with the appropriate language tag for code examples: ```csharp or ```razor or ```html or ```css etc.

                        **COMPLAINTS OR ISSUES:**
                        - If a user complains about something, reports a problem, mentions bugs, issues, errors, or expresses dissatisfaction
                        - Ask the user to provide their email address, explaining that it is needed for the support follow-up
                        - Once you have their email, call the AskForSupport tool with their email and the conversation history
                        - Be empathetic and assure them that their issue will be addressed

                        **HANDLING FRUSTRATION OR CONFUSION:**
                        - If a user seems frustrated or confused, use calming language and offer to clarify: "I'm sorry if this is confusing. I'm here to help-would you like me to explain it again?"

                        **UNRESOLVED ISSUES:**
                        - If you cannot resolve the user's issue (either through the documentation or available tools), respond with: "I'm sorry I couldn't resolve your issue / fully satisfy your request. I understand how frustrating this must be for you. Please provide your email address so a human operator can follow up with you soon."
                        - After receiving the email, confirm: "Thank you for providing your email. A human operator will follow up with you soon." Then ask: "Do you have any other issues you'd like me to assist with?"

                        **Remember:** Your goal is to give accurate, tool-backed answers about the bitplatform libraries, and to route users to the appropriate human channel when the answer is not yours to give.
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

    private async Task<LibraryMcpServers> ConnectToLibraryMcpServers(CancellationToken cancellationToken)
    {
        var clients = await Task.WhenAll(librariesWithMcpServers.Select(async library =>
        {
            try
            {
                return await McpClient.CreateAsync(new HttpClientTransport(new()
                {
                    Name = $"bit{library}",
                    Endpoint = new($"https://{library.ToLowerInvariant()}.bitplatform.dev/mcp"),
                    TransportMode = HttpTransportMode.StreamableHttp
                }), new() { InitializationTimeout = TimeSpan.FromSeconds(10) }, loggerFactory, cancellationToken);
            }
            catch (Exception exp) when (cancellationToken.IsCancellationRequested is false)
            {
                logger.LogWarning(exp, "The chatbot could not connect to the MCP server of {Library}.", library);
                return null;
            }
        }));

        return new([.. clients.OfType<McpClient>()]);
    }

    private async Task<IList<McpClientTool>> ListTools(McpClient client, CancellationToken cancellationToken)
    {
        try
        {
            return await client.ListToolsAsync(cancellationToken: cancellationToken);
        }
        catch (Exception exp) when (cancellationToken.IsCancellationRequested is false)
        {
            logger.LogWarning(exp, "The chatbot could not list the tools of {Server}.", client.ServerInfo?.Name);
            return [];
        }
    }

    private static int GetChatbotPriority(string toolName) => toolName switch
    {
        _ when Regex.IsMatch(toolName, "^(Inspect|Simulate|Analyze|Run|Review|Check|Plan|Compare|Generate)") => 4,
        _ when Regex.IsMatch(toolName, "^Search|Overview$") => 0,
        _ when Regex.IsMatch(toolName, "Guide|Docs?Page$|Document$|Api") => 1,
        _ when Regex.IsMatch(toolName, "SourceFile$|Recipes?$") => 2,
        _ => 3
    };

    private async Task HandleException(Exception exp)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var serverExceptionHandler = scope.ServiceProvider.GetRequiredService<IExceptionHandler>();
        serverExceptionHandler.Handle(exp);
    }

    /// <summary>The connections one chat holds to the libraries' MCP servers, closed together when it ends.</summary>
    private sealed class LibraryMcpServers(McpClient[] clients) : IAsyncDisposable
    {
        public McpClient[] Clients => clients;

        public async ValueTask DisposeAsync()
        {
            foreach (var client in clients)
            {
                await client.DisposeAsync();
            }
        }
    }
}
