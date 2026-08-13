using Microsoft.Extensions.AI;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// Dictation writes what the recognizer hears into the message box, and the box is what the user sends - so a word
/// written twice is a word they have to delete before they can send anything. Writing it twice is the easy mistake:
/// a session reports the same utterance over and over while it revises what it heard, and Android's recognizer
/// reports it again after it has settled, so a transcript that appends what arrives turns a single spoken "hello"
/// into "hellohello". The recognizer is scripted here (see <see cref="AiChatPanelTestBase.MicrophoneProbeScript"/>) -
/// a headless browser has no microphone, and what is under test is what the panel does with what it is told.
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public class AiChatPanelDictationUITests : AiChatPanelTestBase
{
    [TestMethod]
    public async Task Dictation_Should_WriteEachUtteranceOnce_WhenTheRecognizerRepeatsItself()
    {
        await using var server = new AppTestServer(Context);

        await server.Build(services =>
        {
            // Nothing is sent in this test, but the panel is only offered when the chatbot is there to talk to.
            services.Replace(ServiceDescriptor.Singleton<IChatClient>(new TestChatClient()));
        },
        configuration =>
        {
            // Without a chat api key the AI agents are never registered and the panel would have nothing to talk to.
            configuration["AI:OpenAI:ChatApiKey"] = "fake-key-never-used-by-these-tests";
        }).Start(TestContext.CancellationToken);

        // Before the app loads: the panel asks whether speech is supported on its first render, and takes the answer
        // as final for the lifetime of the component.
        await Page.AddInitScriptAsync(MicrophoneProbeScript);

        await Page.GotoAsync(new Uri(server.WebAppServerAddress, PageUrls.Home).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        var panel = await OpenChatPanel();
        var messageBox = panel.Locator("textarea");

        await panel.GetByTitle(AppStrings.AiChatPanelDictate).ClickAsync();
        await Expect(panel.GetByTitle(AppStrings.AiChatPanelStopDictation)).ToBeVisibleAsync();

        // One utterance, heard as it is spoken and then settled...
        await ReportSpeech("hel", isFinal: false);
        await ReportSpeech("hello", isFinal: false);
        await ReportSpeech("hello", isFinal: true);

        // ... and reported once more after it was settled, which is what Android's recognizer does.
        await ReportSpeech("hello", isFinal: true);

        await Expect(messageBox).ToHaveValueAsync("hello");

        // A session ends at the end of an utterance and the panel opens the next one by itself, so dictation carries
        // on - and what the next session hears is a new utterance rather than more of the last one.
        await EndSession();
        await ReportSpeech("world", isFinal: true);

        await Expect(messageBox).ToHaveValueAsync("hello world");

        // Stop leaves the transcript in the box for the user to read, correct and send, which is the whole reason
        // dictation writes there instead of sending what it heard.
        await panel.GetByTitle(AppStrings.AiChatPanelStopDictation).ClickAsync();
        await Expect(panel.GetByTitle(AppStrings.AiChatPanelDictate)).ToBeVisibleAsync();

        await Expect(messageBox).ToHaveValueAsync("hello world");
    }

    /// <summary>Reports one transcript to the recognizer that is listening, the way a real engine would.</summary>
    private Task ReportSpeech(string transcript, bool isFinal)
    {
        return Page.EvaluateAsync("""
            speech => {
                const result = [{ transcript: speech.transcript, confidence: 1 }];
                result.isFinal = speech.isFinal;
                window.__microphoneProbe.recognizer.onresult({ resultIndex: 0, results: [result] });
            }
            """, new { transcript, isFinal });
    }

    /// <summary>
    /// Ends the session the way the browser does when an utterance is over, and waits for the panel to have opened
    /// the next one: speech reported in between would go to a recognizer that nobody is listening to any more.
    /// </summary>
    private async Task EndSession()
    {
        var startsBefore = await Page.EvaluateAsync<int>("window.__microphoneProbe.startCount");

        await Page.EvaluateAsync("window.__microphoneProbe.recognizer.onend()");

        await Page.WaitForFunctionAsync($"() => window.__microphoneProbe.startCount > {startsBefore}");
    }
}
