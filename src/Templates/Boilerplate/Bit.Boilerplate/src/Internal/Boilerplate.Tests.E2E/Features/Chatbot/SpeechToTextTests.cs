using System.Net.Http.Headers;
using Boilerplate.Shared.Features.Chatbot;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Chatbot;

/// <summary>
/// The dictation endpoint the chat panel's microphone posts to, called from this process rather than a browser: a
/// sentence is spoken into a wav file and the deployment is asked what it heard.
/// <para>
/// Browserless on purpose - the recording is simply the request body, so a failure is the provider, the key or the
/// endpoint, never the browser's recorder. <c>WebAiChatbotDictationTests</c> covers the panel's own path.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public partial class SpeechToTextTests
{
    /// <summary>The kind of request a user would actually dictate into the chat panel.</summary>
    private const string SpokenSentence = "Please change the application language to Persian.";

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task TranscribeSpeech_Should_ReturnWhatWasSaid()
    {
        var wavFile = await SpokenAudio.WavFileOf(SpokenSentence, TestContext.CancellationToken);

        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);

        using MultipartFormDataContent form = [];
        var recording = new ByteArrayContent(await File.ReadAllBytesAsync(wavFile, TestContext.CancellationToken));
        recording.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        // The name the endpoint's IFormFile parameter binds by; the file name isn't read, the container comes from the
        // leading bytes (See ChatbotController.TranscribeSpeech).
        form.Add(recording, name: "file", fileName: "recording.wav");

        using var response = await globalApiClient.HttpClient.PostAsync("api/v1/Chatbot/TranscribeSpeech", form, TestContext.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Without AI:OpenAI:SpeechToTextApiKey the panel doesn't offer the microphone either, so this is a coverage
        // gap rather than a failure. The name is what ChatbotController throws by.
        if (response.IsSuccessStatusCode is false && body.Contains("ISpeechToTextClient", StringComparison.Ordinal))
            Assert.Inconclusive("This deployment has no speech to text client configured, so dictation is off there.");

        Assert.IsTrue(response.IsSuccessStatusCode, $"The deployment answered {(int)response.StatusCode} to a {new FileInfo(wavFile).Length} byte recording: {body}");

        var transcription = JsonSerializer.Deserialize<TranscribeSpeechResponseDto>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // Word for word is the wrong bar: transcribed synthetic speech loses punctuation and mishears the odd word, so
        // what has to survive is the meaning.
        await AiAnswerJudge.AssertAnswer($"[a recording of someone saying] {SpokenSentence}",
            $"The transcription is a plain, faithful transcription of that sentence - the same request, in the same " +
            $"words or near enough that a person reading it would act on it identically. Small differences in " +
            $"punctuation, casing or a single misheard word are fine.",
            transcription?.Text, TestContext.CancellationToken);
    }
}
