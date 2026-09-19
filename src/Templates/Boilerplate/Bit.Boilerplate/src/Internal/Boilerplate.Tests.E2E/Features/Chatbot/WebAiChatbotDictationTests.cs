using Microsoft.Playwright.TestAdapter;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Chatbot;

/// <summary>
/// Speaking to the chat panel: a sentence is synthesised to a wav file, chromium runs with that file as its
/// microphone, and the panel is asked to dictate. The sentence reaching the message box means the recorder captured
/// it, the upload carried it and <c>ChatbotController.TranscribeSpeech</c> understood it (See
/// <c>AppAiChatPanel.razor.Dictation.cs</c>).
/// <para>
/// Chromium only - firefox can fake a device but not its contents, webkit neither - so other runs report
/// inconclusive. <see cref="SpeechToTextTests"/> covers the same endpoint with no browser at all.
/// </para>
/// <para>
/// On a browser of its own (See <c>AppPageTest.NewBrowserContext</c>): the shared one is whatever the worker's first
/// class launched - normally the headless shell, with none of the switches below, where getUserMedia answers
/// NotSupportedError.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebAiChatbotDictationTests : AppTestBase
{
    private const string SpokenSentence = "Please change the application language to Persian.";

    /// <summary>On top of the recording's own length: the microphone opens a moment after the button is clicked.</summary>
    private static readonly TimeSpan SpeakingMargin = TimeSpan.FromSeconds(1);

    private static bool IsChromium => PlaywrightSettingsProvider.BrowserName is Microsoft.Playwright.BrowserType.Chromium;

    protected override IAppOpener AppOpener => new WebAppOpener();

    /// <summary>
    /// Hands chromium the recording to play whenever the page opens the microphone, and auto-answers the permission
    /// prompt - a browser dialog the test cannot click.
    /// </summary>
    private async Task<BrowserTypeLaunchOptions> DictationLaunchOptions(string wavFile)
    {
        var options = (await LaunchOptionsAsync())!;

        // The full browser rather than playwright's default headless shell, which ships without the audio stack the
        // switches below feed: in the shell the microphone never opens and the panel never starts listening.
        options.Channel = "chromium";

        options.Args =
        [
            .. options.Args ?? [],
            "--use-fake-ui-for-media-stream",
            "--use-fake-device-for-media-stream",
            // %noloop: chromium otherwise plays the file over and over for as long as the microphone is open, so a
            // take longer than the sentence records it twice and the transcription faithfully repeats it.
            $"--use-file-for-fake-audio-capture={wavFile}%noloop",
        ];

        return options;
    }

    /// <summary>
    /// getUserMedia asks the page's own permission first, and the browser switch above never gets a say. Chromium
    /// only: the others answer "Unknown permission: microphone", failing the test where it should have skipped.
    /// </summary>
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();

        if (IsChromium)
        {
            options.Permissions = ["microphone"];
        }

        return options;
    }

    [TestMethod]
    public async Task Dictation_Should_PutWhatWasSaid_IntoTheMessageBox()
    {
        if (IsChromium is false)
            Assert.Inconclusive($"Only chromium can play a file into the microphone; this run is on {PlaywrightSettingsProvider.BrowserName}.");

        var wavFile = await SpokenAudio.WavFileOf(SpokenSentence, TestContext.CancellationToken);

        // Its own browser rather than Page, which is the worker's shared one (See the class summary).
        await using var browser = await BrowserType.LaunchAsync(await DictationLaunchOptions(wavFile));
        await using var context = await NewBrowserContext(browser);

        var page = await context.NewPageAsync();
        await page.GotoAsync(DeployedApps.Sales);

        // Cloudflare in front of the demo turns away a large multipart POST from an automated browser with its own
        // html 403, before the deployment sees it. The same bytes from this process are accepted (See
        // SpeechToTextTests), so a block here says nothing about the app.
        var blockedByTheEdge = false;
        page.Response += (_, response) =>
        {
            if (response.Url.Contains("TranscribeSpeech", StringComparison.OrdinalIgnoreCase)
                && response.Status is (int)HttpStatusCode.Forbidden
                && response.Headers.GetValueOrDefault("server", string.Empty).Contains("cloudflare", StringComparison.OrdinalIgnoreCase))
            {
                blockedByTheEdge = true;
            }
        };

        await WaitUntilInteractive(page);

        // Recording is free, the transcription request behind it needs an account (See
        // AppAiChatPanel.EnsureSignedInForSpeech).
        await SignIn(page, StoreUser.Email, StoreUser.Password);

        var panel = await AiChatPanel.Open(page);

        // Hidden outright where the browser cannot record, so its absence is the first thing worth reporting.
        await Expect(panel.DictateButton).ToBeVisibleAsync();

        await panel.DictateButton.ClickAsync();

        // The timer beside the button is the panel saying the microphone is open; without it the recording never began.
        await Expect(page.Locator(".speech-timer")).ToBeVisibleAsync();

        // Ended as the sentence finishes: what follows it is silence, which a transcriber fills by repeating itself.
        await page.WaitForTimeoutAsync((float)(SpokenAudio.DurationOf(wavFile) + SpeakingMargin).TotalMilliseconds);

        await panel.DictateButton.ClickAsync();

        // What was heard is written into the box rather than sent, so the user reads it first.
        try
        {
            await Expect(panel.MessageBox).Not.ToBeEmptyAsync(new() { Timeout = (float)TimeSpan.FromMinutes(2).TotalMilliseconds });
        }
        catch (PlaywrightException) when (blockedByTheEdge)
        {
            Assert.Inconclusive("Cloudflare answered the panel's recording upload with a 403 of its own, so the recording never reached the deployment.");
        }

        var dictated = await panel.MessageBox.InputValueAsync();

        await AiAnswerJudge.AssertAnswer($"[a recording of someone saying] {SpokenSentence}",
            "The text is a plain, faithful transcription of that sentence - the same request, in the same words or " +
            "near enough that a person reading it would act on it identically. Small differences in punctuation, " +
            "casing or a single misheard word are fine.",
            dictated, TestContext.CancellationToken);
    }
}
