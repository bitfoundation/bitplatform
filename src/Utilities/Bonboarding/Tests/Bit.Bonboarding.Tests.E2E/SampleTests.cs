using Bit.Bonboarding.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bonboarding.Tests.E2E;

/// <summary>
/// The samples the other suites do not drive, each doing what its page says it does: the three ways of naming a
/// target, one spotlight around several elements, a step that advances on a change event, a filtered click or an
/// application signal, a target that is replaced or taken away, a tour gated by <c>CanStart</c>, a checklist item for
/// administrators ticked from another page, a beacon that runs a handler, and a step recomposed or drawn by the page.
/// </summary>
[TestClass]
public sealed class SampleTests : BonboardingTest
{
    private static Task NextAsync(IPage page) => page.Locator("[data-bonboarding-action='next']").ClickAsync();

    [TestMethod]
    public async Task A_target_is_found_by_anchor_by_selector_and_by_element_reference()
    {
        var page = await OpenAsync("/getting-started");

        await page.GetByRole(AriaRole.Button, new() { Name = "Run the targeting tour" }).ClickAsync();
        await WaitForStepAsync(page, "Found by its anchor");
        await NextAsync(page);
        await WaitForStepAsync(page, "Found by a CSS selector");
        await NextAsync(page);
        await WaitForStepAsync(page, "Found by an element reference");

        var target = await page.GetByRole(AriaRole.Button, new() { Name = "By element reference" }).BoundingBoxAsync();
        var spotlight = await BoxOfAsync(page, ".bit-bonboarding-spotlight");
        Assert.IsNotNull(target);
        AssertNear(target.X - 8, spotlight.X, 1.5, "The spotlight's left edge");
        AssertNear(target.Width + 16, spotlight.Width, 1.5, "The spotlight's width");

        await NextAsync(page);
        await WaitForStepAsync(page, "And no element at all");
        Assert.AreEqual(0, await page.Locator(".bit-bonboarding-spotlight").CountAsync(), "A step with no target has no spotlight.");
    }

    [TestMethod]
    public async Task Additional_targets_share_one_spotlight()
    {
        var page = await OpenAsync("/placement");

        await page.GetByRole(AriaRole.Button, new() { Name = "Spotlight the editing controls" }).ClickAsync();
        await WaitForStepAsync(page, "Editing controls");

        var save = await BoxOfAsync(page, "[data-bonboarding-anchor='editor-save']");
        var redo = await BoxOfAsync(page, "[data-bonboarding-anchor='editor-redo']");
        var spotlight = await BoxOfAsync(page, ".bit-bonboarding-spotlight");

        AssertNear(save.X - 8, spotlight.X, 1.5, "The spotlight's left edge");
        AssertNear(redo.X + redo.Width + 8, spotlight.X + spotlight.Width, 1.5, "The spotlight's right edge");
    }

    [TestMethod]
    public async Task A_step_advances_on_a_change_event_and_on_a_filtered_click()
    {
        var page = await OpenAsync("/dynamic");

        await page.GetByRole(AriaRole.Button, new() { Name = "Run the event tour" }).ClickAsync();
        await WaitForStepAsync(page, "Pick a plan");

        await page.Locator("[data-bonboarding-anchor='plan-picker']").SelectOptionAsync("team");
        await WaitForStepAsync(page, "Now choose Large");

        // MatchSelector lets only Large through.
        await page.Locator("[data-size='small']").ClickAsync();
        await page.WaitForTimeoutAsync(600);
        await WaitForStepAsync(page, "Now choose Large");

        await page.Locator("[data-size='large']").ClickAsync();
        await WaitForStepAsync(page, "Two kinds of trigger");
    }

    [TestMethod]
    public async Task A_step_advances_when_the_application_signals_not_when_the_button_is_clicked()
    {
        var page = await OpenAsync("/dynamic");

        await page.GetByRole(AriaRole.Button, new() { Name = "Run the signal tour" }).ClickAsync();
        await WaitForStepAsync(page, "Create a project");

        await page.Locator("[data-bonboarding-anchor='create-project']").ClickAsync();
        await WaitForStepAsync(page, "It exists now");
        await Assertions.Expect(page.GetByText("SignalAsync returned true")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task A_replaced_target_is_followed_and_a_removed_one_is_waited_for()
    {
        var page = await OpenAsync("/dynamic");
        const string Card = "[data-bonboarding-anchor='rerender-card']";

        await page.GetByRole(AriaRole.Button, new() { Name = "Run the re-render tour" }).ClickAsync();
        await WaitForStepAsync(page, "Replace it, or take it away");
        var before = await BoxOfAsync(page, Card);

        // A new element, on the other side of its lane: the step re-binds and follows it.
        await page.GetByRole(AriaRole.Button, new() { Name = "Replace this element" }).ClickAsync();
        await Assertions.Expect(page.Locator(Card)).ToContainTextAsync("Element #2");
        await WaitForSettledAsync(page);

        var after = await BoxOfAsync(page, Card);
        var spotlight = await BoxOfAsync(page, ".bit-bonboarding-spotlight");
        Assert.IsGreaterThan(before.X + 50, after.X, "The replacement was rendered somewhere else.");
        AssertNear(after.X - 8, spotlight.X, 1.5, "The spotlight's left edge, on the new element");

        // Nothing to re-bind to: the step waits, with the page usable, and comes back with the element.
        await page.GetByRole(AriaRole.Button, new() { Name = "Remove it for two seconds" }).ClickAsync();
        await Assertions.Expect(page.Locator(".bit-bonboarding-waiting")).ToBeVisibleAsync();
        Assert.AreEqual(0, await page.Locator(".bit-bonboarding-blocker").CountAsync());

        await WaitForStepAsync(page, "Replace it, or take it away");
        AssertNoConsoleErrors("the re-render tour");
    }

    [TestMethod]
    public async Task CanStart_refuses_a_viewer_and_Force_starts_the_tour_anyway()
    {
        var page = await OpenAsync("/flows");
        var result = page.Locator(".bb-note[role='status']");

        await page.GetByRole(AriaRole.Button, new() { Name = "Start the members' tour" }).ClickAsync();
        await Assertions.Expect(result).ToContainTextAsync("BonboardingException");
        Assert.IsTrue(await HostIsEmptyAsync(page), "CanStart said no, yet the tour started.");

        await page.GetByRole(AriaRole.Button, new() { Name = "Start it with Force" }).ClickAsync();
        await WaitForStepAsync(page, "For members");
    }

    [TestMethod]
    public async Task An_administrators_task_is_listed_for_administrators_and_ticked_off_from_another_page()
    {
        var page = await OpenAsync("/guidance");
        var checklist = page.Locator(".bit-bonboarding-checklist");
        var administrator = page.GetByRole(AriaRole.Checkbox, new() { Name = "Show the administrator's task" });

        await Assertions.Expect(checklist.Locator(".bit-bonboarding-checklist-count")).ToHaveTextAsync("0 of 3 done");
        await administrator.CheckAsync();
        await Assertions.Expect(checklist.Locator(".bit-bonboarding-checklist-count")).ToHaveTextAsync("0 of 4 done");
        await Assertions.Expect(checklist.Locator("[data-bonboarding-item='api-keys']")).ToHaveAttributeAsync("data-bonboarding-state", "todo");

        // The settings page ticks it off as it renders, with SetChecklistItemAsync: no checklist on screen there.
        await GotoAsync(page, "/workspace/settings");
        await WaitForInteractiveAsync(page);
        await page.WaitForFunctionAsync(
            "() => (localStorage.getItem('bit-bonboarding-demo:checklist:demo-getting-started') || '').includes('api-keys')",
            null, new PageWaitForFunctionOptions { Timeout = 10_000 });

        await GotoAsync(page, "/guidance");
        await WaitForInteractiveAsync(page);
        await administrator.CheckAsync();
        await Assertions.Expect(checklist.Locator("[data-bonboarding-item='api-keys']")).ToHaveAttributeAsync("data-bonboarding-state", "done");

        // And straight from the page's own code, both ways.
        await page.GetByRole(AriaRole.Button, new() { Name = "Tick the search task off" }).ClickAsync();
        await Assertions.Expect(checklist.Locator("[data-bonboarding-item='search']")).ToHaveAttributeAsync("data-bonboarding-state", "done");
        await page.GetByRole(AriaRole.Button, new() { Name = "Untick it" }).ClickAsync();
        await Assertions.Expect(checklist.Locator("[data-bonboarding-item='search']")).ToHaveAttributeAsync("data-bonboarding-state", "todo");
    }

    [TestMethod]
    public async Task A_beacon_can_run_a_handler_of_the_page()
    {
        var page = await OpenAsync("/guidance");
        var beacon = page.Locator("button.bit-bonboarding-beacon[aria-label=\"What's new\"]");

        await Assertions.Expect(beacon).ToHaveAttributeAsync("data-bonboarding-beacon-state", "visible");
        await beacon.ClickAsync();
        await Assertions.Expect(page.GetByText("OnClick ran once")).ToBeVisibleAsync();
        Assert.IsTrue(await HostIsEmptyAsync(page), "A beacon with OnClick opened a hint as well.");
    }

    [TestMethod]
    public async Task A_step_can_be_recomposed_and_a_page_can_draw_the_whole_card_once()
    {
        var page = await OpenAsync("/headless");

        await page.GetByRole(AriaRole.Button, new() { Name = "Run the recomposed tour" }).ClickAsync();
        var plan = await WaitForStepAsync(page, "Choose a plan");

        // The body's own button moves the tour on.
        await plan.GetByRole(AriaRole.Button, new() { Name = "Team", Exact = true }).ClickAsync();
        var summary = await WaitForStepAsync(page, "A header of its own");
        await Assertions.Expect(summary).ToContainTextAsync("Team");
        Assert.AreEqual(1, await page.Locator(".bit-bonboarding-popover").CountAsync(), "The tour was drawn by more than one host.");

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(page.Locator(".bit-bonboarding-root")).ToHaveCountAsync(0);

        // The page's own host claims its session: its card, and none of the library's.
        await page.GetByRole(AriaRole.Button, new() { Name = "Run the custom-rendered tour" }).ClickAsync();
        await Assertions.Expect(page.Locator(".custom-card")).ToContainTextAsync("Completely custom");
        await Assertions.Expect(page.Locator(".bit-bonboarding-popover")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("svg.custom-overlay")).ToHaveCountAsync(1);

        AssertNoConsoleErrors("the headless page");
    }
}
