namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiChatMessage at /e2e/aichatmessage in a real browser: the article each role is drawn as (a bubble at the end
/// of the line, full-width prose, a centered pill), its data attributes, aria-busy flipping live while a message streams
/// and the thinking indicator before its first token, the actions revealed on hover and on focus (and always shown under
/// a preference for more contrast), editing a question (save regenerates the answer, cancel keeps it), feedback raised
/// with the message, the retry of a failed reply, the entrance animation and reduced motion, ChildContent, Disabled, the
/// theme tokens, every --bit-AiChatMessage-* variable, Styles/Classes, RTL (the bubble and the corner it speaks from),
/// forced colors and print.
/// </summary>
[TestClass]
public class BitAiChatMessageE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aichatmessage";

    private ILocator Message(string id) => Page.Locator($"#{id}");

    private ILocator SessionMessages(string role) => Page.Locator($"#cms-session > article[data-role='{role}']");

    // ---------- roles ----------

    [TestMethod]
    public async Task Roles_EachRole_IsAnArticleWithItsRoleAndIdAsData()
    {
        await OpenAsync(Route);

        foreach (var (id, role, cls, messageId) in new[]
                 {
                     ("cms-user", "user", "bit-aicms-usr", "cms-u"),
                     ("cms-assistant", "assistant", "bit-aicms-ast", "cms-a"),
                     ("cms-system", "system", "bit-aicms-sys", "cms-s"),
                 })
        {
            var message = Message(id);
            Assert.AreEqual("ARTICLE", await message.EvaluateAsync<string>("e => e.tagName"), id);
            await Expect(message).ToHaveClassAsync(HasClass("bit-aicms"));
            await Expect(message).ToHaveClassAsync(HasClass(cls));
            await Expect(message).ToHaveAttributeAsync("data-role", role);
            await Expect(message).ToHaveAttributeAsync("data-message-id", messageId);
            await Expect(message).ToHaveAttributeAsync("aria-busy", "false");
        }

        await Expect(Page.GetByRole(AriaRole.Article)).Not.ToHaveCountAsync(0);
        await Expect(Message("cms-user").Locator(".bit-aicms-bbl")).ToHaveTextAsync("How do I install it?");
        await Expect(Message("cms-assistant").Locator(".bit-aicms-bdy")).ToHaveTextAsync("Run dotnet add package Bit.BlazorUI.AI.");
        await Expect(Message("cms-system").Locator(".bit-aicms-pil")).ToHaveTextAsync("The assistant joined the conversation.");
    }

    [TestMethod]
    public async Task Roles_TheLayout_PutsTheQuestionAtTheEndTheAnswerAcrossAndTheNoteInTheMiddle()
    {
        await OpenAsync(Route);

        var column = await Page.Locator("#cms-roles").RectAsync();

        var bubble = await Message("cms-user").Locator(".bit-aicms-bbl").RectAsync();
        Assert.AreEqual(column.Right, bubble.Right, 1, "The question ends at the end of the line.");
        Assert.IsTrue(bubble.Left > column.Left + 100, "The bubble is as wide as its text.");

        var body = await Message("cms-assistant").Locator(".bit-aicms-bdy").RectAsync();
        Assert.AreEqual(column.Left, body.Left, 1);
        Assert.AreEqual(column.Right, body.Right, 1, "The answer takes the whole column.");

        var pill = await Message("cms-system").Locator(".bit-aicms-pil").RectAsync();
        Assert.AreEqual((column.Left + column.Right) / 2, (pill.Left + pill.Right) / 2, 1.5, "The note is centered.");
    }

    [TestMethod]
    public async Task Roles_TheThemeTokens_PaintTheBubbleTheAnswerAndTheNote()
    {
        await OpenAsync(Route);

        var bubble = Message("cms-user").Locator(".bit-aicms-bbl");
        await Expect(bubble).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-sec)"));
        await Expect(bubble).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-pri)"));
        await Expect(bubble).ToHaveCSSAsync("white-space", "pre-wrap");
        await Expect(bubble).ToHaveCSSAsync("border-top-left-radius", await bubble.ResolveLengthInAsync("var(--bit-shp-radius-lg)"));
        await Expect(bubble).ToHaveCSSAsync("border-bottom-right-radius", await bubble.ResolveLengthInAsync("var(--bit-shp-radius-sm)"));

        var body = Message("cms-assistant").Locator(".bit-aicms-bdy");
        await Expect(body).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-pri)"));
        await Expect(body).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");

        var pill = Message("cms-system").Locator(".bit-aicms-pil");
        await Expect(pill).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));
        await Expect(pill).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-sec)"));
        await Expect(pill).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-brd-ter)"));
    }

    [TestMethod]
    public async Task Roles_NoSession_TheQuestionOffersNoActionsAndTheAnswerNoRegeneration()
    {
        await OpenAsync(Route);

        await Expect(Message("cms-user").Locator(".bit-aicms-act")).ToHaveCountAsync(0);
        var actions = Message("cms-assistant").Locator(".bit-aimac");
        await Expect(actions.Locator("button[aria-label='Copy']")).ToHaveCountAsync(1);
        await Expect(actions.Locator("button[aria-label='Good response']")).ToHaveCountAsync(1);
        await Expect(actions.Locator("button[aria-label='Regenerate']")).ToHaveCountAsync(0);
    }

    // ---------- busy ----------

    [TestMethod]
    public async Task Busy_AStreamingMessage_IsBusyUntilItCompletes()
    {
        await OpenAsync(Route);

        var message = Message("cms-busy");
        await Expect(message).ToHaveAttributeAsync("aria-busy", "false");

        // Waiting for its first token, the reply is busy and says it is thinking in a polite status.
        await Page.Locator("#cms-busy-pending").ClickAsync();
        await Expect(message).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(message.Locator(".bit-aithi")).ToHaveCountAsync(1);
        await Expect(message.GetByRole(AriaRole.Status)).ToHaveCountAsync(1);
        await Expect(message.Locator(".bit-aimac")).ToHaveCountAsync(0);

        // Streaming, it has text and no thinking indicator, and no actions until it is done.
        await Page.Locator("#cms-busy-streaming").ClickAsync();
        await Expect(message.Locator(".bit-aicms-bdy")).ToContainTextAsync("Half an answer");
        await Expect(message.Locator(".bit-aithi")).ToHaveCountAsync(0);
        await Expect(message).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(message.Locator(".bit-aimac")).ToHaveCountAsync(0);

        // A status line replaces the indicator's default label.
        await Page.Locator("#cms-busy-status").ClickAsync();
        await Expect(message.Locator(".bit-aithi")).ToContainTextAsync("Searching the documentation");

        await Page.Locator("#cms-busy-complete").ClickAsync();
        await Expect(message).ToHaveAttributeAsync("aria-busy", "false");
        await Expect(message.Locator(".bit-aithi")).ToHaveCountAsync(0);
        await Expect(message.Locator(".bit-aimac")).ToHaveCountAsync(1);
    }

    // ---------- reveal ----------

    [TestMethod]
    public async Task Actions_OfAQuestion_AreRevealedUnderThePointerAndHiddenAgainWhenItLeaves()
    {
        await OpenAsync(Route);

        var question = SessionMessages("user").First;
        var actions = question.Locator(".bit-aicms-act");
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(actions).ToHaveCSSAsync("opacity", "0");
        await Expect(actions.Locator("button[aria-label='Copy']")).ToHaveCountAsync(1);
        await Expect(actions.Locator("button[aria-label='Edit']")).ToHaveCountAsync(1);

        await question.HoverAsync();
        await Expect(actions).ToHaveCSSAsync("opacity", "1");

        await Page.Mouse.MoveAsync(0, 0);
        await Expect(actions).ToHaveCSSAsync("opacity", "0");
    }

    [TestMethod]
    public async Task Actions_OfAnAnswer_AreRevealedWhileTheFocusIsInsideTheMessage()
    {
        await OpenAsync(Route);

        var answer = SessionMessages("assistant").First;
        var actions = answer.Locator(".bit-aimac-act");
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(actions).ToHaveCSSAsync("opacity", "0");

        // Tabbing to the first action reveals the row; the focus leaving it hides it again.
        await SessionMessages("user").First.Locator("button[aria-label='Edit']").FocusAsync();
        await Expect(SessionMessages("user").First.Locator(".bit-aicms-act")).ToHaveCSSAsync("opacity", "1");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(answer.Locator("button[aria-label='Copy']")).ToBeFocusedAsync();
        await Expect(actions).ToHaveCSSAsync("opacity", "1");
        await Expect(SessionMessages("user").First.Locator(".bit-aicms-act")).ToHaveCSSAsync("opacity", "0");

        await answer.Locator("button[aria-label='Copy']").BlurAsync();
        await Expect(actions).ToHaveCSSAsync("opacity", "0");
    }

    [TestMethod]
    public async Task Actions_UnderAPreferenceForMoreContrast_AreAlwaysShown()
    {
        // Emulated before the page loads: Firefox does not re-apply the rules of a loaded stylesheet to a changed one.
        await Page.EmulateMediaAsync(new() { Contrast = Contrast.More });
        await OpenAsync(Route);
        await Page.Mouse.MoveAsync(0, 0);
        if (await Page.MatchesMediaAsync("(prefers-contrast: more)") is false) Assert.Inconclusive("The engine does not emulate prefers-contrast.");

        await Expect(SessionMessages("user").First.Locator(".bit-aicms-act")).ToHaveCSSAsync("opacity", "1");
        await Expect(SessionMessages("assistant").First.Locator(".bit-aimac-act")).ToHaveCSSAsync("opacity", "1");
    }

    // ---------- edit ----------

    [TestMethod]
    public async Task Edit_TheQuestionSaved_ReplacesItAndRegeneratesTheAnswer()
    {
        await OpenAsync(Route);

        var question = SessionMessages("user").First;
        await question.HoverAsync();
        await question.Locator("button[aria-label='Edit']").ClickAsync();

        // The bubble turns into an editor holding the text, with Cancel and Save under it.
        var editor = question.Locator(".bit-aicms-edt textarea");
        await Expect(editor).ToHaveValueAsync("Original question");
        await Expect(editor).ToHaveAttributeAsync("aria-label", "Edit");
        await Expect(question.Locator(".bit-aicms-bbl")).ToHaveCountAsync(0);
        await Expect(question.Locator(".bit-aicms-eda .bit-btn")).ToHaveTextAsync(["Cancel", "Save"]);

        await editor.FillAsync("Edited question");
        await question.Locator(".bit-aicms-eda .bit-btn", new() { HasText = "Save" }).ClickAsync();

        await Expect(question.Locator(".bit-aicms-bbl")).ToHaveTextAsync("Edited question");
        await Expect(Page.Locator("#cms-session > article[data-message-id='ses-a1']")).ToHaveCountAsync(0);
        await Expect(SessionMessages("assistant")).ToHaveCountAsync(1);
        await Expect(SessionMessages("assistant").First.Locator(".bit-aicms-bdy")).ToHaveTextAsync("You said: Edited question");
        await Expect(Page.Locator("#cms-session-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Edit_Cancelled_KeepsTheQuestionAndTheAnswer()
    {
        await OpenAsync(Route);

        var question = SessionMessages("user").First;
        await question.HoverAsync();
        await question.Locator("button[aria-label='Edit']").ClickAsync();
        await question.Locator(".bit-aicms-edt textarea").FillAsync("Never sent");
        await question.Locator(".bit-aicms-eda .bit-btn", new() { HasText = "Cancel" }).ClickAsync();

        await Expect(question.Locator(".bit-aicms-edt")).ToHaveCountAsync(0);
        await Expect(question.Locator(".bit-aicms-bbl")).ToHaveTextAsync("Original question");
        await Expect(Page.Locator("#cms-session > article[data-message-id='ses-a1'] .bit-aicms-bdy")).ToHaveTextAsync("Original answer");
    }

    // ---------- feedback, regenerate ----------

    [TestMethod]
    public async Task Feedback_OnTheAnswer_IsRaisedWithTheMessage()
    {
        await OpenAsync(Route);

        var answer = SessionMessages("assistant").First;
        await answer.HoverAsync();
        await answer.Locator("button[aria-label='Good response']").ClickAsync();
        await Expect(answer.Locator("button[aria-label='Good response']")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator("#cms-feedback-log")).ToHaveTextAsync("ses-a1:Positive");

        await answer.Locator("button[aria-label='Bad response']").ClickAsync();
        await Expect(Page.Locator("#cms-feedback-log")).ToHaveTextAsync("ses-a1:Positive,ses-a1:Negative");
        await Expect(answer.Locator("button[aria-label='Good response']")).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(answer.Locator("button[aria-label='Bad response']")).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod]
    public async Task Regenerate_TheNewestAnswer_IsReplacedByANewOne()
    {
        await OpenAsync(Route);

        var answer = Page.Locator("#cms-session > article[data-message-id='ses-a1']");
        await answer.HoverAsync();
        await answer.Locator("button[aria-label='Regenerate']").ClickAsync();

        await Expect(answer).ToHaveCountAsync(0);
        await Expect(SessionMessages("assistant")).ToHaveCountAsync(1);
        await Expect(SessionMessages("assistant").First.Locator(".bit-aicms-bdy")).ToHaveTextAsync("You said: Original question");
    }

    // ---------- retry ----------

    [TestMethod]
    public async Task Retry_AFailedReply_OffersTryAgainWhichAnswers()
    {
        await OpenAsync(Route);

        await Page.Locator("#cms-retry-ask").ClickAsync();
        var reply = Page.Locator("#cms-retry > article[data-role='assistant']");
        await Expect(reply.Locator(".bit-aierv")).ToHaveCountAsync(1);
        await Expect(reply).ToHaveAttributeAsync("aria-busy", "false");

        await reply.Locator(".bit-aierv .bit-btn", new() { HasText = "Try again" }).ClickAsync();

        await Expect(reply).ToHaveCountAsync(1);
        await Expect(reply.Locator(".bit-aicms-bdy")).ToHaveTextAsync("That worked on the second attempt.");
        await Expect(reply.Locator(".bit-aierv")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cms-retry > article[data-role='user']")).ToHaveCountAsync(1);
    }

    // ---------- entrance ----------

    [TestMethod]
    public async Task Entrance_AnAnimatedMessage_FadesInAndReducedMotionCollapsesIt()
    {
        await OpenAsync(Route);

        await Expect(Message("cms-animated")).ToHaveClassAsync(HasClass("bit-aicms-ent"));
        await Expect(Message("cms-still")).Not.ToHaveClassAsync(HasClass("bit-aicms-ent"));
        await Expect(Message("cms-animated")).ToHaveCSSAsync("animation-name", "bit-aicms-ent");
        await Expect(Message("cms-still")).ToHaveCSSAsync("animation-name", "none");

        var duration = AiE2EHelpers.ParseSeconds(await ComputedStyleAsync("#cms-animated", "animation-duration"));
        Assert.IsTrue(duration > 0.05, $"The entrance plays: {duration}s.");
        // Backwards only: a transform animation still in effect after it has run would keep the message a containing
        // block for what is fixed inside it (a full-screen artifact).
        await Expect(Message("cms-animated")).ToHaveCSSAsync("animation-fill-mode", "backwards");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await Page.Locator("#cms-replay").ClickAsync();
        await Expect(Message("cms-animated")).ToHaveClassAsync(HasClass("bit-aicms-ent"));
        Assert.IsTrue(AiE2EHelpers.ParseSeconds(await ComputedStyleAsync("#cms-animated", "animation-duration")) <= 0.001, "The entrance collapses.");
    }

    // ---------- content ----------

    [TestMethod]
    public async Task ChildContent_IsRenderedAtTheEndOfTheAnswer()
    {
        await OpenAsync(Route);

        var body = Message("cms-child").Locator(".bit-aicms-bdy");
        await Expect(body.Locator(":scope > #cms-child-content")).ToHaveTextAsync("Was this what you were looking for?");
        Assert.IsTrue(await body.Locator("#cms-child-content").EvaluateAsync<bool>("e => e === e.parentElement.lastElementChild"));
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_TheMessage_DisablesItsActions()
    {
        await OpenAsync(Route);

        await Expect(Message("cms-disabled-user")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Message("cms-disabled-user").Locator("button[aria-label='Edit']")).ToBeDisabledAsync();
        await Expect(Message("cms-disabled-assistant").Locator("button[aria-label='Regenerate']")).ToBeDisabledAsync();
        await Expect(Message("cms-disabled-assistant").Locator("button[aria-label='Good response']")).ToBeDisabledAsync();

        await Message("cms-disabled-user").Locator("button[aria-label='Edit']").DispatchEventAsync("click");
        await Page.WaitForTimeoutAsync(300);
        await Expect(Message("cms-disabled-user").Locator(".bit-aicms-edt")).ToHaveCountAsync(0);
    }

    // ---------- style ----------

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleEveryRole()
    {
        await OpenAsync(Route);

        var bubble = Message("cms-var-user").Locator(".bit-aicms-bbl");
        await Expect(bubble).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(bubble).ToHaveCSSAsync("color", "rgb(240, 241, 242)");
        await Expect(bubble).ToHaveCSSAsync("border-top-left-radius", "20px");
        Assert.IsTrue((await bubble.RectAsync()) is var b && b.Right - b.Left <= 200.5, "The bubble stops growing at the max width.");

        var answer = Message("cms-var-assistant");
        await Expect(answer.Locator(".bit-aicms-bdy")).ToHaveCSSAsync("background-color", "rgb(250, 251, 252)");
        await Expect(answer.Locator(".bit-aicms-bdy")).ToHaveCSSAsync("color", "rgb(11, 12, 13)");
        await Expect(answer.Locator(".bit-aicms-bdy")).ToHaveCSSAsync("row-gap", "17px");
        await Expect(answer).ToHaveCSSAsync("row-gap", "21px");

        var pill = Message("cms-var-system").Locator(".bit-aicms-pil");
        await Expect(pill).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(pill).ToHaveCSSAsync("color", "rgb(4, 5, 6)");
        await Expect(pill).ToHaveCSSAsync("border-top-color", "rgb(7, 8, 9)");
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheBodyAndTheActions()
    {
        await OpenAsync(Route);

        await Expect(Message("cms-classes")).ToHaveClassAsync(HasClass("e2e-cms-root"));
        await Expect(Message("cms-classes").Locator(".bit-aicms-bdy")).ToHaveClassAsync(HasClass("e2e-cms-body"));
        await Expect(Message("cms-classes").Locator(".bit-aicms-bdy")).ToHaveCSSAsync("outline-color", "rgb(9, 8, 7)");
        await Expect(Message("cms-classes").Locator(".bit-aimac")).ToHaveClassAsync(HasClass("e2e-cms-actions"));
    }

    // ---------- rtl, forced colors, print ----------

    [TestMethod]
    public async Task Rtl_TheQuestion_SitsAtTheLeftAndSpeaksFromTheBottomLeftCorner()
    {
        await OpenAsync(Route);

        var message = Message("cms-rtl-user");
        await Expect(message).ToHaveAttributeAsync("dir", "rtl");
        await Expect(message).ToHaveClassAsync(HasClass("bit-rtl"));

        var root = await message.RectAsync();
        var bubble = message.Locator(".bit-aicms-bbl");
        var rect = await bubble.RectAsync();
        Assert.AreEqual(root.Left, rect.Left, 1, "The question ends at the left in right-to-left text.");

        await Expect(bubble).ToHaveCSSAsync("border-bottom-left-radius", await bubble.ResolveLengthInAsync("var(--bit-shp-radius-sm)"));
        await Expect(bubble).ToHaveCSSAsync("border-bottom-right-radius", await bubble.ResolveLengthInAsync("var(--bit-shp-radius-lg)"));

        // The row of actions under the answer reads from the right.
        var copy = await Message("cms-rtl-assistant").Locator("button[aria-label='Copy']").RectAsync();
        var good = await Message("cms-rtl-assistant").Locator("button[aria-label='Good response']").RectAsync();
        Assert.IsTrue(copy.Left > good.Left, "The first action is the rightmost one.");
    }

    [TestMethod]
    public async Task ForcedColors_TheBubbleAndThePill_AreOutlinedInCanvasText()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        var canvasText = await Page.ResolveColorAsync("CanvasText");
        await Expect(Message("cms-user").Locator(".bit-aicms-bbl")).ToHaveCSSAsync("border-top-style", "solid");
        await Expect(Message("cms-user").Locator(".bit-aicms-bbl")).ToHaveCSSAsync("border-top-color", canvasText);
        await Expect(Message("cms-system").Locator(".bit-aicms-pil")).ToHaveCSSAsync("border-top-color", canvasText);
    }

    [TestMethod]
    public async Task Print_TheActions_AreLeftOut()
    {
        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { Media = Media.Print });

        await Expect(SessionMessages("user").First.Locator(".bit-aicms-act")).ToHaveCSSAsync("display", "none");
        await Expect(SessionMessages("assistant").First.Locator(".bit-aimac-act")).ToHaveCSSAsync("display", "none");
        await Expect(SessionMessages("assistant").First.Locator(".bit-aicms-bdy")).ToBeVisibleAsync();
    }
}
