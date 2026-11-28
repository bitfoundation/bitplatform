using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitSortable's rules, with real drags, on /e2e/sortable/rules: a column CanDrop refuses is marked as refusing the
/// card and a release there puts the card back (RevertOnSpill); a full column (MaxItems) does not take the drag at all;
/// a drop the server refuses (ConfirmDrop) slides back, and one it approves stays; a pipeline with MinItems="2" lets a
/// step go while it has three and keeps its last two. And on /e2e/sortable/accessibility: the items' buttons move them
/// without dragging (MoveAsync, MoveToAsync) and are announced, in the words Strings gives.
/// </summary>
[TestClass]
[E2ERetry]
[DoNotParallelize]
public class BitSortableRulesE2ETests : BitSortableE2ETestBase
{
    private const string Url = "/e2e/sortable/rules";

    [TestMethod]
    public async Task CanDrop_AColumnThatRefusesTheCard_ShowsItAndAReleaseTherePutsTheCardBack()
    {
        await OpenAsync(Url);

        await PressAsync(await CentreOfAsync("rules-todo", "Write the spec"));
        await Expect(Page.Locator("#rules-done")).ToHaveAttributeAsync("data-bit-srt-accepts", "");
        await MoveAsync((await BoxAsync(Item("rules-done", "Set up CI"))).At(0.5, 0.3));

        await Expect(Page.Locator("#rules-done")).ToHaveAttributeAsync("data-bit-srt-refused", "");
        await Expect(Page.Locator("#rules-done")).ToHaveCSSAsync("outline-color", "rgb(198, 40, 40)");
        await Expect(Placeholder("rules-done")).ToHaveCountAsync(0);
        await ReleaseAsync();

        await AssertOrderAsync("rules-todo", "rules-todo-order", "Write the spec", "Draw the icons", "Plan the launch");
        await AssertOrderAsync("rules-done", "rules-done-order", "Set up CI");
        await Expect(Page.Locator("[data-bit-srt-accepts], [data-bit-srt-over], [data-bit-srt-refused]")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task MaxItems_AFullColumn_TakesNoMoreCards()
    {
        await OpenAsync(Url);

        // In progress holds two of its three: one more fits - aimed at the upper part of Review the PR, which a card coming
        // from another list lands before in every engine, however many dragovers the way there fired.
        await DragAsync(await CentreOfAsync("rules-todo", "Write the spec"), (await BoxAsync(Item("rules-doing", "Review the PR"))).At(0.5, 0.3));
        await AssertOrderAsync("rules-doing", "rules-doing-order", "Build the API", "Write the spec", "Review the PR");

        // Now it is full: it does not even take the drag.
        await PressAsync(await CentreOfAsync("rules-todo", "Draw the icons"));
        await Expect(Page.Locator("#rules-todo")).ToHaveAttributeAsync("data-bit-srt-accepts", "");
        await Expect(Page.Locator("#rules-doing")).Not.ToHaveAttributeAsync("data-bit-srt-accepts", "");
        await MoveAsync(await CentreOfAsync("rules-doing", "Review the PR"));
        await Expect(Placeholder("rules-doing")).ToHaveCountAsync(0);
        await ReleaseAsync();

        await AssertOrderAsync("rules-doing", "rules-doing-order", "Build the API", "Write the spec", "Review the PR");
        await AssertOrderAsync("rules-todo", "rules-todo-order", "Draw the icons", "Plan the launch");
    }

    [TestMethod]
    public async Task ConfirmDrop_ADropTheServerRefuses_SlidesBack_AndOneItApprovesStays()
    {
        await OpenAsync(Url);
        await Page.Locator("#rules-approve").UncheckAsync();
        await WaitForQuietAsync();

        await DragAsync(await CentreOfAsync("rules-doing", "Build the API"), (await BoxAsync(Item("rules-done", "Set up CI"))).At(0.5, 0.3));
        await Expect(Page.Locator("#rules-status")).ToHaveTextAsync("refused Build the API");
        await AssertOrderAsync("rules-doing", "rules-doing-order", "Build the API", "Review the PR");
        await AssertOrderAsync("rules-done", "rules-done-order", "Set up CI");

        await Page.Locator("#rules-approve").CheckAsync();
        await WaitForQuietAsync();
        await DragAsync(await CentreOfAsync("rules-doing", "Review the PR"), (await BoxAsync(Item("rules-done", "Set up CI"))).At(0.5, 0.3));
        await Expect(Page.Locator("#rules-status")).ToHaveTextAsync("saved Review the PR");
        await AssertOrderAsync("rules-doing", "rules-doing-order", "Build the API");
        // Before the card it was aimed at: Set up CI is pushed down.
        await AssertOrderAsync("rules-done", "rules-done-order", "Review the PR", "Set up CI");
        await Expect(Page.Locator("#rules-last")).ToHaveTextAsync("Review the PR: rules-doing -> rules-done#0");
    }

    [TestMethod]
    public async Task MinItems_ThePipeline_KeepsItsTwoSteps()
    {
        await OpenAsync(Url);

        // Three steps: one may go.
        await DragAsync(await CentreOfAsync("steps-pipeline", "Deploy"), (await ListBoxAsync("steps-shelf")).At(0.5, 0.95));
        await Expect(Page.Locator("#steps-order")).ToHaveTextAsync("Build → Test");
        await AssertLabelsAsync("steps-shelf", ["Lint", "Benchmark", "Deploy"]);

        // Two steps: the shelf does not take a third away.
        await PressAsync(await CentreOfAsync("steps-pipeline", "Test"));
        await MoveAsync(await CentreOfAsync("steps-shelf", "Lint"));
        await Expect(Placeholder("steps-shelf")).ToHaveCountAsync(0);
        await ReleaseAsync();
        await Expect(Page.Locator("#steps-order")).ToHaveTextAsync("Build → Test");
        await AssertLabelsAsync("steps-shelf", ["Lint", "Benchmark", "Deploy"]);

        // A step added from the shelf lets one leave again.
        await DragAsync(await CentreOfAsync("steps-shelf", "Lint"), (await BoxAsync(Item("steps-pipeline", "Build"))).At(0.5, 0.3));
        await Expect(Page.Locator("#steps-order")).ToHaveTextAsync("Lint → Build → Test");
        await DragAsync(await CentreOfAsync("steps-pipeline", "Test"), (await ListBoxAsync("steps-shelf")).At(0.5, 0.95));
        await Expect(Page.Locator("#steps-order")).ToHaveTextAsync("Lint → Build");
    }

    [TestMethod]
    public async Task MoveAsync_TheButtons_MoveItemsWithoutDraggingAndSaySo()
    {
        await OpenAsync("/e2e/sortable/accessibility");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Move Search down" }).ClickAsync();
        await WaitForQuietAsync();
        await Expect(Page.Locator("#a11y-sprint-order")).ToHaveTextAsync("Export to CSV, Search");
        await Expect(Page.Locator("[data-bit-srt-live]")).ToHaveTextAsync("Search moved to position 2 of 2 in Sprint.");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Move Audit log to the Sprint" }).ClickAsync();
        await WaitForQuietAsync();
        await Expect(Page.Locator("#a11y-sprint-order")).ToHaveTextAsync("Export to CSV, Search, Audit log");
        await Expect(Page.Locator("#a11y-backlog-order")).ToHaveTextAsync("Sign-up page, Password reset, Dark mode");
        await Expect(Page.Locator("#a11y-log")).ToContainTextAsync("[api] Audit log: a11y-backlog -> a11y-sprint#2");
    }

    [TestMethod]
    public async Task Strings_TheAnnouncements_AreSpokenInTheGivenWords()
    {
        await OpenAsync("/e2e/sortable/accessibility");
        await Page.Locator("#a11y-german").ClickAsync();
        await Expect(Page.Locator("#a11y-german")).ToHaveAttributeAsync("aria-pressed", "true");
        // The items show their move buttons beside their names, so they are found by the name they contain.
        var audit = Page.Locator("#a11y-backlog > [data-bit-srt-item]").Filter(new() { HasText = "Audit log" });
        await Expect(audit).ToHaveAttributeAsync("aria-roledescription", "sortierbares Element");

        await audit.FocusAsync();
        await PressKeyAsync(" ");
        await Expect(Page.Locator("[data-bit-srt-live]")).ToHaveTextAsync("Audit log aufgenommen. Position 3 von 4 in Backlog.");
        await PressKeyAsync("ArrowUp");
        await Expect(Page.Locator("[data-bit-srt-live]")).ToHaveTextAsync("Audit log auf Position 2 von 4 in Backlog verschoben.");
        await PressKeyAsync(" ");

        await Expect(Page.Locator("#a11y-backlog-order")).ToHaveTextAsync("Sign-up page, Audit log, Password reset, Dark mode");
        await Expect(Page.Locator("#a11y-heard")).ToContainTextAsync("Audit log auf Position 2 von 4 in Backlog abgelegt.");
        await Expect(Page.Locator("#a11y-log")).ToContainTextAsync(new Regex(@"\[keyboard\] Audit log: #2 -> #1"));
    }

    [TestMethod]
    public async Task Axe_EachSection_HasNoWcagAOrAAViolation()
    {
        await OpenAsync(Url);
        await AssertNoAxeViolationsAsync("#rules");
        await AssertNoAxeViolationsAsync("#steps");

        await OpenAsync("/e2e/sortable/accessibility");
        await AssertNoAxeViolationsAsync("#a11y");
    }
}
