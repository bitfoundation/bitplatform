using System.ClientModel.Primitives;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading.RateLimiting;
using Microsoft.Extensions.AI;

namespace Boilerplate.LandingPage.Server.Features.Chat;

public sealed partial class LandingChatService(IServiceProvider serviceProvider, LandingServerSettings settings, IStringLocalizer<LandingStrings> localizer, ILogger<LandingChatService> logger)
{
    public const int MaxTurns = 20;

    public const int MaxTurnLength = 1_000;

    public async IAsyncEnumerable<string> AskAsync(LandingChatAsk ask, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var site = LandingSites.Find(ask.Site);
        var culture = LandingCultures.Find(ask.Culture) ?? LandingCultures.Default;

        if (site is null || ask.Turns is not { Length: > 0 and <= MaxTurns } || ask.Turns[^1].FromVisitor is false || ask.Turns.Any(t => t.Text is not { Length: > 0 and <= MaxTurnLength }))
            throw new LandingChatException(localizer.In(culture, nameof(LandingStrings.ChatInvalidQuestion)), retryable: false);

        if (serviceProvider.GetService<IChatClient>() is not { } chatClient)
        {
            foreach (var word in localizer.In(culture, nameof(LandingStrings.ChatNoModelAnswer), site.Name).Split(' '))
            {
                await Task.Delay(Random.Shared.Next(25, 70), cancellationToken);
                yield return $"{word} ";
            }

            yield break;
        }

        List<ChatMessage> messages = [new(ChatRole.System, LandingChatPrompts.Get(site, culture))];
        messages.AddRange(ask.Turns.TakeLast(12).Select(t => new ChatMessage(t.FromVisitor ? ChatRole.User : ChatRole.Assistant, t.Text)));

        var options = settings.AI.ChatOptions.Clone();

        await using var updates = chatClient.GetStreamingResponseAsync(messages, options, cancellationToken).GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            string? text;

            try
            {
                if (await updates.MoveNextAsync() is false)
                    yield break;

                text = updates.Current.Text;
            }
            catch (Exception exp) when (exp is not OperationCanceledException)
            {
                LogModelFailed(logger, exp, site.Key);
                throw new LandingChatException(localizer.In(culture, nameof(LandingStrings.ChatFailed)), retryable: true);
            }

            if (string.IsNullOrEmpty(text) is false)
                yield return text;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "The chat of {Site} couldn't get an answer from the model")]
    private static partial void LogModelFailed(ILogger logger, Exception exception, string site);
}

public sealed class DirectLandingChatClient(LandingChatService chatService) : ILandingChatClient
{
    public IAsyncEnumerable<string> AskAsync(LandingChatAsk ask, CancellationToken cancellationToken) => chatService.AskAsync(ask, cancellationToken);
}

public static partial class LandingChatEndpoints
{
    public static IServiceCollection AddLandingChat(this IServiceCollection services, LandingServerSettings settings)
    {
        services.AddScoped<LandingChatService>();
        services.AddScoped<ILandingChatClient, DirectLandingChatClient>();

        services.AddSingleton(PartitionedRateLimiter.Create<string, string>(ip => RateLimitPartition.GetSlidingWindowLimiter(ip, _ => new()
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(10),
            SegmentsPerWindow = 5,
            QueueLimit = 0
        })));

        if (string.IsNullOrWhiteSpace(settings.AI.OpenAI.ChatApiKey) is false)
        {
            services.AddHttpClient("AI");

            services.AddChatClient(sp => new OpenAI.Chat.ChatClient(model: settings.AI.OpenAI.ChatModel, credential: new(settings.AI.OpenAI.ChatApiKey), options: new()
            {
                Endpoint = settings.AI.OpenAI.ChatEndpoint,
                Transport = new HttpClientPipelineTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("AI"))
            }).AsIChatClient())
            .UseLogging();
        }

        return services;
    }

    public static void MapLandingChat(this WebApplication app)
    {
        app.Map("/api/chat", async (HttpContext context, LandingChatService chatService, PartitionedRateLimiter<string> limiter, IStringLocalizer<LandingStrings> localizer, ILoggerFactory loggerFactory) =>
        {
            if (context.WebSockets.IsWebSocketRequest is false)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (context.Request.Headers.Origin is [{ } origin, ..]
                && Uri.TryCreate(origin, UriKind.Absolute, out var originUri)
                && string.Equals(originUri.Authority, context.Request.Host.Value, StringComparison.OrdinalIgnoreCase) is false)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            var buffer = new byte[8 * 1024];
            var cancellationToken = context.RequestAborted;

            try
            {
                while (socket.State is WebSocketState.Open)
                {
                    var ask = await ReceiveAsync(socket, buffer, cancellationToken);

                    if (ask is null)
                        break;

                    var culture = LandingCultures.Find(ask.Culture) ?? LandingCultures.Default;

                    using var lease = limiter.AttemptAcquire(context.GetClientIp());

                    if (lease.IsAcquired is false)
                    {
                        await SendAsync(socket, new(Error: localizer.In(culture, nameof(LandingStrings.ChatTooManyQuestions)), Retryable: false), cancellationToken);
                        continue;
                    }

                    try
                    {
                        await foreach (var delta in chatService.AskAsync(ask, cancellationToken))
                        {
                            await SendAsync(socket, new(Delta: delta), cancellationToken);
                        }

                        await SendAsync(socket, new(Done: true), cancellationToken);
                    }
                    catch (LandingChatException exp)
                    {
                        await SendAsync(socket, new(Error: exp.Message, Retryable: exp.Retryable), cancellationToken);
                    }
                }

                if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                }
            }
            catch (Exception exp) when (exp is OperationCanceledException or WebSocketException)
            {
                LogSocketClosed(loggerFactory.CreateLogger(nameof(LandingChatEndpoints)), exp.Message);
            }
        }).RequireRateLimiting(LandingRateLimits.Chat).ExcludeFromDescription();
    }

    private static async Task<LandingChatAsk?> ReceiveAsync(WebSocket socket, byte[] buffer, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken);

            if (result.MessageType is WebSocketMessageType.Close)
                return null;

            stream.Write(buffer, 0, result.Count);

            if (stream.Length > LandingChatService.MaxTurns * LandingChatService.MaxTurnLength * 4)
                return null;

            if (result.EndOfMessage)
                break;
        }

        stream.Position = 0;

        try
        {
            return JsonSerializer.Deserialize(stream, LandingJsonContext.Default.LandingChatAsk);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Task SendAsync(WebSocket socket, LandingChatFrame frame, CancellationToken cancellationToken)
    {
        return socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(frame, LandingJsonContext.Default.LandingChatFrame), WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "A chat connection closed: {Reason}")]
    private static partial void LogSocketClosed(ILogger logger, string reason);
}
