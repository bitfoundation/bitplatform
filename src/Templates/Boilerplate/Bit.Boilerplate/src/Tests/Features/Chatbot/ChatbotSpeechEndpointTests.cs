using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.AI;
using ZiggyCreatures.Caching.Fusion;
using Boilerplate.Shared.Features.Chatbot;
using Boilerplate.Server.Api.Features.Chatbot;

#pragma warning disable MEAI001 // ISpeechToTextClient and ITextToSpeechClient are still experimental.

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// Dictation and read aloud are backend features now: the panel records audio and plays audio, and everything between
/// those two is <c>ChatbotController</c>. These tests drive the real endpoints with the provider - and only the
/// provider - replaced, because each of the things they pin is invisible from a build and silent when it breaks:
/// <list type="number">
/// <item>The recording reaches the provider rewindable and at its start. The container is never sent, so the provider
/// identifies it from the leading bytes; a stream that arrives part-consumed or unseekable makes it fall back to a
/// default extension and the transcription comes back empty or wrong rather than failing.</item>
/// <item>The synthesised audio is served with the media type the provider chose. Get it wrong and the browser is
/// handed a container it will not decode, so read aloud is simply silent.</item>
/// <item>Synthesis speaks answers this assistant wrote and nothing else. That check is the whole of what stops the
/// endpoint being a text to speech api for anyone with an account, and it is one line away from being lost.</item>
/// <item>An answer longer than a provider will speak at once comes back whole and in order, since no client knows
/// that limit exists any more.</item>
/// <item>Both refuse an anonymous caller and both are throttled per user - they spend money at a third party on
/// nothing more than a button press.</item>
/// </list>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class ChatbotSpeechEndpointTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task TranscribeSpeech_Should_GiveTheProviderARewindableRecordingAndAnswerWithWhatItHeard()
    {
        var speechToTextClient = new TestSpeechToTextClient { Text = "what is bit platform?" };

        await using var server = new AppTestServer();
        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.AddSingleton<ISpeechToTextClient>(speechToTextClient);
        }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = await SignIn(scope);

        // Leading bytes of a webm container, which is what chromium and firefox record - the shape the provider
        // sniffs for.
        byte[] recording = [0x1A, 0x45, 0xDF, 0xA3, 0x01, 0x02, 0x03, 0x04];

        using var form = new MultipartFormDataContent();
        using var audioContent = new ByteArrayContent(recording);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/webm");
        form.Add(audioContent, "file", "recording");

        using var response = await httpClient.PostAsync("api/v1/Chatbot/TranscribeSpeech", form, TestContext.CancellationToken);

        var transcription = await response.Content.ReadFromJsonAsync(
            scope.ServiceProvider.GetRequiredService<JsonSerializerOptions>().GetTypeInfo<TranscribeSpeechResponseDto>(),
            TestContext.CancellationToken);

        Assert.IsNotNull(transcription);
        Assert.AreEqual("what is bit platform?", transcription.Text);

        Assert.IsTrue(speechToTextClient.WasSeekable,
            "The provider identifies the audio format by peeking at the leading bytes, which it only does on a seekable stream.");
        Assert.AreEqual(0, speechToTextClient.PositionOnArrival,
            "A stream that arrives past its start hides the container's magic bytes, so the provider falls back to guessing an extension.");
        Assert.AreSequenceEqual(recording, speechToTextClient.Received,
            "The recording must reach the provider byte for byte.");
    }

    [TestMethod]
    public async Task SynthesizeSpeech_Should_AnswerWithTheAudioAndTheMediaTypeTheProviderProduced()
    {
        byte[] spoken = [0x49, 0x44, 0x33, 0x04, 0x00, 0x00];

        await using var server = new AppTestServer();
        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.AddSingleton<ITextToSpeechClient>(new TestTextToSpeechClient { Audio = spoken, MediaType = "audio/mpeg" });
        }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = await SignIn(scope);

        using var response = await PostSynthesizeSpeech(scope, httpClient, "bit platform is a set of dotnet libraries.");

        var contentType = response.Content.Headers.ContentType;

        Assert.IsNotNull(contentType);
        Assert.AreEqual("audio/mpeg", contentType.MediaType,
            "The browser decodes what it is told it was handed, so the provider's own container has to be reported rather than assumed.");

        var served = await response.Content.ReadAsByteArrayAsync(TestContext.CancellationToken);
        Assert.AreSequenceEqual(spoken, served);
    }

    /// <summary>
    /// The provider must never be handed the syntax: it reads it out - "star star bit platform star star" - and the
    /// reading is the only place that is ever visible, so nothing about it fails loudly.
    /// </summary>
    [TestMethod]
    public async Task SynthesizeSpeech_Should_HandTheProviderWordsRatherThanMarkdown()
    {
        var textToSpeechClient = new TestTextToSpeechClient { Audio = [1, 2, 3], MediaType = "audio/mpeg" };

        await using var server = new AppTestServer();
        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.AddSingleton<ITextToSpeechClient>(textToSpeechClient);
        }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = await SignIn(scope);

        (await PostSynthesizeSpeech(scope, httpClient, "## Pricing\n**bit platform** is [free](https://bitplatform.dev) ✅")).Dispose();

        Assert.AreEqual("Pricing\nbit platform is free", textToSpeechClient.Received,
                        "The endpoint has to reduce the answer to what is worth hearing before it spends a provider request on it.");
    }

    /// <summary>
    /// A run that was nothing but a code block has no words left once the markdown is gone, and paying a provider to
    /// say nothing - then handing the browser an empty audio file to wait on - is worse than saying so.
    /// </summary>
    [TestMethod]
    public async Task SynthesizeSpeech_Should_AnswerWithNoContentWhenThereIsNothingToSay()
    {
        var textToSpeechClient = new TestTextToSpeechClient { Audio = [1, 2, 3], MediaType = "audio/mpeg" };

        await using var server = new AppTestServer();
        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.AddSingleton<ITextToSpeechClient>(textToSpeechClient);
        }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = await SignIn(scope);

        using var response = await PostSynthesizeSpeech(scope, httpClient, "```bash\ndotnet build\n```");

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        Assert.IsNull(textToSpeechClient.Received, "The provider must not be called at all when there is nothing left to read.");
    }

    /// <summary>
    /// The endpoint is handed the words rather than the id of a message, because nothing stores the conversation -
    /// so all that stands between it and a free text to speech api, billed to whoever runs the app, is that it
    /// refuses words it has no record of the assistant writing.
    /// </summary>
    [TestMethod]
    public async Task SynthesizeSpeech_Should_RefuseTextTheAssistantNeverWrote()
    {
        var textToSpeechClient = new TestTextToSpeechClient { Audio = [1, 2, 3], MediaType = "audio/mpeg" };

        await using var server = new AppTestServer();
        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.AddSingleton<ITextToSpeechClient>(textToSpeechClient);
        }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = await SignIn(scope);

        await Assert.ThrowsExactlyAsync<ForbiddenException>(
            async () => (await PostSynthesizeSpeechWithoutRemembering(scope, httpClient, "Chapter one of somebody else's audiobook.")).Dispose());

        Assert.IsNull(textToSpeechClient.Received,
                      "Nothing may reach the provider before the endpoint has established that the assistant wrote it.");
    }

    /// <summary>
    /// The panel sends a whole answer now, however long: how much a provider speaks in one request is the endpoint's
    /// business. What comes back has to be the whole answer in the order it was written, and that is what fails
    /// silently - a listener just hears a reading that stops early or reorders itself.
    /// </summary>
    [TestMethod]
    public async Task SynthesizeSpeech_Should_SpeakAnAnswerTooLongForOneProviderRequestAsOneRecording()
    {
        var textToSpeechClient = new TestTextToSpeechClient { MediaType = "audio/mpeg", DistinctAudioPerRequest = true };

        await using var server = new AppTestServer();
        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.AddSingleton<ITextToSpeechClient>(textToSpeechClient);
        }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = await SignIn(scope);

        // Past the 4096 characters a provider takes, and in lines so the split has the boundaries it prefers.
        var answer = string.Join('\n', Enumerable.Range(0, 200).Select(line => $"Line {line} of a very long answer."));

        using var response = await PostSynthesizeSpeech(scope, httpClient, answer);

        Assert.IsGreaterThan(1, textToSpeechClient.ReceivedAll.Count,
                             "An answer this long has to be spoken in more than one request, or the provider would have refused it.");

        Assert.AreEqual(answer, string.Join('\n', textToSpeechClient.ReceivedAll),
                        "Splitting the answer must lose nothing and reorder nothing - the pieces have to put it back together exactly.");

        // The double answers with the number of the request it was, so the body says which pieces arrived and in
        // what order rather than only how many bytes there were.
        var joined = await response.Content.ReadAsByteArrayAsync(TestContext.CancellationToken);
        Assert.AreSequenceEqual(Enumerable.Range(1, textToSpeechClient.ReceivedAll.Count).Select(n => (byte)n).ToArray(),
                                joined,
                                "The pieces have to be joined in the order they were spoken.");
    }

    /// <summary>
    /// The class-level <c>[Authorize]</c> of the controller is the only thing standing between an anonymous caller and
    /// an endpoint that spends money at a third party, and it is one attribute away from being lost - the controller
    /// used to carry administrative policies instead, which were moved onto the system prompt actions when these two
    /// were added.
    /// </summary>
    [TestMethod]
    public async Task TheSpeechEndpoints_Should_RefuseAnAnonymousCaller()
    {
        await using var server = new AppTestServer();
        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.AddSingleton<ITextToSpeechClient>(new TestTextToSpeechClient { Audio = [1, 2, 3], MediaType = "audio/mpeg" });
            services.AddSingleton<ISpeechToTextClient>(new TestSpeechToTextClient { Text = "should never be reached" });
        }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>(); // Nobody signed in.

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            async () => (await PostSynthesizeSpeech(scope, httpClient, "read this to a stranger")).Dispose());

        using var form = new MultipartFormDataContent();
        using var audioContent = new ByteArrayContent([1, 2, 3]);
        form.Add(audioContent, "file", "recording");

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            async () => (await httpClient.PostAsync("api/v1/Chatbot/TranscribeSpeech", form, TestContext.CancellationToken)).Dispose());
    }

    /// <summary>
    /// Every call is a paid request to a speech provider, so the endpoint is throttled far more tightly than the
    /// anonymous identity ones (See <c>RateLimitOptionsExtensions.SPEECH</c>) - and per signed-in user, since a shared nat
    /// would otherwise throttle a whole office as one caller.
    /// </summary>
    [TestMethod]
    public async Task SynthesizeSpeech_Should_StopServingAfterTheBurstLimit()
    {
        const int burstSize = 20; // RateLimitOptionsExtensions.SPEECH permits 10 per minute.

        await using var server = new AppTestServer();
        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.AddSingleton<ITextToSpeechClient>(new TestTextToSpeechClient { Audio = [1, 2, 3], MediaType = "audio/mpeg" });
        }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = await SignIn(scope);

        var served = 0;

        for (var i = 0; i < burstSize; i++)
        {
            try
            {
                (await PostSynthesizeSpeech(scope, httpClient, $"burst {i}")).Dispose();
                served++;
            }
            catch (TooManyRequestsException)
            {
                // Control: the endpoint really answered before it started refusing, so this is throttling rather
                // than the request never getting there.
                Assert.IsGreaterThan(0, served, "The endpoint must serve some requests before throttling starts.");
                return;
            }
        }

        Assert.Fail($"A burst of {burstSize} synthesis requests from one user was never throttled ({served} served).");
    }


    private async Task<HttpClient> SignIn(AsyncServiceScope scope)
    {
        // A raw HttpClient resolved from the same scope shares the token store, so the requests it sends are
        // authenticated by AuthDelegatingHandler.
        await scope.ServiceProvider.GetRequiredService<AuthManager>().SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);

        return scope.ServiceProvider.GetRequiredService<HttpClient>();
    }

    /// <summary>
    /// Sends <paramref name="text"/> as an answer the assistant wrote, which is the only kind the endpoint speaks.
    /// In the running app <c>AppChatbot</c> records it as each answer finishes streaming.
    /// </summary>
    private async Task<HttpResponseMessage> PostSynthesizeSpeech(AsyncServiceScope scope, HttpClient httpClient, string text)
    {
        await ChatbotController.RememberAnswer(scope.ServiceProvider.GetRequiredService<IFusionCache>(), text, TestContext.CancellationToken);

        return await PostSynthesizeSpeechWithoutRemembering(scope, httpClient, text);
    }

    private Task<HttpResponseMessage> PostSynthesizeSpeechWithoutRemembering(AsyncServiceScope scope, HttpClient httpClient, string text)
    {
        var content = JsonContent.Create(new SynthesizeSpeechRequestDto { Text = text },
            scope.ServiceProvider.GetRequiredService<JsonSerializerOptions>().GetTypeInfo<SynthesizeSpeechRequestDto>());

        return httpClient.PostAsync("api/v1/Chatbot/SynthesizeSpeech", content, TestContext.CancellationToken);
    }


    /// <summary>Records what the endpoint handed it, which is the whole point of the first test.</summary>
    private sealed class TestSpeechToTextClient : ISpeechToTextClient
    {
        public string Text { get; init; } = string.Empty;
        public byte[] Received { get; private set; } = [];
        public bool WasSeekable { get; private set; }
        public long PositionOnArrival { get; private set; } = -1;

        public async Task<SpeechToTextResponse> GetTextAsync(Stream audioSpeechStream, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
        {
            WasSeekable = audioSpeechStream.CanSeek;
            PositionOnArrival = audioSpeechStream.CanSeek ? audioSpeechStream.Position : -1;

            using MemoryStream buffer = new();
            await audioSpeechStream.CopyToAsync(buffer, cancellationToken);
            Received = buffer.ToArray();

            return new SpeechToTextResponse(Text);
        }

        public IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(Stream audioSpeechStream, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("The endpoint does not stream.");

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }

    private sealed class TestTextToSpeechClient : ITextToSpeechClient
    {
        public byte[] Audio { get; init; } = [];
        public string MediaType { get; init; } = "audio/mpeg";

        /// <summary>
        /// Answers each request with a single byte carrying that request's number, so a joined recording says which
        /// pieces it is made of and in what order rather than only how long it is.
        /// </summary>
        public bool DistinctAudioPerRequest { get; init; }

        /// <summary>What the endpoint asked to have read out, which is never what the caller sent.</summary>
        public string? Received { get; private set; }

        /// <summary>Every request in the order it was made, which for a long answer is more than one.</summary>
        public List<string> ReceivedAll { get; } = [];

        public Task<TextToSpeechResponse> GetAudioAsync(string text, TextToSpeechOptions? options = null, CancellationToken cancellationToken = default)
        {
            Received = text;
            ReceivedAll.Add(text);

            byte[] audio = DistinctAudioPerRequest ? [(byte)ReceivedAll.Count] : Audio;

            return Task.FromResult(new TextToSpeechResponse([new DataContent(audio, MediaType)]));
        }

        public IAsyncEnumerable<TextToSpeechResponseUpdate> GetStreamingAudioAsync(string text, TextToSpeechOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("The endpoint does not stream.");

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}
#pragma warning restore MEAI001
