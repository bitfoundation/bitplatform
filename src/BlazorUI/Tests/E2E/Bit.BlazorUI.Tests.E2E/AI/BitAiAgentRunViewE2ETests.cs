using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiAgentRunView at /e2e/aiagentrunview in a real browser: runs a session builds from the agent, agent-input
/// and agent-fail scripts (the plan, the steps changing state with their classes, the bar and the count, the clock
/// ticking while the run goes on and standing still once it ends, the question answered by a suggestion and by a typed
/// answer, the run resuming and failing), a run the page moves a transition at a time, OnRespond, the usage of the
/// header, Disabled, the theme tokens and every --bit-AiAgentRunView-* variable, Styles and Classes, RTL, forced colors,
/// the accessibility of the live DOM and the glyph of the header.
/// </summary>
[TestClass]
public class BitAiAgentRunViewE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aiagentrunview";

    private ILocator Run(string id) => Page.Locator($"#{id}");

    private ILocator Badge(string id) => Page.Locator($"#{id} > .bit-aiarv-hdr > .bit-aiasb");

    private ILocator Steps(string id) => Page.Locator($"#{id} .bit-aiasv");

    private ILocator Timer(string id) => Page.Locator($"#{id} [role=timer]");

    private ILocator Bar(string id) => Page.Locator($"#{id} .bit-aiarv-prg [role=progressbar]");

    // ---------- a run of the session ----------

    [TestMethod]
    public async Task Session_AnAgentRun_ShowsItsPlanAndFollowsEveryStepToTheEnd()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-session-start").ClickAsync();

        var run = Run("arv-session");
        await Expect(run.Locator(".bit-aiarv-ttl")).ToHaveTextAsync("Research agent");
        await Expect(run.Locator(".bit-aiarv-gol")).ToHaveTextAsync("Find out why the three studies disagree");
        await Expect(run.Locator(".bit-aiasv-ttl > span:first-child")).ToHaveTextAsync(["Search for sources", "Read the sources", "Reconcile them", "Write the summary"]);
        await Expect(run.Locator("ol.bit-aiarv-stl > li.bit-aiarv-sti")).ToHaveCountAsync(4);

        // Every step is seen running before it completes, one after the other, the run running all the while.
        await ExpectStepStatesAsync("arv-session", "run", "pnd", "pnd", "pnd");
        await Expect(Badge("arv-session")).ToHaveTextAsync("Running");
        await ExpectStepStatesAsync("arv-session", "cmp", "run", "pnd", "pnd");
        await Expect(run.Locator(".bit-aiasv-smr").First).ToHaveTextAsync("3 sources found");
        await ExpectStepStatesAsync("arv-session", "cmp", "cmp", "run", "pnd");
        await ExpectStepStatesAsync("arv-session", "cmp", "cmp", "cmp", "run");
        await ExpectStepStatesAsync("arv-session", "cmp", "cmp", "cmp", "cmp");

        await Expect(Badge("arv-session")).ToHaveTextAsync("Done");
        await Expect(Badge("arv-session").Locator(".bit-tag")).ToHaveClassAsync(HasClass("bit-tag-suc"));
        await Expect(run.Locator(".bit-aiasv-smr")).ToHaveTextAsync(["3 sources found", "Two agree, one does not", "They differ in scope", "Summary written"]);
        await Expect(run.Locator(".bit-aiarv-cnt")).ToHaveTextAsync("4 / 4");
        await Expect(Bar("arv-session")).ToHaveAttributeAsync("aria-valuenow", "4");
        await Expect(Page.Locator("#session-reply")).ToHaveTextAsync("The sources agree on direction and differ in scope.");
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Session_TheBarAndTheCount_FollowTheCompletedSteps()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-session-start").ClickAsync();

        var bar = Bar("arv-session");
        await Expect(bar).ToHaveAttributeAsync("aria-valuemin", "0");
        await Expect(bar).ToHaveAttributeAsync("aria-valuemax", "4");

        // Read in one frame while the second step runs, so the bar and the count are seen part of the way.
        await Page.WaitForFunctionAsync(
            """
            () => {
                const run = document.getElementById('arv-session');
                const steps = run ? [...run.querySelectorAll('.bit-aiasv')] : [];
                const bar = run?.querySelector('.bit-aiarv-prg [role=progressbar]');
                return steps.length === 4 && steps[1].classList.contains('bit-aiasv-run')
                    && bar?.getAttribute('aria-valuenow') === '1' && bar?.getAttribute('aria-valuetext') === '1 / 4'
                    && run.querySelector('.bit-aiarv-cnt')?.textContent === '1 / 4';
            }
            """, null, new() { Timeout = DefaultTimeout });

        await Expect(bar).ToHaveAttributeAsync("aria-valuenow", "4");
        await Expect(bar).ToHaveAttributeAsync("aria-valuetext", "4 / 4");
    }

    [TestMethod]
    public async Task Session_TheClock_TicksWhileTheRunGoesOnAndStandsStillOnceItEnds()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-session-start").ClickAsync();

        var timer = Timer("arv-session");
        await Expect(timer).ToHaveTextAsync(new Regex(@"^\d+(ms|(\.\d)?s)$"));
        var first = await timer.TextContentAsync();
        await Expect(timer).Not.ToHaveTextAsync(first!);

        await Expect(Badge("arv-session")).ToHaveTextAsync("Done");
        await AssertStandsStillAsync(timer);
    }

    // ---------- asking the user ----------

    [TestMethod]
    public async Task Input_ASuggestion_AnswersTheQuestionAndTheRunResumes()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-input-pick-start").ClickAsync();

        var run = Run("arv-input-pick");
        await Expect(run.Locator(".bit-aiarv-qst")).ToHaveTextAsync("When should I take the downtime?");
        await Expect(Badge("arv-input-pick")).ToHaveTextAsync("Waiting for your answer");
        await Expect(Badge("arv-input-pick").Locator(".bit-tag")).ToHaveClassAsync(HasClass("bit-tag-wrn"));

        // The step the question was asked in waits for the answer with the run, rather than being drawn as running.
        await ExpectStepStatesAsync("arv-input-pick", "cmp", "blk", "pnd");
        await Expect(Page.Locator("#input-pick-reply")).ToHaveTextAsync("I need one answer before I go on.");
        await Expect(Page.Locator("#input-pick-generating")).ToHaveTextAsync("False");

        var suggestions = run.Locator(".bit-aiarv-prm > .bit-btn");
        await Expect(suggestions).ToHaveTextAsync(["Tonight", "Saturday morning", "Ask me later"]);

        await suggestions.Nth(1).ClickAsync();

        await Expect(run.Locator(".bit-aiarv-prm")).ToHaveCountAsync(0);
        await ExpectStepStatesAsync("arv-input-pick", "cmp", "cmp", "cmp");
        await Expect(run.Locator(".bit-aiasv-smr")).ToHaveTextAsync(["2 tables need rewriting", "Window: Saturday morning", "14 tables migrated"]);
        await Expect(Badge("arv-input-pick")).ToHaveTextAsync("Done");
        await Expect(Page.Locator("#input-pick-reply")).ToHaveTextAsync("Booked for Saturday morning.");
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Input_AnAnswerTypedAndSubmittedWithEnter_ResumesTheRun()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-input-type-start").ClickAsync();

        var run = Run("arv-input-type");
        var field = run.Locator(".bit-aiarv-fld input");
        var answer = run.Locator(".bit-aiarv-ans > .bit-btn");
        await Expect(field).ToBeVisibleAsync();
        await Expect(answer).ToHaveTextAsync("Answer");
        await Expect(answer).ToBeDisabledAsync();

        // Nothing but blanks is no answer.
        await field.FillAsync("   ");
        await Expect(answer).ToBeDisabledAsync();

        await field.FillAsync("Sunday at noon");
        await Expect(answer).ToBeEnabledAsync();
        // Answered once the turn that asked has ended (see the report: an answer given while it still streams is lost).
        await Expect(Page.Locator("#input-type-generating")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#input-type-reply")).ToHaveTextAsync("I need one answer before I go on.");
        await field.PressAsync("Enter");

        await Expect(run.Locator(".bit-aiarv-prm")).ToHaveCountAsync(0);
        await ExpectStepStatesAsync("arv-input-type", "cmp", "cmp", "cmp");
        await Expect(run.Locator(".bit-aiasv-smr").Nth(1)).ToHaveTextAsync("Window: Sunday at noon");
        await Expect(Badge("arv-input-type")).ToHaveTextAsync("Done");
        await Expect(Page.Locator("#input-type-reply")).ToHaveTextAsync("Booked for Sunday at noon.");
    }

    [TestMethod]
    public async Task Input_AnAnswerGivenWhileTheQuestionStillStreams_IsNotLost()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-input-busy-start").ClickAsync();

        var run = Run("arv-input-busy");
        var field = run.Locator(".bit-aiarv-fld input");
        var answer = run.Locator(".bit-aiarv-ans > .bit-btn");
        await Expect(run.Locator(".bit-aiarv-qst")).ToHaveTextAsync("When should I take the downtime?");
        await field.FillAsync("Sunday at noon");
        await Expect(answer).ToBeEnabledAsync();

        // The turn that asked is still streaming its last sentence when the answer is sent.
        await Expect(Page.Locator("#input-busy-generating")).ToHaveTextAsync("True");
        await answer.ClickAsync();
        await Expect(Page.Locator("#input-busy-generating")).ToHaveTextAsync("False", new() { Timeout = 15000 });

        // Either the run took the answer, or it is still in the field, ready to be sent once the turn is over - it is
        // never thrown away while the question stays up.
        var outcome = await Page.WaitForFunctionAsync(
            """
            () => {
                const run = document.getElementById('arv-input-busy');
                if (!run) return null;
                if (!run.querySelector('.bit-aiarv-prm')) return 'resumed';
                const input = run.querySelector('.bit-aiarv-fld input');
                const button = run.querySelector('.bit-aiarv-ans > .bit-btn');
                if (input && input.value === 'Sunday at noon' && button && !button.disabled && button.getAttribute('aria-disabled') !== 'true') return 'kept';
                if (input && input.value === '') return 'lost';
                return null;
            }
            """, null, new() { Timeout = 10000 });
        var result = await outcome.JsonValueAsync<string>();
        Assert.AreNotEqual("lost", result, "The answer was cleared from the field while the run still waits for it.");

        if (result == "kept") await answer.ClickAsync();

        await ExpectStepStatesAsync("arv-input-busy", "cmp", "cmp", "cmp");
        await Expect(run.Locator(".bit-aiasv-smr").Nth(1)).ToHaveTextAsync("Window: Sunday at noon");
        await Expect(Page.Locator("#input-busy-reply")).ToHaveTextAsync("Booked for Sunday at noon.");
    }

    [TestMethod]
    public async Task Input_TheAnswerField_IsNamedByTheQuestionItAnswers()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-input-type-start").ClickAsync();

        var run = Run("arv-input-type");
        var question = run.Locator(".bit-aiarv-qst");
        await Expect(question).ToHaveTextAsync("When should I take the downtime?");
        var questionId = await question.GetAttributeAsync("id");
        Assert.IsFalse(string.IsNullOrEmpty(questionId));

        var field = run.Locator(".bit-aiarv-fld input");
        await Expect(field).ToHaveAttributeAsync("aria-labelledby", questionId!);
        await Expect(field).ToHaveAccessibleNameAsync("When should I take the downtime?");
        await Expect(field).ToHaveAttributeAsync("placeholder", "Type your answer...");

        // The suggestions come first in the tab order, then the field, then the button that sends it.
        await run.Locator(".bit-aiarv-prm > .bit-btn").First.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(run.Locator(".bit-aiarv-prm > .bit-btn").Nth(2)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(field).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Respond_OnRespond_TakesTheAnswerByASuggestionAndByTheButton()
    {
        await OpenAsync(Route);

        var run = Run("arv-respond");
        await Expect(run.Locator(".bit-aiarv-qst")).ToHaveTextAsync("Which color should I use?");
        await run.Locator(".bit-aiarv-prm > .bit-btn", new() { HasText = "Blue" }).ClickAsync();

        await Expect(Page.Locator("#respond-answer")).ToHaveTextAsync("Blue");
        await Expect(Page.Locator("#respond-count")).ToHaveTextAsync("1");
        await Expect(run.Locator(".bit-aiarv-prm")).ToHaveCountAsync(0);
        await ExpectStepStatesAsync("arv-respond", "cmp", "run");
        await Expect(run.Locator(".bit-aiasv-smr")).ToHaveTextAsync("Picked Blue");
        await Expect(Badge("arv-respond")).ToHaveTextAsync("Running");

        // Asked again, an answer of the user's own goes through the button just the same - once.
        await Page.Locator("#btn-respond-reset").ClickAsync();
        await Expect(Page.Locator("#respond-answer")).ToHaveTextAsync("-");
        var field = run.Locator(".bit-aiarv-fld input");
        await Expect(field).ToHaveValueAsync("");
        await field.FillAsync("Green");
        await run.Locator(".bit-aiarv-ans > .bit-btn").ClickAsync();

        await Expect(Page.Locator("#respond-answer")).ToHaveTextAsync("Green");
        await Expect(Page.Locator("#respond-count")).ToHaveTextAsync("1");
        await Expect(run.Locator(".bit-aiasv-smr")).ToHaveTextAsync("Picked Green");
    }

    // ---------- a failed run ----------

    [TestMethod]
    public async Task Fail_AStepThatFails_FailsTheRunAndSaysWhy()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-fail-start").ClickAsync();

        var run = Run("arv-fail");
        await Expect(Badge("arv-fail")).ToHaveTextAsync("Failed");
        await Expect(Badge("arv-fail").Locator(".bit-tag")).ToHaveClassAsync(HasClass("bit-tag-err"));
        // The step the failed run never reached is abandoned, not left pending under a run that has ended.
        await ExpectStepStatesAsync("arv-fail", "cmp", "fld", "skp");
        await Expect(Steps("arv-fail").Nth(1).Locator(".bit-aiasv-err")).ToHaveTextAsync("3 of 812 tests failed.");

        // The detail of the step's error - the failing tests - is behind a disclosure under the message.
        await Steps("arv-fail").Nth(1).GetByRole(AriaRole.Button, new() { Name = "Show details", Exact = true }).ClickAsync();
        await Expect(Steps("arv-fail").Nth(1).Locator(".bit-aiasv-edt")).ToContainTextAsync("Checkout.RoundsTax");
        await Expect(Steps("arv-fail").Nth(1).GetByRole(AriaRole.Button, new() { Name = "Hide details", Exact = true })).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(run.Locator(".bit-aiarv-cnt")).ToHaveTextAsync("1 / 3");
        await Expect(Page.Locator("#fail-reply")).ToHaveTextAsync("I stopped before deploying: three checkout tests fail.");

        // The failed step's marker is the error color, and the clock of a finished run stands still.
        await Expect(Steps("arv-fail").Nth(1).Locator(".bit-aiasv-mrk")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-err)"));
        await AssertStandsStillAsync(Timer("arv-fail"));
    }

    // ---------- a run moved by the page ----------

    [TestMethod]
    public async Task Manual_EveryTransition_ChangesTheStepsClassesTheCountAndTheBadge()
    {
        await OpenAsync(Route);

        var run = Run("arv-manual");
        await Expect(Badge("arv-manual")).ToHaveTextAsync("Planning");
        await ExpectStepStatesAsync("arv-manual", "pnd", "pnd", "pnd");
        await Expect(run.Locator(".bit-aiarv-cnt")).ToHaveTextAsync("0 / 3");

        await Page.Locator("#btn-manual-start").ClickAsync();
        await ExpectStepStatesAsync("arv-manual", "run", "pnd", "pnd");
        await Expect(Badge("arv-manual")).ToHaveTextAsync("Running");

        await Page.Locator("#btn-manual-complete").ClickAsync();
        await ExpectStepStatesAsync("arv-manual", "cmp", "pnd", "pnd");
        await Expect(run.Locator(".bit-aiasv-smr")).ToHaveTextAsync("Draft done");
        await Expect(run.Locator(".bit-aiarv-cnt")).ToHaveTextAsync("1 / 3");
        await Expect(Bar("arv-manual")).ToHaveAttributeAsync("aria-valuenow", "1");
        await Expect(Steps("arv-manual").First.Locator(".bit-aiasv-mic")).ToBeVisibleAsync();

        await Page.Locator("#btn-manual-start").ClickAsync();
        await ExpectStepStatesAsync("arv-manual", "cmp", "run", "pnd");
        await Page.Locator("#btn-manual-fail-step").ClickAsync();
        await ExpectStepStatesAsync("arv-manual", "cmp", "fld", "pnd");
        await Expect(Steps("arv-manual").Nth(1).Locator(".bit-aiasv-err")).ToHaveTextAsync("Review rejected");
        await Expect(Badge("arv-manual")).ToHaveTextAsync("Running");

        // The run's own failure ends it and is shown under the steps.
        await Page.Locator("#btn-manual-fail-run").ClickAsync();
        await Expect(Badge("arv-manual")).ToHaveTextAsync("Failed");
        await Expect(run.Locator(".bit-aiarv-err")).ToContainTextAsync("The agent gave up");
        await AssertStandsStillAsync(Timer("arv-manual"));
    }

    [TestMethod]
    public async Task Manual_TheClock_TicksAtItsIntervalUntilTheRunIsFinished()
    {
        await OpenAsync(Route);

        // A run with a plan is under way, so its clock moves (every 100ms here) before any step has started.
        var timer = Timer("arv-manual");
        var first = await timer.TextContentAsync();
        await Expect(timer).Not.ToHaveTextAsync(first!);
        var second = await timer.TextContentAsync();
        await Expect(timer).Not.ToHaveTextAsync(second!);

        await Page.Locator("#btn-manual-finish").ClickAsync();
        await Expect(Badge("arv-manual")).ToHaveTextAsync("Done");
        await AssertStandsStillAsync(timer);

        // A new run starts a clock of its own.
        await Page.Locator("#btn-manual-reset").ClickAsync();
        await Expect(Badge("arv-manual")).ToHaveTextAsync("Planning");
        var restarted = await timer.TextContentAsync();
        await Expect(timer).Not.ToHaveTextAsync(restarted!);
    }

    [TestMethod]
    public async Task Accessibility_TheStatusTheBarAndTheClock_AreAnnouncedTheirOwnWay()
    {
        await OpenAsync(Route);

        // The status is a polite live region; the bar is named after the plan and read as a count of steps; the clock
        // is a timer, which stays quiet until asked.
        await Expect(Badge("arv-manual")).ToHaveAttributeAsync("role", "status");
        await Expect(Badge("arv-manual")).ToHaveAttributeAsync("aria-live", "polite");

        var bar = Bar("arv-manual");
        await Expect(bar).ToHaveAttributeAsync("aria-label", "Plan");
        await Expect(bar).ToHaveAccessibleNameAsync("Plan");
        await Expect(bar).ToHaveAttributeAsync("aria-valuetext", "0 / 3");

        await Expect(Timer("arv-manual")).ToHaveAttributeAsync("aria-label", "Elapsed");
        await Expect(Timer("arv-manual")).Not.ToHaveAttributeAsync("aria-live", new Regex(".+"));

        // The steps are a list, each marker hidden from assistive technologies since its words say the same.
        await Expect(Page.Locator("#arv-manual ol > li")).ToHaveCountAsync(3);
        await Expect(Page.Locator("#arv-manual .bit-aiasv-mrk").First).ToHaveAttributeAsync("aria-hidden", "true");
    }

    // ---------- usage ----------

    [TestMethod]
    public async Task Usage_ShowUsage_TotalsTheUsageOfTheRunInTheHeader()
    {
        await OpenAsync(Route);

        var usage = Page.Locator("#arv-usage > .bit-aiarv-hdr > .bit-aituv");
        await Expect(usage).ToHaveCountAsync(0);

        await Page.Locator("#btn-usage-add").ClickAsync();
        await Expect(usage.Locator(".bit-aituv-tok")).ToHaveTextAsync("1.5k tokens");
        await Expect(usage.Locator(".bit-aituv-cst")).ToHaveTextAsync("$0.0081");

        await Page.Locator("#btn-usage-add").ClickAsync();
        await Expect(usage.Locator(".bit-aituv-tok")).ToHaveTextAsync("3k tokens");
        await Expect(usage.Locator(".bit-aituv-cst")).ToHaveTextAsync("$0.02");

        // Without ShowUsage the same run shows none.
        await Expect(Page.Locator("#arv-usage-hidden .bit-aituv")).ToHaveCountAsync(0);
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_ARunWaitingForAnAnswer_TakesNone()
    {
        await OpenAsync(Route);

        var run = Run("arv-disabled");
        await Expect(run).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(run).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-dis)"));

        foreach (var button in await run.Locator(".bit-aiarv-prm .bit-btn").AllAsync())
        {
            await Expect(button).ToBeDisabledAsync();
        }

        await Expect(run.Locator(".bit-aiarv-fld input")).ToBeDisabledAsync();
        await Expect(Badge("arv-disabled").Locator(".bit-tag")).ToHaveClassAsync(HasClass("bit-dis"));

        await run.Locator(".bit-aiarv-prm > .bit-btn").First.ClickAsync(new() { Force = true });
        await Expect(run.Locator(".bit-aiarv-qst")).ToHaveTextAsync("Shall I go on?");
        await Expect(Badge("arv-disabled")).ToHaveTextAsync("Waiting for your answer");
    }

    // ---------- look ----------

    [TestMethod]
    public async Task Theme_ThePanel_ReadsTheThemeTokens()
    {
        await OpenAsync(Route);

        var run = Run("arv-rtl");
        await Expect(run).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-sec)"));
        await Expect(run).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-brd-sec)"));
        await Expect(run.Locator(".bit-aiarv-bdy")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-pri)"));
        await Expect(run.Locator(".bit-aiarv-hdr")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));
        await Expect(run.Locator(".bit-aiarv-prm")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-wrn-tint)"));
        await Expect(run.Locator(".bit-aiarv-qst")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-pri)"));

        // The count and the clock are set in digits of one width, so the row never jitters as they change.
        await Expect(run.Locator(".bit-aiarv-prg")).ToHaveCSSAsync("font-variant-numeric", "tabular-nums");
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleThePanel()
    {
        await OpenAsync(Route);

        var run = Run("arv-vars");
        await Expect(run).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(run).ToHaveCSSAsync("border-top-color", "rgb(7, 8, 9)");
        await Expect(run).ToHaveCSSAsync("border-top-width", "3px");
        await Expect(run).ToHaveCSSAsync("border-top-left-radius", "9px");
        await Expect(run.Locator(".bit-aiarv-bdy")).ToHaveCSSAsync("background-color", "rgb(4, 5, 6)");
        await Expect(run.Locator(".bit-aiarv-bdy")).ToHaveCSSAsync("border-top-color", "rgb(7, 8, 9)");
        await Expect(run.Locator(".bit-aiarv-hdr")).ToHaveCSSAsync("color", "rgb(10, 11, 12)");
        await Expect(run.Locator(".bit-aiarv-gol")).ToHaveCSSAsync("color", "rgb(13, 14, 15)");
        await Expect(run.Locator(".bit-aiarv-prg")).ToHaveCSSAsync("color", "rgb(13, 14, 15)");
        await Expect(run.Locator(".bit-aiarv-prm")).ToHaveCSSAsync("background-color", "rgb(16, 17, 18)");
        await Expect(run.Locator(".bit-aiarv-bar")).ToHaveCSSAsync("max-width", "50px");
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheRootAndTheParts()
    {
        await OpenAsync(Route);

        var run = Run("arv-styles");
        await Expect(run).ToHaveCSSAsync("background-color", "rgb(20, 21, 22)");
        await Expect(run.Locator(".bit-aiarv-gol")).ToHaveCSSAsync("color", "rgb(30, 31, 32)");
        await Expect(run.Locator(".bit-aiarv-qst")).ToHaveCSSAsync("font-weight", "700");
        await Expect(run.Locator(".bit-aiarv-hdr")).ToHaveClassAsync(HasClass("e2e-arv-header"));
        await Expect(run.Locator("li.bit-aiarv-sti.e2e-arv-item")).ToHaveCountAsync(3);
        await Expect(run.Locator(".bit-aiasv.e2e-arv-step")).ToHaveCountAsync(3);
    }

    [TestMethod]
    public async Task Rtl_AnExplicitOrAnInheritedDirection_MirrorsTheHeaderAndTheTimeline()
    {
        await OpenAsync(Route);

        await Expect(Run("arv-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Run("arv-rtl")).ToHaveCSSAsync("direction", "rtl");
        await Expect(Run("arv-rtl-inherited")).ToHaveCSSAsync("direction", "rtl");

        foreach (var id in new[] { "arv-rtl", "arv-rtl-inherited" })
        {
            var icon = await Page.Locator($"#{id} .bit-aiarv-hic").RectAsync();
            var title = await Page.Locator($"#{id} .bit-aiarv-ttl").RectAsync();
            Assert.IsTrue(icon.Left > title.Left, $"{id}: the icon leads the header from the right.");

            var marker = await Page.Locator($"#{id} .bit-aiasv-mrk").First.RectAsync();
            var body = await Page.Locator($"#{id} .bit-aiasv-bdy").First.RectAsync();
            Assert.IsTrue(marker.Left > body.Left, $"{id}: the marker of a step is on its right.");
        }

        var ltrMarker = await Page.Locator("#arv-styles .bit-aiasv-mrk").First.RectAsync();
        var ltrBody = await Page.Locator("#arv-styles .bit-aiasv-bdy").First.RectAsync();
        Assert.IsTrue(ltrMarker.Left < ltrBody.Left, "Left to right, the marker of a step is on its left.");
    }

    [TestMethod]
    public async Task ForcedColors_TheQuestion_KeepsAnEdgeOfItsOwn()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        var prompt = Run("arv-rtl").Locator(".bit-aiarv-prm");
        await Expect(prompt).ToHaveCSSAsync("border-top-style", "solid");
        await Expect(prompt).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("CanvasText"));
    }

    [TestMethod]
    public async Task Glyph_TheHeaderIcon_IsDrawnFromTheAiFont()
    {
        await OpenAsync(Route);

        var icon = Page.Locator("#arv-manual .bit-aiarv-hic");
        await Expect(icon).ToBeVisibleAsync();
        await icon.AssertAiGlyphAsync("Glimmer");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon-ai"));
    }

    // ---------- helpers ----------

    // Waits until the steps of a run are in the given states, read off their classes all at once on every frame - a
    // state the transport holds for a few hundred milliseconds is caught, which a poll of each step in turn would miss.
    private async Task ExpectStepStatesAsync(string runId, params string[] states)
    {
        const string script =
            """
            ([id, expected]) => {
                const steps = [...document.querySelectorAll('#' + id + ' .bit-aiasv')];
                return steps.length === expected.length && steps.every((s, i) => s.classList.contains('bit-aiasv-' + expected[i]));
            }
            """;

        try
        {
            await Page.WaitForFunctionAsync(script, new object[] { runId, states }, new() { Timeout = DefaultTimeout });
        }
        catch (TimeoutException)
        {
            var actual = await Page.EvaluateAsync<string>(
                "id => [...document.querySelectorAll('#' + id + ' .bit-aiasv')].map(s => s.className).join(' | ')", runId);
            Assert.Fail($"The steps of {runId} never were [{string.Join(", ", states)}]; they are: {actual}");
        }
    }

    // A clock that has stopped shows the same time after several of its intervals have gone by.
    private async Task AssertStandsStillAsync(ILocator timer)
    {
        var stopped = await timer.TextContentAsync();
        await Page.WaitForTimeoutAsync(700);
        await Expect(timer).ToHaveTextAsync(stopped!);
    }
}
