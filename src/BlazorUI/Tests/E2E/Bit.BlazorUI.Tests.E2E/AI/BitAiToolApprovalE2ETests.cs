namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiToolApproval at /e2e/aitoolapproval in a real browser: the group named by its question (or the reason a
/// call gives, or an AriaLabel), the three answers from the most cautious on and what each records on a call with no
/// session, the keyboard, a destructive tool set apart in the error role, a session that is handed the decision and goes
/// on with the turn (Allow, Decline, Always allow remembered for the next call) inside a form that is never submitted,
/// Disabled with the page's activation handlers dropped and the BitParams cascade, every --bit-AiToolApproval-* variable
/// and the BitButtons' own variables winning over the defaults the prompt gives them, Styles and Classes, RTL, a narrow
/// box and forced colors.
/// </summary>
[TestClass]
public class BitAiToolApprovalE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aitoolapproval";

    private ILocator Prompt(string id) => Page.Locator($"#{id}");

    private ILocator Answers(string id) => Page.Locator($"#{id} .bit-aitap-act button");

    private ILocator Answer(string id, string name) => Page.Locator($"#{id}").GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    [TestMethod]
    public async Task Basic_TheAnswersAreAGroupNamedByTheQuestion_FromTheMostCautiousOn_AndNoneSubmits()
    {
        await OpenAsync(Route);

        var prompt = Prompt("tap-destructive");
        await Expect(prompt).ToHaveAttributeAsync("role", "group");
        await Expect(prompt).ToHaveAttributeAsync("aria-label", "Allow Delete records to run?");
        await Expect(Page.Locator("#basic").GetByRole(AriaRole.Group, new() { Name = "Allow Delete records to run?", Exact = true })).ToHaveCountAsync(1);
        await Expect(prompt.Locator(".bit-aitap-txt")).ToHaveTextAsync("Allow Delete records to run?");

        await Expect(Answers("tap-destructive")).ToHaveTextAsync(["Decline", "Always allow", "Allow"]);
        for (var i = 0; i < 3; i++) await Expect(Answers("tap-destructive").Nth(i)).ToHaveAttributeAsync("type", "button");

        // A tool with no definition is asked about by the name the model called it.
        await Expect(Prompt("tap-unnamed")).ToHaveAttributeAsync("aria-label", "Allow archive_files to run?");
    }

    [TestMethod]
    public async Task Basic_EachAnswer_IsRecordedOnTheCall_WhenThereIsNoSession_AndRaisedOnce()
    {
        await OpenAsync(Route);

        await Answer("tap-destructive", "Allow").ClickAsync();
        await Expect(Page.Locator("#tap-log")).ToHaveTextAsync("call-e2e-destructive:Approved:Pending");

        await Answer("tap-write", "Decline").ClickAsync();
        await Expect(Page.Locator("#tap-log")).ToHaveTextAsync("call-e2e-destructive:Approved:Pending,call-e2e-write:Rejected:Rejected");

        await Answer("tap-unnamed", "Always allow").ClickAsync();
        await Expect(Page.Locator("#tap-log")).ToHaveTextAsync(
            "call-e2e-destructive:Approved:Pending,call-e2e-write:Rejected:Rejected,call-e2e-unnamed:ApprovedAlways:Pending");
    }

    [TestMethod]
    public async Task Reason_ACallsReasonIsTheQuestionShown_AndTheGroupKeepsTheToolsName_OrTheAriaLabel()
    {
        await OpenAsync(Route);

        await Expect(Prompt("tap-write").Locator(".bit-aitap-txt")).ToHaveTextAsync("Send the summary to ana@example.com?");
        await Expect(Prompt("tap-write")).ToHaveAttributeAsync("aria-label", "Allow Send email to run?");
        await Expect(Prompt("tap-write")).Not.ToHaveClassAsync(HasClass("bit-aitap-dst"));

        await Expect(Prompt("tap-aria")).ToHaveAttributeAsync("aria-label", "Confirm sending the email");
        await Expect(Page.Locator("#basic").GetByRole(AriaRole.Group, new() { Name = "Confirm sending the email", Exact = true })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Destructive_TheSurfaceAndTheAllowAnswerOfADestructiveTool_TakeTheErrorRole()
    {
        await OpenAsync(Route);

        await Expect(Prompt("tap-destructive")).ToHaveClassAsync(HasClass("bit-aitap-dst"));
        await Expect(Prompt("tap-destructive")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-err-tint)"));
        await Expect(Answer("tap-destructive", "Allow")).ToHaveClassAsync(HasClass("bit-btn-dft-err"));
        await Expect(Answer("tap-destructive", "Allow")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-err)"));

        // Anything else asks on the warning tint, with the allow answer in the primary role.
        await Expect(Prompt("tap-write")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-wrn-tint)"));
        await Expect(Answer("tap-write", "Allow")).Not.ToHaveClassAsync(HasClass("bit-btn-dft-err"));
        await Expect(Answer("tap-write", "Allow")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-pri)"));

        // The other two are tertiary outlines either way.
        foreach (var id in new[] { "tap-destructive", "tap-write" })
        {
            foreach (var name in new[] { "Decline", "Always allow" })
            {
                await Expect(Answer(id, name)).ToHaveClassAsync(HasClass("bit-btn-dft-ter"));
                await Expect(Answer(id, name)).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-ter)"));
            }
        }

        await Expect(Prompt("tap-write")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-pri)"));
        await Expect(Prompt("tap-write")).ToHaveCSSAsync("border-top-left-radius", await Prompt("tap-write").ResolveLengthInAsync("var(--bit-shp-radius-surface)"));
    }

    [TestMethod]
    public async Task Keyboard_TheAnswersAreTheNextThreeStops_AndEnterOrSpaceGivesOne()
    {
        await OpenAsync(Route);

        await Page.Locator("#tap-kb-before").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Answer("tap-kb", "Decline")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Answer("tap-kb", "Always allow")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Answer("tap-kb", "Allow")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#tap-kb-after")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Answer("tap-kb", "Allow")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#tap-log")).ToHaveTextAsync("call-e2e-keyboard:Approved:Pending");

        await Answer("tap-kb", "Decline").FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#tap-log")).ToHaveTextAsync("call-e2e-keyboard:Approved:Pending,call-e2e-keyboard:Rejected:Rejected");
    }

    // ---------- session ----------

    [TestMethod]
    public async Task Session_Allow_HandsTheDecisionToTheWaitingSession_AndTheTurnGoesOn()
    {
        await OpenAsync(Route);

        await Page.Locator("#ss-ask").ClickAsync();

        var prompt = Prompt("tap-ss-call-1-1");
        await Expect(prompt).ToBeVisibleAsync();
        await Expect(prompt).ToHaveClassAsync(HasClass("bit-aitap-dst"));
        await Expect(prompt).ToHaveAttributeAsync("aria-label", "Allow Delete records to run?");
        await Expect(Page.Locator("#ss-requests")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#ss-state")).ToHaveTextAsync("Generating");
        await Expect(Page.Locator("#ss-calls")).ToHaveTextAsync("call-1-1:AwaitingApproval:Undecided");

        await Answer("tap-ss-call-1-1", "Allow").ClickAsync();

        await Expect(Page.Locator("#ss-answer")).ToHaveTextAsync("Deleted 3 records matching \"inactive users\".");
        await Expect(Page.Locator("#ss-state")).ToHaveTextAsync("Idle");
        await Expect(Page.Locator("#ss-calls")).ToHaveTextAsync("call-1-1:Completed:Approved");
        await Expect(Page.Locator("#ss-decisions")).ToHaveTextAsync("Approved");
        await Expect(prompt).ToHaveCountAsync(0);
        await Expect(Page.Locator("#ss-submits")).ToHaveTextAsync("0");
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Session_Decline_LeavesTheToolUnrun_AndTheTurnSaysSo()
    {
        await OpenAsync(Route);

        await Page.Locator("#ss-ask").ClickAsync();
        await Answer("tap-ss-call-1-1", "Decline").ClickAsync();

        await Expect(Page.Locator("#ss-answer")).ToHaveTextAsync("Understood. I left the records alone.");
        await Expect(Page.Locator("#ss-calls")).ToHaveTextAsync("call-1-1:Rejected:Rejected");
        await Expect(Page.Locator("#ss-decisions")).ToHaveTextAsync("Rejected");
        await Expect(Page.Locator("#ss-state")).ToHaveTextAsync("Idle");
        await Expect(Page.Locator("#ss-submits")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Session_AlwaysAllowByEnter_IsRememberedForTheNextCall_AndSubmitsNothing()
    {
        await OpenAsync(Route);

        await Page.Locator("#ss-ask").ClickAsync();
        await Answer("tap-ss-call-1-1", "Always allow").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#ss-calls")).ToHaveTextAsync("call-1-1:Completed:ApprovedAlways");
        await Expect(Page.Locator("#ss-state")).ToHaveTextAsync("Idle");

        await Page.Locator("#ss-ask").ClickAsync();

        await Expect(Page.Locator("#ss-calls")).ToHaveTextAsync("call-1-1:Completed:ApprovedAlways,call-2-1:Completed:Undecided");
        await Expect(Page.Locator("#ss-state")).ToHaveTextAsync("Idle");
        await Expect(Page.Locator("#ss-requests")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#session .bit-aitap")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#ss-answer")).ToHaveTextAsync("Deleted 3 records matching \"inactive users\".");
        await Expect(Page.Locator("#ss-submits")).ToHaveTextAsync("0");
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_APromptAnswersNothing_AndDropsThePagesActivationHandlers()
    {
        await OpenAsync(Route);

        var prompt = Prompt("tap-dis");
        await Expect(prompt).ToHaveClassAsync(HasClass("bit-dis"));
        for (var i = 0; i < 3; i++)
        {
            await Expect(Answers("tap-dis").Nth(i)).ToBeDisabledAsync();
            await Expect(Answers("tap-dis").Nth(i)).ToHaveAttributeAsync("aria-disabled", "true");
        }

        await Answers("tap-dis").Nth(2).DispatchEventAsync("click");
        await prompt.Locator(".bit-aitap-txt").ClickAsync();

        // An enabled prompt keeps the page's handlers on its root, which is what tells the drop from a missed click.
        await Prompt("tap-enabled").Locator(".bit-aitap-txt").ClickAsync();
        await Expect(Page.Locator("#tap-root-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#tap-log")).ToHaveTextAsync("");

        // The cascade disables a region's prompts by default; one that says otherwise keeps its own.
        await Expect(Answers("tap-dis-params").First).ToBeDisabledAsync();
        await Expect(Prompt("tap-dis-params")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Answers("tap-dis-override").First).ToBeEnabledAsync();
        await Expect(Prompt("tap-dis-override")).Not.ToHaveClassAsync(HasClass("bit-dis"));
    }

    // ---------- style ----------

    [TestMethod]
    public async Task Style_ThePublicVariables_RestyleThePrompt_AndADestructiveOneReadsItsOwnSurface()
    {
        await OpenAsync(Route);

        var prompt = Prompt("tap-vars-write");
        await Expect(prompt).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(prompt).ToHaveCSSAsync("background-color", "rgb(4, 5, 6)");
        await Expect(prompt).ToHaveCSSAsync("border-top-left-radius", "11px");
        await Expect(prompt).ToHaveCSSAsync("padding-top", "13px");
        await Expect(prompt).ToHaveCSSAsync("column-gap", "17px");
        await Expect(prompt.Locator(".bit-aitap-act")).ToHaveCSSAsync("column-gap", "17px");
        await Expect(Prompt("tap-vars-destructive")).ToHaveCSSAsync("background-color", "rgb(7, 8, 9)");

        // The surface of every prompt is not the surface of a destructive one.
        await Expect(Prompt("tap-bg-write")).ToHaveCSSAsync("background-color", "rgb(4, 5, 6)");
        await Expect(Prompt("tap-bg-destructive")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-err-tint)"));

        // The defaults the prompt gives its buttons are defaults: the buttons' own variables win over them.
        await Expect(Answer("tap-buttons", "Allow")).ToHaveCSSAsync("background-color", "rgb(20, 21, 22)");
        await Expect(Answer("tap-buttons", "Decline")).ToHaveCSSAsync("border-top-color", "rgb(23, 24, 25)");

        await Expect(Prompt("tap-styles").Locator(".bit-aitap-txt")).ToHaveCSSAsync("color", "rgb(200, 0, 0)");
        await Expect(Prompt("tap-styles").Locator(".bit-aitap-act")).ToHaveCSSAsync("column-gap", "3px");
        await Expect(Prompt("tap-styles")).ToHaveClassAsync(HasClass("e2e-tap-root"));
        await Expect(Answer("tap-styles", "Allow")).ToHaveClassAsync(HasClass("e2e-tap-allow"));
    }

    [TestMethod]
    public async Task Rtl_TheQuestionAndTheAnswersReadFromTheRight_InAnExplicitAndAnInheritedDirection()
    {
        await OpenAsync(Route);

        var (declineLeft, _, _, _) = await Answer("tap-ltr", "Decline").RectAsync();
        var (allowLeft, _, _, _) = await Answer("tap-ltr", "Allow").RectAsync();
        var (textLeft, _, _, _) = await Prompt("tap-ltr").Locator(".bit-aitap-txt").RectAsync();
        Assert.IsTrue(declineLeft < allowLeft, "Left to right, the most cautious answer comes first from the left.");
        Assert.IsTrue(textLeft < declineLeft, "Left to right, the question leads the answers.");

        foreach (var id in new[] { "tap-rtl", "tap-rtl-inherited" })
        {
            var (_, _, declineRight, _) = await Answer(id, "Decline").RectAsync();
            var (_, _, allowRight, _) = await Answer(id, "Allow").RectAsync();
            var (_, _, textRight, _) = await Prompt(id).Locator(".bit-aitap-txt").RectAsync();
            Assert.IsTrue(declineRight > allowRight, $"{id}: right to left, the most cautious answer comes first from the right.");
            Assert.IsTrue(textRight > declineRight, $"{id}: right to left, the question leads the answers from the right.");
        }

        await Expect(Prompt("tap-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Prompt("tap-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
    }

    [TestMethod]
    public async Task Narrow_TheAnswersWrapOntoALineOfTheirOwn_UnderAQuestionLeftReadable()
    {
        await OpenAsync(Route);

        var prompt = Prompt("tap-narrow");
        var (_, _, _, textBottom) = await prompt.Locator(".bit-aitap-txt").RectAsync();
        var (_, actionsTop, _, _) = await prompt.Locator(".bit-aitap-act").RectAsync();
        Assert.IsTrue(actionsTop >= textBottom - 1, "The answers wrap under the question.");
        Assert.IsTrue(await prompt.EvaluateAsync<bool>("e => e.scrollWidth <= e.clientWidth + 1"), "Nothing runs off the side of the prompt.");
    }

    [TestMethod]
    public async Task ForcedColors_ThePromptIsOutlinedInsideItsBox_SinceItsTintIsGone()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync(Route);
        await Expect(Prompt("tap-write")).ToHaveCSSAsync("outline-style", "none");

        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        var prompt = Prompt("tap-write");
        await Expect(prompt).ToHaveCSSAsync("outline-style", "solid");
        await Expect(prompt).ToHaveCSSAsync("outline-color", await Page.ResolveColorAsync("CanvasText"));
        var width = await prompt.EvaluateAsync<string>("e => getComputedStyle(e).outlineWidth");
        await Expect(prompt).ToHaveCSSAsync("outline-offset", $"-{width}");
    }
}
