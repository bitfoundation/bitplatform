using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiPromptInput at /e2e/aipromptinput in a real browser: the submit key (Enter, Shift+Enter, Ctrl+Enter, an
/// IME composition, SubmitOnEnter off), the text growing up to its row cap, the slash command and mention menu read at
/// the caret (the arrows, Enter and Tab, a click, a handler, the Escape it claims ahead of a dialog around it), a session
/// streaming a reply and stopped by the button or Escape, files pasted, dropped and picked (previews, the drop overlay,
/// removal) and turned away (type, size, count), a composer without attachments, Disabled refusing input and files, the
/// character limit, a draft surviving a reload, dictation landing in the text, the AI glyph font, the theme tokens and
/// the public variables, and right-to-left.
/// </summary>
[TestClass]
public class BitAiPromptInputE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aipromptinput";

    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Input(string id) => Page.Locator($"#{id} textarea.bit-aipri-inp");

    private ILocator Send(string id) => Page.Locator($"#{id} .bit-aipri-snd");

    private ILocator Attach(string id) => Page.Locator($"#{id} .bit-aipri-atb");

    private ILocator Menu(string id) => Page.Locator($"#{id}-mnu");

    private ILocator Options(string id) => Page.Locator($"#{id}-mnu [role=option]");

    private ILocator Chips(string id) => Page.Locator($"#{id} .bit-aipri-ats .bit-aiach");

    private ILocator Notice(string id) => Page.Locator($"#{id} .bit-aipri-ntc");

    private ILocator Text(string id) => Page.Locator($"#{id}");

    private static Regex Exactly(string text) => new($"^{Regex.Escape(text)}$");

    // ---------- the submit key ----------

    [TestMethod]
    public async Task Enter_SubmitsTheTrimmedText_AndEmptiesTheComposer()
    {
        await OpenAsync(Route);

        var input = Input("pi-basic");
        await input.FillAsync("  Hello there  ");
        await Expect(Text("pi-basic-value")).ToHaveTextAsync(Exactly("  Hello there  "));
        await Expect(Send("pi-basic")).Not.ToHaveAttributeAsync("aria-disabled", "true");

        await input.PressAsync("Enter");

        await Expect(Text("pi-basic-count")).ToHaveTextAsync("1");
        await Expect(Text("pi-basic-sent")).ToHaveTextAsync(Exactly("Hello there"));
        await Expect(input).ToHaveValueAsync("");
        await Expect(Text("pi-basic-value")).ToHaveTextAsync(Exactly(""));
        await Expect(input).ToBeFocusedAsync();
        await Expect(Send("pi-basic")).ToHaveAttributeAsync("aria-disabled", "true");
    }

    [TestMethod]
    public async Task ShiftEnter_BreaksTheLine_AndTheNextEnterSendsBothLines()
    {
        await OpenAsync(Route);

        var input = Input("pi-basic");
        await input.FillAsync("first");
        await Expect(Text("pi-basic-value")).ToHaveTextAsync("first");

        await input.PressAsync("Shift+Enter");
        await Expect(Text("pi-basic-value")).ToHaveTextAsync(Exactly("first\\n"));
        await Page.Keyboard.InsertTextAsync("second");
        await Expect(Text("pi-basic-value")).ToHaveTextAsync(Exactly("first\\nsecond"));
        await Expect(input).ToHaveValueAsync("first\nsecond");
        await Expect(Text("pi-basic-count")).ToHaveTextAsync("0");

        await input.PressAsync("Enter");

        await Expect(Text("pi-basic-count")).ToHaveTextAsync("1");
        await Expect(Text("pi-basic-sent")).ToHaveTextAsync(Exactly("first\\nsecond"));
    }

    [TestMethod]
    public async Task CtrlEnter_SubmitsToo_WithoutBreakingTheLine()
    {
        await OpenAsync(Route);

        var input = Input("pi-basic");
        await input.FillAsync("with a modifier");
        await Expect(Text("pi-basic-value")).ToHaveTextAsync("with a modifier");

        await input.PressAsync("Control+Enter");

        await Expect(Text("pi-basic-count")).ToHaveTextAsync("1");
        await Expect(Text("pi-basic-sent")).ToHaveTextAsync(Exactly("with a modifier"));
        await Expect(input).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task BlankText_NeitherSendsNorEnablesTheSendButton()
    {
        await OpenAsync(Route);

        var input = Input("pi-basic");
        await input.FillAsync("   ");
        await Expect(Text("pi-basic-value")).ToHaveTextAsync(Exactly("   "));
        await Expect(Send("pi-basic")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Send("pi-basic")).ToHaveClassAsync(HasClass("bit-dis"));

        await input.PressAsync("Enter");
        await Send("pi-basic").ClickAsync(new() { Force = true });

        // Then a real one: the first and only submission.
        await input.FillAsync("real");
        await Expect(Text("pi-basic-value")).ToHaveTextAsync("real");
        await Send("pi-basic").ClickAsync();

        await Expect(Text("pi-basic-count")).ToHaveTextAsync("1");
        await Expect(Text("pi-basic-sent")).ToHaveTextAsync(Exactly("real"));
    }

    [TestMethod]
    public async Task AnImeComposition_KeepsItsEnter_AndNeverSubmits()
    {
        await OpenAsync(Route);

        var input = Input("pi-basic");
        await input.FillAsync("ni");
        await Expect(Text("pi-basic-value")).ToHaveTextAsync("ni");

        // The Enter that commits a candidate - as the standard reports it (isComposing) and as the legacy key code 229
        // with the "Process" key does - is the composition's: the composer neither prevents it nor submits on it.
        var defaults = await input.EvaluateAsync<bool[]>(
            """
            el => {
                el.dispatchEvent(new CompositionEvent('compositionstart', { bubbles: true, data: '' }));
                const standard = el.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', code: 'Enter', bubbles: true, cancelable: true, isComposing: true }));
                const legacy = new KeyboardEvent('keydown', { key: 'Process', code: 'Enter', bubbles: true, cancelable: true });
                Object.defineProperty(legacy, 'keyCode', { get: () => 229 });
                const legacyKept = el.dispatchEvent(legacy);
                el.dispatchEvent(new CompositionEvent('compositionend', { bubbles: true, data: 'ni' }));
                return [standard, legacyKept];
            }
            """);
        CollectionAssert.AreEqual(new[] { true, true }, defaults, "The Enter of a composition is left to the composition.");

        // A real Enter after it is the first submission, and it carries the text the composition left.
        await input.FillAsync("ni hao");
        await Expect(Text("pi-basic-value")).ToHaveTextAsync("ni hao");
        await input.PressAsync("Enter");

        await Expect(Text("pi-basic-sent")).ToHaveTextAsync(Exactly("ni hao"));
        await Expect(Text("pi-basic-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task TheText_IsANamedTextBox_OrComboboxWithAMenu_AndTheSendButtonIsNamedForWhatItDoes()
    {
        await OpenAsync(Route);

        // With no commands and no mentions there is no popup to announce: a plain, named text box.
        var input = Input("pi-basic");
        await Expect(input).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(input).ToHaveAttributeAsync("aria-label", "Send a message...");
        await Expect(input).ToHaveAttributeAsync("placeholder", "Send a message...");
        await Expect(input).Not.ToHaveAttributeAsync("aria-haspopup", new Regex(".*"));
        await Expect(input).Not.ToHaveAttributeAsync("aria-autocomplete", new Regex(".*"));
        await Expect(input).Not.ToHaveAttributeAsync("aria-expanded", new Regex(".*"));
        await Expect(input).Not.ToHaveAttributeAsync("aria-controls", new Regex(".*"));
        await Expect(input).Not.ToHaveAttributeAsync("data-bit-esc", new Regex(".*"));

        // With them, a combobox whose listbox is closed.
        var menu = Input("pi-menu");
        await Expect(menu).ToHaveAttributeAsync("role", "combobox");
        await Expect(menu).ToHaveAttributeAsync("aria-haspopup", "listbox");
        await Expect(menu).ToHaveAttributeAsync("aria-autocomplete", "list");
        await Expect(menu).ToHaveAttributeAsync("aria-expanded", "false");

        await Expect(Page.GetByRole(AriaRole.Textbox, new() { Name = "Ask anything about your account..." })).ToHaveCountAsync(1);
        await Expect(Page.GetByRole(AriaRole.Textbox, new() { Name = "Message the assistant" })).ToHaveCountAsync(1);
        await Expect(Input("pi-aria")).ToHaveAttributeAsync("placeholder", "Shown placeholder");

        await Expect(Send("pi-basic")).ToHaveAttributeAsync("aria-label", "Send");
        await Expect(Send("pi-basic")).ToHaveAttributeAsync("title", "Send");
        await Expect(Attach("pi-basic")).ToHaveAttributeAsync("aria-label", "Attach files");

        // The file input is only the means of the picker: out of the tab order and of the accessibility tree.
        var fileInput = Page.Locator("#pi-basic input.bit-aipri-fil");
        await Expect(fileInput).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(fileInput).ToHaveAttributeAsync("aria-hidden", "true");

        // Tab goes from the text to the attach button and on to the send button once there is something to send.
        await input.FillAsync("x");
        await Expect(Text("pi-basic-value")).ToHaveTextAsync("x");
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Attach("pi-basic")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Send("pi-basic")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task SubmitOnEnterOff_EnterBreaksTheLine_AndCtrlEnterSends()
    {
        await OpenAsync(Route);

        var input = Input("pi-noenter");
        await input.FillAsync("line one");
        await Expect(Text("pi-noenter-value")).ToHaveTextAsync("line one");

        await input.PressAsync("Enter");
        await Expect(Text("pi-noenter-value")).ToHaveTextAsync(Exactly("line one\\n"));
        await Page.Keyboard.InsertTextAsync("line two");
        await Expect(Text("pi-noenter-value")).ToHaveTextAsync(Exactly("line one\\nline two"));
        await Expect(Text("pi-noenter-count")).ToHaveTextAsync("0");

        await input.PressAsync("Control+Enter");

        await Expect(Text("pi-noenter-count")).ToHaveTextAsync("1");
        await Expect(Text("pi-noenter-sent")).ToHaveTextAsync(Exactly("line one\\nline two"));
        await Expect(input).ToHaveValueAsync("");
    }

    // ---------- auto-resize ----------

    [TestMethod]
    public async Task TheText_GrowsWithItsLines_UpToTheRowCap_ThenScrolls_AndShrinksBack()
    {
        await OpenAsync(Route);

        var input = Input("pi-resize");
        await WaitForHeightToSettleAsync(input);
        var single = (await input.BoundingBoxAsync())!.Height;
        var lineHeight = AiE2EHelpers.ParsePx(await ComputedStyleAsync("#pi-resize textarea", "line-height"));
        var padding = AiE2EHelpers.ParsePx(await ComputedStyleAsync("#pi-resize textarea", "padding-top"))
                    + AiE2EHelpers.ParsePx(await ComputedStyleAsync("#pi-resize textarea", "padding-bottom"));

        // The leading is the unit the rows are counted in: 1.5 times the size of the text.
        Assert.AreEqual(1.5 * AiE2EHelpers.ParsePx(await ComputedStyleAsync("#pi-resize textarea", "font-size")), lineHeight, 0.5);

        await input.FillAsync("one\ntwo\nthree");
        await WaitForHeightAsync(input, 3 * lineHeight + padding);
        await Expect(input).ToHaveCSSAsync("overflow-y", "hidden");

        // Past the twelve rows of the default cap it stops growing and scrolls.
        await input.FillAsync(string.Join("\n", Enumerable.Range(1, 20).Select(i => $"line {i}")));
        await WaitForHeightAsync(input, 12 * lineHeight + padding);
        await Expect(input).ToHaveCSSAsync("overflow-y", "auto");
        Assert.IsTrue(await input.EvaluateAsync<bool>("t => t.scrollHeight > t.clientHeight"), "The text scrolls inside the cap.");

        await input.FillAsync("short again");
        await WaitForHeightAsync(input, single);
        await Expect(input).ToHaveCSSAsync("overflow-y", "hidden");

        // MaxComposerRows of the options in force sets the cap.
        var capped = Input("pi-resize-capped");
        await capped.FillAsync(string.Join("\n", Enumerable.Range(1, 6).Select(i => $"row {i}")));
        await WaitForHeightAsync(capped, 3 * lineHeight + padding);
        await Expect(capped).ToHaveCSSAsync("overflow-y", "auto");
    }

    // ---------- slash commands and mentions ----------

    [TestMethod]
    public async Task Menu_ASlashAtTheStart_OpensTheCommands_AndTheArrowsWalkThemWithTheFocusInTheText()
    {
        await OpenAsync(Route);

        var input = Input("pi-menu");
        await input.FillAsync("/");

        var menu = Menu("pi-menu");
        await Expect(menu).ToBeVisibleAsync();
        await Expect(menu).ToHaveAttributeAsync("role", "listbox");
        await Expect(menu).ToHaveAttributeAsync("aria-label", "Commands");
        await Expect(Options("pi-menu")).ToHaveCountAsync(3);
        await Expect(Options("pi-menu").Locator(".bit-aipri-mlb")).ToHaveTextAsync(["/summarize", "/translate", "/clear"]);
        await Expect(Options("pi-menu").Nth(0).Locator(".bit-aipri-mhn")).ToHaveTextAsync("Summarize the conversation");
        await Expect(Options("pi-menu").Nth(0)).ToHaveAttributeAsync("tabindex", "-1");

        await Expect(input).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(input).ToHaveAttributeAsync("aria-controls", "pi-menu-mnu");
        await Expect(input).ToHaveAttributeAsync("aria-activedescendant", "pi-menu-opt-0");
        await Expect(Options("pi-menu").Nth(0)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Options("pi-menu").Nth(0)).ToHaveClassAsync(HasClass("bit-aipri-mia"));

        // The menu opens above the text, where a composer at the foot of a chat has room for it.
        var menuBox = (await menu.BoundingBoxAsync())!;
        var rootBox = (await Root("pi-menu").BoundingBoxAsync())!;
        Assert.IsTrue(menuBox.Y + menuBox.Height <= rootBox.Y + 1, "The menu sits above the composer.");

        await input.PressAsync("ArrowDown");
        await Expect(input).ToHaveAttributeAsync("aria-activedescendant", "pi-menu-opt-1");
        await Expect(Options("pi-menu").Nth(1)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Options("pi-menu").Nth(0)).ToHaveAttributeAsync("aria-selected", "false");

        await input.PressAsync("ArrowDown");
        await Expect(input).ToHaveAttributeAsync("aria-activedescendant", "pi-menu-opt-2");
        await input.PressAsync("ArrowDown");
        await Expect(input).ToHaveAttributeAsync("aria-activedescendant", "pi-menu-opt-0");
        await input.PressAsync("ArrowUp");
        await Expect(input).ToHaveAttributeAsync("aria-activedescendant", "pi-menu-opt-2");

        // The keys drove the menu and not the caret, and the focus never left the text.
        await Expect(input).ToBeFocusedAsync();
        Assert.AreEqual(1, await input.EvaluateAsync<int>("t => t.selectionStart"), "ArrowUp did not move the caret.");

        // The option the arrows reached is painted as the hover would paint it.
        var hover = await Page.ResolveColorAsync("var(--bit-clr-bg-pri-hover)");
        await Expect(Options("pi-menu").Nth(2)).ToHaveCSSAsync("background-color", hover);
        await Expect(Options("pi-menu").Nth(0)).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
    }

    [TestMethod]
    public async Task Menu_EnterInsertsTheTemplate_AndTabTheCommand_WithoutSendingAnything()
    {
        await OpenAsync(Route);

        var input = Input("pi-menu");
        await input.FillAsync("/sum");
        await Expect(Options("pi-menu")).ToHaveCountAsync(1);

        await input.PressAsync("Enter");

        await Expect(Menu("pi-menu")).ToHaveCountAsync(0);
        await Expect(Text("pi-menu-value")).ToHaveTextAsync(Exactly("Summarize this: "));
        await Expect(input).ToHaveValueAsync("Summarize this: ");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        await WaitForCaretAsync(input, "Summarize this: ".Length);

        await input.FillAsync("/tr");
        await Expect(Options("pi-menu")).ToHaveCountAsync(1);
        await input.PressAsync("Tab");

        await Expect(Text("pi-menu-value")).ToHaveTextAsync(Exactly("/translate "));
        await Expect(input).ToBeFocusedAsync();
        await Expect(Text("pi-menu-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Menu_AMentionTypedMidSentence_IsInsertedAtTheCaret_AndTheCaretLandsAfterIt()
    {
        await OpenAsync(Route);

        var input = Input("pi-menu");
        await input.FillAsync("Ask  today");
        await Expect(Text("pi-menu-value")).ToHaveTextAsync(Exactly("Ask  today"));

        await input.EvaluateAsync("t => { t.focus(); t.setSelectionRange(4, 4); }");
        await Page.Keyboard.InsertTextAsync("@al");

        await Expect(Menu("pi-menu")).ToBeVisibleAsync();
        await Expect(Menu("pi-menu")).ToHaveAttributeAsync("aria-label", "Mentions");
        await Expect(Options("pi-menu")).ToHaveCountAsync(1);
        await Expect(Options("pi-menu").Locator(".bit-aipri-mlb")).ToHaveTextAsync("Alice");
        await Expect(Options("pi-menu").Locator(".bit-aipri-mhn")).ToHaveTextAsync("Designer");

        await input.PressAsync("Enter");

        // Only the trigger and its query were replaced; the text after the caret is kept, without a doubled space.
        await Expect(Text("pi-menu-value")).ToHaveTextAsync(Exactly("Ask @Alice today"));
        await WaitForCaretAsync(input, "Ask @Alice ".Length);
        await Expect(Menu("pi-menu")).ToHaveCountAsync(0);
        await Expect(Text("pi-menu-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Menu_TheQueryFiltersTheOptions_AndOneMatchingNothingClosesTheMenu()
    {
        await OpenAsync(Route);

        var input = Input("pi-menu");
        await input.FillAsync("@");
        await Expect(Options("pi-menu")).ToHaveCountAsync(3);

        await input.FillAsync("@o");
        await Expect(Options("pi-menu").Locator(".bit-aipri-mlb")).ToHaveTextAsync(["Bob", "Docs folder"]);

        await input.FillAsync("@zz");
        await Expect(Menu("pi-menu")).ToHaveCountAsync(0);
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(input).Not.ToHaveAttributeAsync("aria-activedescendant", new Regex(".*"));
    }

    [TestMethod]
    public async Task Menu_ASlashAfterTextOrAnAtSignInsideAWord_OpensNothing_SoEnterSends()
    {
        await OpenAsync(Route);
        await CountCaretReadsAsync();

        var input = Input("pi-menu");
        await input.FillAsync("mail@al");
        await Expect(Text("pi-menu-value")).ToHaveTextAsync("mail@al");
        await WaitForCaretReadsAsync(1);
        await Expect(Menu("pi-menu")).ToHaveCountAsync(0);

        await input.FillAsync("a /sum");
        await Expect(Text("pi-menu-value")).ToHaveTextAsync("a /sum");
        await WaitForCaretReadsAsync(2);
        await Expect(Menu("pi-menu")).ToHaveCountAsync(0);

        await input.PressAsync("Enter");
        await Expect(Text("pi-menu-count")).ToHaveTextAsync("1");

        // An at sign after a space opens the mentions anywhere in the text.
        await input.FillAsync("hi @b");
        await Expect(Options("pi-menu").Locator(".bit-aipri-mlb")).ToHaveTextAsync("Bob");
    }

    [TestMethod]
    public async Task Menu_ACommandWithAHandler_RunsIt_AndDropsItsToken()
    {
        await OpenAsync(Route);

        var input = Input("pi-menu");
        await input.FillAsync("/cl");
        await Expect(Options("pi-menu").Locator(".bit-aipri-mlb")).ToHaveTextAsync("/clear");

        await input.PressAsync("Enter");

        await Expect(Text("pi-menu-cleared")).ToHaveTextAsync("1");
        await Expect(Text("pi-menu-value")).ToHaveTextAsync(Exactly(""));
        await Expect(Menu("pi-menu")).ToHaveCountAsync(0);
        await Expect(Text("pi-menu-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Menu_AClickOnAnOption_AcceptsIt_AndKeepsTheFocusInTheText()
    {
        await OpenAsync(Route);

        var input = Input("pi-menu");
        await input.FillAsync("@");
        await Expect(Options("pi-menu")).ToHaveCountAsync(3);

        // The pointer paints the option it is over the way the arrows do.
        await Options("pi-menu").Nth(1).HoverAsync();
        await Expect(Options("pi-menu").Nth(1)).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-pri-hover)"));

        await Options("pi-menu").Nth(1).ClickAsync();

        await Expect(Text("pi-menu-value")).ToHaveTextAsync(Exactly("@Bob "));
        await Expect(Menu("pi-menu")).ToHaveCountAsync(0);
        await Expect(input).ToBeFocusedAsync();
        await WaitForCaretAsync(input, "@Bob ".Length);
    }

    [TestMethod]
    public async Task Menu_MovingTheCaretOutOfTheToken_ClosesIt_AndBackInReopensIt()
    {
        await OpenAsync(Route);

        var input = Input("pi-menu");
        await input.FillAsync("@al");
        await Expect(Options("pi-menu")).ToHaveCountAsync(1);

        await input.PressAsync("Home");
        await Expect(Menu("pi-menu")).ToHaveCountAsync(0);

        await input.PressAsync("End");
        await Expect(Options("pi-menu")).ToHaveCountAsync(1);
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [TestMethod]
    public async Task Menu_ACommandTypedAndEnteredStraightAway_IsTaken_NotSent()
    {
        await OpenAsync(Route);

        // No wait for the menu between the typing and the Enter.
        var input = Input("pi-menu");
        await input.PressSequentiallyAsync("/cl");
        await input.PressAsync("Enter");

        await Expect(Text("pi-menu-cleared")).ToHaveTextAsync("1");
        await Expect(Text("pi-menu-value")).ToHaveTextAsync(Exactly(""));
        await Expect(Text("pi-menu-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Menu_AModifiedEnterOrTabKeepsItsOwnMeaning_AndTheMenuClosesAsTheFocusLeaves()
    {
        await OpenAsync(Route);

        // Shift+Enter breaks the line rather than taking the option.
        var input = Input("pi-menu");
        await input.FillAsync("@b");
        await Expect(Options("pi-menu")).ToHaveCountAsync(1);
        await input.PressAsync("Shift+Enter");
        await Expect(Text("pi-menu-value")).ToHaveTextAsync(Exactly("@b\\n"));
        await Expect(Menu("pi-menu")).ToHaveCountAsync(0);

        // Shift+Tab moves the focus back rather than taking the option, and the menu goes with the focus.
        await input.FillAsync("@b");
        await Expect(Options("pi-menu")).ToHaveCountAsync(1);
        await input.PressAsync("Shift+Tab");
        await Expect(input).Not.ToBeFocusedAsync();
        await Expect(Menu("pi-menu")).ToHaveCountAsync(0);
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Text("pi-menu-value")).ToHaveTextAsync(Exactly("@b"));

        // So does a click elsewhere on the page.
        await input.FillAsync("@bo");
        await Expect(Options("pi-menu")).ToHaveCountAsync(1);
        await Page.Locator("h1").First.ClickAsync();
        await Expect(Menu("pi-menu")).ToHaveCountAsync(0);
        await Expect(Text("pi-menu-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Escape_ClosesTheMenuFirst_AndTheDialogAroundTheComposerOnTheNextPress()
    {
        await OpenAsync(Route);

        await Page.Locator("#pi-open-dialog").ClickAsync();
        await Expect(Page.Locator(".bit-dlg-ctn")).ToBeVisibleAsync();

        // The dialog puts the focus on its first control a round trip after it opens; typed into before that, the
        // composer would lose the focus to it - and its menu with the focus - in the middle of the test.
        await Expect(Page.Locator(".bit-dlg-ctn :focus")).ToHaveCountAsync(1);

        var input = Input("pi-esc");
        await input.FillAsync("/");
        await Expect(Menu("pi-esc")).ToBeVisibleAsync();

        // The composer claims the Escape it acts on, so the dialog leaves this press alone.
        await Expect(input).ToHaveAttributeAsync("data-bit-esc", "claim");
        await input.PressAsync("Escape");

        await Expect(Menu("pi-esc")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#pi-dialog-open")).ToHaveTextAsync("True");
        await Expect(Page.Locator(".bit-dlg-ctn")).ToBeVisibleAsync();
        await Expect(input).ToBeFocusedAsync();
        await Expect(input).ToHaveValueAsync("/");

        // With nothing left for the composer to do, the next Escape is the dialog's.
        await Expect(input).Not.ToHaveAttributeAsync("data-bit-esc", new Regex(".*"));
        await input.PressAsync("Escape");

        await Expect(Page.Locator("#pi-dialog-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator(".bit-dlg-ctn")).ToHaveCountAsync(0);
    }

    // ---------- a turn in flight ----------

    [TestMethod]
    public async Task Session_ASubmission_StreamsTheReply_AndTheSendButtonBecomesAStopButton()
    {
        await OpenAsync(Route);

        var input = Input("pi-session");
        await input.FillAsync("hello");
        await Expect(Send("pi-session")).Not.ToHaveAttributeAsync("aria-disabled", "true");
        await input.PressAsync("Enter");

        await Expect(Text("pi-session-state")).ToHaveTextAsync("generating");
        await Expect(Text("pi-session-reply")).ToHaveTextAsync("Working on it");
        await Expect(Text("pi-session-messages")).ToHaveTextAsync("2");

        var stop = Send("pi-session");
        await Expect(stop).ToHaveAttributeAsync("aria-label", "Stop");
        await Expect(stop).ToHaveAttributeAsync("title", "Stop");
        await Expect(stop).ToHaveClassAsync(HasClass("bit-btn-otl"));
        await Expect(stop).Not.ToHaveAttributeAsync("aria-disabled", "true");
        await stop.Locator("i.bit-btn-icn").AssertAiGlyphAsync("StopSolid");

        // While the turn is in flight, Escape in the text is the composer's to act on.
        await Expect(input).ToHaveAttributeAsync("data-bit-esc", "claim");
        await Expect(input).ToHaveValueAsync("");

        await stop.ClickAsync();

        await Expect(Text("pi-session-state")).ToHaveTextAsync("idle");
        await Expect(Text("pi-session-status")).ToHaveTextAsync("Cancelled");
        await Expect(Send("pi-session")).ToHaveAttributeAsync("aria-label", "Send");
        await Expect(input).Not.ToHaveAttributeAsync("data-bit-esc", new Regex(".*"));
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Session_EscapeStopsTheTurnInFlight_AndClaimsThePress()
    {
        await OpenAsync(Route);
        await Page.EvaluateAsync("() => window.addEventListener('keydown', e => { if (e.key === 'Escape') window.__e2eEscapePrevented = e.defaultPrevented; })");

        var input = Input("pi-session");
        await input.FillAsync("hello");
        await input.PressAsync("Enter");
        await Expect(Text("pi-session-state")).ToHaveTextAsync("generating");
        await Expect(input).ToHaveAttributeAsync("data-bit-esc", "claim");

        // A menu open during the turn takes the first Escape; the turn goes on.
        await input.FillAsync("/");
        await Expect(Text("pi-session-state")).ToHaveTextAsync("generating");
        await input.PressAsync("Escape");
        await Expect(Menu("pi-session")).ToHaveCountAsync(0);
        await Expect(Text("pi-session-state")).ToHaveTextAsync("generating");

        await input.PressAsync("Escape");

        await Expect(Text("pi-session-state")).ToHaveTextAsync("idle");
        await Expect(Text("pi-session-status")).ToHaveTextAsync("Cancelled");
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__e2eEscapePrevented === true"), "The composer claimed the Escape that stopped the turn.");
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Session_StoppingWithTheButton_KeepsTheFocusOnIt_AsItTurnsBackIntoSend()
    {
        await OpenAsync(Route);

        var input = Input("pi-session");
        await input.FillAsync("hello");
        await input.PressAsync("Enter");
        await Expect(Send("pi-session")).ToHaveAttributeAsync("aria-label", "Stop");

        // The next question is typed while the reply streams in.
        await input.FillAsync("and then?");
        await Expect(input).ToHaveValueAsync("and then?");

        await Send("pi-session").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Text("pi-session-state")).ToHaveTextAsync("idle");
        await Expect(Send("pi-session")).ToHaveAttributeAsync("aria-label", "Send");
        await Expect(Send("pi-session")).ToBeFocusedAsync();
        await Send("pi-session").Locator("i.bit-btn-icn").AssertAiGlyphAsync("Send");

        // And sends the next one.
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("pi-session-state")).ToHaveTextAsync("generating");
        await Expect(Text("pi-session-messages")).ToHaveTextAsync("4");
        await Send("pi-session").ClickAsync();
        await Expect(Text("pi-session-state")).ToHaveTextAsync("idle");
    }

    [TestMethod]
    public async Task Generating_TheStopButtonAndEscape_RaiseOnStop()
    {
        await OpenAsync(Route);

        var input = Input("pi-gen");
        await input.FillAsync("go");
        await input.PressAsync("Enter");
        await Expect(Text("pi-gen-state")).ToHaveTextAsync("True");
        await Expect(Send("pi-gen")).ToHaveAttributeAsync("aria-label", "Stop");

        await Send("pi-gen").ClickAsync();
        await Expect(Text("pi-gen-stops")).ToHaveTextAsync("1");
        await Expect(Text("pi-gen-state")).ToHaveTextAsync("False");
        await Expect(Send("pi-gen")).ToHaveAttributeAsync("aria-label", "Send");

        await input.FillAsync("again");
        await input.PressAsync("Enter");
        await Expect(Text("pi-gen-state")).ToHaveTextAsync("True");
        await Expect(input).ToHaveAttributeAsync("data-bit-esc", "claim");

        await input.PressAsync("Escape");
        await Expect(Text("pi-gen-stops")).ToHaveTextAsync("2");
        await Expect(Text("pi-gen-state")).ToHaveTextAsync("False");
    }

    // ---------- attachments ----------

    [TestMethod]
    public async Task Paste_AnImage_StagesAChipWithAThumbnailMadeInTheBrowser_AndTheSubmissionCarriesIt()
    {
        await OpenAsync(Route);

        var kept = await Input("pi-att").DispatchPasteAsync(null, new E2EFile("shot.png", "image/png", Bytes: TinyPng));
        if (kept is null) Assert.Inconclusive("This engine does not let a page build a paste event carrying files.");
        Assert.IsFalse(kept.Value, "A paste of a file is the composer's, so the browser does not paste it as text.");

        await Expect(Chips("pi-att")).ToHaveCountAsync(1);
        await Expect(Chips("pi-att").Locator(".bit-tag-tex")).ToHaveTextAsync("shot.png");
        await Expect(Chips("pi-att").Locator(".bit-tag-stx")).ToHaveTextAsync($"{TinyPng.Length} B");

        // The thumbnail is an object URL of the file, drawn without the bytes ever leaving the browser.
        var thumbnail = Chips("pi-att").Locator("img.bit-aiach-thm");
        await Expect(thumbnail).ToHaveAttributeAsync("src", new Regex("^blob:"));
        await thumbnail.ScrollIntoViewIfNeededAsync();
        await Page.WaitForFunctionAsync("() => { const i = document.querySelector('#pi-att img.bit-aiach-thm'); return i && i.complete && i.naturalWidth === 1; }");

        // A file alone is enough to send.
        await Expect(Send("pi-att")).Not.ToHaveAttributeAsync("aria-disabled", "true");
        await Input("pi-att").FillAsync("see attached");
        await Input("pi-att").PressAsync("Enter");

        await Expect(Text("pi-att-text")).ToHaveTextAsync("see attached");
        await Expect(Text("pi-att-sent")).ToHaveTextAsync($"shot.png:{TinyPng.Length}:Image:inline");
        await Expect(Chips("pi-att")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Paste_OfTextAlone_IsLeftToTheBrowser()
    {
        await OpenAsync(Route);

        var kept = await Input("pi-att").DispatchPasteAsync("just words");
        if (kept is null) Assert.Inconclusive("This engine does not let a page build a paste event carrying data.");

        Assert.IsTrue(kept.Value, "A paste of text alone is not prevented.");

        // A real file afterwards is the first and only chip.
        if (await Input("pi-att").DispatchPasteAsync(null, new E2EFile("after.png", "image/png", Bytes: TinyPng)) is null)
        {
            Assert.Inconclusive("This engine does not let a page build a paste event carrying files; the paste of text was left alone.");
        }

        await Expect(Chips("pi-att")).ToHaveCountAsync(1);
        await Expect(Chips("pi-att").Locator(".bit-tag-tex")).ToHaveTextAsync("after.png");
    }

    [TestMethod]
    public async Task Drop_FilesDraggedOver_ShowTheOverlay_AndDroppedOnesAreStaged()
    {
        await OpenAsync(Route);

        var root = Root("pi-att");
        var input = Input("pi-att");
        var overlay = Page.Locator("#pi-att .bit-aipri-dro");

        // A drag of text is not one of files: no overlay.
        Assert.IsTrue(await input.EvaluateAsync<bool>(
            """
            el => {
                const dt = new DataTransfer();
                dt.setData('text/plain', 'words');
                return el.dispatchEvent(new DragEvent('dragenter', { dataTransfer: dt, bubbles: true, cancelable: true }));
            }
            """), "A drag of text is left alone.");

        Assert.IsFalse(await input.DispatchFileDragAsync("dragenter", new E2EFile("a.pdf", "application/pdf", 10)), "A drag of files is taken.");
        Assert.IsFalse(await input.DispatchFileDragAsync("dragover", new E2EFile("a.pdf", "application/pdf", 10)), "The drag over the composer is a drop target.");

        await Expect(overlay).ToBeVisibleAsync();
        await Expect(overlay).ToHaveTextAsync("Drop files to attach");
        await Expect(root).ToHaveClassAsync(HasClass("bit-aipri-dnd"));
        await Expect(root).ToHaveCSSAsync("border-top-style", "dashed");
        await Expect(root).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-pri)"));
        await Expect(overlay).ToHaveCSSAsync("pointer-events", "none");

        Assert.IsFalse(await input.DispatchFileDragAsync("drop",
                                                         new E2EFile("a.pdf", "application/pdf", 10),
                                                         new E2EFile("b.png", "image/png", Bytes: TinyPng)), "The drop is the composer's.");

        await Expect(overlay).ToHaveCountAsync(0);
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-aipri-dnd"));
        await Expect(Chips("pi-att")).ToHaveCountAsync(2);
        await Expect(Chips("pi-att").Locator(".bit-tag-tex")).ToHaveTextAsync(["a.pdf", "b.png"]);
        await Chips("pi-att").Nth(0).Locator("i.bit-aiach-ico").AssertAiGlyphAsync("Page");
        await Expect(Chips("pi-att").Nth(1).Locator("img.bit-aiach-thm")).ToHaveAttributeAsync("src", new Regex("^blob:"));
    }

    [TestMethod]
    public async Task Drop_ADragLeavingTheComposer_TakesTheOverlayAway()
    {
        await OpenAsync(Route);

        var input = Input("pi-att");
        var overlay = Page.Locator("#pi-att .bit-aipri-dro");

        await input.DispatchFileDragAsync("dragenter", new E2EFile("a.pdf", "application/pdf"));
        await Expect(overlay).ToBeVisibleAsync();

        await input.DispatchFileDragAsync("dragleave", new E2EFile("a.pdf", "application/pdf"));
        await Expect(overlay).ToHaveCountAsync(0);
        await Expect(Root("pi-att")).Not.ToHaveClassAsync(HasClass("bit-aipri-dnd"));

        // A drag abandoned outside the window ends with a dragend there, which takes it away too.
        await input.DispatchFileDragAsync("dragenter", new E2EFile("a.pdf", "application/pdf"));
        await Expect(overlay).ToBeVisibleAsync();
        await Page.EvaluateAsync("() => window.dispatchEvent(new DragEvent('dragend'))");
        await Expect(overlay).ToHaveCountAsync(0);
        await Expect(Chips("pi-att")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Attachments_RemovingAChip_UnstagesIt_AndTheSubmissionCarriesTheRest()
    {
        await OpenAsync(Route);

        await Input("pi-att").DispatchFileDragAsync("drop",
                                                    new E2EFile("keep.pdf", "application/pdf", 300 * 1024),
                                                    new E2EFile("drop.txt", "text/plain", 4));
        await Expect(Chips("pi-att")).ToHaveCountAsync(2);

        var remove = Page.GetByRole(AriaRole.Button, new() { Name = "Remove drop.txt", Exact = true });
        await Expect(remove).ToBeVisibleAsync();
        await remove.ClickAsync();

        await Expect(Chips("pi-att")).ToHaveCountAsync(1);
        await Expect(Chips("pi-att").Locator(".bit-tag-tex")).ToHaveTextAsync("keep.pdf");
        await Expect(Chips("pi-att").Locator(".bit-tag-stx")).ToHaveTextAsync("300 KB");

        await Send("pi-att").ClickAsync();

        // A file past the inline size stays in the browser and is read when something opens it.
        await Expect(Text("pi-att-sent")).ToHaveTextAsync($"keep.pdf:{300 * 1024}:Document:lazy");
        await Expect(Chips("pi-att")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Attachments_TheAttachButtonOpensThePicker_AndThePickedFilesAreStaged()
    {
        await OpenAsync(Route);

        var chooser = await Page.RunAndWaitForFileChooserAsync(() => Attach("pi-att").ClickAsync());
        Assert.IsTrue(chooser.IsMultiple, "The picker takes several files at once.");

        await chooser.SetFilesAsync(
        [
            new FilePayload { Name = "notes.txt", MimeType = "text/plain", Buffer = "hello"u8.ToArray() },
            new FilePayload { Name = "pixel.png", MimeType = "image/png", Buffer = TinyPng },
        ]);

        await Expect(Chips("pi-att")).ToHaveCountAsync(2);
        await Expect(Chips("pi-att").Locator(".bit-tag-tex")).ToHaveTextAsync(["notes.txt", "pixel.png"]);
        await Expect(Chips("pi-att").Nth(1).Locator("img.bit-aiach-thm")).ToHaveAttributeAsync("src", new Regex("^blob:"));

        await Send("pi-att").ClickAsync();
        await Expect(Text("pi-att-sent")).ToHaveTextAsync($"notes.txt:5:File:inline,pixel.png:{TinyPng.Length}:Image:inline");
    }

    [TestMethod]
    public async Task Limits_AFileOfATypeNotAccepted_IsNamedInAnAlert_AndReported()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#pi-lim input.bit-aipri-fil")).ToHaveAttributeAsync("accept", "image/*,.pdf");

        await Input("pi-lim").DispatchFileDragAsync("drop", new E2EFile("notes.txt", "text/plain", 10), new E2EFile("ok.pdf", "application/pdf", 10));

        var alert = Notice("pi-lim").Locator("[role=alert]");
        await Expect(alert).ToContainTextAsync("notes.txt is not a supported file type.");
        await Expect(Text("pi-lim-rejected")).ToHaveTextAsync("notes.txt:TypeNotAllowed");
        await Expect(Chips("pi-lim")).ToHaveCountAsync(1);
        await Expect(Chips("pi-lim").Locator(".bit-tag-tex")).ToHaveTextAsync("ok.pdf");

        // The picker goes through the same check, whatever the file it was handed.
        await Page.Locator("#pi-lim input.bit-aipri-fil").SetInputFilesAsync(new FilePayload { Name = "data.csv", MimeType = "text/csv", Buffer = "a,b"u8.ToArray() });
        await Expect(Text("pi-lim-rejected")).ToHaveTextAsync("notes.txt:TypeNotAllowed;data.csv:TypeNotAllowed");
        await Expect(alert).ToContainTextAsync("data.csv is not a supported file type.");
        await Expect(Chips("pi-lim")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Limits_AFileOverTheSizeLimit_IsTurnedAway()
    {
        await OpenAsync(Route);

        await Input("pi-lim").DispatchFileDragAsync("drop", new E2EFile("big.png", "image/png", 2048), new E2EFile("small.png", "image/png", Bytes: TinyPng));

        await Expect(Notice("pi-lim").Locator("[role=alert]")).ToContainTextAsync("big.png is too large. Files can be up to 1 KB.");
        await Expect(Text("pi-lim-rejected")).ToHaveTextAsync("big.png:TooLarge");
        await Expect(Chips("pi-lim").Locator(".bit-tag-tex")).ToHaveTextAsync("small.png");
    }

    [TestMethod]
    public async Task Limits_MoreFilesThanAllowed_TurnsAwayTheRest_AndTheNoticeIsDismissed()
    {
        await OpenAsync(Route);

        await Input("pi-lim").DispatchFileDragAsync("drop",
                                                    new E2EFile("a.png", "image/png", 10),
                                                    new E2EFile("b.png", "image/png", 10),
                                                    new E2EFile("c.png", "image/png", 10));

        await Expect(Chips("pi-lim")).ToHaveCountAsync(2);
        await Expect(Notice("pi-lim").Locator("[role=alert]")).ToContainTextAsync("c.png was not attached. Up to 2 files can be attached at once.");
        await Expect(Text("pi-lim-rejected")).ToHaveTextAsync("c.png:TooMany");

        var dismiss = Notice("pi-lim").GetByRole(AriaRole.Button, new() { Name = "Dismiss" });
        await dismiss.ClickAsync();
        await Expect(Notice("pi-lim")).ToHaveCountAsync(0);
        await Expect(Chips("pi-lim")).ToHaveCountAsync(2);

        // The notice keeps the latest three, so a drop of many does not outgrow the composer.
        await Input("pi-lim").DispatchFileDragAsync("drop",
                                                    new E2EFile("w.txt", "text/plain"),
                                                    new E2EFile("x.txt", "text/plain"),
                                                    new E2EFile("y.txt", "text/plain"),
                                                    new E2EFile("z.txt", "text/plain"));
        await Expect(Text("pi-lim-rejected")).ToHaveTextAsync("c.png:TooMany;w.txt:TooMany;x.txt:TooMany;y.txt:TooMany;z.txt:TooMany");
        await Expect(Notice("pi-lim").Locator("[role=alert]")).ToContainTextAsync("x.txt");
        await Expect(Notice("pi-lim").Locator("[role=alert]")).ToContainTextAsync("y.txt");
        await Expect(Notice("pi-lim").Locator("[role=alert]")).ToContainTextAsync("z.txt");
        await Expect(Notice("pi-lim").Locator("[role=alert]")).Not.ToContainTextAsync("w.txt");
    }

    [TestMethod]
    public async Task NoAttachments_NoAttachButton_AndADropIsLeftToThePage()
    {
        await OpenAsync(Route);

        await Expect(Attach("pi-noatt")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#pi-noatt input[type=file]")).ToHaveCountAsync(0);

        // A model that takes no files hides them; one with vision takes images.
        await Expect(Attach("pi-textonly")).ToHaveCountAsync(0);
        await Expect(Attach("pi-vision")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#pi-vision input.bit-aipri-fil")).ToHaveAttributeAsync("accept", "image/*");

        Assert.IsTrue(await Input("pi-noatt").DispatchFileDragAsync("dragenter", new E2EFile("a.png", "image/png")), "The drag is not taken.");
        Assert.IsTrue(await Input("pi-noatt").DispatchFileDragAsync("drop", new E2EFile("a.png", "image/png")), "The drop is not taken.");
        var kept = await Input("pi-noatt").DispatchPasteAsync(null, new E2EFile("b.png", "image/png"));
        if (kept is not null) Assert.IsTrue(kept.Value, "The paste is not taken.");

        // A drop on the composer with attachments beside it is answered, and this one still has nothing.
        await Input("pi-vision").DispatchFileDragAsync("drop", new E2EFile("v.png", "image/png", Bytes: TinyPng));
        await Expect(Chips("pi-vision")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#pi-noatt .bit-aiach")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#pi-noatt .bit-aipri-dro")).ToHaveCountAsync(0);
        await Expect(Text("pi-noatt-rejected")).ToHaveTextAsync("");
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_TheTextRefusesInput_AndDropsAndPastesStageNothing()
    {
        await OpenAsync(Route);

        var root = Root("pi-dis");
        var input = Input("pi-dis");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(input).ToBeDisabledAsync();
        await Expect(input).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(input).ToHaveValueAsync("Locked text");
        await Expect(Send("pi-dis")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Attach("pi-dis")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(input).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-dis)"));
        await Expect(root).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-dis)"));

        // A drag of files is refused - the pointer says nothing can be dropped - and a drop is swallowed rather than left
        // to the browser, which would open the file in place of the page.
        var refused = await Input("pi-dis").EvaluateAsync<string[]>(
            """
            el => {
                const dt = new DataTransfer();
                dt.items.add(new File([new Uint8Array(4)], 'a.png', { type: 'image/png' }));
                dt.dropEffect = 'copy';
                const kept = el.dispatchEvent(new DragEvent('dragover', { dataTransfer: dt, bubbles: true, cancelable: true }));
                return [String(kept), dt.dropEffect];
            }
            """);
        Assert.AreEqual("false", refused[0], "The drag over a disabled composer is taken from the browser.");
        Assert.AreEqual("none", refused[1], "The drag over a disabled composer drops nothing.");

        Assert.IsFalse(await Input("pi-dis").DispatchFileDragAsync("dragenter", new E2EFile("a.png", "image/png", Bytes: TinyPng)));
        Assert.IsFalse(await Input("pi-dis").DispatchFileDragAsync("drop", new E2EFile("a.png", "image/png", Bytes: TinyPng)), "The drop is swallowed.");
        await Root("pi-dis").DispatchFileDragAsync("drop", new E2EFile("b.pdf", "application/pdf", 10));
        await Input("pi-dis").DispatchPasteAsync(null, new E2EFile("c.png", "image/png", Bytes: TinyPng));
        await Expect(Page.Locator("#pi-dis .bit-aipri-dro")).ToHaveCountAsync(0);
        await Expect(Root("pi-dis")).Not.ToHaveClassAsync(HasClass("bit-aipri-dnd"));
        await Send("pi-dis").ClickAsync(new() { Force = true });

        // Enabling it is what lets the same drop through - and proves the ones before it were answered.
        await Page.Locator("#pi-dis-toggle").ClickAsync();
        await Expect(Text("pi-dis-state")).ToHaveTextAsync("False");
        await Expect(input).ToBeEnabledAsync();
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await WaitForFilesAcceptedAsync("pi-dis");
        await Input("pi-dis").DispatchFileDragAsync("drop", new E2EFile("after.pdf", "application/pdf", 10));
        await Expect(Chips("pi-dis")).ToHaveCountAsync(1);
        await Expect(Chips("pi-dis").Locator(".bit-tag-tex")).ToHaveTextAsync("after.pdf");
        await Expect(Text("pi-dis-rejected")).ToHaveTextAsync("");
        await Expect(Text("pi-dis-count")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#pi-dis .bit-aipri-dro")).ToHaveCountAsync(0);

        await input.PressAsync("End");
        await input.PressAsync("Enter");
        await Expect(Text("pi-dis-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Disabled_StagedChipsCannotBeRemoved_WhileItIs()
    {
        await OpenAsync(Route);

        await Page.Locator("#pi-dis-toggle").ClickAsync();
        await Expect(Input("pi-dis")).ToBeEnabledAsync();
        await WaitForFilesAcceptedAsync("pi-dis");
        await Input("pi-dis").DispatchFileDragAsync("drop", new E2EFile("staged.pdf", "application/pdf", 10));
        await Expect(Chips("pi-dis")).ToHaveCountAsync(1);

        await Page.Locator("#pi-dis-toggle").ClickAsync();
        await Expect(Input("pi-dis")).ToBeDisabledAsync();

        var remove = Chips("pi-dis").Locator("button.bit-tag-cls");
        await Expect(remove).ToBeDisabledAsync();
        await remove.EvaluateAsync("b => b.click()");
        await Page.Locator("#pi-dis-toggle").ClickAsync();
        await Expect(Input("pi-dis")).ToBeEnabledAsync();
        await Expect(Chips("pi-dis")).ToHaveCountAsync(1);
    }

    // ---------- MaxLength ----------

    [TestMethod]
    public async Task MaxLength_TheCounterCounts_WarnsCloseToTheLimit_AndIsAnnouncedThere()
    {
        await OpenAsync(Route);

        var input = Input("pi-max");
        var counter = Page.Locator("#pi-max-ctr");
        var live = Page.Locator("#pi-max .bit-aipri-sts");

        await Expect(counter).ToHaveTextAsync("0 / 20");
        await Expect(counter).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(input).ToHaveAttributeAsync("maxlength", "20");
        await Expect(input).ToHaveAttributeAsync("aria-describedby", "pi-max-ctr");
        await Expect(live).ToHaveAttributeAsync("role", "status");
        await Expect(live).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(live).ToHaveTextAsync("");

        await input.FillAsync("123456789");
        await Expect(counter).ToHaveTextAsync("9 / 20");
        await Expect(counter).Not.ToHaveClassAsync(HasClass("bit-aipri-ctn"));
        await Expect(live).ToHaveTextAsync("");

        await input.FillAsync("123456789012");
        await Expect(counter).ToHaveTextAsync("12 / 20");
        await Expect(counter).ToHaveClassAsync(HasClass("bit-aipri-ctn"));
        await Expect(counter).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-wrn)"));
        await Expect(live).ToHaveTextAsync("8 characters remaining");

        // The browser holds the text at the limit.
        await input.FillAsync("12345678901234567890");
        await Expect(counter).ToHaveTextAsync("20 / 20");
        await input.PressAsync("End");
        await input.PressAsync("x");
        await Expect(input).ToHaveValueAsync("12345678901234567890");
        await Expect(live).ToHaveTextAsync("0 characters remaining");
    }

    [TestMethod]
    public async Task MaxLength_ATextPastTheLimit_IsFlaggedAndCannotBeSent()
    {
        await OpenAsync(Route);

        await Page.Locator("#pi-max-overfill").ClickAsync();

        var counter = Page.Locator("#pi-max-ctr");
        await Expect(counter).ToHaveTextAsync("25 / 20");
        await Expect(counter).ToHaveClassAsync(HasClass("bit-aipri-cto"));
        await Expect(counter).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-err)"));
        await Expect(Send("pi-max")).ToHaveAttributeAsync("aria-disabled", "true");

        await Input("pi-max").FocusAsync();
        await Input("pi-max").PressAsync("Enter");
        await Input("pi-max").FillAsync("short");
        await Expect(counter).ToHaveTextAsync("5 / 20");
        await Input("pi-max").PressAsync("Enter");
        await Expect(Text("pi-max-count")).ToHaveTextAsync("1");
    }

    // ---------- drafts ----------

    [TestMethod]
    public async Task Drafts_AReloadRestoresTheDraft_AndSendingItClearsIt()
    {
        await OpenAsync(Route);

        var input = Input("pi-draft");
        await input.FillAsync("half-written thought");
        await Expect(Text("pi-draft-value")).ToHaveTextAsync("half-written thought");
        await Page.WaitForFunctionAsync("() => sessionStorage.getItem('bit-ai-draft:e2e-pi-draft') === 'half-written thought'");

        await Page.ReloadAsync();
        await WaitForStatusAsync("Ready");

        await Expect(input).ToHaveValueAsync("half-written thought");
        await Expect(Text("pi-draft-value")).ToHaveTextAsync("half-written thought");

        await input.PressAsync("End");
        await input.PressAsync("Enter");
        await Expect(Text("pi-draft-count")).ToHaveTextAsync("1");
        await Page.WaitForFunctionAsync("() => sessionStorage.getItem('bit-ai-draft:e2e-pi-draft') === null");

        await Page.ReloadAsync();
        await WaitForStatusAsync("Ready");
        await Expect(input).ToHaveValueAsync("");

        // An emptied composer takes its draft away too.
        await input.FillAsync("again");
        await Page.WaitForFunctionAsync("() => sessionStorage.getItem('bit-ai-draft:e2e-pi-draft') === 'again'");
        await input.FillAsync("");
        await Page.WaitForFunctionAsync("() => sessionStorage.getItem('bit-ai-draft:e2e-pi-draft') === null");
    }

    // ---------- dictation ----------

    [TestMethod]
    public async Task Dictation_AFinalPhrase_IsAppendedToTheText_AndTheProvisionalOneShownBelowIt()
    {
        await Page.AddInitScriptAsync(AiE2EHelpers.FakeSpeechRecognitionScript);
        await OpenAsync(Route);

        // A model without audio offers no dictation.
        await Expect(Page.Locator("#pi-dict-off .bit-aivci")).ToHaveCountAsync(0);

        var mic = Page.Locator("#pi-dict .bit-aivci-btn");
        await Expect(mic).ToBeVisibleAsync();
        await mic.Locator("i.bit-tgb-ico").AssertAiGlyphAsync("Microphone");
        await mic.ClickAsync();
        await Expect(mic).ToHaveAttributeAsync("aria-label", "Stop dictation");
        Assert.AreEqual("en-GB", await Page.EvaluateAsync<string>("() => window.__e2eSpeech.current.lang"));

        await Page.EvaluateAsync("() => window.__e2eSpeech.result('hello world', null)");
        await Expect(Text("pi-dict-value")).ToHaveTextAsync("hello world");
        await Expect(Input("pi-dict")).ToHaveValueAsync("hello world");

        await Page.EvaluateAsync("() => window.__e2eSpeech.result(null, 'and more')");
        var interim = Page.Locator("#pi-dict .bit-aipri-itr");
        await Expect(interim).ToHaveTextAsync("and more");
        await Expect(interim).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Input("pi-dict")).ToHaveValueAsync("hello world");

        await Page.EvaluateAsync("() => window.__e2eSpeech.result('and more', null)");
        await Expect(Input("pi-dict")).ToHaveValueAsync("hello world and more");
        await Expect(interim).ToHaveCountAsync(0);

        await mic.ClickAsync();
        await Expect(mic).ToHaveAttributeAsync("aria-label", "Dictate");
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__e2eSpeech.current.stopped"));
    }

    // ---------- the glyph font ----------

    [TestMethod]
    public async Task Glyphs_TheAttachAndSendIcons_AreDrawnFromTheAiFont()
    {
        await OpenAsync(Route);

        await Attach("pi-basic").Locator("i.bit-btn-icn").AssertAiGlyphAsync("Attach");
        await Send("pi-basic").Locator("i.bit-btn-icn").AssertAiGlyphAsync("Send");

        Assert.IsTrue(await Page.AiGlyphFontLoadsAsync(""), "The AI font is fetched and draws the Send glyph.");

        await Input("pi-gen").FillAsync("go");
        await Input("pi-gen").PressAsync("Enter");
        await Send("pi-gen").Locator("i.bit-btn-icn").AssertAiGlyphAsync("StopSolid");
        await Expect(Send("pi-gen").Locator("i.bit-btn-icn")).Not.ToHaveClassAsync(HasClass("bit-icon-ai--Send"));
    }

    // ---------- styles ----------

    [TestMethod]
    public async Task Style_TheThemeTokensPaintTheFrame_AndTheFocusRingFollowsTheFocusIntoIt()
    {
        await OpenAsync(Route);

        var root = Root("pi-basic");
        await Expect(root).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-pri)"));
        await Expect(root).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-brd-pri)"));
        await Expect(root).ToHaveCSSAsync("border-top-style", "solid");
        await Expect(root).ToHaveCSSAsync("display", "flex");
        await Expect(root).ToHaveCSSAsync("flex-direction", "column");
        await Expect(Input("pi-basic")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-pri)"));
        await Expect(Input("pi-basic")).ToHaveCSSAsync("resize", "none");
        await Expect(Input("pi-basic")).ToHaveCSSAsync("border-top-style", "none");

        await Expect(root).ToHaveCSSAsync("box-shadow", "none");
        await Input("pi-basic").FocusAsync();
        await Expect(root).Not.ToHaveCSSAsync("box-shadow", "none");
        StringAssert.Contains(await ComputedStyleAsync("#pi-basic", "box-shadow"), "rgb", "The frame draws a ring while the focus is in it.");

        // A shadow of its own stays under the ring.
        await Input("pi-style").FocusAsync();
        await Expect(Root("pi-style")).ToHaveCSSAsync("box-shadow", new Regex(@"^rgb.*, rgb\(10, 11, 12\) 0px 0px 0px 2px$"));

        // The text has no outline of its own: the composer is the field.
        await Expect(Input("pi-basic")).ToHaveCSSAsync("outline-style", "none");
    }

    [TestMethod]
    public async Task Style_ThePublicVariables_RestyleTheComposer_OnTheInstanceOrAnAncestor()
    {
        await OpenAsync(Route);

        var root = Root("pi-style");
        await Expect(root).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(root).ToHaveCSSAsync("border-top-color", "rgb(4, 5, 6)");
        await Expect(root).ToHaveCSSAsync("border-top-width", "3px");
        await Expect(root).ToHaveCSSAsync("border-top-left-radius", "7px");
        await Expect(root).ToHaveCSSAsync("padding-top", "11px");
        await Expect(root).ToHaveCSSAsync("box-shadow", "rgb(10, 11, 12) 0px 0px 0px 2px");
        await Expect(Input("pi-style")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(Input("pi-style")).ToHaveCSSAsync("font-size", "19px");
        await Expect(Input("pi-style")).ToHaveCSSAsync("line-height", "28.5px");

        await Expect(Root("pi-style-ancestor")).ToHaveCSSAsync("background-color", "rgb(21, 22, 23)");
        await Expect(Root("pi-style-ancestor-dis")).ToHaveCSSAsync("background-color", "rgb(24, 25, 26)");

        await Expect(Root("pi-style-parts")).ToHaveClassAsync(HasClass("pi-custom-root"));
        await Expect(Input("pi-style-parts")).ToHaveClassAsync(HasClass("pi-custom-input"));
        await Expect(Input("pi-style-parts")).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(Page.Locator("#pi-style-parts .bit-aipri-bdy")).ToHaveCSSAsync("column-gap", "13px");
    }

    // ---------- right to left ----------

    [TestMethod]
    public async Task Rtl_TheToolsLeadOnTheLeft_AndTheSendArrowIsMirrored_ExplicitOrInherited()
    {
        await OpenAsync(Route);

        await Expect(Root("pi-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Root("pi-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));

        foreach (var id in new[] { "pi-rtl", "pi-rtl-inherited" })
        {
            await Expect(Send(id).Locator("i.bit-btn-icn")).ToHaveCSSAsync("transform", "matrix(-1, 0, 0, 1, 0, 0)");

            var input = await Input(id).RectAsync();
            var send = await Send(id).RectAsync();
            Assert.IsTrue(send.Right <= input.Left + 1, $"#{id}: the send button is on the left of the text.");
            await Expect(Input(id)).ToHaveCSSAsync("direction", "rtl");
        }

        await Expect(Send("pi-ltr").Locator("i.bit-btn-icn")).ToHaveCSSAsync("transform", "none");
        var ltrInput = await Input("pi-ltr").RectAsync();
        var ltrSend = await Send("pi-ltr").RectAsync();
        Assert.IsTrue(ltrSend.Left >= ltrInput.Right - 1, "The send button is on the right of the text left to right.");

        // The menu opens from the inline start: the right edge, right to left.
        await Input("pi-rtl-menu").FillAsync("/");
        await Expect(Menu("pi-rtl-menu")).ToBeVisibleAsync();
        var menu = await Menu("pi-rtl-menu").RectAsync();
        var root = await Root("pi-rtl-menu").RectAsync();
        Assert.AreEqual(root.Right, menu.Right, 1.5, "The menu is aligned with the right edge of the composer.");
    }

    // ---------- helpers ----------

    // The listeners of a composer are registered again after the render that enabled it, in a round trip of their own:
    // waits until a drag of files over it raises the overlay rather than being refused, then ends that drag.
    private async Task WaitForFilesAcceptedAsync(string id)
    {
        var overlay = Page.Locator($"#{id} .bit-aipri-dro");

        for (var attempt = 0; ; attempt++)
        {
            await Input(id).DispatchFileDragAsync("dragenter", new E2EFile("probe.bin", "application/octet-stream"));

            try
            {
                await Expect(overlay).ToBeVisibleAsync(new() { Timeout = 2000 });
                break;
            }
            catch (PlaywrightException) when (attempt < 10)
            {
                // still refused: the registration of the enabled composer has not landed yet
            }
        }

        await Page.EvaluateAsync("() => window.dispatchEvent(new DragEvent('dragend'))");
        await Expect(overlay).ToHaveCountAsync(0);
    }

    // Waits for the height the script gave the text to stop changing after the first render.
    private async Task WaitForHeightToSettleAsync(ILocator input)
    {
        await NextFramesAsync();
        await input.EvaluateAsync("t => new Promise(r => requestAnimationFrame(() => r()))");
    }

    private static Task WaitForHeightAsync(ILocator input, double expected) =>
        input.EvaluateAsync(
            """
            (t, expected) => new Promise((resolve, reject) => {
                const started = performance.now();
                const check = () => {
                    const h = t.getBoundingClientRect().height;
                    if (Math.abs(h - expected) <= 1.5) return resolve();
                    if (performance.now() - started > 20000) return reject(new Error(`The height is ${h}, not ${expected}.`));
                    requestAnimationFrame(check);
                };
                check();
            })
            """, expected);

    private static Task WaitForCaretAsync(ILocator input, int position) =>
        input.EvaluateAsync(
            """
            (t, position) => new Promise((resolve, reject) => {
                const started = performance.now();
                const check = () => {
                    if (document.activeElement === t && t.selectionStart === position && t.selectionEnd === position) return resolve();
                    if (performance.now() - started > 20000) return reject(new Error(`The caret is at ${t.selectionStart}, not ${position}.`));
                    requestAnimationFrame(check);
                };
                check();
            })
            """, position);

    // Counts the reads of the caret the composer asks the browser for, which it does once the text holds a trigger: a
    // menu the read would open is on the page once the read has answered.
    private Task CountCaretReadsAsync() =>
        Page.EvaluateAsync(
            """
            () => {
                const api = BitBlazorUI.AiPromptInput;
                const read = api.getCaret;
                window.__e2eCaretReads = 0;
                api.getCaret = function (...args) {
                    const result = read.apply(this, args);
                    window.__e2eCaretReads++;
                    return result;
                };
            }
            """);

    private async Task WaitForCaretReadsAsync(int count)
    {
        await Page.WaitForFunctionAsync("count => window.__e2eCaretReads >= count", count);
        await NextFramesAsync();
    }
}
