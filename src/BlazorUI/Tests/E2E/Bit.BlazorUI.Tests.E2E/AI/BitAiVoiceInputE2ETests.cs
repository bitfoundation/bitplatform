namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiVoiceInput at /e2e/aivoiceinput in a real browser: the button absent where the browser cannot transcribe
/// (and present where the engine can), and - over a scripted recognizer standing in for the speech API, so every engine
/// runs it - a press starting the dictation and announcing it, the provisional and final phrases, a second press
/// stopping it, a refused microphone, a recognizer ending on its own, the language handed over, Disabled (and disabling
/// while listening), removal while listening, the AI glyph font, the Styles/Classes of the button, and right-to-left.
/// </summary>
[TestClass]
public class BitAiVoiceInputE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aivoiceinput";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Button(string id) => Page.Locator($"#{id} .bit-aivci-btn");

    private ILocator Status(string id) => Page.Locator($"#{id} .bit-aivci-sts");

    private ILocator Text(string id) => Page.Locator($"#{id}");

    private async Task OpenWithRecognizerAsync()
    {
        await Page.AddInitScriptAsync(AiE2EHelpers.FakeSpeechRecognitionScript);
        await OpenAsync(Route);
        await Expect(Button("vi-basic")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task WithoutTheSpeechApi_NothingIsRendered()
    {
        await Page.AddInitScriptAsync(AiE2EHelpers.NoSpeechRecognitionScript);
        await OpenAsync(Route);

        Assert.IsFalse(await Page.EvaluateAsync<bool>("() => !!(window.SpeechRecognition || window.webkitSpeechRecognition)"));

        // The other sections render, so the page is interactive and the voice inputs have answered.
        await Expect(Page.Locator("#vi-dis-toggle")).ToBeVisibleAsync();
        await Expect(Page.Locator(".bit-aivci")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#vi-basic-host")).ToBeEmptyAsync();
    }

    [TestMethod]
    public async Task TheEngine_DecidesWhetherTheButtonIsThere()
    {
        await OpenAsync(Route);

        var supported = await Page.EvaluateAsync<bool>("() => !!(window.SpeechRecognition || window.webkitSpeechRecognition)");

        // Chromium transcribes; Firefox has no speech recognition at all.
        if (IsChromium) Assert.IsTrue(supported, "Chromium has the speech API.");
        if (IsFirefox) Assert.IsFalse(supported, "Firefox has no speech API.");

        if (supported)
        {
            await Expect(Button("vi-basic")).ToBeVisibleAsync();
            await Expect(Button("vi-basic")).ToHaveAttributeAsync("aria-label", "Dictate");
        }
        else
        {
            await Expect(Page.Locator("#vi-dis-toggle")).ToBeVisibleAsync();
            await Expect(Page.Locator(".bit-aivci")).ToHaveCountAsync(0);
        }
    }

    [TestMethod]
    public async Task APress_StartsListening_AndSaysSoInItsNameItsStateAndALiveRegion()
    {
        await OpenWithRecognizerAsync();

        var button = Button("vi-basic");
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Dictate");
        await Expect(button).ToHaveAttributeAsync("title", "Dictate");
        await Expect(Status("vi-basic")).ToHaveAttributeAsync("role", "status");
        await Expect(Status("vi-basic")).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(Status("vi-basic")).ToHaveTextAsync("");

        await button.ClickAsync();

        await Expect(button).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Stop dictation");
        await Expect(Status("vi-basic")).ToHaveTextAsync("Listening");
        await Expect(Text("vi-listening")).ToHaveTextAsync("True");

        var recognizer = await Page.EvaluateAsync<bool[]>("() => { const r = window.__e2eSpeech.current; return [r.started, r.continuous, r.interimResults]; }");
        CollectionAssert.AreEqual(new[] { true, true, true }, recognizer, "A continuous recognizer reporting interim results is started.");

        // Without a Language, the language of the browser.
        Assert.AreEqual(await Page.EvaluateAsync<string>("() => navigator.language"), await Page.EvaluateAsync<string>("() => window.__e2eSpeech.current.lang"));
    }

    [TestMethod]
    public async Task ThePhrases_AreRaised_TheProvisionalOneUntilItIsFinal()
    {
        await OpenWithRecognizerAsync();
        await Button("vi-basic").ClickAsync();
        await Expect(Status("vi-basic")).ToHaveTextAsync("Listening");

        await Page.EvaluateAsync("() => window.__e2eSpeech.result(null, 'hello wor')");
        await Expect(Text("vi-interim")).ToHaveTextAsync("hello wor");
        await Expect(Text("vi-transcripts")).ToHaveTextAsync("");

        await Page.EvaluateAsync("() => window.__e2eSpeech.result('  hello world  ', null)");
        await Expect(Text("vi-transcripts")).ToHaveTextAsync("hello world");
        await Expect(Text("vi-interim")).ToHaveTextAsync("-");

        // A final and a provisional phrase in one result.
        await Page.EvaluateAsync("() => window.__e2eSpeech.result('second', 'third is')");
        await Expect(Text("vi-transcripts")).ToHaveTextAsync("hello world|second");
        await Expect(Text("vi-interim")).ToHaveTextAsync("third is");

        // A blank final phrase is nothing to raise.
        await Page.EvaluateAsync("() => window.__e2eSpeech.result('   ', null)");
        await Expect(Text("vi-interim")).ToHaveTextAsync("-");
        await Expect(Text("vi-transcripts")).ToHaveTextAsync("hello world|second");
    }

    [TestMethod]
    public async Task ASecondPress_StopsTheRecognizer_AndItsLateEndChangesNothing()
    {
        await OpenWithRecognizerAsync();

        var button = Button("vi-basic");
        await button.ClickAsync();
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "true");

        await button.ClickAsync();

        await Expect(button).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(button).ToHaveAttributeAsync("aria-label", "Dictate");
        await Expect(Status("vi-basic")).ToHaveTextAsync("Dictation stopped");
        await Expect(Text("vi-listening")).ToHaveTextAsync("True,False");
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__e2eSpeech.current.stopped"));

        // A press after it starts a new recognizer.
        await button.ClickAsync();
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Text("vi-listening")).ToHaveTextAsync("True,False,True");
        Assert.AreEqual(2, await Page.EvaluateAsync<int>("() => window.__e2eSpeech.instances.length"));
        Assert.IsFalse(await Page.EvaluateAsync<bool>("() => window.__e2eSpeech.current.stopped"));
    }

    [TestMethod]
    public async Task StartingAnotherVoiceInput_TurnsTheFirstOneOff()
    {
        await OpenWithRecognizerAsync();

        await Button("vi-basic").ClickAsync();
        await Expect(Button("vi-basic")).ToHaveAttributeAsync("aria-pressed", "true");

        // One recognizer runs at a time: the second start ends the first one's dictation, and tells it so.
        await Button("vi-lang").ClickAsync();

        await Expect(Button("vi-lang")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Button("vi-basic")).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Button("vi-basic")).ToHaveAttributeAsync("aria-label", "Dictate");
        await Expect(Status("vi-basic")).ToHaveTextAsync("Dictation stopped");
        await Expect(Text("vi-listening")).ToHaveTextAsync("True,False");
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__e2eSpeech.instances[0].stopped"), "The first recognizer was stopped.");

        // What the live recognizer hears is the second one's alone.
        await Page.EvaluateAsync("() => window.__e2eSpeech.result('for the second', null)");
        await Expect(Status("vi-lang")).ToHaveTextAsync("Listening");
        await Expect(Text("vi-transcripts")).ToHaveTextAsync("");

        // And the first one starts again over it.
        await Button("vi-basic").ClickAsync();
        await Expect(Button("vi-basic")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Button("vi-lang")).ToHaveAttributeAsync("aria-pressed", "false");
        await Page.EvaluateAsync("() => window.__e2eSpeech.result('for the first', null)");
        await Expect(Text("vi-transcripts")).ToHaveTextAsync("for the first");
    }

    [TestMethod]
    public async Task ARefusedMicrophone_IsReported_AndTurnsTheButtonOff()
    {
        await OpenWithRecognizerAsync();

        var button = Button("vi-basic");
        await button.ClickAsync();
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.EvaluateAsync("() => window.__e2eSpeech.error('not-allowed')");

        await Expect(Text("vi-errors")).ToHaveTextAsync("not-allowed");
        await Expect(Status("vi-basic")).ToHaveTextAsync("Dictation failed: not-allowed");
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Text("vi-listening")).ToHaveTextAsync("True,False");
    }

    [TestMethod]
    public async Task ARecognizerEndingOnItsOwn_TurnsTheButtonOff()
    {
        await OpenWithRecognizerAsync();

        var button = Button("vi-basic");
        await button.ClickAsync();
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.EvaluateAsync("() => window.__e2eSpeech.end()");

        await Expect(button).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Status("vi-basic")).ToHaveTextAsync("Dictation stopped");
        await Expect(Text("vi-listening")).ToHaveTextAsync("True,False");

        // The next press starts again rather than stopping a recognizer that is gone.
        await button.ClickAsync();
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "true");
        Assert.AreEqual(2, await Page.EvaluateAsync<int>("() => window.__e2eSpeech.instances.length"));
    }

    [TestMethod]
    public async Task Language_IsHandedToTheRecognizer()
    {
        await OpenWithRecognizerAsync();

        await Button("vi-lang").ClickAsync();
        await Expect(Button("vi-lang")).ToHaveAttributeAsync("aria-pressed", "true");

        Assert.AreEqual("fa-IR", await Page.EvaluateAsync<string>("() => window.__e2eSpeech.current.lang"));
    }

    [TestMethod]
    public async Task Disabled_TheButtonCannotBePressed_AndDisablingItWhileListeningStopsTheMicrophone()
    {
        await OpenWithRecognizerAsync();

        var button = Button("vi-dis");
        await button.ClickAsync();
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Text("vi-dis-listening")).ToHaveTextAsync("True");

        await Page.Locator("#vi-dis-toggle").ClickAsync();

        await Expect(Text("vi-dis-state")).ToHaveTextAsync("True");
        await Expect(Root("vi-dis")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(button).ToBeDisabledAsync();
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Text("vi-dis-listening")).ToHaveTextAsync("True,False");
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__e2eSpeech.current.stopped"), "Disabling it stopped the recognizer.");

        // A press on the disabled button starts nothing.
        await button.ClickAsync(new() { Force = true });
        await Page.Locator("#vi-dis-toggle").ClickAsync();
        await Expect(button).ToBeEnabledAsync();
        Assert.AreEqual(1, await Page.EvaluateAsync<int>("() => window.__e2eSpeech.instances.length"));
        await Expect(Text("vi-dis-listening")).ToHaveTextAsync("True,False");
    }

    [TestMethod]
    public async Task Removed_WhileListening_TheRecognizerIsStopped()
    {
        await OpenWithRecognizerAsync();

        await Button("vi-mounted").ClickAsync();
        await Expect(Button("vi-mounted")).ToHaveAttributeAsync("aria-pressed", "true");

        await Page.Locator("#vi-unmount").ClickAsync();

        await Expect(Root("vi-mounted")).ToHaveCountAsync(0);
        await Page.WaitForFunctionAsync("() => window.__e2eSpeech.current.stopped === true");
        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Glyph_TheMicrophoneIsDrawnFromTheAiFont()
    {
        await OpenWithRecognizerAsync();

        await Button("vi-basic").Locator("i.bit-tgb-ico").AssertAiGlyphAsync("Microphone");
        Assert.IsTrue(await Page.AiGlyphFontLoadsAsync(""), "The AI font is fetched and draws the Microphone glyph.");
    }

    [TestMethod]
    public async Task Style_TheStylesAndClassesReachTheButtonAndItsIcon()
    {
        await OpenWithRecognizerAsync();

        await Expect(Root("vi-style")).ToHaveClassAsync(HasClass("vi-custom-root"));
        await Expect(Button("vi-style")).ToHaveClassAsync(HasClass("vi-custom-button"));
        await Expect(Button("vi-style")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");

        // The app's icon class goes beside the glyph classes rather than in place of them.
        var icon = Button("vi-style").Locator("i.bit-tgb-ico");
        await Expect(icon).ToHaveClassAsync(HasClass("vi-custom-icon"));
        await icon.AssertAiGlyphAsync("Microphone");

        // The live region is out of sight but on the page.
        var box = await Status("vi-basic").BoundingBoxAsync();
        Assert.IsTrue(box is null || box.Width <= 1, "The live region is visually hidden.");
    }

    [TestMethod]
    public async Task Rtl_TheVoiceInputReadsRightToLeft()
    {
        await OpenWithRecognizerAsync();

        await Expect(Root("vi-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Button("vi-rtl")).ToHaveCSSAsync("direction", "rtl");
        await Expect(Root("vi-basic")).Not.ToHaveAttributeAsync("dir", new System.Text.RegularExpressions.Regex(".*"));
    }
}
