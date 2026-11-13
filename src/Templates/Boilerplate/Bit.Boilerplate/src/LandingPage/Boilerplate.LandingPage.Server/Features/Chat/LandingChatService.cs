using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.RateLimiting;
using Microsoft.Extensions.AI;

namespace Boilerplate.LandingPage.Server.Features.Chat;

/// <summary>
/// Answers the visitors' questions, in the events of Bit.BlazorUI.AI that the chat renders: about one sample, from the
/// facts in <see cref="LandingChatPrompts"/>, or - in the gallery's own chat - about all of them.
/// </summary>
public sealed partial class LandingChatService(IServiceProvider serviceProvider,
    LandingServerSettings settings,
    LandingChatQuestionLimiter questionLimiter,
    IStringLocalizer<LandingStrings> localizer,
    IStringLocalizerFactory localizerFactory,
    ILogger<LandingChatService> logger)
{
    /// <summary>The longest question; the chat's text box takes no more (See LandingChatSurface).</summary>
    public const int MaxTurnLength = 1_000;

    /// <summary>The longest answer sent back as part of the conversation: well over what AI:ChatOptions:MaxOutputTokens lets the model write.</summary>
    public const int MaxAnswerLength = 8_000;

    /// <summary>
    /// Answers a question once it's within the visitor's allowance (See LandingChatEndpoints.AddLandingChat). The one way in
    /// for the socket and for Blazor Server alike, so neither asks the model more often than the other.
    /// </summary>
    public IAsyncEnumerable<BitAiEvent> Answer(BitAiRequest request, string visitor, CancellationToken cancellationToken)
    {
        using var lease = questionLimiter.Limiter.AttemptAcquire(visitor);

        if (lease.IsAcquired is false)
        {
            var culture = LandingCultures.Find(ContextValue(request, LandingChatContext.Culture)) ?? LandingCultures.Default;

            return Only(Failure(localizer.In(culture, nameof(LandingStrings.ChatTooManyQuestions)), retryable: false));
        }

        return StreamAsync(request, cancellationToken);
    }

    public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var culture = LandingCultures.Find(ContextValue(request, LandingChatContext.Culture)) ?? LandingCultures.Default;
        var siteKey = ContextValue(request, LandingChatContext.Site);
        var site = LandingSites.Find(siteKey);
        var isGallery = siteKey == LandingChatContext.Gallery;

        // Bounds what one question can send the model, and the last turn has to be the visitor's own question
        if ((site is null && isGallery is false) || Turns(request) is not { } turns)
        {
            yield return Failure(localizer.In(culture, nameof(LandingStrings.ChatInvalidQuestion)), retryable: false);
            yield break;
        }

        // Without an AI key a sample answer streams word by word, so the chat works on a fresh project too
        if (serviceProvider.GetService<IChatClient>() is not { } chatClient)
        {
            yield return new BitAiMessageStartedEvent();

            foreach (var word in localizer.In(culture, nameof(LandingStrings.ChatNoModelAnswer), site?.Name ?? localizer.In(culture, nameof(LandingStrings.GalleryEyebrow))).Split(' '))
            {
                await Task.Delay(Random.Shared.Next(25, 70), cancellationToken);
                yield return new BitAiTextDeltaEvent($"{word} ");
            }

            yield return new BitAiMessageCompletedEvent();
            yield break;
        }

        List<ChatMessage> messages = [new(ChatRole.System, isGallery ? LandingChatPrompts.GetGallery(culture, localizerFactory) : LandingChatPrompts.Get(site!, culture))];
        messages.AddRange(turns);

        // A copy for each question, as ChatOptions is mutable and the settings' instance is shared by every request
        var options = settings.AI.ChatOptions.Clone();

        if (isGallery)
        {
            options.Tools = [AIFunctionFactory.Create(ShowSamples, LandingChatContext.ShowSamplesTool)];
        }

        await using var events = chatClient.GetStreamingResponseAsync(messages, options, cancellationToken)
                                           .ToBitAiEventsAsync(cancellationToken)
                                           .GetAsyncEnumerator(cancellationToken);

        // C# can't yield inside a try with a catch, so only MoveNextAsync is guarded and the event is yielded below
        while (true)
        {
            BitAiEvent? evt = null;

            try
            {
                if (await events.MoveNextAsync() is false)
                    yield break;

                evt = events.Current;
            }
            catch (Exception exp) when (exp is not OperationCanceledException)
            {
                LogModelFailed(logger, exp, siteKey!);
            }

            // A failure, or the provider reporting one of its own, which says nothing a visitor can act on (and in English)
            if (evt is null or BitAiErrorEvent)
            {
                if (evt is BitAiErrorEvent reported)
                {
                    LogModelReportedError(logger, siteKey!, reported.Error.Code);
                }

                yield return Failure(localizer.In(culture, nameof(LandingStrings.ChatFailed)), retryable: true);
                yield break;
            }

            yield return evt;
        }
    }

    [Description("Shows the visitor the samples you recommend as cards they can open, best match first. Call it whenever you recommend samples, then say in a sentence or two why each one fits. Never write the samples' links yourself.")]
    private static string[] ShowSamples([Description("The keys of the samples to show, best match first, at most 3")] string[] keys)
        => [.. keys.Select(LandingSites.Find).OfType<LandingSite>().Select(site => site.Key).Distinct().Take(3)];

    /// <summary>
    /// The newest turns of the conversation, as the model reads them (See LandingChatContext.MaxTurns), or null when one of
    /// them is too long or the last isn't the visitor's question. Older turns are left out rather than refused, so a long
    /// conversation goes on.
    /// </summary>
    private static ChatMessage[]? Turns(BitAiRequest request)
    {
        // A message the visitor stopped half way still says what it said; tool cards and the like aren't words of the conversation
        ChatMessage[] turns = [.. request.Messages.Where(message => message.Role.IsUser || message.Role.IsAssistant)
                                                  .Select(message => new ChatMessage(message.Role.IsUser ? ChatRole.User : ChatRole.Assistant, message.GetText().Trim()))
                                                  .Where(turn => turn.Text.Length > 0)
                                                  .TakeLast(LandingChatContext.MaxTurns)];

        if (turns is not { Length: > 0 } || turns[^1].Role != ChatRole.User)
            return null;

        return turns.All(turn => turn.Text.Length <= (turn.Role == ChatRole.User ? MaxTurnLength : MaxAnswerLength)) ? turns : null;
    }

    private static string? ContextValue(BitAiRequest request, string key)
        => request.Context?.GetValueOrDefault(key) switch
        {
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } json => json.GetString(),
            _ => null
        };

    public static BitAiErrorEvent Failure(string message, bool retryable) => new(new BitAiError(message) { IsRetryable = retryable });

#pragma warning disable CS1998 // An iterator of one event needs no await
    private static async IAsyncEnumerable<BitAiEvent> Only(BitAiEvent evt)
    {
        yield return evt;
    }
#pragma warning restore CS1998

    [LoggerMessage(Level = LogLevel.Error, Message = "The chat of {Site} couldn't get an answer from the model")]
    private static partial void LogModelFailed(ILogger logger, Exception exception, string site);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The model answering the chat of {Site} reported the error {ErrorCode}")]
    private static partial void LogModelReportedError(ILogger logger, string site, string? errorCode);
}

/// <summary>
/// The chat's server end under Blazor Server, where the components already run on the server (See ILandingChatBackend). Its
/// questions count against the same allowance as the socket's: the visitor is told by the address of the circuit's
/// connection, read as the chat first asks, and a circuit that can't tell counts as a visitor of its own.
/// </summary>
public sealed class DirectLandingChatBackend(LandingChatService chatService, IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider) : ILandingChatBackend
{
    private readonly string visitor = httpContextAccessor.HttpContext?.GetClientIp() ?? $"circuit-{Guid.NewGuid():N}";

    public async IAsyncEnumerable<string> StreamAsync(string request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var question = BitAiWireSerializer.ToRequest(BitAiWireSerializer.DeserializeRequest(request), services: serviceProvider);

        await foreach (var evt in chatService.Answer(question, visitor, cancellationToken))
        {
            yield return BitAiWireSerializer.Serialize(evt);
        }
    }
}

public static partial class LandingChatEndpoints
{
    public static IServiceCollection AddLandingChat(this IServiceCollection services, LandingServerSettings settings)
    {
        services.AddScoped<LandingChatService>();
        services.AddScoped<ILandingChatBackend, DirectLandingChatBackend>();

        // The endpoint's own limit counts sockets and bounds them alone; this one counts questions, and answers with words the
        // visitor can read rather than a refused connection, so it runs out first
        services.AddSingleton(new LandingChatQuestionLimiter(PartitionedRateLimiter.Create<string, string>(ip => RateLimitPartition.GetSlidingWindowLimiter(ip, _ => new()
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(10),
            SegmentsPerWindow = 5,
            QueueLimit = 0
        }))));

        if (string.IsNullOrWhiteSpace(settings.AI.OpenAI.ChatApiKey) is false)
        {
            services.AddHttpClient("AI");

#pragma warning disable OPENAI001 // The Responses api's client is still marked experimental.
            var chatClient = services.AddChatClient(sp =>
            {
                var transport = new HttpClientPipelineTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AI"));

                // See AI:OpenAI:ChatUseResponsesApi comment in LandingPage.Server's appsettings.json
                return settings.AI.OpenAI.ChatUseResponsesApi
                    ? new OpenAI.Responses.ResponsesClient(credential: new(settings.AI.OpenAI.ChatApiKey), options: new()
                    {
                        Endpoint = settings.AI.OpenAI.ChatEndpoint,
                        Transport = transport
                    }).AsIChatClient(settings.AI.OpenAI.ChatModel)
                    : new OpenAI.Chat.ChatClient(model: settings.AI.OpenAI.ChatModel, credential: new(settings.AI.OpenAI.ChatApiKey), options: new()
                    {
                        Endpoint = settings.AI.OpenAI.ChatEndpoint,
                        Transport = transport
                    }).AsIChatClient();
            });

            if (settings.AI.OpenAI.ChatUseResponsesApi)
            {
                // Nothing is kept on the provider's side, as with /v1/chat/completions: the visitor's browser keeps the
                // conversation and sends it with every question
                chatClient.ConfigureOptions(options => options.RawRepresentationFactory ??= _ => new OpenAI.Responses.CreateResponseOptions { StoredOutputEnabled = false });
            }
#pragma warning restore OPENAI001

            // Runs the gallery chat's ShowSamples (See LandingChatService); a sample's chat has no tools
            chatClient.UseFunctionInvocation()
                .UseLogging()
                .UseOpenTelemetry();
        }

        return services;
    }

    /// <summary>
    /// A WebSocket per question (MapBitAiWebSocket of Bit.BlazorUI.AI.Server): the question and the conversation before it go
    /// up, and the answer's events come back as the model writes them. Stopping the answer closes the socket, which stops the
    /// model too.
    /// </summary>
    public static void MapLandingChat(this WebApplication app)
    {
        app.MapBitAiWebSocket("/api/chat", Answer, new()
        {
            // Many times what the turns a question carries can take (See LandingChatContext.MaxTurns), and far less than the
            // library's default
            MaxRequestSize = 512 * 1024,
            RequestTimeout = TimeSpan.FromSeconds(15),
            WebSockets = new() { KeepAliveInterval = TimeSpan.FromSeconds(20) }
        })
        .RequireSameOrigin()
        .RequireRateLimiting(LandingRateLimits.Chat)
        .ExcludeFromDescription();
    }

    private static IAsyncEnumerable<BitAiEvent> Answer(BitAiRequest request, HttpContext context, CancellationToken cancellationToken)
        => context.RequestServices.GetRequiredService<LandingChatService>().Answer(request, context.GetClientIp(), cancellationToken);

    /// <summary>Browsers apply no CORS to WebSockets, so this keeps other sites' pages from using the chat.</summary>
    private static TBuilder RequireSameOrigin<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpoint =>
        {
            var next = endpoint.RequestDelegate!;

            endpoint.RequestDelegate = context =>
            {
                if (context.Request.Headers.Origin is [{ } origin, ..]
                    && Uri.TryCreate(origin, UriKind.Absolute, out var originUri)
                    && string.Equals(originUri.Authority, context.Request.Host.Value, StringComparison.OrdinalIgnoreCase) is false)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                return next(context);
            };
        });

        return builder;
    }
}

/// <summary>How many questions each visitor may ask (See <see cref="LandingChatEndpoints.AddLandingChat"/>).</summary>
public sealed record LandingChatQuestionLimiter(PartitionedRateLimiter<string> Limiter);
