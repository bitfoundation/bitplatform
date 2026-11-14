using System.Text.RegularExpressions;
using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Client.Core.Components.Layout.AiChat;

// Voice call: audio goes to the provider over WebRTC, while the key, prompt and tools stay on the server (See
// ChatbotController.StartVoiceCall). Both sides' words land in the conversation as they're spoken, and what the tools show
// - products, a form, an approval - lands there too (See AppChatbot.VoiceCallUi). Messages typed during the call go into it.
public partial class AppAiChat
{
    [AutoInject] private WebRtc webRtc = default!;
    [AutoInject] private MediaDevices mediaDevices = default!;
    [AutoInject] private IPermissionService permissionService = default!;
    [AutoInject] private IChatbotController chatbotController = default!;

    private bool isVoiceCallSupported;
    private bool isVoiceCallConnecting;
    private bool isInVoiceCall;
    private ElementReference voiceCallAudioRef;
    private MediaStreamHandle? voiceCallMicrophone;
    private PeerConnectionHandle? voiceCallConnection;
    private RtcDataChannelHandle? voiceCallEvents;
    private IDisposable? voiceCallEventsSubscription;
    private IDisposable? voiceCallDecisionSubscription;

    /// <summary>The call's own: dialing, then its countdown.</summary>
    private CancellationTokenSource? voiceCallCts;

    /// <summary>Completes when the model's current response is done; null while it isn't responding.</summary>
    private TaskCompletionSource? voiceCallResponse;

    /// <summary>The model's audio is still playing, which can outlast its response.</summary>
    private bool isVoiceCallSpeaking;

    /// <summary>Transcript messages, keyed by the provider's item id (user) or response id (assistant).</summary>
    private readonly Dictionary<string, BitAiMessage> voiceCallMessages = [];

    /// <summary>How much of its max duration the call has used.</summary>
    private double speechPercent;
    private TimeSpan speechTimeLeft;

    /// <summary>Primary, then warning for the last 30% and error for the last 10%.</summary>
    private BitColor SpeechTimerColor => speechPercent >= 90 ? BitColor.Error : speechPercent >= 70 ? BitColor.Warning : BitColor.Primary;

    private async Task InitializeVoiceCall()
    {
        // Both are asked about because they are separate globals that go missing separately: only navigator.mediaDevices
        // needs a secure context.
        isVoiceCallSupported = await webRtc.IsSupported() && await mediaDevices.IsSupported();

        voiceCallEventsSubscription = hubConnection.On<string[], string?>(SharedAppMessages.SHOW_AI_CHAT_EVENTS, ShowVoiceCallEvents);
        voiceCallDecisionSubscription = hubConnection.On<string[], string?>(SharedAppMessages.AWAIT_AI_CHAT_DECISION, AwaitVoiceCallDecision);

        StateHasChanged();
    }

    private async Task ToggleVoiceCall()
    {
        if (isInVoiceCall || isVoiceCallConnecting)
        {
            await EndVoiceCall();
            return;
        }

        if (await EnsureSignedInForSpeech() is false) return;

        if (await permissionService.RequestMicrophonePermission() is false)
        {
            SnackBarService.Error(Localizer[nameof(AppStrings.AiChatPanelVoiceCall)], Localizer[nameof(AppStrings.AiChatPanelMicrophoneBlocked)]);
            return;
        }

        // Talking and being read to at once would put the reading into the call
        if (chat?.ReadAloud is { } readAloud)
        {
            await readAloud.StopAsync();
        }

        session?.Stop();

        isVoiceCallConnecting = true;
        StateHasChanged();

        try
        {
            var microphone = voiceCallMicrophone = await mediaDevices.GetUserMedia(audio: true, video: false);

            if (microphone is null)
            {
                SnackBarService.Error(Localizer[nameof(AppStrings.AiChatPanelVoiceCall)], Localizer[nameof(AppStrings.AiChatPanelMicrophoneBlocked)]);
                await EndVoiceCall();
                return;
            }

            // Hanging up cancels it, dialing included, so the server hangs up the call it was creating.
            var callCts = voiceCallCts = CancellationTokenSource.CreateLinkedTokenSource(CurrentCancellationToken);

            PeerConnectionHandle? connection = null;
            connection = voiceCallConnection = await webRtc.CreatePeerConnection(
                onConnectionState: state => _ = InvokeAsync(() => HandleVoiceCallState(connection, state)))
                ?? throw new InvalidOperationException("The browser could not create the call's connection.");

            // Tracks and channel before the offer, which describes them.
            await connection.AddTracksFrom(microphone);
            voiceCallEvents = await connection.CreateDataChannel("oai-events");
            // The provider hanging up closes the channel at once; the connection only reports it much later, if ever.
            voiceCallEvents!.Listen(onMessage: message => _ = InvokeAsync(() => HandleVoiceCallEvent(message)),
                                    onClose: () => _ = InvokeAsync(() => VoiceCallEnded(connection)));

            await connection.AttachRemoteMedia(voiceCallAudioRef);

            var offer = await connection.CreateOffer();

            if (offer?.Sdp is null)
                throw new InvalidOperationException($"The browser could not describe the call: {offer?.Error ?? "Unknown error"}");

            await connection.SetLocalDescription(offer);

            var answer = await chatbotController.StartVoiceCall(new()
            {
                Sdp = offer.Sdp,
                CultureId = CultureInfo.CurrentCulture.LCID,
                TimeZoneId = (await TimeZoneService.GetCurrentTimeZone()).Id,
                DeviceInfo = TelemetryContext.Platform,
                ChatMessagesHistory = VoiceCallHistory()
            }, callCts.Token);

            // Hung up while the server was answering.
            if (ReferenceEquals(voiceCallConnection, connection) is false) return;

            await connection.SetRemoteDescription(new RtcSessionDescription("answer", answer!.Sdp, null));

            isInVoiceCall = true;

            _ = CountDownVoiceCall(answer.MaxDuration, connection, callCts.Token);
        }
        catch
        {
            await EndVoiceCall();
            throw;
        }
        finally
        {
            isVoiceCallConnecting = false;
            RaiseStatusChanged();
            StateHasChanged();
        }
    }

    /// <summary>
    /// The conversation so far, for the call to carry on from: what the assistant wrote, as the server signed it, and
    /// everything else as it reads. The server trusts no more of it than it trusts of the text chat's.
    /// </summary>
    private List<AiChatMessage> VoiceCallHistory()
    {
        return [.. session!.Conversation.Messages
            .Where(message => message.Status is not (BitAiMessageStatus.Cancelled or BitAiMessageStatus.Failed) && (message.Role.IsUser || message.Role.IsAssistant))
            .TakeLast(AiChatProtocol.MaxHistory)
            .Select(message => message.Role.IsAssistant && ExtraValue(message, AiChatProtocol.Replay) is { } replay
                ? new AiChatMessage { Role = AiChatMessageRole.Assistant, Content = replay, Signature = ExtraValue(message, AiChatProtocol.ReplaySignature) }
                : message.Role.IsAssistant && ExtraValue(message, AiChatProtocol.Answer) is { } answer
                    ? new AiChatMessage { Role = AiChatMessageRole.Assistant, Content = answer, Signature = ExtraValue(message, AiChatProtocol.Signature) }
                    : new AiChatMessage { Role = message.Role.IsUser ? AiChatMessageRole.User : AiChatMessageRole.Assistant, Content = message.GetText() })
            .Where(message => string.IsNullOrWhiteSpace(message.Content) is false)];
    }

    private async Task HandleVoiceCallState(PeerConnectionHandle? connection, string state)
    {
        if (state is not ("failed" or "closed")) return;

        await VoiceCallEnded(connection);
    }

    /// <summary>The server hangs up on time; this ends the call here too, should the browser not notice.</summary>
    private async Task CountDownVoiceCall(TimeSpan maxDuration, PeerConnectionHandle connection, CancellationToken cancellationToken)
    {
        var startedAt = TimeProvider.GetUtcNow();
        speechTimeLeft = maxDuration;
        speechPercent = 0;

        try
        {
            using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(500), TimeProvider);

            do
            {
                var elapsed = TimeProvider.GetUtcNow() - startedAt;

                if (elapsed >= maxDuration)
                {
                    await InvokeAsync(() => VoiceCallEnded(connection));
                    return;
                }

                await InvokeAsync(() =>
                {
                    // Already reset by whoever cancelled.
                    if (cancellationToken.IsCancellationRequested) return;

                    speechTimeLeft = maxDuration - elapsed;
                    speechPercent = elapsed / maxDuration * 100;
                    StateHasChanged();
                });
            }
            while (await timer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>Ended by the provider, the network or the time limit, rather than by the user.</summary>
    private async Task VoiceCallEnded(PeerConnectionHandle? connection)
    {
        // Hanging up ends it too, and closes a call that a newer one has replaced.
        if (isInVoiceCall is false || ReferenceEquals(voiceCallConnection, connection) is false) return;

        await EndVoiceCall();

        SnackBarService.Info(Localizer[nameof(AppStrings.AiChatPanelVoiceCallEnded)]);

        StateHasChanged();
    }

    /// <summary>Writes both sides' words into the conversation as they're spoken; the tools run on the server.</summary>
    private async Task HandleVoiceCallEvent(ButilMessage message)
    {
        if (message.IsBinary || message.Json is null) return;

        // A text frame arrives as a json string wrapping the provider's json event.
        var eventJson = message.Json.StartsWith('"')
            ? JsonSerializer.Deserialize(message.Json, JsonSerializerOptions.GetTypeInfo<string>())
            : message.Json;

        if (string.IsNullOrWhiteSpace(eventJson)) return;

        using var document = JsonDocument.Parse(eventJson);
        var root = document.RootElement;

        switch (ReadString(root, "type"))
        {
            case "response.created":
                voiceCallResponse ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
                break;

            case "response.done":
                voiceCallResponse?.TrySetResult();
                voiceCallResponse = null;
                WarnOfFailedVoiceCallResponse(root);
                break;

            case "output_audio_buffer.started":
                isVoiceCallSpeaking = true;
                break;

            case "output_audio_buffer.stopped" or "output_audio_buffer.cleared":
                isVoiceCallSpeaking = false;
                break;

            case "conversation.item.input_audio_transcription.delta" when ReadString(root, "item_id") is { } itemId:
                Speak(VoiceCallMessage(itemId, BitAiRole.User), ReadString(root, "delta"));
                break;

            case "conversation.item.input_audio_transcription.completed" when ReadString(root, "item_id") is { } itemId:
                await CompleteVoiceCallMessage(VoiceCallMessage(itemId, BitAiRole.User), ReadString(root, "transcript"));
                break;

            case "response.output_audio_transcript.delta" when ReadString(root, "response_id") is { } responseId:
                Speak(VoiceCallMessage(responseId, BitAiRole.Assistant), ReadString(root, "delta"));
                break;

            case "response.output_audio_transcript.done" when ReadString(root, "response_id") is { } responseId:
                await CompleteVoiceCallMessage(VoiceCallMessage(responseId, BitAiRole.Assistant), ReadString(root, "transcript"));
                break;

            case "error":
                logger.LogWarning("The voice call reported {VoiceCallError}.",
                                  root.TryGetProperty("error", out var error) ? ReadString(error, "message") : null);
                break;
        }
    }

    /// <summary>
    /// A message typed during a call goes into the call and cuts the model short, as speaking over it would. Images don't
    /// go into a call. False when it could not be sent, so the composer gives it back.
    /// </summary>
    private async Task<bool> SendVoiceCallMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return true;

        if (voiceCallEvents is { } interrupted)
        {
            if (voiceCallResponse is not null)
            {
                await interrupted.SendText("""{"type":"response.cancel"}""");
            }

            if (voiceCallResponse is not null || isVoiceCallSpeaking)
            {
                await interrupted.SendText("""{"type":"output_audio_buffer.clear"}""");
            }
        }

        // The provider refuses a new response while another is active.
        if (voiceCallResponse is { } responding)
        {
            await responding.Task;
        }

        var content = JsonSerializer.Serialize(text, JsonSerializerOptions.GetTypeInfo<string>());

        // voiceCallEvents is null once hung up while waiting.
        var sent = voiceCallEvents is { } events
            && await events.SendText($$$"""{"type":"conversation.item.create","item":{"type":"message","role":"user","content":[{"type":"input_text","text":{{{content}}}}]}}""")
            && await events.SendText("""{"type":"response.create"}""");

        if (sent is false)
        {
            SnackBarService.Error(Localizer[nameof(AppStrings.AiChatPanelVoiceCall)], Localizer[nameof(AppStrings.AiChatPanelVoiceCallMessageNotSent)]);
            return false;
        }

        session!.Conversation.AddMessage(BitAiRole.User, text);

        await SaveHistory();

        return true;
    }

    /// <summary>The message a speaker's words of one turn go into, added the first time they're heard.</summary>
    private BitAiMessage VoiceCallMessage(string key, BitAiRole role)
    {
        if (voiceCallMessages.TryGetValue(key, out var message)) return message;

        message = new BitAiMessage(role) { CreatedAt = TimeProvider.GetUtcNow() };
        message.SetStatus(BitAiMessageStatus.Streaming);

        voiceCallMessages[key] = message;
        session!.Conversation.AddMessage(message);

        return message;
    }

    private void Speak(BitAiMessage message, string? delta)
    {
        if (string.IsNullOrEmpty(delta)) return;

        session!.Processor.Apply(new BitAiTextDeltaEvent(delta) { MessageId = message.MessageId });
    }

    /// <summary>
    /// Settles what was said with the provider's final transcript. Unsigned, so a spoken answer isn't read aloud, and the
    /// server replays it as the user's words.
    /// </summary>
    private async Task CompleteVoiceCallMessage(BitAiMessage message, string? transcript)
    {
        if (transcript is not null)
        {
            if (message.PartsOf<BitAiTextPart>().FirstOrDefault() is { } text)
            {
                text.SetText(transcript);
            }
            else
            {
                message.AddPart(new BitAiTextPart(transcript));
            }
        }

        message.CompleteOpenParts();

        if (string.IsNullOrWhiteSpace(message.GetText()))
        {
            session!.Conversation.RemoveMessage(message.MessageId);
            return;
        }

        message.SetStatus(BitAiMessageStatus.Complete);

        await SaveHistory();
    }

    /// <summary>
    /// What a tool shows during a call (See AppChatbot.VoiceCallUi): the events a text answer would have streamed, applied
    /// to the assistant's message being spoken, or to one of their own.
    /// </summary>
    private async Task<string?> ShowVoiceCallEvents(string[] events)
    {
        var shown = false;

        await InvokeAsync(async () =>
        {
            if (session is null) return;

            var message = VoiceCallToolsMessage();

            foreach (var json in events)
            {
                if (BitAiWireSerializer.Deserialize(json) is { } evt)
                {
                    session.Processor.Apply(evt with { MessageId = message.MessageId });
                }
            }

            shown = true;

            await SaveHistory();
        });

        return shown ? "shown" : null;
    }

    /// <summary>
    /// Like <see cref="ShowVoiceCallEvents"/>, for a call that waits on the user's approval (ClearAppFiles): the approval
    /// shows in the conversation, and its decision is the answer. Fails closed: hanging up or clearing the chat answers
    /// <see cref="BitAiApprovalDecision.Undecided"/>.
    /// </summary>
    private async Task<string?> AwaitVoiceCallDecision(string[] events)
    {
        TaskCompletionSource<BitAiApprovalDecision> decision = new(TaskCreationOptions.RunContinuationsAsynchronously);
        string? toolCallId = null;
        BitAiToolCallPart? awaited = null;

        void OnDecided(BitAiToolCallPart call, BitAiApprovalDecision value)
        {
            if (call.ToolCallId == toolCallId) decision.TrySetResult(value);
        }

        await InvokeAsync(() =>
        {
            if (session is null || isInVoiceCall is false)
            {
                decision.TrySetResult(BitAiApprovalDecision.Undecided);
                return;
            }

            var message = VoiceCallToolsMessage();

            foreach (var json in events)
            {
                if (BitAiWireSerializer.Deserialize(json) is not { } evt) continue;

                if (evt is BitAiToolCallStartedEvent started) toolCallId = started.ToolCallId;

                session.Processor.Apply(evt with { MessageId = message.MessageId });
            }

            awaited = toolCallId is null ? null : message.FindToolCall(toolCallId);

            session.ApprovalDecided += OnDecided;
            RaiseStatusChanged();
        });

        using var hungUp = voiceCallCts is { } callCts ? callCts.Token.Register(() => decision.TrySetResult(BitAiApprovalDecision.Undecided)) : default;

        try
        {
            return (await decision.Task).ToString();
        }
        finally
        {
            if (session is not null) session.ApprovalDecided -= OnDecided;

            _ = InvokeAsync(async () =>
            {
                // Hung up before the user decided: the approval is withdrawn, so its buttons don't stay on screen offering a
                // decision nothing waits for any more
                if (awaited is { Status: BitAiToolCallStatus.AwaitingApproval })
                {
                    awaited.SetStatus(BitAiToolCallStatus.Cancelled);
                    await SaveHistory();
                }

                RaiseStatusChanged();
            });
        }
    }

    /// <summary>The assistant's message a tool's events of the call go into: the one being spoken, else a new one.</summary>
    private BitAiMessage VoiceCallToolsMessage()
    {
        if (session!.Conversation.LastMessage is { Role.IsAssistant: true } last && voiceCallMessages.ContainsValue(last))
            return last;

        var message = new BitAiMessage(BitAiRole.Assistant) { CreatedAt = TimeProvider.GetUtcNow() };
        session.Conversation.AddMessage(message);

        return message;
    }

    /// <summary>A failed response is otherwise silence. A rate limit's message says when to try again.</summary>
    private void WarnOfFailedVoiceCallResponse(JsonElement responseDone)
    {
        if (responseDone.TryGetProperty("response", out var response) is false || ReadString(response, "status") is not "failed") return;

        var message = response.TryGetProperty("status_details", out var details) && details.ValueKind is JsonValueKind.Object
                      && details.TryGetProperty("error", out var error) && error.ValueKind is JsonValueKind.Object
            ? ReadString(error, "message")
            : null;

        // Not the message itself: it names the provider's organization.
        if (message is not null && RetryAfterRegex().Match(message) is { Success: true } match)
        {
            var seconds = double.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture) / (match.Groups["unit"].Value is "ms" ? 1000 : 1);

            SnackBarService.Warning(Localizer[nameof(AppStrings.AiChatPanelVoiceCall)], Localizer[nameof(AppStrings.AiChatPanelVoiceCallAssistantBusy), Math.Ceiling(seconds)]);
        }
        else
        {
            SnackBarService.Error(Localizer[nameof(AppStrings.AiChatPanelVoiceCall)], Localizer[nameof(AppStrings.AiChatPanelVoiceCallAssistantFailed)]);
        }
    }

    [GeneratedRegex(@"try again in (?<value>\d+(\.\d+)?)(?<unit>ms|s)")]
    private static partial Regex RetryAfterRegex();

    private static string? ReadString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind is JsonValueKind.String ? value.GetString() : null;

    /// <summary>Closing the connection ends the call at the provider, and the server's side with it.</summary>
    private async Task EndVoiceCall()
    {
        isInVoiceCall = false;
        isVoiceCallSpeaking = false;
        speechPercent = 0;

        var interrupted = voiceCallMessages.Values.Where(message => message.IsBusy).ToArray();

        foreach (var spoken in interrupted)
        {
            spoken.CompleteOpenParts();
            spoken.SetStatus(BitAiMessageStatus.Complete);
        }

        voiceCallMessages.Clear();

        // What was said before the call ended mid-sentence is kept like the rest, rather than only until the next save
        if (interrupted.Length > 0)
        {
            await SaveHistory();
        }

        // Releases a message waiting for a response that will never finish.
        voiceCallResponse?.TrySetResult();
        voiceCallResponse = null;

        if (voiceCallCts is not null)
        {
            var callCts = voiceCallCts;
            voiceCallCts = null;
            await callCts.TryCancel();
            callCts.Dispose();
        }

        var events = voiceCallEvents;
        var connection = voiceCallConnection;
        var microphone = voiceCallMicrophone;

        voiceCallEvents = null;
        voiceCallConnection = null;
        voiceCallMicrophone = null;

        if (events is not null)
        {
            await events.DisposeAsync();
        }

        if (connection is not null)
        {
            await connection.DisposeAsync();
        }

        if (microphone is not null)
        {
            await microphone.DisposeAsync();
        }

        RaiseStatusChanged();
    }
}
