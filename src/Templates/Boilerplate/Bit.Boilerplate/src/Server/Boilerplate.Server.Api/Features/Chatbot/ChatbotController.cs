//+:cnd:noEmit
using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using Microsoft.AspNetCore.RateLimiting;
using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Server.Api.Features.Chatbot;

[ApiVersion(1)]
[ApiController, Route("api/v{v:apiVersion}/[controller]/[action]"), Authorize]
public partial class ChatbotController : AppControllerBase, IChatbotController
{
    [AutoInject] private IFusionCache cache = default!;
    [AutoInject] private IServiceProvider serviceProvider = default!;

    /// <summary>The largest recording the speech endpoints accept; the Dev MCP reports this value rather than a copy of it.</summary>
    public const int MaxSpeechUploadSizeBytes = 2 * 1024 * 1024;

    // For open telemetry metrics. Both providers bill by characters spoken and seconds heard rather than by request,
    // so a request count says nothing about what speech costs - these are what any limit worth setting is chosen
    // from, and what shows a bill running away before the invoice does.
    private static readonly Counter<long> synthesizedCharactersCounter = Meter.Current.CreateCounter<long>("chatbot.synthesized_characters", "{character}", "Characters handed to the text to speech provider.");
    private static readonly Histogram<long> transcribedBytesHistogram = Meter.Current.CreateHistogram<long>("chatbot.transcribed_bytes", "By", "Size of each recording handed to the speech to text provider.");
    /// <summary>Only some providers report where in the audio each stretch of text was heard, so this is thinner than <see cref="transcribedBytesHistogram"/>, which is always recorded.</summary>
    private static readonly Histogram<double> transcribedSecondsHistogram = Meter.Current.CreateHistogram<double>("chatbot.transcribed_seconds", "s", "Length of each recording handed to the speech to text provider, where it reports one.");

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

        transcribedBytesHistogram.Record(recording.Length);

        var response = await speechToTextClient.GetTextAsync(recording, new()
        {
            SpeechLanguage = CultureInfoManager.InvariantGlobalization ? null : CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
        }, cancellationToken);
#pragma warning restore MEAI001

        // For a whole-file transcription this is the length of the recording, the unit this half is billed in. Not
        // every provider fills it in.
        if (response.EndTime - response.StartTime is TimeSpan heard)
        {
            transcribedSecondsHistogram.Record(heard.TotalSeconds);
        }

        return new() { Text = response.Text?.Trim() ?? string.Empty };
    }

    /// <summary>
    /// Reads a whole assistant answer out loud, answering with audio the AI chat panel plays through Butil.
    /// <para>
    /// It accepts the text itself rather than the id of a message because nothing stores the conversation -
    /// <c>AppChatbot</c> holds it in memory for the lifetime of one SignalR connection - so there is no id that
    /// would name an answer. <see cref="RememberAnswer"/> stands in for that id; without it this endpoint reads out
    /// whatever it is sent, which is a text to speech api billed to whoever runs the app.
    /// </para>
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(MaxSpeechUploadSizeBytes)]
    [EnableRateLimiting(RateLimitOptionsExtensions.SPEECH)]
    public async Task<IActionResult> SynthesizeSpeech(SynthesizeSpeechRequestDto request, CancellationToken cancellationToken)
    {
        // Before anything is read or reduced: whatever the answer turns out to be worth saying, this caller has to be
        // handing back words this assistant wrote.
        if (await cache.GetOrDefaultAsync(AnswerCacheKey(request.Text), false, token: cancellationToken) is false)
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

            synthesizedCharactersCounter.Add(segment.Length);
        }
#pragma warning restore MEAI001

        return File(Join(spoken), mediaType!);
    }

    /// <summary>
    /// How long an answer stays speakable. Long enough that a user scrolling back to something said early in the
    /// conversation still gets it, short enough that the record - one small entry per answer - does not accumulate.
    /// </summary>
    private static readonly TimeSpan AnswersStaySpeakableFor = TimeSpan.FromHours(2);

    /// <summary>
    /// Records that this assistant wrote <paramref name="markdown"/>, which is what lets
    /// <see cref="SynthesizeSpeech"/> read it out. Called by <c>AppChatbot</c> as each answer finishes streaming.
    /// </summary>
    public static async Task RememberAnswer(IFusionCache cache, string markdown, CancellationToken cancellationToken)
    {
        await cache.SetAsync(AnswerCacheKey(markdown), true, options => options.Duration = AnswersStaySpeakableFor, cancellationToken);
    }

    /// <summary>
    /// The markdown as it was written rather than what <see cref="SpeakableText.FromMarkdown"/> makes of it: reducing
    /// an answer costs a pass of regexes, and on the recording side nobody knows yet whether the speaker will ever be
    /// pressed. Hashed because the entry is only ever compared for equality.
    /// </summary>
    private static string AnswerCacheKey(string markdown)
    {
        return $"SpeakableAnswer_{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(markdown)))}";
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
