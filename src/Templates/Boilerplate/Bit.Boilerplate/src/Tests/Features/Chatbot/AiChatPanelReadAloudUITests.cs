using Microsoft.Extensions.AI;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// Read aloud is a mode, not a one-shot: pressing it once means "read the answers to me from here on", so a user who
/// is listening rather than reading never has to reach for the button again. Three behaviours make that work and each
/// is easy to break without the others noticing, so one test walks all of them in the order a user would meet them:
/// <list type="number">
/// <item>The answer that is still arriving is read as it arrives, in runs, rather than in one go at the end - the
/// server streams at its own speed and the voice reads at its own, and neither waits for the other. A new answer
/// takes over from whatever is left of the previous one, at the moment it has enough to say and not before.</item>
/// <item>Opening the microphone silences the voice, and keeps it silent for as long as it is open - they would
/// otherwise talk over each other, and the engine's own voice is what the recognizer would hear. The mode itself
/// stays on, so the answer to what the user is about to say is read out without them asking again.</item>
/// <item>Stop means stop: not just the answer being read, but the following ones too.</item>
/// </list>
/// <para>
/// The browser's speech engine is replaced with a recorder (see <see cref="SpeechProbeScript"/>). That is the only
/// fake besides the model: a headless browser has no voice to listen to, and what is under test is which text the app
/// decides to hand over and when - not whether Chromium can pronounce it.
/// </para>
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public class AiChatPanelReadAloudUITests : AiChatPanelTestBase
{
    /// <summary>
    /// Stands in for <c>window.speechSynthesis</c>, recording what it is asked to say instead of saying it (See
    /// <c>BitButil.speech</c> for everything the app touches). The microphone half is
    /// <see cref="AiChatPanelTestBase.MicrophoneProbeScript"/>.
    /// <para>
    /// The utterance log is append-only and survives a cancel, so the test can tell "was never asked to say this"
    /// from "was asked, then cancelled" - which is the difference between the mode being off and merely silenced.
    /// </para>
    /// </summary>
    private const string SpeechProbeScript = """
        (() => {
            const probe = { utterances: [], cancelCount: 0 };
            window.__speechProbe = probe;

            window.SpeechSynthesisUtterance = function (text) { this.text = text ?? ''; };

            Object.defineProperty(window, 'speechSynthesis', {
                configurable: true,
                value: {
                    speak: u => probe.utterances.push(u.text),
                    cancel: () => probe.cancelCount++,
                    pause: () => { },
                    resume: () => { },
                    getVoices: () => [],
                    speaking: false,
                    pending: false
                }
            });
        })();
        """;

    // The two answers that are held half way are long enough in their first chunk to clear the minimum run the panel
    // holds text back to, so the steps that follow are testing the streaming and not the size gate. All of them are
    // free of markdown syntax, which the clean-up on the way to the engine legitimately rewrites.
    private const string SecondAnswerFirstChunk =
        "Bit platform is a set of dotnet libraries for building web, mobile and desktop apps from a single codebase. " +
        "It ships a Blazor component library, a project template and a collection of javascript utilities. ";
    private const string SecondAnswerLastChunk = "Everything in it is free and open source.";

    private const string ThirdAnswerFirstChunk =
        "Bswup is a service worker that keeps the app up to date in the background and lets it start while offline. " +
        "It reports its progress, so the app can show how far along the download of a new version has got. ";
    private const string ThirdAnswerLastChunk = "It is enabled by default in the template.";

    private const string FirstAnswer = "Bit BlazorUI is the component library.";
    private const string FourthAnswer = "Besql stores a real database inside the browser.";
    private const string FifthAnswer = "Butil wraps the browser apis that Blazor does not reach.";

    [TestMethod]
    public async Task ReadAloud_Should_FollowTheConversation_UntilTheUserStopsIt()
    {
        var chatClient = new TestChatClient
        {
            StreamingChunks = callIndex => callIndex switch
            {
                0 => [FirstAnswer],
                1 => [SecondAnswerFirstChunk, SecondAnswerLastChunk],
                2 => [ThirdAnswerFirstChunk, ThirdAnswerLastChunk],
                3 => [FourthAnswer],
                _ => [FifthAnswer]
            }
        };

        await using var server = new AppTestServer(Context);

        await server.Build(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IChatClient>(chatClient));
        },
        configuration =>
        {
            // Without a chat api key the AI agents are never registered and the panel would have nothing to talk to.
            configuration["AI:OpenAI:ChatApiKey"] = "fake-key-never-used-by-these-tests";
        }).Start(TestContext.CancellationToken);

        // Before the app loads: the panel asks whether speech is supported on its first render, and takes the answer
        // as final for the lifetime of the component.
        await Page.AddInitScriptAsync(SpeechProbeScript);
        await Page.AddInitScriptAsync(MicrophoneProbeScript);

        await Page.GotoAsync(new Uri(server.WebAppServerAddress, PageUrls.Home).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        var panel = await OpenChatPanel();

        // 1. A finished answer, read on request. The plainest case, and the baseline the rest is measured against.
        await SendChatMessage(panel, "what is bit blazorui", chatClient);
        await Expect(panel.GetByText(FirstAnswer)).ToBeVisibleAsync();

        // The greeting has one of these too, so it is the newest answer that is asked for, not the first on screen.
        await panel.GetByTitle(AppStrings.AiChatPanelReadAloud).Last.ClickAsync();

        Assert.IsTrue(await WaitForSpokenText(FirstAnswer),
                      $"Pressing read aloud did not hand the answer to the engine. Spoken so far: {await SpokenText()}");

        // 2. The next answer, read while it is still arriving and without being asked for again.
        var pausedMidAnswer = new TaskCompletionSource();
        chatClient.PauseAfterFirstChunk = pausedMidAnswer;

        var cancelsBeforeTheNextAnswer = await CancelCount();

        await SendFollowUpMessage(panel, "tell me more", chatClient);

        // Nothing beyond the first chunk has left the server yet, so anything spoken now was spoken mid-answer. This
        // is the whole point of the feature: waiting for the answer to finish would deadlock here.
        Assert.IsTrue(await WaitForSpokenText(SecondAnswerFirstChunk.Split('.')[0]),
                      $"The answer was not read while it was still streaming in. Spoken so far: {await SpokenText()}");

        // The previous answer was dropped to make way for this one - and dropped now rather than at the moment the
        // prompt was sent, which would have left the user in silence for as long as the model took to start replying.
        Assert.IsGreaterThan(cancelsBeforeTheNextAnswer, await CancelCount(),
                             "The previous answer was left to play out instead of giving way to the new one.");

        pausedMidAnswer.SetResult();
        chatClient.PauseAfterFirstChunk = null;

        // ... and the tail, which is shorter than the panel's minimum run, still gets read once the answer is known
        // to be complete.
        Assert.IsTrue(await WaitForSpokenText(SecondAnswerLastChunk),
                      $"The end of the answer was never read out. Spoken so far: {await SpokenText()}");

        // 3. The microphone silences the voice, and keeps it silent for as long as it is open - an answer that is
        // mid-flight when it opens does not go on being read into the recording.
        var pausedMidDictatedAnswer = new TaskCompletionSource();
        chatClient.PauseAfterFirstChunk = pausedMidDictatedAnswer;

        await SendFollowUpMessage(panel, "what is bswup", chatClient);

        Assert.IsTrue(await WaitForSpokenText(ThirdAnswerFirstChunk.Split('.')[0]),
                      $"The answer to dictate over was never started. Spoken so far: {await SpokenText()}");

        var cancelsBeforeDictation = await CancelCount();

        await panel.GetByTitle(AppStrings.AiChatPanelDictate).ClickAsync();
        await Expect(panel.GetByTitle(AppStrings.AiChatPanelStopDictation)).ToBeVisibleAsync();

        Assert.IsGreaterThan(cancelsBeforeDictation, await CancelCount(),
                             "Opening the microphone left the engine talking over the user.");

        // The rest of that answer arrives while the microphone is open. It reaches the screen, which is the proof
        // that it arrived at all - and it must not reach the engine.
        pausedMidDictatedAnswer.SetResult();
        chatClient.PauseAfterFirstChunk = null;

        await Expect(panel.GetByText(ThirdAnswerLastChunk).First).ToBeVisibleAsync();

        Assert.DoesNotContain(ThirdAnswerLastChunk, await SpokenText(),
                              "The rest of the answer was read out into the user's own recording.");

        // The mode is still on, which is only observable through what happens to the NEXT answer - so ask for one.
        await panel.GetByTitle(AppStrings.AiChatPanelStopDictation).ClickAsync();

        await SendFollowUpMessage(panel, "what is besql", chatClient);

        Assert.IsTrue(await WaitForSpokenText(FourthAnswer),
                      $"Dictation switched read aloud off instead of merely silencing it. Spoken so far: {await SpokenText()}");

        // 4. Stop, and it stays stopped for the answers that follow.
        await panel.GetByTitle(AppStrings.AiChatPanelStopReading).Last.ClickAsync();

        await SendFollowUpMessage(panel, "what is butil", chatClient);
        await Expect(panel.GetByText(FifthAnswer)).ToBeVisibleAsync();

        // The answer is on screen, so it has fully arrived: had the mode still been on it would already have been
        // handed over, and there is nothing left to wait for.
        Assert.DoesNotContain(FifthAnswer, await SpokenText(),
                              "An answer was read out after the user pressed stop.");
    }

    private async Task<string> SpokenText()
    {
        var utterances = await Page.EvaluateAsync<string[]>("window.__speechProbe.utterances");

        return string.Join(" ", utterances);
    }

    private async Task<int> CancelCount() => await Page.EvaluateAsync<int>("window.__speechProbe.cancelCount");

    /// <summary>
    /// Waits for <paramref name="expected"/> to turn up in what the engine has been asked to say. Polls rather than
    /// reads once, because everything it is waiting on - a chunk crossing SignalR, a render, an interop call - happens
    /// after the step that triggered it has already returned.
    /// </summary>
    private async Task<bool> WaitForSpokenText(string expected)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(20);

        do
        {
            if ((await SpokenText()).Contains(expected.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            await Task.Delay(TimeSpan.FromMilliseconds(250), TestContext.CancellationToken);
        }
        while (DateTimeOffset.UtcNow < deadline);

        return false;
    }
}
