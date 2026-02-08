//+:cnd:noEmit
using System.Text;
using System.Threading.Channels;
using Boilerplate.Shared.Features.Chatbot;
using Microsoft.AspNetCore.Authentication.BearerToken;

namespace Boilerplate.Server.Api.Infrastructure.SignalR;

/// <summary>
/// Service responsible for managing chatbot conversations, maintaining chat history,
/// and handling AI interactions including getting user feedbacks, describing app's features and pages etc.
/// This service is exposed over SignalR's AppHub.Chat.cs, so it can accept stream of user messages and return stream of AI responses using AiChatPanel.razor
/// Every tool method is decorated with [McpServerTool] attribute, so it can be also be used by other external MCP-Client if needed (Checkout AppChatbot.Tools.cs)
/// 
/// Microsoft.Agents.AI:
/// Workflows are not implemented in this project, but with AIAgent, achieving them is now easier compared to using IChatClient directly.
/// For example, it would be better to have separate Agents: one for product search, one for support, and one for app guidance.
/// A coordinator Agent could determine which specialized Agent to delegate the task to based on the user's message,
/// rather than having a single Agent with a very long System Prompt and many Tools.
/// </summary>
public partial class AppChatbot
{
    private IChatClient? chatClient = default!;

    [AutoInject] private IFusionCache cache = default!;
    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private ILogger<AppChatbot> logger = default!;
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private IServiceProvider serviceProvider = default!;
    [AutoInject] private IOptionsMonitor<BearerTokenOptions> bearerTokenOptions = default!;

    private string? variablesDefault;
    private string? supportSystemPrompt;
    private string? signalRConnectionId;
    private List<ChatMessage> chatMessages = [];

    /// <summary>
    /// This is a heart of streaming AI responses back to the client.
    /// </summary>
    private readonly Channel<string> responseChannel = Channel.CreateUnbounded<string>(new() { SingleReader = true, SingleWriter = true });

    /// <summary>
    /// Starts the chat session with history and system prompt
    /// </summary>
    public async Task StartChat(
        StartChatRequest request,
        string? signalRConnectionId,
        CancellationToken cancellationToken)
    {
        chatMessages = [.. request.ChatMessagesHistory.Select(c => new ChatMessage(c.Role is AiChatMessageRole.Assistant ? ChatRole.Assistant : ChatRole.User, c.Content))];

        CultureInfo? culture = null;
        if (request.CultureId is not null && CultureInfoManager.InvariantGlobalization is false)
        {
            culture = CultureInfo.GetCultureInfo(request.CultureId.Value);
        }

        supportSystemPrompt = await cache.GetOrSetAsync(
            $"SystemPrompt_{PromptKind.Support}",
            async cancel =>
            {
                var prompt = await dbContext.SystemPrompts
                    .FirstOrDefaultAsync(p => p.PromptKind == PromptKind.Support, cancel);
                return prompt?.Markdown ?? throw new ResourceNotFoundException();
            },
            new FusionCacheEntryOptions()
            {
                Duration = TimeSpan.FromHours(1)
            },
            token: cancellationToken);

        // The following variables won't change unless SignalR connection restarts and StartChat gets called again, so setting variables once here is sufficient.
        // For example, the user's culture won't change unless they restart the app.
        variablesDefault = @$"
{{{{UserCulture}}}}: ""{culture?.NativeName ?? "English"}""
{{{{DeviceInfo}}}}: ""{request.DeviceInfo ?? "Generic Device"}""
{{{{UserTimeZoneId}}}}: ""{request.TimeZoneId ?? "Unknown"}""
";

        this.signalRConnectionId = signalRConnectionId;
    }

    /// <summary>
    /// Get the response channel for streaming AI responses
    /// </summary>
    public ChannelReader<string> GetStreamingChannel() => responseChannel.Reader;

    /// <summary>
    /// Stops streaming
    /// </summary>
    public void Stop() => responseChannel.Writer.Complete();

    /// <summary>
    /// Process an incoming message and stream the AI response
    /// </summary>
    public async Task ProcessNewMessage(
        bool generateFollowUpSuggestions,
        string incomingMessage,
        Uri? serverApiAddress,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(supportSystemPrompt))
            throw new InvalidOperationException("Chat session must be started before processing messages. Call Start method first.");

        chatClient ??= serviceProvider.GetRequiredService<IChatClient>();

        var supportAgent = chatClient.AsAIAgent(
            instructions: supportSystemPrompt,
            name: "SupportAgent",
            description: "Provides user support, answers questions, and assists with app features and troubleshooting");

        StringBuilder assistantResponse = new();
        try
        {
            chatMessages.Add(new(ChatRole.User, incomingMessage));

            var chatOptions = CreateChatOptions();

            // The following variables might change without SignalR connection restarts, so these should set here every time a new message is about to be processed.
            // For example, user can sign-in/sign-out during chat without restarting the app or SignalR connection.
            // User can change these values using prompt injection, so it's important not to rely on these variables for critical logic or security decisions,
            // but rather use them for providing better context to the model to generate more relevant responses.
            // You can either check if user is authenticated or not at a time tools are being called, or add dedicated tool so the LLM model would call it to figure out the user's authentication state instead of relying on variables.
            var variablesPrompt = @$"
### Variables:
{variablesDefault}
{{{{IsAuthenticated}}}}: ""{user.IsAuthenticated()}""}} 
{{{{UserEmail}}}}: ""{(user.IsAuthenticated() ? user!.GetEmail()?.ToString() : "null")}""
";

            await foreach (var response in supportAgent.RunStreamingAsync([
                new (ChatRole.System, variablesPrompt),
                .. chatMessages,
                ], options: new Microsoft.Agents.AI.ChatClientAgentRunOptions(chatOptions), cancellationToken: cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var result = response.Text;
                assistantResponse.Append(result);
                await responseChannel.Writer.WriteAsync(result, cancellationToken);
            }

            await responseChannel.Writer.WriteAsync(SharedAppMessages.MESSAGE_PROCESS_SUCCESS, cancellationToken);

            if (generateFollowUpSuggestions)
            {
                // Generate follow-up suggestions
                var followUpSuggestions = await GenerateFollowUpSuggestions(
                    incomingMessage,
                    assistantResponse.ToString(),
                    chatOptions,
                    cancellationToken);

                await responseChannel.Writer.WriteAsync(JsonSerializer.Serialize(followUpSuggestions), cancellationToken);
            }
        }
        catch (Exception exp)
        {
            logger.LogError(exp, "Error processing message in chatbot service");
            await responseChannel.Writer.WriteAsync(SharedAppMessages.MESSAGE_PROCESS_ERROR, cancellationToken);
        }
        finally
        {
            chatMessages.Add(new(ChatRole.Assistant, assistantResponse.ToString()));
        }
    }

    /// <summary>
    /// Create chat options with AI tools
    /// </summary>
    private ChatOptions CreateChatOptions()
    {
        var tools = new List<AIFunction>
        {
            AIFunctionFactory.Create(GetCurrentDateTime),
            AIFunctionFactory.Create(SaveUserEmailAndConversationHistory),
            AIFunctionFactory.Create(NavigateToPage),
            AIFunctionFactory.Create(ShowSignInModal),
            AIFunctionFactory.Create(SetCulture),
            AIFunctionFactory.Create(SetTheme),
            AIFunctionFactory.Create(CheckLastError),
            AIFunctionFactory.Create(ClearAppFiles),
            //#if (module == "Sales")
            //#if (database == "PostgreSQL" || database == "SqlServer")
            AIFunctionFactory.Create(GetProductRecommendations)
            //#endif
            //#endif
        };

        var chatOptions = new ChatOptions { Tools = [.. tools] };
        configuration.GetRequiredSection("AI:ChatOptions").Bind(chatOptions);
        return chatOptions;
    }

    /// <summary>
    /// Generate follow-up suggestions based on the conversation
    /// </summary>
    private async Task<AiChatFollowUpList> GenerateFollowUpSuggestions(
        string incomingMessage,
        string assistantResponse,
        ChatOptions chatOptions,
        CancellationToken cancellationToken)
    {
        // This would generate a list of follow-up questions/suggestions to keep the conversation going.
        // You could instead generate that list in previous chat completion call:
        // 1: Using "tools" or "functions" feature of the model, that would not consider the latest assistant response.
        // 2: Returning a json object containing the response and follow-up suggestions all together, losing IAsyncEnumerable streaming capability.

        var followUpAgent = chatClient!.AsAIAgent(
            instructions: """
            You are a Follow-Up Suggestion Agent. Your role is to generate natural, contextual follow-up questions or actions for users.

            ANALYSIS PROCESS:
            1. Review the conversation context carefully
            2. Identify logical next steps or questions the user might ask
            3. Ensure suggestions are within the assistant's capabilities
            4. Make suggestions actionable and user-centric

            RESPONSE FORMAT:
            Return ONLY a JSON object with:
            - "FollowUpSuggestions": array of exactly 3 strings

            VALIDATION RULES:
            - Only suggest follow-up actions/questions that are within the assistant's scope and knowledge
            - Do not suggest questions that require access to data or functionality that is unavailable or out of scope
            - Avoid suggesting questions that the assistant would not be able to answer
            - Written from the user's perspective (never from the assistant)
            - Direct, natural, clickable actions/questions
            - Keep each suggestion concise (under 60 characters)
            """,
            name: "FollowUpSuggestionAgent",
            description: "Generates contextual follow-up suggestions to keep conversations flowing naturally");

        chatOptions.ResponseFormat = ChatResponseFormat.Json;
        chatOptions.AdditionalProperties = new() { ["response_format"] = new { type = "json_object" } };

        var followUpItems = await followUpAgent.RunAsync<AiChatFollowUpList>(
            messages: [
                new(ChatRole.System, supportSystemPrompt),
                new(ChatRole.User, incomingMessage),
                new(ChatRole.Assistant, assistantResponse),
                new(ChatRole.User, "Generate 3 short follow-up suggestions for what I might want to ask or do next.")
            ],
            cancellationToken: cancellationToken,
            options: new Microsoft.Agents.AI.ChatClientAgentRunOptions(chatOptions));

        return followUpItems.Result ?? new AiChatFollowUpList();
    }

    private async Task EnsureSignalRConnectionIdIsPresent()
    {
        // If the AIFunction tool is getting called by the IChatClient, the signalRConnectionId is already set in the AppChatbot instance using
        // StartChat method, so we can return it directly without querying the database again.

        // The SignalRConnectionId gives access to the currently exposed SignalR Client methods (e.g., NavigateToPage, ShowSignInModal)
        // that are essential for some of the AI tools to work properly, so it's important to ensure that we have it available when processing AI tool calls.

        // If the AIFunction tool is getting called by an external MCP client, then the signalRConnectionId won't be set,
        // so we need to query the database to get the active SignalR connection id for the current user session, assuming that the external MCP client is using authentication headers.

        if (string.IsNullOrEmpty(signalRConnectionId) is false)
            return;

        await using var scope = serviceProvider.CreateAsyncScope();
        var httpContextAccessor = scope.ServiceProvider.GetService<IHttpContextAccessor>();

        if (httpContextAccessor?.HttpContext?.User?.IsAuthenticated() is false)
            throw new UnauthorizedException("User must be authenticated to use this tool when calling from an external MCP client.");
        // While these tools can be called internally even for unauthenticated users,
        // we require authentication for external MCP clients to ensure we can associate the request with a user session and retrieve the correct SignalR connection id.
        // accepting SignalR connection id from external MCP clients would not be secure as it can be easily manipulated using prompt injection in external LLM that's calling the MCP tool.

        var userSessionId = httpContextAccessor?.HttpContext?.User.GetSessionId();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        signalRConnectionId = await dbContext.UserSessions
            .Where(s => s.Id == userSessionId)
            .Select(s => s.SignalRConnectionId)
            .FirstOrDefaultAsync() ?? throw new InvalidOperationException("There's no access to your app on your device.");
    }
}
