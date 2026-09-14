//+:cnd:noEmit
using Microsoft.AspNetCore.RateLimiting;
using Boilerplate.Shared.Features.Chatbot;
using Boilerplate.Server.Api.Features.Chatbot.VoiceCall;

namespace Boilerplate.Server.Api.Features.Chatbot;

[ApiVersion(1)]
[ApiController, Route("api/v{v:apiVersion}/[controller]/[action]"), Authorize]
public partial class ChatbotController : AppControllerBase, IChatbotController
{
    [AutoInject] private IFusionCache cache = default!;
    [AutoInject] private IServiceProvider serviceProvider = default!;
    [AutoInject] private ChatbotAnswerSigner answerSigner = default!;

    /// <summary>The largest recording the speech endpoints accept; the Dev MCP reports this value rather than a copy of it.</summary>
    public const int MaxSpeechUploadSizeBytes = 2 * 1024 * 1024;

    [HttpGet]
    [EnableQuery]
    //#if (multitenant == true)
    [Authorize(Policy = AuthPolicies.TENANT_SELECTED)]
    //#endif
    [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
    [Authorize(Policy = AppFeatures.Management.SystemPrompts_Write)]
    public IQueryable<SystemPromptDto> GetSystemPrompts()
    {
        return DbContext.SystemPrompts
            .Project();
    }

    [HttpPost]
    //#if (multitenant == true)
    [Authorize(Policy = AuthPolicies.TENANT_SELECTED)]
    //#endif
    [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
    [Authorize(Policy = AppFeatures.Management.SystemPrompts_Write)]
    [Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<SystemPromptDto> UpdateSystemPrompt(SystemPromptDto dto, CancellationToken cancellationToken)
    {
        var entityToUpdate = await DbContext.SystemPrompts.FirstOrDefaultAsync(sp => sp.PromptKind == dto.PromptKind, cancellationToken)
            ?? throw new ResourceNotFoundException();

        dto.Patch(entityToUpdate);

        await DbContext.SaveChangesAsync(cancellationToken);

        // Invalidate cache for the updated system prompt
        //#if (multitenant == true)
        await cache.RemoveAsync($"SystemPrompt_{TenantProvider.GetCurrentTenantId()}_{dto.PromptKind}");
        //#else
        await cache.RemoveAsync($"SystemPrompt_{dto.PromptKind}");
        //#endif

        return entityToUpdate.Map();
    }

    /// <summary>
    /// Turns a recording made by the AI chat panel's microphone into the text it puts in the message box.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(MaxSpeechUploadSizeBytes)]
    [EnableRateLimiting(RateLimitOptionsExtensions.SPEECH)]
    public async Task<TranscribeSpeechResponseDto> TranscribeSpeech(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length is 0)
            throw new BadRequestException().WithData("Reason", "No recording provided.");

#pragma warning disable MEAI001 // ISpeechToTextClient is still experimental.
        var speechToTextClient = serviceProvider.GetService<ISpeechToTextClient>()
            ?? throw new InvalidOperationException($"No {nameof(ISpeechToTextClient)} is registered. Set AI:OpenAI:SpeechToTextApiKey to enable dictation.");

        // Which container the browser recorded in is decided by the browser (webm on chromium and firefox, mp4 on
        // safari) and is never sent, so the provider reads it out of the leading bytes - and only from a stream it
        // can rewind. An IFormFile's stream is not guaranteed to be either rewindable or at its start, so the
        // recording is buffered first. RequestSizeLimit above is what bounds that buffer.
        using MemoryStream recording = new();
        await using (var uploadedStream = file.OpenReadStream())
        {
            await uploadedStream.CopyToAsync(recording, cancellationToken);
        }
        recording.Position = 0;

        ChatbotMetrics.TranscribedBytes.Record(recording.Length);

        var response = await speechToTextClient.GetTextAsync(recording, new()
        {
            SpeechLanguage = CultureInfoManager.InvariantGlobalization ? null : CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
        }, cancellationToken);
#pragma warning restore MEAI001

        // For a whole-file transcription this is the length of the recording, the unit this half is billed in. Not
        // every provider fills it in.
        if (response.EndTime - response.StartTime is TimeSpan heard)
        {
            ChatbotMetrics.RecordTranscribedSeconds(heard.TotalSeconds, AppSettings.AI?.OpenAI?.SpeechToTextModel);
        }

        ChatbotMetrics.RecordSpeechToTextUsage(response.Usage, AppSettings.AI?.OpenAI?.SpeechToTextModel);

        return new() { Text = response.Text?.Trim() ?? string.Empty };
    }

    /// <summary>
    /// Reads a whole assistant answer out loud, answering with audio the AI chat panel plays through Butil.
    /// <para>
    /// It accepts the text itself rather than the id of a message because nothing stores the conversation -
    /// <c>AppChatbot</c> holds it in memory for the lifetime of one SignalR connection - so there is no id that
    /// would name an answer. The signature <c>AppChatbot</c> sent with the answer stands in for that id; without it
    /// this endpoint reads out whatever it is sent, which is a text to speech api billed to whoever runs the app.
    /// </para>
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(MaxSpeechUploadSizeBytes)]
    [EnableRateLimiting(RateLimitOptionsExtensions.SPEECH)]
    public async Task<IActionResult> SynthesizeSpeech(SynthesizeSpeechRequestDto request, CancellationToken cancellationToken)
    {
        // Before anything is read or reduced: whatever the answer turns out to be worth saying, this caller has to be
        // handing back words this assistant wrote.
        if (answerSigner.Verify(request.Text, request.Signature) is false)
            throw new ForbiddenException().WithData("Reason", "Only an answer this assistant wrote can be read aloud.");

        var text = SpeakableText.FromMarkdown(request.Text);

        // An answer that was nothing but a code block or a picture has nothing left to say, and silence is not worth
        // paying a provider for.
        if (string.IsNullOrWhiteSpace(text))
            return NoContent();

#pragma warning disable MEAI001 // ITextToSpeechClient is still experimental.
        var textToSpeechClient = serviceProvider.GetService<ITextToSpeechClient>()
            ?? throw new InvalidOperationException($"No {nameof(ITextToSpeechClient)} is registered. Set AI:OpenAI:TextToSpeechApiKey to enable read aloud.");

        List<ReadOnlyMemory<byte>> spoken = [];
        string? mediaType = null;

        foreach (var segment in SpeakableText.Segment(text))
        {
            var response = await textToSpeechClient.GetAudioAsync(segment, new()
            {
                VoiceId = AppSettings.AI?.OpenAI?.TextToSpeechVoice,
                AudioFormat = "mp3",
                Language = CultureInfoManager.InvariantGlobalization ? null : CultureInfo.CurrentUICulture.Name
            }, cancellationToken);

            var audio = response.Contents.OfType<DataContent>().FirstOrDefault()
                ?? throw new InvalidOperationException("The text to speech provider returned no audio.");

            spoken.Add(audio.Data);

            // The media type comes from the provider rather than being assumed: it decides the container, and the
            // browser needs to be told which one to decode. Every piece is the same request to the same deployment,
            // so the first speaks for all.
            mediaType ??= audio.MediaType;

            ChatbotMetrics.SynthesizedCharacters.Add(segment.Length);
        }
#pragma warning restore MEAI001

        return File(Join(spoken), mediaType!);
    }

    /// <summary>
    /// Starts a voice call. Audio flows browser-to-provider over WebRTC; the call itself is created here with the
    /// server's key, prompt and tools, and <see cref="VoiceCallRunner"/> stays in it.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitOptionsExtensions.SPEECH)]
    public async Task<StartVoiceCallResponseDto> StartVoiceCall(StartVoiceCallRequestDto request, CancellationToken cancellationToken)
    {
        var voiceCallRunner = serviceProvider.GetService<VoiceCallRunner>()
            ?? throw new InvalidOperationException($"No {nameof(VoiceCallRunner)} is registered. Set AI:OpenAI:RealtimeApiKey to enable voice calls.");

        // Where the app-side tools act: this session's most recently connected tab or app (See UserSession.SignalRConnectionId).
        var signalRConnectionId = await DbContext.UserSessions
            .Where(us => us.Id == User.GetSessionId())
            .Select(us => us.SignalRConnectionId)
            .FirstOrDefaultAsync(cancellationToken);

        var webAppUrl = Request.GetWebAppUrl();

        var culture = request.CultureId is int cultureId && CultureInfoManager.SupportedCultures.Any(sc => sc.Culture.LCID == cultureId)
            ? CultureInfo.GetCultureInfo(cultureId)
            : null;

        // The text chat's prompt and variables (See AppChatbot.ProcessNewMessage). The realtime model speaks plainly on its
        // own, but that prompt explicitly asks for markdown links, images and headings - hence the override.
        var instructions = $$$"""
            {{{SystemPromptProvider.GetSystemPrompt(PromptKind.Support, HttpContext.RequestServices)}}}

            ### Voice call:
            This is a live voice call and nothing you say is shown as text: ignore the markdown, link, image and heading rules above, and never read out a URL. Your tools still show the user cards and suggestions on the screen. When a tool asks the user to approve something there, tell them to tap the button: a spoken yes is not an approval.

            ### Variables:
            {{UserCulture}}: "{{{culture?.NativeName ?? "English"}}}"
            {{DeviceInfo}}: "{{{SystemPromptProvider.SanitizeVariable(request.DeviceInfo) ?? "Generic Device"}}}"
            {{UserTimeZoneId}}: "{{{SystemPromptProvider.KnownTimeZoneId(request.TimeZoneId) ?? "Unknown"}}}"
            {{IsAuthenticated}}: "True"
            {{WebAppUrl}}: "{{{SystemPromptProvider.EscapeVariable(webAppUrl.ToString())}}}"
            """;

        var answerSdp = await voiceCallRunner.Start(new(UserId: User.GetUserId(),
                                                        User: User.Clone(),
                                                        OfferSdp: request.Sdp,
                                                        Instructions: instructions,
                                                        SignalRConnectionId: signalRConnectionId,
                                                        BaseUrl: Request.GetBaseUrl(),
                                                        WebAppUrl: webAppUrl,
                                                        SafetyIdentifier: Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(User.GetUserId().ToByteArray())),
                                                        Language: culture?.TwoLetterISOLanguageName,
                                                        History: request.ChatMessagesHistory),
                                                    cancellationToken);

        return new() { Sdp = answerSdp, MaxDuration = voiceCallRunner.MaxCallDuration };
    }

    /// <summary>
    /// Lays the pieces of one answer end to end. mp3 - which <see cref="SynthesizeSpeech"/> asks for by name rather
    /// than leaving to the provider - is a stream of self contained frames, so one after another plays as a single
    /// recording. A container that describes the whole file in a header up front, wav being the obvious one, would
    /// need muxing, which is why the format is pinned there instead of taken as it comes.
    /// <para>
    /// Nearly every answer arrives as one piece and is handed straight back: the cap is on the spoken words, and the
    /// syntax, code blocks and urls that make an answer look long are gone by then.
    /// </para>
    /// </summary>
    private static byte[] Join(List<ReadOnlyMemory<byte>> spoken)
    {
        if (spoken.Count is 1)
            return spoken[0].ToArray();

        var joined = new byte[spoken.Sum(piece => piece.Length)];
        var offset = 0;

        foreach (var piece in spoken)
        {
            piece.Span.CopyTo(joined.AsSpan(offset));
            offset += piece.Length;
        }

        return joined;
    }
}
