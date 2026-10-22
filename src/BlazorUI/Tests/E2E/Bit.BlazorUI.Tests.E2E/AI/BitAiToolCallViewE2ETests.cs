using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiToolCallView at /e2e/aitoolcallview in a real browser: one call moved through its life a step at a time
/// (arguments streaming in, pending, running with a progress bar, completed or failed), a call driven off the renderer's
/// thread that only its cards follow, the cards of a session playing the tool, tool-approval and tool-parallel scripts
/// (the turn going on after Allow, Decline and Always allow, inside a form that is never submitted), a renderer
/// registered for a tool, ended calls (failed, declined, cancelled), the header by keyboard, Disabled and the BitParams
/// cascade, the theme tokens and every --bit-AiToolCallView-* variable (a status outline winning over the one at rest),
/// Styles and Classes, RTL, reduced motion and the glyphs of the header.
/// </summary>
[TestClass]
public class BitAiToolCallViewE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aitoolcallview";

    private ILocator Card(string id) => Page.Locator($"#{id}");

    private ILocator Header(string id) => Page.Locator($"#{id} > .bit-aitcv-hdr");

    private ILocator Title(string id) => Page.Locator($"#{id} > .bit-aitcv-hdr .bit-aitcv-ttl");

    private ILocator Body(string id) => Page.Locator($"#{id}-bdy");

    // ---------- lifecycle ----------

    [TestMethod]
    public async Task Lifecycle_ACallStreamsRunsAndCompletes_TheCardFollowsEachStep()
    {
        await OpenAsync(Route);

        await Page.Locator("#lc-start").ClickAsync();

        // Streaming: the call is being written, which the card says it is calling, busy dots and all - collapsed, as
        // every card starts.
        await Expect(Title("tcv-lc")).ToHaveTextAsync("Calling Index the docs");
        await Expect(Card("tcv-lc")).ToHaveClassAsync(HasClass("bit-aitcv-run"));
        var dots = Page.Locator("#tcv-lc .bit-aitcv-sts .bit-aitcv-spn");
        await Expect(dots).ToBeVisibleAsync();
        await Expect(dots).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Header("tcv-lc")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Body("tcv-lc")).ToHaveCountAsync(0);
        await Expect(Body("tcv-lc-open").Locator(".bit-aitcv-shd")).ToHaveTextAsync(["Arguments"]);

        // The arguments show as they stream in.
        await Page.Locator("#lc-append").ClickAsync();
        await Page.Locator("#lc-append").ClickAsync();
        await Expect(Body("tcv-lc-open")).ToContainTextAsync("docs/");
        await Expect(Body("tcv-lc-open")).Not.ToContainTextAsync("recursive");

        await Page.Locator("#lc-append").ClickAsync();
        await Page.Locator("#lc-complete").ClickAsync();
        await Expect(Body("tcv-lc-open")).ToContainTextAsync("recursive");
        await Expect(Body("tcv-lc-open").Locator(".bit-aijsv")).ToContainTextAsync("folder");

        // Pending: the arguments are whole and nothing runs yet, so there is nothing to be busy about.
        await Expect(Card("tcv-lc")).Not.ToHaveClassAsync(HasClass("bit-aitcv-run"));
        await Expect(dots).ToHaveCountAsync(0);
        await Expect(Title("tcv-lc")).ToHaveTextAsync("Calling Index the docs");

        await Page.Locator("#lc-run").ClickAsync();
        await Expect(Card("tcv-lc")).ToHaveClassAsync(HasClass("bit-aitcv-run"));
        await Expect(dots).ToBeVisibleAsync();

        // The progress the tool reports is a bar named by its message, which also leads the open body.
        await Page.Locator("#lc-progress").ClickAsync();
        var bar = Page.Locator("#tcv-lc .bit-aitcv-prg [role=progressbar]");
        await Expect(bar).ToHaveAttributeAsync("aria-valuenow", "40");
        await Expect(bar).ToHaveAttributeAsync("aria-label", "Reading 12 of 30 files");
        await Expect(Body("tcv-lc-open").Locator(".bit-aitcv-pms")).ToHaveTextAsync("Reading 12 of 30 files");

        // A progress the tool cannot measure has no value to announce.
        await Page.Locator("#lc-progress-unknown").ClickAsync();
        await Expect(bar).ToHaveAttributeAsync("aria-label", "Listing the files");
        await Expect(bar).Not.ToHaveAttributeAsync("aria-valuenow", new Regex(".*"));

        await Page.Locator("#lc-succeed").ClickAsync();
        await Expect(Title("tcv-lc")).ToHaveTextAsync("Used Index the docs");
        await Expect(Card("tcv-lc")).Not.ToHaveClassAsync(HasClass("bit-aitcv-run"));
        await Expect(bar).ToHaveCountAsync(0);
        await Expect(dots).ToHaveCountAsync(0);
        await Expect(Page.Locator("#tcv-lc .bit-aitcv-sts")).ToHaveTextAsync(new Regex(@"^1[.,]5s$"));

        // A structured result is a JSON tree under its own label; a success leaves a collapsed card collapsed.
        await Expect(Body("tcv-lc-open").Locator(".bit-aitcv-shd")).ToHaveTextAsync(["Arguments", "Result"]);
        await Expect(Body("tcv-lc-open").Locator(".bit-aitcv-sec").Nth(1).Locator(".bit-aijsv")).ToContainTextAsync("indexed");
        await Expect(Body("tcv-lc-open").Locator(".bit-aitcv-sec").Nth(1).Locator(".bit-aijsv")).ToContainTextAsync("412");
        await Expect(Header("tcv-lc")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Lifecycle_AFailure_OutlinesTheCardAndOpensItOnACompactError()
    {
        await OpenAsync(Route);

        await Page.Locator("#lc-start").ClickAsync();
        await Page.Locator("#lc-complete").ClickAsync();
        await Page.Locator("#lc-run").ClickAsync();
        await Expect(Card("tcv-lc")).ToHaveClassAsync(HasClass("bit-aitcv-run"));
        await Expect(Header("tcv-lc")).ToHaveAttributeAsync("aria-expanded", "false");

        await Page.Locator("#lc-fail").ClickAsync();

        await Expect(Title("tcv-lc")).ToHaveTextAsync("Index the docs failed");
        await Expect(Card("tcv-lc")).ToHaveClassAsync(HasClass("bit-aitcv-fld"));
        await Expect(Card("tcv-lc")).ToHaveClassAsync(HasClass("bit-aitcv-opn"));
        await Expect(Header("tcv-lc")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("#tcv-lc .bit-aitcv-sts")).ToHaveTextAsync(new Regex(@"^2[.,]5s$"));

        // The failure is a compact error view titled by its own message, with its detail behind a disclosure.
        var error = Body("tcv-lc").Locator(".bit-aierv");
        await Expect(error).ToBeVisibleAsync();
        await Expect(error).ToContainTextAsync("The index is locked.");
        await Expect(error.GetByRole(AriaRole.Button, new() { Name = "Show details" })).ToBeVisibleAsync();
        await Expect(Card("tcv-lc")).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-err)"));
    }

    [TestMethod]
    public async Task Lifecycle_ACallChangedOffTheRenderersThread_IsFollowedByItsCardsAlone()
    {
        await OpenAsync(Route);

        await Page.Locator("#lc-background").ClickAsync();

        await Expect(Title("tcv-lc")).ToHaveTextAsync("Used Index the docs");
        await Expect(Title("tcv-lc-open")).ToHaveTextAsync("Used Index the docs");
        await Expect(Body("tcv-lc-open")).ToContainTextAsync("recursive");
        await Expect(Body("tcv-lc-open").Locator(".bit-aitcv-raw").Last).ToHaveTextAsync("Indexed 30 files.");
        await Expect(Page.Locator("#tcv-lc .bit-aitcv-prg")).ToHaveCountAsync(0);

        // The page itself never rendered again: its own line still reads the status the call had when it was made.
        await Expect(Page.Locator("#lc-status")).ToHaveTextAsync("Streaming");
        await AssertNoServerErrorsAsync();
    }

    // ---------- keyboard ----------

    [TestMethod]
    public async Task Keyboard_EnterAndSpaceOnTheHeader_OpenAndCloseTheCard_AndTabMovesToTheNextCard()
    {
        await OpenAsync(Route);

        var header = Header("tcv-ltr");
        await Expect(header).ToHaveAttributeAsync("type", "button");
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "false");
        await header.FocusAsync();

        await Page.Keyboard.PressAsync("Enter");

        await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(header).ToHaveAttributeAsync("aria-controls", "tcv-ltr-bdy");
        await Expect(Card("tcv-ltr")).ToHaveClassAsync(HasClass("bit-aitcv-opn"));
        await Expect(Body("tcv-ltr").Locator(".bit-aitcv-sec").First.Locator(".bit-aijsv")).ToContainTextAsync("Oslo");
        await Expect(Body("tcv-ltr").Locator(".bit-aitcv-sec").Nth(1).Locator(".bit-aijsv")).ToContainTextAsync("-3");
        await Expect(header).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Space");

        await Expect(header).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Body("tcv-ltr")).ToHaveCountAsync(0);
        await Expect(Card("tcv-ltr")).Not.ToHaveClassAsync(HasClass("bit-aitcv-opn"));

        // A closed card is one stop of the tab order: the next one is the header of the next card.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Header("tcv-rtl")).ToBeFocusedAsync();
    }

    // ---------- session ----------

    [TestMethod]
    public async Task Session_TheToolScript_StreamsTheArgumentsRunsTheToolAndAnswersFromIt()
    {
        await OpenAsync(Route);

        await Page.Locator("#ss-ask").ClickAsync();

        // The model writes the call first: its tool is named as the model calls it until the session resolves it.
        await Expect(Title("tcv-ssx-call-1-1")).ToHaveTextAsync("Calling get_weather");
        await Expect(Card("tcv-ssx-call-1-1")).ToHaveClassAsync(HasClass("bit-aitcv-run"));
        await Expect(Body("tcv-ssx-call-1-1")).ToContainTextAsync("Oslo");

        // Then the session runs it, and the answer is read from its result.
        await Expect(Title("tcv-ssx-call-1-1")).ToHaveTextAsync("Used Weather");
        await Expect(Title("tcv-ss-call-1-1")).ToHaveTextAsync("Used Weather");
        await Expect(Body("tcv-ssx-call-1-1").Locator(".bit-aitcv-raw").Last).ToHaveTextAsync("Oslo: -3 degrees, Snow");
        await Expect(Page.Locator("#ss-answer")).ToHaveTextAsync("The weather: Oslo is -3 degrees and Snow.");
        await Expect(Page.Locator("#ss-state")).ToHaveTextAsync("Idle");
        await Expect(Card("tcv-ss-call-1-1")).Not.ToHaveClassAsync(HasClass("bit-aitcv-run"));
        await Expect(Header("tcv-ss-call-1-1")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#tcv-ss-call-1-1 .bit-aitcv-sts")).ToHaveTextAsync(new Regex(@"^\d+ms$|^\d+[.,]\ds$"));

        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Approval_ADestructiveCall_OpensItsCardOnTheThreeAnswers_AndAllowGoesOnWithTheTurn()
    {
        await OpenAsync(Route);

        await Page.Locator("#apv-ask").ClickAsync();

        var card = Card("tcv-apv-call-1-1");
        await Expect(Title("tcv-apv-call-1-1")).ToHaveTextAsync("Allow Delete records to run?");
        await Expect(card).ToHaveClassAsync(HasClass("bit-aitcv-apr"));
        await Expect(Header("tcv-apv-call-1-1")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(card).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-wrn)"));
        await Expect(Body("tcv-apv-call-1-1")).ToContainTextAsync("inactive users");

        // The prompt closes the card: a group named by its question, the answers from the most cautious on, none of
        // them a submit button, the allow one in the error role since the tool is destructive.
        var prompt = card.Locator(".bit-aitcv-apv");
        await Expect(prompt).ToHaveAttributeAsync("role", "group");
        await Expect(card.GetByRole(AriaRole.Group, new() { Name = "Allow Delete records to run?" })).ToHaveCountAsync(1);
        await Expect(prompt).ToHaveClassAsync(HasClass("bit-aitap-dst"));
        var buttons = prompt.Locator("button");
        await Expect(buttons).ToHaveTextAsync(["Decline", "Always allow", "Allow"]);
        for (var i = 0; i < 3; i++) await Expect(buttons.Nth(i)).ToHaveAttributeAsync("type", "button");
        await Expect(buttons.Nth(2)).ToHaveClassAsync(HasClass("bit-btn-dft-err"));
        await Expect(Page.Locator("#apv-state")).ToHaveTextAsync("Generating");

        await buttons.Nth(2).ClickAsync();

        await Expect(Title("tcv-apv-call-1-1")).ToHaveTextAsync("Used Delete records");
        await Expect(card.Locator(".bit-aitcv-apv")).ToHaveCountAsync(0);
        await Expect(card).Not.ToHaveClassAsync(HasClass("bit-aitcv-apr"));
        await Expect(Page.Locator("#apv-answer")).ToHaveTextAsync("Deleted 3 records matching \"inactive users\".");
        await Expect(Page.Locator("#apv-state")).ToHaveTextAsync("Idle");
        await Expect(Page.Locator("#apv-submits")).ToHaveTextAsync("0");
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Approval_Decline_LeavesTheRecordsAlone_AndTheCardSaysTheCallDidNotRun()
    {
        await OpenAsync(Route);

        await Page.Locator("#apv-ask").ClickAsync();
        var card = Card("tcv-apv-call-1-1");
        await card.GetByRole(AriaRole.Button, new() { Name = "Decline" }).ClickAsync();

        await Expect(Page.Locator("#apv-answer")).ToHaveTextAsync("Understood. I left the records alone.");
        await Expect(Page.Locator("#apv-state")).ToHaveTextAsync("Idle");
        await Expect(card.Locator(".bit-aitcv-apv")).ToHaveCountAsync(0);
        await Expect(Title("tcv-apv-call-1-1")).ToHaveTextAsync("Delete records was declined");
        await Expect(card).ToHaveClassAsync(HasClass("bit-aitcv-fld"));
        await Expect(card).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-err)"));
        await Expect(Body("tcv-apv-call-1-1")).ToContainTextAsync("The user declined this tool call.");
        await Expect(Page.Locator("#apv-submits")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Approval_AlwaysAllowByKeyboard_RunsTheNextCallWithoutAsking_AndSubmitsNothing()
    {
        await OpenAsync(Route);

        await Page.Locator("#apv-ask").ClickAsync();
        var always = Card("tcv-apv-call-1-1").GetByRole(AriaRole.Button, new() { Name = "Always allow" });
        await always.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Title("tcv-apv-call-1-1")).ToHaveTextAsync("Used Delete records");
        await Expect(Page.Locator("#apv-state")).ToHaveTextAsync("Idle");

        await Page.Locator("#apv-ask").ClickAsync();

        await Expect(Title("tcv-apv-call-2-1")).ToHaveTextAsync("Used Delete records");
        await Expect(Page.Locator("#apv-state")).ToHaveTextAsync("Idle");
        await Expect(Page.Locator("#approval .bit-aitap")).ToHaveCountAsync(0);
        await Expect(Card("tcv-apv-call-2-1")).Not.ToHaveClassAsync(HasClass("bit-aitcv-apr"));
        await Expect(Page.Locator("#apv-answer")).ToHaveTextAsync("Deleted 3 records matching \"inactive users\".");
        await Expect(Page.Locator("#apv-submits")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Parallel_TwoCallsOfOneRound_EachGetACard_AndOneAnswerReadsBoth()
    {
        await OpenAsync(Route);

        await Page.Locator("#par-ask").ClickAsync();

        await Expect(Page.Locator("#par-answer")).ToHaveTextAsync("Oslo is -3 degrees and Snow and Lisbon is 17 degrees and Clear.");
        await Expect(Page.Locator("#parallel .bit-aitcv")).ToHaveCountAsync(2);
        await Expect(Title("tcv-par-call-1-1")).ToHaveTextAsync("Used Weather");
        await Expect(Title("tcv-par-call-1-2")).ToHaveTextAsync("Used Weather");

        await Header("tcv-par-call-1-1").ClickAsync();
        await Header("tcv-par-call-1-2").ClickAsync();

        await Expect(Body("tcv-par-call-1-1").Locator(".bit-aitcv-sec").First).ToContainTextAsync("Oslo");
        await Expect(Body("tcv-par-call-1-1").Locator(".bit-aitcv-raw").Last).ToHaveTextAsync("Oslo: -3 degrees, Snow");
        await Expect(Body("tcv-par-call-1-2").Locator(".bit-aitcv-sec").First).ToContainTextAsync("Lisbon");
        await Expect(Body("tcv-par-call-1-2").Locator(".bit-aitcv-raw").Last).ToHaveTextAsync("Lisbon: 17 degrees, Clear");
        await AssertNoServerErrorsAsync();
    }

    // ---------- custom renderer ----------

    [TestMethod]
    public async Task Custom_ARendererRegisteredForTheTool_DrawsTheCallInTheCardsPlace_AndFollowsIt()
    {
        await OpenAsync(Route);

        var card = Card("tcv-cst");
        var custom = card.Locator(".e2e-tcv-custom");
        await Expect(card).ToHaveClassAsync(HasClass("bit-aitcv-cst"));
        await Expect(card).ToHaveClassAsync(HasClass("e2e-tcv-cst-root"));
        await Expect(custom).ToHaveTextAsync("e2e_tcv_custom is Streaming:");
        await Expect(custom).ToHaveAttributeAsync("data-message", "msg-e2e-custom");
        await Expect(custom).ToHaveAttributeAsync("data-session", "none");

        // Nothing of the card's own is drawn around it: no header, frame or surface.
        await Expect(card.Locator(".bit-aitcv-hdr")).ToHaveCountAsync(0);
        await Expect(card).ToHaveCSSAsync("border-top-style", "none");
        await Expect(card).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(card).ToHaveCSSAsync("display", "block");

        await Page.Locator("#cst-complete").ClickAsync();
        await Expect(custom).ToHaveTextAsync("e2e_tcv_custom is Pending: {\"zone\":\"north\"}");
        await Page.Locator("#cst-run").ClickAsync();
        await Expect(custom).ToHaveAttributeAsync("data-status", "Running");
        await Page.Locator("#cst-succeed").ClickAsync();
        await Expect(custom).ToHaveTextAsync("e2e_tcv_custom is Completed: {\"zone\":\"north\"}North zone ready.");
        await Expect(card.Locator(".bit-airbd")).ToHaveCountAsync(0);

        // A tool nobody registered a renderer for keeps the built-in card.
        await Expect(Header("tcv-cst-plain")).ToBeVisibleAsync();
        await Expect(Card("tcv-cst-plain")).Not.ToHaveClassAsync(HasClass("bit-aitcv-cst"));
    }

    // ---------- ended calls ----------

    [TestMethod]
    public async Task Ended_AFailedCall_OpensByItself_AndACardTheReaderClosesStaysClosed()
    {
        await OpenAsync(Route);

        var card = Card("tcv-failed");
        await Expect(Title("tcv-failed")).ToHaveTextAsync("Weather failed");
        await Expect(card).ToHaveClassAsync(HasClass("bit-aitcv-fld"));
        await Expect(Header("tcv-failed")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Body("tcv-failed").Locator(".bit-aierv")).ToContainTextAsync("No weather station reports for Atlantis.");
        await Expect(Body("tcv-failed").Locator(".bit-aierv [role=alert]")).ToHaveCountAsync(1);

        await Header("tcv-failed").ClickAsync();
        await Expect(Header("tcv-failed")).ToHaveAttributeAsync("aria-expanded", "false");

        // Only moving into a status opens a card: a render that finds it still failed leaves the reader's choice.
        await Page.Locator("#ended-rerender").ClickAsync();
        await Expect(Page.Locator("#ended-rerender")).ToHaveTextAsync("Re-render the page (1)");
        await Expect(Header("tcv-failed")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Body("tcv-failed")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Ended_ADeclinedAndACancelledCall_AreHeadedForWhatHappenedToThem()
    {
        await OpenAsync(Route);

        // Declined: the call never ran, so it neither failed nor is it still being called.
        await Expect(Title("tcv-declined")).ToHaveTextAsync("Delete records was declined");
        await Expect(Card("tcv-declined")).ToHaveClassAsync(HasClass("bit-aitcv-fld"));

        // Cancelled: stopped while it ran, so it is no longer being called and is not busy.
        await Expect(Title("tcv-cancelled")).ToHaveTextAsync("Index the docs was cancelled");
        await Expect(Card("tcv-cancelled")).Not.ToHaveClassAsync(HasClass("bit-aitcv-run"));
        await Expect(Page.Locator("#tcv-cancelled .bit-aitcv-spn")).ToHaveCountAsync(0);
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_TheHeaderAndTheApprovalAreTurnedOff_AndTheContentStaysReadable()
    {
        await OpenAsync(Route);

        var card = Card("tcv-dis");
        var header = Header("tcv-dis");
        await Expect(card).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(header).ToBeDisabledAsync();
        await Expect(header).ToHaveAttributeAsync("aria-disabled", "true");

        // A call awaiting a decision opened the card by itself; its content is there to read, its answers are not
        // there to give.
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Body("tcv-dis")).ToContainTextAsync("archived");
        var answers = card.Locator(".bit-aitcv-apv button");
        await Expect(answers).ToHaveCountAsync(3);
        for (var i = 0; i < 3; i++)
        {
            await Expect(answers.Nth(i)).ToBeDisabledAsync();
            await Expect(answers.Nth(i)).ToHaveAttributeAsync("aria-disabled", "true");
        }

        await header.DispatchEventAsync("click");
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(header).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-dis)"));

        // The cascade disables a region's cards by default, and a card that says otherwise keeps its own.
        await Expect(Header("tcv-dis-params")).ToBeDisabledAsync();
        await Expect(Card("tcv-dis-params")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Header("tcv-dis-override")).ToBeEnabledAsync();
        await Header("tcv-dis-override").ClickAsync();
        await Expect(Header("tcv-dis-override")).ToHaveAttributeAsync("aria-expanded", "true");
    }

    // ---------- style ----------

    [TestMethod]
    public async Task Style_TheThemeTokens_PaintTheCardItsPartsAndTheOutlineOfEachStatus()
    {
        await OpenAsync(Route);

        var card = Card("tcv-def-done");
        await Expect(card).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-sec)"));
        await Expect(card).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-brd-sec)"));
        await Expect(card).ToHaveCSSAsync("border-top-left-radius", await card.ResolveLengthInAsync("var(--bit-shp-radius-surface)"));
        await Expect(Header("tcv-def-done")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));
        await Expect(Body("tcv-def-done")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-pri)"));
        await Expect(Body("tcv-def-done").Locator(".bit-aitcv-shd").First).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-ter)"));
        await Expect(Body("tcv-def-done").Locator(".bit-aitcv-shd").First).ToHaveCSSAsync("text-transform", "uppercase");
        await Expect(Title("tcv-def-done")).ToHaveCSSAsync("white-space", "nowrap");

        await Expect(Card("tcv-def-run")).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-pri)"));
        await Expect(Card("tcv-def-fail")).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-err)"));
        await Expect(Card("tcv-def-apr")).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-wrn)"));
    }

    [TestMethod]
    public async Task Style_ThePublicVariables_RestyleEveryPart_AndAStatusOutlineWinsOverTheOneAtRest()
    {
        await OpenAsync(Route);

        var card = Card("tcv-vars-done");
        await Expect(card).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(card).ToHaveCSSAsync("border-top-color", "rgb(4, 5, 6)");
        await Expect(card).ToHaveCSSAsync("border-top-left-radius", "9px");
        await Expect(Header("tcv-vars-done")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(Body("tcv-vars-done")).ToHaveCSSAsync("background-color", "rgb(10, 11, 12)");
        await Expect(Body("tcv-vars-done")).ToHaveCSSAsync("border-top-color", "rgb(4, 5, 6)");
        await Expect(Body("tcv-vars-done").Locator(".bit-aitcv-shd").First).ToHaveCSSAsync("color", "rgb(13, 14, 15)");

        await Expect(Card("tcv-vars-run")).ToHaveCSSAsync("border-top-color", "rgb(30, 31, 32)");
        await Expect(Card("tcv-vars-fail")).ToHaveCSSAsync("border-top-color", "rgb(33, 34, 35)");
        await Expect(Card("tcv-vars-apr")).ToHaveCSSAsync("border-top-color", "rgb(36, 37, 38)");

        // The outline at rest alone does not reach a card whose status outlines it.
        await Expect(Card("tcv-rest-done")).ToHaveCSSAsync("border-top-color", "rgb(4, 5, 6)");
        await Expect(Card("tcv-rest-fail")).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-err)"));

        // A variable on one card's Style; Styles and Classes on the parts.
        await Expect(Card("tcv-instance")).ToHaveCSSAsync("background-color", "rgb(40, 41, 42)");
        await Expect(Title("tcv-styles")).ToHaveCSSAsync("color", "rgb(200, 0, 0)");
        await Expect(Page.Locator("#tcv-styles .bit-aitcv-sts")).ToHaveCSSAsync("color", "rgb(0, 120, 0)");
        await Expect(Header("tcv-styles")).ToHaveClassAsync(HasClass("e2e-tcv-header"));
        await Expect(Card("tcv-styles")).ToHaveClassAsync(HasClass("e2e-tcv-root"));
    }

    [TestMethod]
    public async Task Style_ThePointerOverTheHeader_MovesItsVariables_AndTheirPublicOnesRestyleIt()
    {
        await OpenAsync(Route);

        await Header("tcv-vars-done").HoverAsync();
        await Expect(Header("tcv-vars-done")).ToHaveCSSAsync("color", "rgb(16, 17, 18)");
        await Expect(Header("tcv-vars-done")).ToHaveCSSAsync("background-color", "rgb(19, 20, 21)");

        await Header("tcv-def-done").HoverAsync();
        await Expect(Header("tcv-def-done")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-pri)"));
        await Expect(Header("tcv-def-done")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-sec-hover)"));
        await Expect(Header("tcv-vars-done")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(Header("tcv-vars-done")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
    }

    // ---------- rtl, motion, glyphs ----------

    [TestMethod]
    public async Task Rtl_TheChevronPointsAtTheTitle_InAnExplicitAndAnInheritedDirection_AndDownOnceOpen()
    {
        await OpenAsync(Route);

        // Where the glyph's own right-pointing arrow points once everything the stylesheet does to it is applied.
        await WaitForChevronAsync("tcv-ltr", 1, 0);
        var (ltrLeft, _, _, _) = await Page.Locator("#tcv-ltr .bit-aitcv-chv").RectAsync();
        var (ltrTitleLeft, _, _, _) = await Title("tcv-ltr").RectAsync();
        Assert.IsTrue(ltrLeft < ltrTitleLeft, "In left-to-right text the chevron leads the title from the left.");

        foreach (var id in new[] { "tcv-rtl", "tcv-rtl-inherited" })
        {
            await WaitForChevronAsync(id, -1, 0);
            var (_, _, right, _) = await Page.Locator($"#{id} .bit-aitcv-chv").RectAsync();
            var (_, _, titleRight, _) = await Title(id).RectAsync();
            Assert.IsTrue(right > titleRight, $"{id}: in right-to-left text the chevron leads the title from the right.");
        }

        await Expect(Card("tcv-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Card("tcv-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));

        // Open, it points down - the same turn in both directions.
        await Header("tcv-rtl").ClickAsync();
        await Header("tcv-ltr").ClickAsync();
        await Header("tcv-rtl-inherited").ClickAsync();
        await WaitForChevronAsync("tcv-rtl", 0, 1);
        await WaitForChevronAsync("tcv-ltr", 0, 1);
        await WaitForChevronAsync("tcv-rtl-inherited", 0, 1);
    }

    // Waits until the chevron of a card, with its transform and its individual transform properties applied, turns the
    // glyph's right-pointing arrow (1, 0) to (x, y) - y growing downwards.
    private Task WaitForChevronAsync(string cardId, int x, int y)
    {
        return Page.WaitForFunctionAsync(
            """
            ([selector, x, y]) => {
                const e = document.querySelector(selector);
                if (!e) return false;
                const cs = getComputedStyle(e);
                let m = new DOMMatrix();
                if (cs.translate && cs.translate !== 'none') { const t = cs.translate.split(' ').map(parseFloat); m = m.translate(t[0], t[1] || 0); }
                if (cs.rotate && cs.rotate !== 'none') m = m.rotate(parseFloat(cs.rotate));
                if (cs.scale && cs.scale !== 'none') { const s = cs.scale.split(' ').map(parseFloat); m = m.scale(s[0], s.length > 1 ? s[1] : s[0]); }
                if (cs.transform && cs.transform !== 'none') m = m.multiply(new DOMMatrix(cs.transform));
                return Math.abs(m.a - x) < 0.01 && Math.abs(m.b - y) < 0.01;
            }
            """, new object[] { $"#{cardId} .bit-aitcv-chv", x, y });
    }

    [TestMethod]
    public async Task ReducedMotion_TheChevronTurnsWithoutATransition()
    {
        await OpenAsync(Route);

        var chevron = Page.Locator("#tcv-ltr .bit-aitcv-chv");
        Assert.IsTrue(Seconds(await chevron.EvaluateAsync<string>("e => getComputedStyle(e).transitionDuration")) > 0.05,
                      "The chevron turns over a transition.");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        await Page.WaitForFunctionAsync(
            "() => parseFloat(getComputedStyle(document.querySelector('#tcv-ltr .bit-aitcv-chv')).transitionDuration) <= 0.001");
    }

    [TestMethod]
    public async Task Icon_TheToolGlyphIsDrawnFromTheAiFont_AndTheChevronFromTheCoreOne()
    {
        await OpenAsync(Route);

        await Page.Locator("#tcv-icon-default .bit-aitcv-ico").AssertAiGlyphAsync("Repair");
        await Expect(Page.Locator("#tcv-icon-default .bit-aitcv-ico")).ToHaveAttributeAsync("aria-hidden", "true");

        var chevron = Page.Locator("#tcv-icon-default .bit-aitcv-chv");
        await Expect(chevron).ToHaveClassAsync(HasClass("bit-icon--ChevronRight"));
        await Expect(chevron).ToHaveAttributeAsync("aria-hidden", "true");
        var (_, content) = await chevron.GlyphAsync();
        Assert.IsFalse(content is "none" or "normal" or "\"\"" or "", $"The chevron draws a character, was {content}.");
    }

    [TestMethod]
    public async Task Icon_TheIconOfTheToolsDefinition_ReplacesTheDefaultGlyph()
    {
        await OpenAsync(Route);

        var icon = Page.Locator("#tcv-icon-definition .bit-aitcv-ico");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--Sunny"));
        await Expect(icon).Not.ToHaveClassAsync(HasClass("bit-icon-ai--Repair"));
        await Expect(icon).ToHaveAttributeAsync("aria-hidden", "true");
        var (_, content) = await icon.GlyphAsync();
        Assert.IsFalse(content is "none" or "normal" or "\"\"" or "", $"The definition's icon draws a character, was {content}.");
    }

    private static double Seconds(string duration)
    {
        // The first of a list of transition durations ("0.367s, 0.1s"), in seconds.
        var first = duration.Split(',')[0].Trim();
        return first.EndsWith("ms", StringComparison.Ordinal)
            ? double.Parse(first[..^2], CultureInfo.InvariantCulture) / 1000
            : double.Parse(first.TrimEnd('s'), CultureInfo.InvariantCulture);
    }
}
