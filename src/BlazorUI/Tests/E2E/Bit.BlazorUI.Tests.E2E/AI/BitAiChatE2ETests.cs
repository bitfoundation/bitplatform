using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiChat at /e2e/aichat in a real browser against the scripted transport: a prompt typed and sent with Enter
/// and the reply it streams, the transcript's log region, the empty state and its suggestions sending a prompt, a reply
/// arriving progressively and the send button turning into a stop button, stopping a turn mid-stream (by the button and
/// by Escape) and sending again, the transcript following a reply while pinned and staying put once the reader scrolls
/// up (the scroll-to-latest button that appears, and takes them back), OnSubmit, FocusComposerAsync, the templates in and
/// out of the scroll area, Disabled, the column the composer lines up with, the theme tokens, every --bit-AiChat-*
/// variable, Styles/Classes, RTL (explicit and inherited) and print.
/// </summary>
[TestClass]
public class BitAiChatE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aichat";

    private ILocator Chat(string id) => Page.Locator($"#{id}");

    private ILocator Transcript(string id) => Chat(id).Locator(".bit-aimsl");

    private ILocator Composer(string id) => Chat(id).Locator("textarea.bit-aipri-inp");

    private ILocator SendButton(string id) => Chat(id).Locator("button.bit-aipri-snd");

    private ILocator Messages(string id, string role) => Chat(id).Locator($"article.bit-aicms[data-role='{role}']");

    private ILocator ScrollToLatest(string id) => Chat(id).Locator(".bit-aicht-ftr > .bit-aistl");

    private async Task SendAsync(string id, string text)
    {
        await Composer(id).FillAsync(text);
        await Expect(Composer(id)).ToHaveValueAsync(text);
        await Composer(id).PressAsync("Enter");
    }

    // ---------- basic ----------

    [TestMethod]
    public async Task Basic_APromptSentWithEnter_ShowsTheQuestionAndStreamsTheReply()
    {
        await OpenAsync(Route);

        await Expect(Chat("cht-basic")).ToHaveClassAsync(HasClass("bit-aicht"));
        await Expect(Chat("cht-basic").Locator(".bit-aiems")).ToBeVisibleAsync();

        await SendAsync("cht-basic", "hello there");

        await Expect(Messages("cht-basic", "user")).ToHaveTextAsync(["hello there"]);
        await Expect(Messages("cht-basic", "assistant")).ToHaveCountAsync(1);
        await Expect(Messages("cht-basic", "assistant").First.Locator(".bit-aicms-bdy")).ToHaveTextAsync("You said: hello there");
        await Expect(Messages("cht-basic", "assistant").First).ToHaveAttributeAsync("aria-busy", "false");

        // The composer is emptied by the send, the empty state gives way to the transcript, and the question is drawn as
        // a bubble at the end of the line while the answer takes the column.
        await Expect(Composer("cht-basic")).ToHaveValueAsync("");
        await Expect(Chat("cht-basic").Locator(".bit-aiems")).ToHaveCountAsync(0);
        var column = await Chat("cht-basic").Locator(".bit-aimsl-con").RectAsync();
        var bubble = await Messages("cht-basic", "user").First.Locator(".bit-aicms-bbl").RectAsync();
        Assert.IsTrue(bubble.Right > column.Right - 40, $"The bubble sits at the end of the line: {bubble.Right} vs {column.Right}.");
        Assert.IsTrue(bubble.Left > column.Left + 40, "The bubble is no wider than its text.");

        await SendAsync("cht-basic", "and again");
        await Expect(Messages("cht-basic", "user")).ToHaveTextAsync(["hello there", "and again"]);
        await Expect(Messages("cht-basic", "assistant").Nth(1).Locator(".bit-aicms-bdy")).ToHaveTextAsync("You said: and again");
    }

    [TestMethod]
    public async Task Basic_TheTranscript_IsAPoliteLogRegionNamedConversation()
    {
        await OpenAsync(Route);

        var transcript = Transcript("cht-basic");
        await Expect(transcript).ToHaveAttributeAsync("role", "log");
        await Expect(transcript).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(transcript).ToHaveAttributeAsync("aria-relevant", "additions");
        await Expect(Page.GetByRole(AriaRole.Log, new() { Name = "Conversation" }).First).ToBeVisibleAsync();

        // The transcript is the scroll container, the composer is below it, and the chat fills the box it is given.
        await Expect(transcript).ToHaveCSSAsync("overflow-y", "auto");
        var box = await Page.Locator("#cht-basic-box").RectAsync();
        var chat = await Chat("cht-basic").RectAsync();
        Assert.AreEqual(box.Bottom - box.Top, chat.Bottom - chat.Top, 1, "The chat takes the height of its container.");
        var list = await transcript.RectAsync();
        var footer = await Chat("cht-basic").Locator(".bit-aicht-ftr").RectAsync();
        Assert.IsTrue(footer.Top >= list.Bottom - 1, "The footer with the composer is under the transcript.");
        Assert.AreEqual(chat.Bottom, footer.Bottom, 1, "The footer sits at the bottom of the chat.");
    }

    [TestMethod]
    public async Task Basic_TheComposer_LinesUpWithTheColumnOfMessages()
    {
        await OpenAsync(Route);

        await SendAsync("cht-basic", "line up");
        await Expect(Messages("cht-basic", "assistant")).ToHaveCountAsync(1);

        var column = await Chat("cht-basic").Locator(".bit-aimsl-con").EvaluateAsync<double[]>("e => { const r = e.getBoundingClientRect(); const s = getComputedStyle(e); return [r.left + parseFloat(s.paddingLeft), r.right - parseFloat(s.paddingRight), parseFloat(s.maxWidth)]; }");
        var footerColumn = Chat("cht-basic").Locator(".bit-aicht-ftc");
        Assert.AreEqual(column[2], AiE2EHelpers.ParsePx(await ComputedStyleAsync("#cht-basic .bit-aicht-ftc", "max-width")), 0.5, "Both columns stop growing at the same width.");

        // Both are centered in the same box, so the middle of the composer is the middle of the messages.
        var composerBox = await footerColumn.RectAsync();
        Assert.AreEqual((column[0] + column[1]) / 2, (composerBox.Left + composerBox.Right) / 2, 1.5);
    }

    [TestMethod]
    public async Task Basic_TheThemeTokens_PaintTheChat()
    {
        await OpenAsync(Route);

        await Expect(Chat("cht-basic")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-pri)"));
        await Expect(Chat("cht-basic")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-pri)"));
        await Expect(Chat("cht-basic")).ToHaveCSSAsync("display", "flex");
        await Expect(Chat("cht-basic")).ToHaveCSSAsync("flex-direction", "column");

        // The footer fades the transcript out behind the composer, into the chat's own background.
        StringAssert.StartsWith(await ComputedStyleAsync("#cht-basic .bit-aicht-ftr", "background-image"), "linear-gradient(");
        await Expect(Chat("cht-basic")).ToHaveCSSAsync("font-family", await Page.ResolveFontFamilyAsync("var(--bit-tpg-font-family)"));
    }

    // ---------- empty state ----------

    [TestMethod]
    public async Task EmptyState_ItsTitleBodyAndSuggestions_ShowUntilTheFirstMessage()
    {
        await OpenAsync(Route);

        var empty = Chat("cht-empty").Locator(".bit-aiems");
        await Expect(empty.Locator(".bit-aiems-ttl")).ToHaveTextAsync("Hi, I am the docs assistant");
        await Expect(empty.Locator(".bit-aiems-bdy")).ToHaveTextAsync("Ask about any component, or start from one of these.");
        await Expect(empty.Locator(".bit-aiems-sgs > .bit-btn")).ToHaveTextAsync(["What is Blazor?", "markdown please"]);

        // The empty state is inside the transcript, centered in its column.
        await Expect(Transcript("cht-empty").Locator(".bit-aiems")).ToHaveCountAsync(1);
        await empty.Locator(".bit-aiems-ico").AssertAiGlyphAsync("AutoEnhanceOn");
    }

    [TestMethod]
    public async Task EmptyState_ASuggestionPressed_SendsItAsThePrompt()
    {
        await OpenAsync(Route);

        await Chat("cht-empty").Locator(".bit-aiems-sgs > .bit-btn", new() { HasText = "What is Blazor?" }).ClickAsync();

        await Expect(Messages("cht-empty", "user")).ToHaveTextAsync(["What is Blazor?"]);
        await Expect(Messages("cht-empty", "assistant").First.Locator(".bit-aicms-bdy")).ToHaveTextAsync("You said: What is Blazor?");
        await Expect(Chat("cht-empty").Locator(".bit-aiems")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task EmptyState_ASuggestionChosenWithTheKeyboard_PlaysTheScriptItNames()
    {
        await OpenAsync(Route);

        var suggestion = Chat("cht-empty").Locator(".bit-aiems-sgs > .bit-btn", new() { HasText = "markdown please" });
        await suggestion.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        // The suggestion is the prompt, and its first word names the markdown script, which answers with a heading.
        await Expect(Messages("cht-empty", "user")).ToHaveTextAsync(["markdown please"]);
        await Expect(Messages("cht-empty", "assistant").First.GetByRole(AriaRole.Heading, new() { Name = "Render modes" })).ToHaveCountAsync(1);
        await Expect(Messages("cht-empty", "assistant").First).ToHaveAttributeAsync("aria-busy", "false");
    }

    // ---------- streaming ----------

    [TestMethod]
    public async Task Streaming_AReply_ArrivesProgressivelyWhileTheMessageIsBusy()
    {
        await OpenAsync(Route);

        await Page.RecordTextLengthsAsync("stream", "#cht-stream article[data-role='assistant'] .bit-aicms-bdy");

        const string prompt = "one two three four five six seven eight nine ten eleven twelve";
        await SendAsync("cht-stream", prompt);

        // While it streams the message is busy, so a screen reader waits for it, and the send button is the stop button.
        var reply = Messages("cht-stream", "assistant").First;
        await Expect(reply).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(SendButton("cht-stream")).ToHaveAttributeAsync("aria-label", "Stop");
        await Expect(reply.Locator(".bit-aimac")).ToHaveCountAsync(0);

        await Expect(reply.Locator(".bit-aicms-bdy")).ToHaveTextAsync($"You said: {prompt}");
        await Expect(reply).ToHaveAttributeAsync("aria-busy", "false");
        await Expect(SendButton("cht-stream")).ToHaveAttributeAsync("aria-label", "Send");
        await Expect(reply.Locator(".bit-aimac")).ToHaveCountAsync(1);

        // The text grew in several steps on its way to the whole reply, rather than landing at once.
        var lengths = await Page.TextLengthsAsync("stream");
        var growing = lengths.Where(l => l > 0 && l < $"You said: {prompt}".Length).Distinct().Count();
        Assert.IsTrue(growing >= 3, $"The reply arrived in steps: {string.Join(", ", lengths)}.");
    }

    [TestMethod]
    public async Task Stop_TheStopButtonPressedMidStream_KeepsThePartialReplyAndEndsTheTurn()
    {
        await OpenAsync(Route);

        await SendAsync("cht-stop", "write a lot");
        var reply = Messages("cht-stop", "assistant").First;
        await Expect(reply.GetByText("Paragraph 2:", new() { Exact = false })).ToHaveCountAsync(1);
        await Expect(SendButton("cht-stop")).ToHaveAttributeAsync("aria-label", "Stop");

        await SendButton("cht-stop").ClickAsync();

        await Expect(reply).ToHaveAttributeAsync("aria-busy", "false");
        await Expect(SendButton("cht-stop")).ToHaveAttributeAsync("aria-label", "Send");
        var stopped = await reply.Locator(".bit-aicms-bdy").InnerTextAsync();
        StringAssert.Contains(stopped, "Paragraph 2:");

        // Nothing more arrives after the stop: the partial answer is kept as it was.
        await Page.WaitForTimeoutAsync(600);
        Assert.AreEqual(stopped, await reply.Locator(".bit-aicms-bdy").InnerTextAsync());
        Assert.IsFalse(stopped.Contains("Paragraph 30:"), "The turn was stopped before its end.");

        // A stopped reply is a finished one: its actions are offered, and the session takes the next prompt.
        await Expect(reply.Locator("button[aria-label='Regenerate']")).ToHaveCountAsync(1);
        await SendAsync("cht-stop", "once more");
        await Expect(Messages("cht-stop", "user")).ToHaveCountAsync(2);
        await Expect(SendButton("cht-stop")).ToHaveAttributeAsync("aria-label", "Stop");
        await SendButton("cht-stop").ClickAsync();
        await Expect(Messages("cht-stop", "assistant").Nth(1)).ToHaveAttributeAsync("aria-busy", "false");
    }

    [TestMethod]
    public async Task Stop_EscapeInTheComposerMidStream_StopsTheTurn()
    {
        await OpenAsync(Route);

        await SendAsync("cht-stop", "write a lot");
        var reply = Messages("cht-stop", "assistant").First;
        await Expect(reply.GetByText("Paragraph 1:", new() { Exact = false })).ToHaveCountAsync(1);
        await Expect(Composer("cht-stop")).ToHaveAttributeAsync("data-bit-esc", "claim");

        await Composer("cht-stop").PressAsync("Escape");

        await Expect(reply).ToHaveAttributeAsync("aria-busy", "false");
        await Expect(SendButton("cht-stop")).ToHaveAttributeAsync("aria-label", "Send");
        await Expect(Composer("cht-stop")).Not.ToHaveAttributeAsync("data-bit-esc", "claim");
    }

    // ---------- auto-scroll ----------

    [TestMethod]
    public async Task AutoScroll_WhilePinned_TheTranscriptFollowsTheReply()
    {
        await OpenAsync(Route);

        var transcript = Transcript("cht-scroll");
        await transcript.WaitForScrolledToBottomAsync();
        await Expect(ScrollToLatest("cht-scroll")).ToHaveCountAsync(0);

        await SendAsync("cht-scroll", "a long answer");
        var reply = Messages("cht-scroll", "assistant").Last;
        await Expect(reply).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(reply.GetByText("Paragraph 12:", new() { Exact = false })).ToHaveCountAsync(1);

        // Mid-stream the view is at the newest line, within the distance that still counts as the bottom.
        var midStream = await transcript.ScrollStateAsync();
        Assert.IsTrue(midStream.FromBottom <= 48, $"The view follows the reply while it streams: {midStream.FromBottom}px from the bottom.");

        await Expect(reply).ToHaveAttributeAsync("aria-busy", "false", new() { Timeout = 20000 });
        await transcript.WaitForScrolledToBottomAsync();
        await Expect(ScrollToLatest("cht-scroll")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task AutoScroll_ScrolledUp_StaysPutWhileTheReplyGrowsAndTheButtonTakesTheReaderBack()
    {
        await OpenAsync(Route);

        var transcript = Transcript("cht-scroll");
        await transcript.WaitForScrolledToBottomAsync();

        await Page.WheelOverAsync(transcript, -600);
        var button = ScrollToLatest("cht-scroll");
        await Expect(button).ToBeVisibleAsync();
        await Expect(button.Locator("button")).ToHaveTextAsync("Scroll to latest");
        await button.Locator("button i.bit-btn-icn").AssertAiGlyphAsync("Down");

        // The button floats centered just above the composer.
        var footer = await Chat("cht-scroll").Locator(".bit-aicht-ftr").RectAsync();
        var floating = await button.RectAsync();
        Assert.IsTrue(floating.Bottom <= footer.Top + 1, "The button floats above the footer.");
        Assert.AreEqual((footer.Left + footer.Right) / 2, (floating.Left + floating.Right) / 2, 1.5, "The button is centered.");

        var before = await transcript.ScrollStateAsync();
        Assert.IsTrue(before.FromBottom > 48, "The reader is away from the newest message.");

        // A reply streams in while the reader is scrolled up: the content grows, the view does not move.
        await SendAsync("cht-scroll", "a long answer");
        var reply = Messages("cht-scroll", "assistant").Last;
        await Expect(reply.GetByText("Paragraph 6:", new() { Exact = false })).ToHaveCountAsync(1);
        var during = await transcript.ScrollStateAsync();
        Assert.AreEqual(before.Top, during.Top, 1, "The view stays where the reader left it.");
        Assert.IsTrue(during.Height > before.Height, "The reply grew the transcript.");
        await Expect(button).ToBeVisibleAsync();

        // The button takes the reader to the newest line, goes away, and the view follows the rest of the reply.
        await button.Locator("button").ClickAsync();
        await Expect(button).ToHaveCountAsync(0);
        await transcript.WaitForScrolledToBottomAsync(48);
        await Expect(reply).ToHaveAttributeAsync("aria-busy", "false", new() { Timeout = 20000 });
        await transcript.WaitForScrolledToBottomAsync();
    }

    [TestMethod]
    public async Task AutoScroll_ScrolledBackDownByHand_HidesTheButtonAgain()
    {
        await OpenAsync(Route);

        var transcript = Transcript("cht-scroll");
        await transcript.WaitForScrolledToBottomAsync();

        await Page.WheelOverAsync(transcript, -600);
        await Expect(ScrollToLatest("cht-scroll")).ToBeVisibleAsync();

        await Page.WheelToBottomAsync(transcript);
        await transcript.WaitForScrolledToBottomAsync();
        await Expect(ScrollToLatest("cht-scroll")).ToHaveCountAsync(0);
    }

    // ---------- OnSubmit, focus ----------

    [TestMethod]
    public async Task OnSubmit_Handled_IsHandedTheSubmissionAndNothingIsSent()
    {
        await OpenAsync(Route);

        await SendAsync("cht-onsubmit", "handled by the page");

        await Expect(Page.Locator("#cht-onsubmit-log")).ToHaveTextAsync("Submitted: handled by the page");
        await Expect(Composer("cht-onsubmit")).ToHaveValueAsync("");
        await Expect(Chat("cht-onsubmit").Locator("article.bit-aicms")).ToHaveCountAsync(0);
        await Expect(Chat("cht-onsubmit").Locator(".bit-aiems")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task FocusComposerAsync_Called_PutsTheCaretInTheComposer()
    {
        await OpenAsync(Route);

        await Page.Locator("#cht-focus-btn").ClickAsync();

        await Expect(Composer("cht-focus")).ToBeFocusedAsync();
        await Page.Keyboard.TypeAsync("typed");
        await Expect(Composer("cht-focus")).ToHaveValueAsync("typed");
    }

    // ---------- templates ----------

    [TestMethod]
    public async Task Templates_TheHeaderAndFooterStayPutWhileTheListOnesScrollWithTheMessages()
    {
        await OpenAsync(Route);

        var chat = Chat("cht-tpl");
        var transcript = Transcript("cht-tpl");
        await transcript.WaitForScrolledToBottomAsync();

        // The header sits above the transcript and the footer under the composer, both outside the scroll area; the
        // list's own header and footer are inside it, around the messages.
        await Expect(chat.Locator(":scope > #cht-tpl-header")).ToHaveCountAsync(1);
        await Expect(transcript.Locator("#cht-tpl-list-header")).ToHaveCountAsync(1);
        await Expect(transcript.Locator("#cht-tpl-list-footer")).ToHaveCountAsync(1);
        await Expect(chat.Locator(".bit-aicht-ftc #cht-tpl-footer")).ToHaveCountAsync(1);
        var header = await Page.Locator("#cht-tpl-header").RectAsync();
        var list = await transcript.RectAsync();
        var composer = await chat.Locator(".bit-aipri").RectAsync();
        var footer = await Page.Locator("#cht-tpl-footer").RectAsync();
        Assert.IsTrue(header.Bottom <= list.Top + 1);
        Assert.IsTrue(footer.Top >= composer.Bottom - 1);

        // At the newest message the list's header is scrolled out of the transcript; at the start it is at its top.
        var listHeaderAtBottom = await Page.Locator("#cht-tpl-list-header").RectAsync();
        Assert.IsTrue(listHeaderAtBottom.Bottom < list.Top, "The list header is scrolled away above the transcript.");
        await transcript.EvaluateAsync("e => e.scrollTop = 0");
        await AiE2EHelpers.PollAsync(async () => (await Page.Locator("#cht-tpl-list-header").RectAsync()).Top >= list.Top, "The list header scrolled into the transcript.");
        var listHeaderAtTop = await Page.Locator("#cht-tpl-list-header").RectAsync();
        Assert.IsTrue(listHeaderAtTop.Bottom <= list.Bottom, "The list header is inside the transcript.");
        Assert.AreEqual(header.Top, (await Page.Locator("#cht-tpl-header").RectAsync()).Top, 0.5, "The header did not move.");
    }

    [TestMethod]
    public async Task Templates_AnEmptyTemplate_ReplacesTheEmptyStateUntilTheFirstMessage()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#cht-tpl-empty-content")).ToBeVisibleAsync();
        await Expect(Chat("cht-tpl-empty").Locator(".bit-aiems")).ToHaveCountAsync(0);

        await SendAsync("cht-tpl-empty", "hi");
        await Expect(Messages("cht-tpl-empty", "assistant").First.Locator(".bit-aicms-bdy")).ToHaveTextAsync("You said: hi");
        await Expect(Page.Locator("#cht-tpl-empty-content")).ToHaveCountAsync(0);
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_TheChat_TakesNoPromptAndNoSuggestion()
    {
        await OpenAsync(Route);

        await Expect(Chat("cht-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Composer("cht-disabled")).ToBeDisabledAsync();
        await Expect(Composer("cht-disabled")).ToHaveAttributeAsync("aria-disabled", "true");

        var suggestion = Chat("cht-disabled").Locator(".bit-aiems-sgs > .bit-btn").First;
        await Expect(suggestion).ToBeDisabledAsync();
        await suggestion.ClickAsync(new() { Force = true });
        await suggestion.DispatchEventAsync("click");

        await Page.WaitForTimeoutAsync(300);
        await Expect(Chat("cht-disabled").Locator("article.bit-aicms")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cht-disabled-log")).ToHaveTextAsync("0");
    }

    // ---------- style ----------

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheChat()
    {
        await OpenAsync(Route);

        await Expect(Chat("cht-var")).ToHaveCSSAsync("background-color", "rgb(250, 240, 230)");
        await Expect(Chat("cht-var").Locator(".bit-aicht-ftc")).ToHaveCSSAsync("max-width", "400px");
        var footer = Chat("cht-var").Locator(".bit-aicht-ftr");
        await Expect(footer).ToHaveCSSAsync("padding-top", "10px");
        await Expect(footer).ToHaveCSSAsync("padding-right", "20px");
        await Expect(footer).ToHaveCSSAsync("padding-bottom", "30px");
        await Expect(footer).ToHaveCSSAsync("padding-left", "20px");
        StringAssert.Contains(await ComputedStyleAsync("#cht-var .bit-aicht-ftr", "background-image"), "rgb(250, 240, 230)");
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheirParts()
    {
        await OpenAsync(Route);

        var chat = Chat("cht-classes");
        await Expect(chat).ToHaveClassAsync(HasClass("e2e-cht-root"));
        await Expect(chat.Locator(".bit-aicht-ftr")).ToHaveClassAsync(HasClass("e2e-cht-footer"));
        await Expect(chat.Locator(".bit-aicht-ftr")).ToHaveCSSAsync("outline-color", "rgb(1, 2, 3)");
        await Expect(chat.Locator(".bit-aicht-ftc")).ToHaveCSSAsync("max-width", "300px");
        await Expect(chat.Locator(".bit-aimsl")).ToHaveClassAsync(HasClass("e2e-cht-list"));
        await Expect(chat.Locator(".bit-aiems")).ToHaveClassAsync(HasClass("e2e-cht-empty"));
    }

    // ---------- rtl, print ----------

    [TestMethod]
    [DataRow("cht-rtl")]
    [DataRow("cht-rtl-inherited")]
    public async Task Rtl_TheChat_MirrorsTheBubblesAndTheirTail(string id)
    {
        await OpenAsync(Route);

        if (id == "cht-rtl")
        {
            await Expect(Chat(id)).ToHaveAttributeAsync("dir", "rtl");
            await Expect(Chat(id)).ToHaveClassAsync(HasClass("bit-rtl"));
        }
        await Expect(Chat(id)).ToHaveCSSAsync("direction", "rtl");

        // The question sits at the end of the line, which is the left in right-to-left text, and the corner it speaks
        // from is the bottom left one.
        var column = await Chat(id).Locator(".bit-aimsl-con").RectAsync();
        var bubble = Messages(id, "user").First.Locator(".bit-aicms-bbl");
        var rect = await bubble.RectAsync();
        Assert.IsTrue(rect.Left < column.Left + 40, $"The bubble is at the left: {rect.Left} vs {column.Left}.");
        Assert.IsTrue(rect.Right < column.Right - 40, "The bubble is no wider than its text.");

        var tight = AiE2EHelpers.ParsePx(await bubble.EvaluateAsync<string>("e => getComputedStyle(e).borderBottomLeftRadius"));
        var round = AiE2EHelpers.ParsePx(await bubble.EvaluateAsync<string>("e => getComputedStyle(e).borderBottomRightRadius"));
        Assert.IsTrue(tight < round, $"The tail corner is the bottom left one: {tight} vs {round}.");
    }

    [TestMethod]
    public async Task Bubble_LeftToRight_SpeaksFromTheBottomRightCorner()
    {
        await OpenAsync(Route);

        await SendAsync("cht-basic", "tail");
        var bubble = Messages("cht-basic", "user").First.Locator(".bit-aicms-bbl");
        await Expect(bubble).ToBeVisibleAsync();
        var tight = AiE2EHelpers.ParsePx(await bubble.EvaluateAsync<string>("e => getComputedStyle(e).borderBottomRightRadius"));
        var round = AiE2EHelpers.ParsePx(await bubble.EvaluateAsync<string>("e => getComputedStyle(e).borderBottomLeftRadius"));
        Assert.IsTrue(tight < round, $"The tail corner is the bottom right one: {tight} vs {round}.");
    }

    [TestMethod]
    public async Task Print_TheChat_LaysOutItsWholeTranscript()
    {
        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { Media = Media.Print });

        await Expect(Transcript("cht-scroll")).ToHaveCSSAsync("overflow-y", "visible");
        await Expect(Chat("cht-scroll").Locator(".bit-aicht-ftr")).ToHaveCSSAsync("background-image", "none");
        var chat = await Chat("cht-scroll").RectAsync();
        Assert.IsTrue(chat.Bottom - chat.Top > 360, "The printed chat is as tall as its whole transcript.");
    }

    [TestMethod]
    public async Task Prerender_TheChat_RendersItsHistoryBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Matches(html, new Regex(@"<div[^>]*id=""cht-basic""[^>]*class=""[^""]*\bbit-aicht\b"));
        StringAssert.Contains(html, "data-message-id=\"scr-24\"");
        StringAssert.Contains(html, "How can I help?");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Unmount_LeavingThePage_DisposesTheSessionsWithoutErrors()
    {
        await OpenAsync(Route);

        await SendAsync("cht-stop", "write a lot");
        await Expect(SendButton("cht-stop")).ToHaveAttributeAsync("aria-label", "Stop");

        // Leaving mid-stream disposes the chats and the sessions of the page, which stops the turn in flight.
        await OpenAsync("/e2e/aiemptystate");
        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
