//+:cnd:noEmit
using System.Text;
using System.ComponentModel;
using System.Threading.Channels;
using Boilerplate.Shared.Dtos.Chatbot;
using Boilerplate.Shared.Dtos.Diagnostic;
using Boilerplate.Server.Api.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.AspNetCore.SignalR;

namespace Boilerplate.Server.Api.SignalR;

/// <summary>
/// Service responsible for managing chatbot conversations, maintaining chat history,
/// and handling AI interactions including getting user feedbacks, describing app's features and pages etc.
/// This service is exposed over SignalR's AppHub.Chat.cs, so it can accept stream of user messages and return stream of AI responses using AiChatPanel.razor
/// It's also exposed as MCP tool over MinimalApi so you can add chatbot capabilities to any MCP-enabled client such as https://CrystaLive.ai and instruct each client differently,
/// based on your needs. For example you can customize CrystaLive.AI as a agent that would allow users to talk with their own voice and CrystaLive.AI would call MCP tool developed in <see cref="AppMcpService"/>
/// to get answers it need about the app.
/// </summary>
public partial class AppChatbot
{
    private IChatClient? chatClient = default!;

    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private HybridCache cache = default!;
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private ILogger<AppChatbot> logger = default!;
    [AutoInject] private IServiceProvider serviceProvider = default!;

    private string? supportSystemPrompt;
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

        supportSystemPrompt = await cache.GetOrCreateAsync(
            $"SystemPrompt_{PromptKind.Support}",
            async cancel =>
            {
                var prompt = await dbContext.SystemPrompts
                    .FirstOrDefaultAsync(p => p.PromptKind == PromptKind.Support, cancel);
                return prompt?.Markdown ?? throw new ResourceNotFoundException();
            },
            new()
            {
                Expiration = TimeSpan.FromHours(1),
                LocalCacheExpiration = TimeSpan.FromHours(1)
            },
            tags: ["SystemPrompts", $"SystemPrompt_{PromptKind.Support}"],
            cancellationToken: cancellationToken);

        supportSystemPrompt = supportSystemPrompt
            .Replace("{{UserCulture}}", culture?.NativeName ?? "English")
            .Replace("{{DeviceInfo}}", request.DeviceInfo ?? "Generic Device")
            .Replace("{{SignalRConnectionId}}", signalRConnectionId ?? "")
            .Replace("{{UserTimeZoneId}}", request.TimeZoneId ?? "Unknown");
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
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(supportSystemPrompt))
            throw new InvalidOperationException("Chat session must be started before processing messages. Call Start method first.");

        chatClient ??= serviceProvider.GetRequiredService<IChatClient>();

        StringBuilder assistantResponse = new();
        try
        {
            chatMessages.Add(new(ChatRole.User, incomingMessage));

            var chatOptions = CreateChatOptions(serverApiAddress, cancellationToken);

            await foreach (var response in chatClient.GetStreamingResponseAsync([
                new (ChatRole.System, supportSystemPrompt),
                    .. chatMessages,
                    new (ChatRole.User, incomingMessage)
                ], options: chatOptions, cancellationToken: cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var result = response.Text;
                assistantResponse.Append(result);
                await responseChannel.Writer.WriteAsync(result, cancellationToken);
            }

            await responseChannel.Writer.WriteAsync(SharedAppMessages.MESSAGE_RPOCESS_SUCCESS, cancellationToken);

            if (generateFollowUpSuggestions)
            {
                // Generate follow-up suggestions
                var followUpSuggestions = await GenerateFollowUpSuggestionsAsync(
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
            await responseChannel.Writer.WriteAsync(SharedAppMessages.MESSAGE_RPOCESS_ERROR, cancellationToken);
        }
        finally
        {
            chatMessages.Add(new(ChatRole.Assistant, assistantResponse.ToString()));
        }
    }

    /// <summary>
    /// Create chat options with AI tools
    /// </summary>
    private ChatOptions CreateChatOptions(Uri? serverApiAddress, CancellationToken cancellationToken)
    {
        var tools = new List<AIFunction>
        {
            AIFunctionFactory.Create(GetCurrentDateTime),
            AIFunctionFactory.Create(SaveUserEmailAndConversationHistory),
            AIFunctionFactory.Create(NavigateToPage),
            AIFunctionFactory.Create(SetCulture),
            AIFunctionFactory.Create(SetTheme),
            AIFunctionFactory.Create(CheckLastError),
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
    /// Returns the current date and time based on the user's timezone.
    /// </summary>
    [Description("Returns the current date and time based on the user's timezone.")]
    private string GetCurrentDateTime([Required, Description("User's timezone id")] string timeZoneId)
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            var userDateTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);

            return $"Current date/time in user's timezone ({timeZoneId}) is {userDateTime:o}";
        }
        catch
        {
            return $"Current date/time in utc is {DateTimeOffset.UtcNow:o}";
        }
    }

    /// <summary>
    /// Saves the user's email address and the conversation history for future reference.
    /// </summary>
    [Description("Saves the user's email address and the conversation history for future reference. Use this tool when the user provides their email address during the conversation.")]
    private async Task<string?> SaveUserEmailAndConversationHistory(
        [Required, Description("User's email address")] string emailAddress,
        [Required, Description("Full conversation history")] string conversationHistory)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            // Ideally, store these in a CRM or app database,
            // but for now, we'll log them!
            scope.ServiceProvider.GetRequiredService<ILogger<IChatClient>>()
                .LogError("Chat reported issue: User email: {emailAddress}, Conversation history: {conversationHistory}", emailAddress, conversationHistory);

            return "User email and conversation history saved successfully.";
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ServerExceptionHandler>().Handle(exp);
            return "Failed to save user email and conversation history.";
        }
    }

    /// <summary>
    /// Navigates the user to a specific page within the application.
    /// </summary>
    [Description("Navigates the user to a specific page within the application. Use this tool when the user requests to go to a particular section or feature of the app.")]
    private async Task<string?> NavigateToPage(
        [Required, Description("Page URL to navigate to")] string pageUrl,
        [Required, Description("SignalR connection id")] string signalRConnectionId)
    {
        if (string.IsNullOrEmpty(signalRConnectionId))
            return "There's no access to your app on your device";

        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            _ = await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId)
                .InvokeAsync<bool>(SharedAppMessages.NAVIGATE_TO, pageUrl, CancellationToken.None);

            return "Navigation completed";
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ServerExceptionHandler>().Handle(exp);
            return "Navigation failed";
        }
    }

    /// <summary>
    /// Changes the user's culture/language setting.
    /// </summary>
    [Description("Changes the user's culture/language setting. Use this tool when the user requests to change the app language. Common LCIDs: 1033=en-US, 1065=fa-IR, 1053=sv-SE, 2057=en-GB, 1043=nl-NL, 1081=hi-IN, 2052=zh-CN, 3082=es-ES, 1036=fr-FR, 1025=ar-SA, 1031=de-DE.")]
    private async Task<string?> SetCulture(
        [Required, Description("Culture LCID (e.g., 1033 for en-US, 1065 for fa-IR)")] int cultureLcid,
        [Required, Description("SignalR connection id")] string signalRConnectionId)
    {
        if (string.IsNullOrEmpty(signalRConnectionId))
            return "There's no access to your app on your device";

        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureLcid);

            if (CultureInfoManager.SupportedCultures.All(c => c.Culture.LCID != cultureLcid))
                return $"The requested culture is not supported. Available cultures: {string.Join(", ", CultureInfoManager.SupportedCultures.Select(c => c.Culture.NativeName))}";

            _ = await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId)
                .InvokeAsync<bool>(SharedAppMessages.CHANGE_CULTURE, cultureLcid, CancellationToken.None);

            return "Culture/Language changed successfully";
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ServerExceptionHandler>().Handle(exp);
            return "Failed to change culture/language";
        }
    }

    /// <summary>
    /// Changes the user's theme preference between light and dark mode.
    /// </summary>
    [Description("Changes the user's theme preference between light and dark mode. Use this tool when the user requests to change the app theme or appearance.")]
    private async Task<string?> SetTheme(
        [Required, Description("Theme name: 'light' or 'dark'")] string theme,
        [Required, Description("SignalR connection id")] string signalRConnectionId)
    {
        if (string.IsNullOrEmpty(signalRConnectionId))
            return "There's no access to your app on your device";

        if (theme != "light" && theme != "dark")
            return "Invalid theme. Use 'light' or 'dark'.";

        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            _ = await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId)
                .InvokeAsync<bool>(SharedAppMessages.CHANGE_THEME, theme, CancellationToken.None);

            return $"Theme changed to {theme} successfully";
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ServerExceptionHandler>().Handle(exp);
            return "Failed to change theme";
        }
    }

    /// <summary>
    /// Retrieves the last error that occurred on the user's device from the diagnostic logs.
    /// </summary>
    [Description("Retrieves the last error that occurred on the user's device from the diagnostic logs. Use this tool when troubleshooting user-reported issues, investigating application crashes, or when the user mentions something isn't working.")]
    private async Task<string?> CheckLastError(
        [Required, Description("SignalR connection id")] string signalRConnectionId)
    {
        if (string.IsNullOrEmpty(signalRConnectionId))
            return "There's no access to your app on your device";

        await using var scope = serviceProvider.CreateAsyncScope();

        try
        {
            var lastError = await scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>()
                .Clients.Client(signalRConnectionId)
                .InvokeAsync<DiagnosticLogDto?>(SharedAppMessages.UPLOAD_LAST_ERROR, CancellationToken.None);

            if (lastError is null)
                return "No errors found in the diagnostic logs.";

            return lastError.ToString();
        }
        catch (Exception exp)
        {
            serviceProvider.GetRequiredService<ServerExceptionHandler>().Handle(exp);
            return "Failed to retrieve error information from the device.";
        }
    }

    //#if (module == "Sales")
    //#if (database == "PostgreSQL" || database == "SqlServer")
    /// <summary>
    /// Searches for and recommends products based on user's needs and preferences.
    /// </summary>
    [Description("This tool searches for and recommends products based on a detailed description of the user's needs and preferences and returns recommended products.")]
    private async Task<object?> GetProductRecommendations(
        [Required, Description("Concise summary of user requirements")] string userNeeds,
        [Description("Car manufacturer's name (Optional)")] string? manufacturer,
        [Description("Car price below this value (Optional)")] decimal? maxPrice,
        [Description("Car price above this value (Optional)")] decimal? minPrice)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var productEmbeddingService = scope.ServiceProvider.GetRequiredService<ProductEmbeddingService>();
        var searchQuery = string.IsNullOrWhiteSpace(manufacturer)
            ? userNeeds
            : $"**{manufacturer}** {userNeeds}";

        // Get serverApiAddress from current context
        var httpContextAccessor = scope.ServiceProvider.GetService<IHttpContextAccessor>();
        var context = httpContextAccessor?.HttpContext;
        var request = context?.Request;
        Uri? serverApiAddress = request != null
            ? new Uri($"{request.Scheme}://{request.Host}")
            : null;

        var recommendedProducts = await (await productEmbeddingService.SearchProducts(searchQuery, context?.RequestAborted ?? default))
            .WhereIf(maxPrice.HasValue, p => p.Price <= maxPrice!.Value)
            .WhereIf(minPrice.HasValue, p => p.Price >= minPrice!.Value)
            .Take(10)
            .Project()
            .Select(p => new
            {
                p.Name,
                p.PageUrl,
                Manufacturer = p.CategoryName,
                Price = p.FormattedPrice,
                Description = p.DescriptionText,
                PreviewImageUrl = p.GetPrimaryMediumImageUrl(serverApiAddress!) ?? "_content/Boilerplate.Client.Core/images/car_placeholder.png"
            })
            .ToArrayAsync(context?.RequestAborted ?? default);

        return recommendedProducts;
    }
    //#endif
    //#endif

    /// <summary>
    /// Generate follow-up suggestions based on the conversation
    /// </summary>
    private async Task<AiChatFollowUpList> GenerateFollowUpSuggestionsAsync(
        string incomingMessage,
        string assistantResponse,
        ChatOptions chatOptions,
        CancellationToken cancellationToken)
    {
        // This would generate a list of follow-up questions/suggestions to keep the conversation going.
        // You could instead generate that list in previous chat completion call:
        // 1: Using "tools" or "functions" feature of the model, that would not consider the latest assistant response.
        // 2: Returning a json object containing the response and follow-up suggestions all together, losing IAsyncEnumerable streaming capability.  
        chatOptions.ResponseFormat = ChatResponseFormat.Json;
        chatOptions.AdditionalProperties = new() { ["response_format"] = new { type = "json_object" } };

        var followUpItems = await chatClient!.GetResponseAsync<AiChatFollowUpList>([
            new(ChatRole.System, supportSystemPrompt),
            new(ChatRole.User, incomingMessage),
            new(ChatRole.Assistant, assistantResponse),
            new(ChatRole.User, @"Return up to 3 relevant follow-up suggestions that help users discover related topics and continue the conversation naturally based on user's query in JSON object containing string[] named FollowUpSuggestions.
Only suggest follow-up questions that are within the assistant's scope and knowledge.
Do not suggest questions that require access to data or functionality that is unavailable or out of scope for this assistant.
Avoid suggesting questions that the assistant would not be able to answer."),],
            chatOptions, cancellationToken: cancellationToken);

        return followUpItems.Result ?? new AiChatFollowUpList();
    }
}
