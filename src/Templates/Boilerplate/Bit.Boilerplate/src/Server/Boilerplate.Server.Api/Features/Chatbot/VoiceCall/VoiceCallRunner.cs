using System.Net.WebSockets;
using System.Text.Json.Nodes;
using System.Collections.Concurrent;
using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Server.Api.Features.Chatbot.VoiceCall;

/// <summary>
/// The server's side of a voice call, which outlives the request that started it: creates the call with the server's
/// instructions and tools, runs the tools the model calls, and hangs up on time limit, when the user starts another or
/// when the app stops.
/// <para>
/// The browser may send session.update to rewrite instructions or declare tools, but only
/// <see cref="AppChatbot.GetAIFunctions"/> ever run here.
/// </para>
/// </summary>
public class VoiceCallRunner(IServiceScopeFactory scopeFactory,
    OpenAIRealtimeCallClient realtimeCallClient,
    ServerApiSettings appSettings,
    IHostApplicationLifetime applicationLifetime,
    ILogger<VoiceCallRunner> logger)
{
    /// <summary>One call per user: each open call is billed by the minute.</summary>
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> callsByUser = new();

    private OpenAIOptions Options => appSettings.AI?.OpenAI ?? throw new InvalidOperationException("The AI:OpenAI configuration section is required.");

    public TimeSpan MaxCallDuration => Options.RealtimeMaxCallDuration > TimeSpan.Zero ? Options.RealtimeMaxCallDuration : TimeSpan.FromMinutes(2);

    /// <summary>Creates and joins the call; returns the provider's answer to the browser's offer.</summary>
    public async Task<string> Start(VoiceCallStart start, CancellationToken cancellationToken)
    {
        var scope = scopeFactory.CreateAsyncScope();
        IRealtimeSideband? sideband = null;
        string? callId = null;

        try
        {
            var chatbot = scope.ServiceProvider.GetRequiredService<AppChatbot>();

            var functions = chatbot.GetAIFunctions();

            var call = await realtimeCallClient.CreateCall(start.OfferSdp, CreateSession(start.Instructions, start.Language, functions), start.SafetyIdentifier, cancellationToken);
            callId = call.CallId;

            // Joined before answering: a call nobody on the server is in can't run tools or be ended.
            sideband = await realtimeCallClient.ConnectSideband(call.CallId, cancellationToken);

            // Before the user can say anything, so the model carries on the conversation rather than starting over.
            await SendHistory(sideband, chatbot.BelievableHistory(start.History), cancellationToken);

            // Ends with the app too, so the call is hung up rather than left without a server.
            var lifetime = CancellationTokenSource.CreateLinkedTokenSource(applicationLifetime.ApplicationStopping);
            lifetime.CancelAfter(MaxCallDuration);

            callsByUser.AddOrUpdate(start.UserId, lifetime, (_, previous) =>
            {
                try
                {
                    previous.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // That call's Run just ended and disposed it.
                }
                return lifetime;
            });

            var runningSideband = sideband;
            sideband = null;
            callId = null;

            _ = Task.Run(() => Run(scope, runningSideband, functions, call.CallId, start, lifetime), CancellationToken.None);

            return call.AnswerSdp;
        }
        catch
        {
            if (sideband is not null)
            {
                await sideband.DisposeAsync();
            }

            if (callId is not null)
            {
                await HangUp(callId);
            }

            await scope.DisposeAsync();
            throw;
        }
    }

    private async Task Run(AsyncServiceScope scope, IRealtimeSideband sideband, List<AIFunction> functions, string callId, VoiceCallStart start, CancellationTokenSource lifetime)
    {
        ChatbotMetrics.ActiveVoiceCalls.Add(1);

        try
        {
            // Tools read the user, tenant and urls off a request; the original one is over and gets reused.
            scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = CreateCallContext(scope, start);

            await foreach (var serverEvent in sideband.ReadEvents(lifetime.Token))
            {
                switch ((string?)serverEvent["type"])
                {
                    case "response.done":
                        ChatbotMetrics.RecordRealtimeUsage(serverEvent["response"]?["usage"], Options.RealtimeModel);

                        // e.g. rate_limit_exceeded, which comes with no error event: the user only hears silence.
                        if ((string?)serverEvent["response"]?["status"] is "failed")
                        {
                            var failure = serverEvent["response"]?["status_details"]?["error"];
                            logger.LogError("A response in voice call {CallId} failed with {RealtimeErrorCode}: {RealtimeError}", callId, (string?)failure?["code"], (string?)failure?["message"]);
                        }

                        await AnswerToolCalls(serverEvent, scope, start.User, sideband, functions, lifetime.Token);
                        break;

                    case "conversation.item.input_audio_transcription.completed":
                        ChatbotMetrics.RecordRealtimeTranscriptionUsage(serverEvent["usage"], Options.RealtimeTranscriptionModel);
                        break;

                    case "error":
                        logger.LogWarning("Voice call {CallId} reported {RealtimeError}.", callId, (string?)serverEvent["error"]?["message"]);
                        break;
                }
            }
        }
        catch (Exception exp) when (exp is OperationCanceledException or WebSocketException)
        {
            // Time limit, a newer call of the same user, the app stopping, or the provider ended it.
        }
        catch (Exception exp)
        {
            logger.LogError(exp, "Voice call {CallId} failed.", callId);
        }
        finally
        {
            ChatbotMetrics.ActiveVoiceCalls.Add(-1);

            callsByUser.TryRemove(new KeyValuePair<Guid, CancellationTokenSource>(start.UserId, lifetime));

            await HangUp(callId);
            await sideband.DisposeAsync();
            await scope.DisposeAsync();
            lifetime.Dispose();
        }
    }

    /// <summary>Runs every function call of a finished response, then asks for one new response.</summary>
    private async Task AnswerToolCalls(JsonObject responseDone, AsyncServiceScope scope, ClaimsPrincipal user, IRealtimeSideband sideband, List<AIFunction> functions, CancellationToken cancellationToken)
    {
        // Cut short by the user, speaking or typing over it: its calls may be half written.
        if ((string?)responseDone["response"]?["status"] is "cancelled") return;

        var items = (responseDone["response"]?["output"] as JsonArray ?? []).OfType<JsonObject>().ToArray();

        var calls = items.Where(item => (string?)item["type"] is "function_call").ToArray();

        if (calls.Length is 0) return;

        await UseCurrentSignalRConnection(scope, user, cancellationToken);

        foreach (var call in calls)
        {
            var output = await InvokeTool(functions, (string?)call["name"], (string?)call["arguments"], cancellationToken);

            await sideband.Send(new JsonObject
            {
                ["type"] = "conversation.item.create",
                ["item"] = new JsonObject
                {
                    ["type"] = "function_call_output",
                    ["call_id"] = (string?)call["call_id"],
                    ["output"] = output
                }
            }, cancellationToken);
        }

        // Already answered and only showed suggestions: a new response would talk about them. A commentary message is
        // only the preamble to what the model says once its tools are done.
        if (items.Any(item => (string?)item["type"] is "message" && (string?)item["phase"] is not "commentary") && calls.All(call => (string?)call["name"] is "ShowFollowUpSuggestions")) return;

        await sideband.Send(new JsonObject { ["type"] = "response.create" }, cancellationToken);
    }

    /// <summary>Read again for each round of calls: the session's tab or app may have reconnected since, even to another instance.</summary>
    private static async Task UseCurrentSignalRConnection(AsyncServiceScope scope, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var sessionId = user.GetSessionId();

        var signalRConnectionId = await scope.ServiceProvider.GetRequiredService<AppDbContext>().UserSessions
            .Where(us => us.Id == sessionId)
            .Select(us => us.SignalRConnectionId)
            .FirstOrDefaultAsync(cancellationToken);

        scope.ServiceProvider.GetRequiredService<AppChatbot>().UseSignalRConnection(signalRConnectionId);
    }

    /// <summary>Text only: images don't go into a call, so a message that was only a picture is skipped.</summary>
    private static async Task SendHistory(IRealtimeSideband sideband, AiChatMessage[] history, CancellationToken cancellationToken)
    {
        foreach (var message in history)
        {
            if (string.IsNullOrWhiteSpace(message.Content)) continue;

            var isAssistant = message.Role is AiChatMessageRole.Assistant;

            await sideband.Send(new JsonObject
            {
                ["type"] = "conversation.item.create",
                ["item"] = new JsonObject
                {
                    ["type"] = "message",
                    ["role"] = isAssistant ? "assistant" : "user",
                    ["content"] = new JsonArray(new JsonObject
                    {
                        ["type"] = isAssistant ? "output_text" : "input_text",
                        ["text"] = message.Content
                    })
                }
            }, cancellationToken);
        }
    }

    private async Task<string> InvokeTool(List<AIFunction> functions, string? name, string? arguments, CancellationToken cancellationToken)
    {
        var function = functions.FirstOrDefault(f => f.Name == name);

        // e.g. a tool the browser declared itself.
        if (function is null)
            return $"There is no tool named '{name}'.";

        try
        {
            var parsedArguments = string.IsNullOrWhiteSpace(arguments)
                ? []
                : JsonSerializer.Deserialize<Dictionary<string, object?>>(arguments) ?? [];

            var result = await function.InvokeAsync(new AIFunctionArguments(parsedArguments), cancellationToken);

            return result switch
            {
                null => "null",
                string text => text,
                JsonElement element => element.GetRawText(),
                _ => JsonSerializer.Serialize(result, AIJsonUtilities.DefaultOptions)
            };
        }
        catch (Exception exp) when (exp is not OperationCanceledException)
        {
            logger.LogWarning(exp, "The voice call's {ToolName} tool failed.", name);
            return $"The {name} tool failed.";
        }
    }

    private JsonObject CreateSession(string instructions, string? language, List<AIFunction> functions)
    {
        JsonObject input = new();

        // Provider defaults are used. Uncomment only if users report background noise or being cut off (See the end of ./Readme.md).
        // input["noise_reduction"] = new JsonObject { ["type"] = "far_field" };
        // input["turn_detection"] = new JsonObject { ["type"] = "semantic_vad", ["eagerness"] = "medium" };

        if (string.IsNullOrWhiteSpace(Options.RealtimeTranscriptionModel) is false)
        {
            input["transcription"] = new JsonObject
            {
                ["model"] = Options.RealtimeTranscriptionModel,
                // Spelling hints for names the transcriber wouldn't know; add product names and acronyms here.
                ["keywords"] = new JsonArray("Boilerplate", "PWA") // These 2 have been added as example.
            };

            // Left to detect it, the transcriber can turn a short or accented sentence into another language entirely.
            if (language is not null)
            {
                input["transcription"]!["language"] = language;
            }
        }

        JsonObject audio = new()
        {
            ["output"] = new JsonObject { ["voice"] = Options.RealtimeVoice }
        };

        if (input.Count > 0)
        {
            audio["input"] = input;
        }

        JsonObject session = new()
        {
            ["type"] = "realtime",
            ["model"] = Options.RealtimeModel,
            ["instructions"] = instructions,
            ["audio"] = audio,
            ["tool_choice"] = "auto",
            ["tools"] = new JsonArray([.. functions.Select(function => (JsonNode)new JsonObject
            {
                ["type"] = "function",
                ["name"] = function.Name,
                ["description"] = function.Description,
                ["parameters"] = JsonNode.Parse(function.JsonSchema.GetRawText())
            })])
        };

        if (string.IsNullOrWhiteSpace(Options.RealtimeReasoningEffort) is false)
        {
            session["reasoning"] = new JsonObject { ["effort"] = Options.RealtimeReasoningEffort };
        }

        return session;
    }

    private DefaultHttpContext CreateCallContext(AsyncServiceScope scope, VoiceCallStart start)
    {
        var context = new DefaultHttpContext { User = start.User, RequestServices = scope.ServiceProvider };

        context.Request.Scheme = start.BaseUrl.Scheme;
        context.Request.Host = HostString.FromUriComponent(start.BaseUrl);

        // So GetWebAppUrl answers as it did for the request. A configured WebAppUrl isn't necessarily a trusted origin,
        // and without the header GetWebAppUrl falls back to it anyway.
        if (start.WebAppUrl == start.BaseUrl || appSettings.IsTrustedOrigin(start.WebAppUrl))
        {
            context.Request.Headers["X-Origin"] = start.WebAppUrl.AbsoluteUri;
        }

        return context;
    }

    private async Task HangUp(string callId)
    {
        try
        {
            await realtimeCallClient.HangUp(callId, CancellationToken.None);
        }
        catch (Exception exp)
        {
            logger.LogInformation(exp, "Voice call {CallId} could not be hung up, and may have ended already.", callId);
        }
    }
}

/// <param name="WebAppUrl">What GetWebAppUrl answered for the request.</param>
/// <param name="SafetyIdentifier">An opaque, stable user id for the provider's abuse checks.</param>
/// <param name="Language">The ISO-639-1 code of the user's culture, which the transcription expects.</param>
/// <param name="History">The conversation as the client sent it; filtered before it reaches the call.</param>
public record VoiceCallStart(Guid UserId,
    ClaimsPrincipal User,
    string OfferSdp,
    string Instructions,
    Uri BaseUrl,
    Uri WebAppUrl,
    string SafetyIdentifier,
    string? Language,
    IReadOnlyList<AiChatMessage> History);
