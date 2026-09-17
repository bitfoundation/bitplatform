using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Boilerplate.Server.Api;
using Boilerplate.Server.Api.Features.Chatbot;
using Boilerplate.Server.Api.Features.Chatbot.VoiceCall;

namespace Boilerplate.Tests.Features.Chatbot;

#pragma warning disable MEAI001 // ISpeechToTextClient and ITextToSpeechClient are still experimental.
/// <summary>
/// What aiSpeechToText, aiTextToSpeech and aiRealtime send, and how each answer is judged.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class SpeechHealthChecksTests
{
    private static readonly string[] checkNames = ["aiSpeechToText", "aiTextToSpeech", "aiRealtime"];

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task Checks_Should_SendMinimalRequests_WithTheFeaturesSettings()
    {
        var providers = new FakeProviders();

        var report = await RunChecks(providers);

        foreach (var name in checkNames)
        {
            Assert.AreEqual(HealthStatus.Healthy, report.Entries[name].Status, $"{name}: {report.Entries[name].Exception?.Message}");
        }

        // Text to speech: two characters, in the voice read aloud uses.
        Assert.AreEqual("OK", providers.SpokenText);
        Assert.IsNotNull(providers.SpokenOptions);
        Assert.AreEqual("test-voice", providers.SpokenOptions.VoiceId);

        // Speech to text: half a second of 16 kHz, 16-bit, mono silence in a WAV container.
        Assert.IsNotNull(providers.TranscribedAudio);
        Assert.AreEqual("RIFF", Encoding.ASCII.GetString(providers.TranscribedAudio, 0, 4));
        Assert.AreEqual("WAVE", Encoding.ASCII.GetString(providers.TranscribedAudio, 8, 4));
        Assert.HasCount(44 + 16_000, providers.TranscribedAudio, "Half a second at 32,000 bytes per second, after the 44 byte header.");
        Assert.IsTrue(providers.TranscribedAudio.Skip(44).All(b => b is 0), "The samples must be silence.");

        // Realtime: the call's own session settings, without the call's prompt or tools.
        Assert.IsNotNull(providers.RealtimeSession);
        Assert.AreEqual("test-realtime-model", providers.RealtimeSession["model"]!.GetValue<string>());
        Assert.AreEqual("test-realtime-voice", providers.RealtimeSession["audio"]!["output"]!["voice"]!.GetValue<string>());
        Assert.IsEmpty(providers.RealtimeSession["tools"]!.AsArray());
    }

    [TestMethod]
    public async Task Checks_Should_ReportDegraded_WhenAProviderFails()
    {
        var providers = new FakeProviders { Failure = new HttpRequestException("insufficient_quota") };

        var report = await RunChecks(providers);

        foreach (var name in checkNames)
        {
            Assert.AreEqual(HealthStatus.Degraded, report.Entries[name].Status, name);
        }
    }

    [TestMethod]
    public async Task TextToSpeechCheck_Should_ReportDegraded_WhenNoAudioComesBack()
    {
        var providers = new FakeProviders { SpokenAudio = [] };

        var report = await RunChecks(providers);

        Assert.AreEqual(HealthStatus.Degraded, report.Entries["aiTextToSpeech"].Status);
    }

    [TestMethod]
    public async Task CreateClientSecret_Should_PostTheSession_WithTheShortestExpiry()
    {
        HttpRequestMessage? sentRequest = null;
        string? sentBody = null;
        var handler = new CapturingHandler(async (request, cancellationToken) =>
        {
            sentRequest = request;
            sentBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"value":"ek_secret"}""") };
        });

        var client = CreateRealtimeCallClient(handler);

        await client.CreateClientSecret(new JsonObject { ["type"] = "realtime", ["model"] = "test-realtime-model" }, TestContext.CancellationToken);

        Assert.IsNotNull(sentRequest);
        Assert.AreEqual(HttpMethod.Post, sentRequest.Method);
        Assert.AreEqual("https://api.openai.com/v1/realtime/client_secrets", sentRequest.RequestUri!.AbsoluteUri);
        Assert.AreEqual("Bearer test-realtime-key", sentRequest.Headers.Authorization!.ToString());

        var body = JsonNode.Parse(sentBody!)!;
        Assert.AreEqual(10, body["expires_after"]!["seconds"]!.GetValue<int>());
        Assert.AreEqual("created_at", body["expires_after"]!["anchor"]!.GetValue<string>());
        Assert.AreEqual("test-realtime-model", body["session"]!["model"]!.GetValue<string>());
    }

    [TestMethod]
    public async Task CreateClientSecret_Should_Throw_WhenTheProviderRefuses()
    {
        var handler = new CapturingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":{"message":"Invalid voice"}}""")
        }));

        var client = CreateRealtimeCallClient(handler);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(()
            => client.CreateClientSecret(new JsonObject { ["type"] = "realtime" }, TestContext.CancellationToken));

        Assert.Contains("400", exception.Message);
        Assert.Contains("Invalid voice", exception.Message);
    }

    private async Task<HealthReport> RunChecks(FakeProviders providers)
    {
        await using var server = new AppTestServer();

        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.Replace(ServiceDescriptor.Singleton(providers.SpeechToTextClient));
            services.Replace(ServiceDescriptor.Singleton(providers.TextToSpeechClient));
            services.Replace(ServiceDescriptor.Singleton<OpenAIRealtimeCallClient>(providers.RealtimeCallClient));
        },
        configuration =>
        {
            // The keys are never used: every client they would build is replaced above.
            configuration["AI:OpenAI:SpeechToTextApiKey"] = "fake-key";
            configuration["AI:OpenAI:TextToSpeechApiKey"] = "fake-key";
            configuration["AI:OpenAI:TextToSpeechVoice"] = "test-voice";
            configuration["AI:OpenAI:RealtimeApiKey"] = "fake-key";
            configuration["AI:OpenAI:RealtimeModel"] = "test-realtime-model";
            configuration["AI:OpenAI:RealtimeVoice"] = "test-realtime-voice";
        }).Start(TestContext.CancellationToken);

        return await server.WebApp.Services.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(r => checkNames.Contains(r.Name), TestContext.CancellationToken);
    }

    private static OpenAIRealtimeCallClient CreateRealtimeCallClient(HttpMessageHandler handler)
    {
        var httpClientFactory = A.Fake<IHttpClientFactory>();
        A.CallTo(() => httpClientFactory.CreateClient("AI")).ReturnsLazily(() => new HttpClient(handler, disposeHandler: false));

        var settings = new ServerApiSettings { AI = new() { OpenAI = new() { RealtimeApiKey = "test-realtime-key" } } };

        return new OpenAIRealtimeCallClient(settings, httpClientFactory);
    }

    private sealed class FakeProviders
    {
        public Exception? Failure { get; init; }

        public byte[] SpokenAudio { get; init; } = [1, 2, 3];

        public string? SpokenText { get; private set; }

        public TextToSpeechOptions? SpokenOptions { get; private set; }

        public byte[]? TranscribedAudio { get; private set; }

        public JsonObject? RealtimeSession { get; private set; }

        public ISpeechToTextClient SpeechToTextClient { get; }

        public ITextToSpeechClient TextToSpeechClient { get; }

        public OpenAIRealtimeCallClient RealtimeCallClient { get; }

        public FakeProviders()
        {
            SpeechToTextClient = A.Fake<ISpeechToTextClient>();
            A.CallTo(() => SpeechToTextClient.GetTextAsync(A<Stream>._, A<SpeechToTextOptions?>._, A<CancellationToken>._))
                .ReturnsLazily((Stream audio, SpeechToTextOptions? _, CancellationToken _) =>
                {
                    using MemoryStream copy = new();
                    audio.CopyTo(copy);
                    TranscribedAudio = copy.ToArray();
                    return Failure is null ? Task.FromResult(new SpeechToTextResponse(string.Empty)) : Task.FromException<SpeechToTextResponse>(Failure);
                });

            TextToSpeechClient = A.Fake<ITextToSpeechClient>();
            A.CallTo(() => TextToSpeechClient.GetAudioAsync(A<string>._, A<TextToSpeechOptions?>._, A<CancellationToken>._))
                .ReturnsLazily((string text, TextToSpeechOptions? options, CancellationToken _) =>
                {
                    SpokenText = text;
                    SpokenOptions = options;
                    return Failure is null
                        ? Task.FromResult(new TextToSpeechResponse([new DataContent(SpokenAudio, "audio/mpeg")]))
                        : Task.FromException<TextToSpeechResponse>(Failure);
                });

            RealtimeCallClient = new FakeRealtimeCallClient(this);
        }

        private sealed class FakeRealtimeCallClient(FakeProviders providers) : OpenAIRealtimeCallClient(new ServerApiSettings(), null!)
        {
            public override Task CreateClientSecret(JsonObject session, CancellationToken cancellationToken)
            {
                providers.RealtimeSession = session;
                return providers.Failure is null ? Task.CompletedTask : Task.FromException(providers.Failure);
            }
        }
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => respond(request, cancellationToken);
    }
}
#pragma warning restore MEAI001
