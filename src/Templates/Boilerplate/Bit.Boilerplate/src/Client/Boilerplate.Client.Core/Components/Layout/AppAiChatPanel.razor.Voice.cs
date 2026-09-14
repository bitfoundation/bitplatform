using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Client.Core.Components.Layout;

// Voice call: audio goes to the provider over WebRTC, while the key, prompt and tools stay on the server
// (See ChatbotController.StartVoiceCall). Never runs alongside dictation or read aloud. Typed messages go into the call too.
public partial class AppAiChatPanel
{
    [AutoInject] private WebRtc webRtc = default!;
    [AutoInject] private IChatbotController chatbotController = default!;


    private bool isVoiceCallSupported;
    private bool isVoiceCallConnecting;
    private bool isInVoiceCall;
    private ElementReference voiceCallAudioRef;
    private MediaStreamHandle? voiceCallMicrophone;
    private PeerConnectionHandle? voiceCallConnection;
    private RtcDataChannelHandle? voiceCallEvents;

    private CancellationTokenSource? voiceCallTimerCts;

    /// <summary>Completes when the model's current response is done; null while it isn't responding.</summary>
    private TaskCompletionSource? voiceCallResponse;

    /// <summary>The model's audio is still playing, which can outlast its response.</summary>
    private bool isVoiceCallSpeaking;

    /// <summary>Transcript bubbles, keyed by the provider's item id (user) or response id (assistant).</summary>
    private readonly Dictionary<string, AiChatMessage> voiceCallMessages = [];


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
            SnackBarService.Error(Localizer["Voice call"], Localizer[nameof(AppStrings.AiChatPanelMicrophoneBlocked)]);
            return;
        }

        await StopDictation();
        await StopReadAloud();

        isVoiceCallConnecting = true;
        followUpSuggestions = [];
        StateHasChanged();

        try
        {
            var microphone = voiceCallMicrophone = await mediaDevices.GetUserMedia(audio: true, video: false);

            if (microphone is null)
            {
                SnackBarService.Error(Localizer["Voice call"], Localizer[nameof(AppStrings.AiChatPanelMicrophoneBlocked)]);
                await EndVoiceCall();
                return;
            }

            PeerConnectionHandle? connection = null;
            connection = voiceCallConnection = await webRtc.CreatePeerConnection(
                onConnectionState: state => _ = InvokeAsync(() => HandleVoiceCallState(connection, state)));

            // Tracks and channel before the offer, which describes them.
            await connection!.AddTracksFrom(microphone);
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
                ChatMessagesHistory = ResentHistory()
            }, CurrentCancellationToken);

            // Hung up while the server was dialing.
            if (ReferenceEquals(voiceCallConnection, connection) is false) return;

            await connection.SetRemoteDescription(new RtcSessionDescription("answer", answer!.Sdp, null));

            isInVoiceCall = true;

            voiceCallTimerCts = CancellationTokenSource.CreateLinkedTokenSource(CurrentCancellationToken);
            _ = CountDownVoiceCall(answer.MaxDuration, connection, voiceCallTimerCts.Token);
        }
        catch
        {
            await EndVoiceCall();
            throw;
        }
        finally
        {
            isVoiceCallConnecting = false;
            StateHasChanged();
        }
    }

    private async Task HandleVoiceCallState(PeerConnectionHandle? connection, string state)
    {
        if (state is not ("failed" or "closed")) return;

        await VoiceCallEnded(connection);
    }

    /// <summary>The server hangs up on time; this ends the call here too, should the browser not notice.</summary>
    private async Task CountDownVoiceCall(TimeSpan maxDuration, PeerConnectionHandle connection, CancellationToken cancellationToken)
    {
        if (await CountDownSpeech(maxDuration, cancellationToken) is false) return;

        await InvokeAsync(() => VoiceCallEnded(connection));
    }

    /// <summary>Ended by the provider, the network or the time limit, rather than by the user.</summary>
    private async Task VoiceCallEnded(PeerConnectionHandle? connection)
    {
        // Hanging up ends it too, and closes a call that a newer one has replaced.
        if (isInVoiceCall is false || ReferenceEquals(voiceCallConnection, connection) is false) return;

        await EndVoiceCall();

        SnackBarService.Info(Localizer["The voice call ended."]);

        StateHasChanged();
    }

    /// <summary>Shows the transcript of both sides as it is spoken; tools run on the server.</summary>
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
                break;

            case "output_audio_buffer.started":
                isVoiceCallSpeaking = true;
                break;

            case "output_audio_buffer.stopped" or "output_audio_buffer.cleared":
                isVoiceCallSpeaking = false;
                break;

            case "conversation.item.input_audio_transcription.delta" when ReadString(root, "item_id") is { } itemId:
                VoiceCallMessage(itemId, AiChatMessageRole.User).Content += ReadString(root, "delta");
                break;

            case "conversation.item.input_audio_transcription.completed" when ReadString(root, "item_id") is { } itemId:
                await CompleteVoiceCallMessage(VoiceCallMessage(itemId, AiChatMessageRole.User), ReadString(root, "transcript"));
                break;

            case "response.output_audio_transcript.delta" when ReadString(root, "response_id") is { } responseId:
                VoiceCallMessage(responseId, AiChatMessageRole.Assistant).Content += ReadString(root, "delta");
                break;

            case "response.output_audio_transcript.done" when ReadString(root, "response_id") is { } responseId:
                await CompleteVoiceCallMessage(VoiceCallMessage(responseId, AiChatMessageRole.Assistant), ReadString(root, "transcript"));
                break;

            case "error":
                logger.LogWarning("The voice call reported {VoiceCallError}.",
                                  root.TryGetProperty("error", out var error) ? ReadString(error, "message") : null);
                break;
        }

        StateHasChanged();
    }

    /// <summary>
    /// A message typed during a call goes into the call and cuts the model short, as speaking over it would. Images don't
    /// go into a call.
    /// </summary>
    private async Task SendVoiceCallMessage()
    {
        if (string.IsNullOrWhiteSpace(userInput)) return;

        var text = userInput;
        userInput = string.Empty;

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
            if (string.IsNullOrWhiteSpace(userInput))
            {
                userInput = text;
            }

            SnackBarService.Error(Localizer["Voice call"], Localizer["The message could not be sent to the call."]);
            return;
        }

        followUpSuggestions = [];

        var message = new AiChatMessage { Role = AiChatMessageRole.User, Content = text, SentAt = TimeProvider.GetUtcNow() };

        chatMessages.Add(message);

        await RememberMessage(message);
    }

    private AiChatMessage VoiceCallMessage(string key, AiChatMessageRole role)
    {
        if (voiceCallMessages.TryGetValue(key, out var message)) return message;

        message = new() { Role = role, Content = string.Empty, SentAt = TimeProvider.GetUtcNow() };

        voiceCallMessages[key] = message;
        chatMessages.Add(message);

        return message;
    }

    /// <summary>Unsigned, so a spoken answer isn't read aloud, and a resent history replays it as the user's words.</summary>
    private async Task CompleteVoiceCallMessage(AiChatMessage message, string? transcript)
    {
        message.Content = transcript ?? message.Content;

        if (string.IsNullOrWhiteSpace(message.Content))
        {
            chatMessages.Remove(message);
            return;
        }

        await RememberMessage(message);
    }

    private static string? ReadString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind is JsonValueKind.String ? value.GetString() : null;

    /// <summary>Closing the connection ends the call at the provider, and the server's side with it.</summary>
    private async Task EndVoiceCall()
    {
        // The call's tool may be waiting on a card.
        if (isInVoiceCall)
        {
            await ExpireAwaitedCards();
        }

        isInVoiceCall = false;
        isVoiceCallSpeaking = false;
        speechPercent = 0;
        voiceCallMessages.Clear();

        // Releases a message waiting for a response that will never finish.
        voiceCallResponse?.TrySetResult();
        voiceCallResponse = null;

        if (voiceCallTimerCts is not null)
        {
            var timerCts = voiceCallTimerCts;
            voiceCallTimerCts = null;
            await timerCts.TryCancel();
            timerCts.Dispose();
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
    }
}
