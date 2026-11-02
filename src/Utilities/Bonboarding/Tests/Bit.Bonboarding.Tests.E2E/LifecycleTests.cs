using Bit.Bonboarding.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bonboarding.Tests.E2E;

/// <summary>
/// The lifecycle page: a session driven from the page's own buttons, tours queued behind each other by priority, a
/// "remind me later" that holds an announcement back, hooks that unlock a later step, and a broken definition that is
/// listed and refused.
/// </summary>
[TestClass]
public sealed class LifecycleTests : BonboardingTest
{
    private static ILocator Command(IPage page, string name) =>
        page.GetByRole(AriaRole.Group, new() { Name = "Session commands" }).GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    [TestMethod]
    public async Task A_session_is_driven_from_the_page_and_says_where_it_stands()
    {
        var page = await OpenAsync("/lifecycle");
        var state = page.Locator("dl.bb-params");

        await page.GetByRole(AriaRole.Button, new() { Name = "Start the session" }).ClickAsync();
        await WaitForStepAsync(page, "Driven from code");

        // Interaction = Free: nothing blocks the page, so its buttons can drive the card.
        Assert.AreEqual(0, await page.Locator(".bit-bonboarding-blocker").CountAsync());

        await Command(page, "Next").ClickAsync();
        await WaitForStepAsync(page, "Second step");

        await Command(page, "Pause").ClickAsync();
        await Assertions.Expect(page.Locator(".bit-bonboarding-root")).ToHaveCountAsync(0);
        await Assertions.Expect(state).ToContainTextAsync("Paused");

        await Command(page, "Resume").ClickAsync();
        await WaitForStepAsync(page, "Second step");

        await Command(page, "Go to \"settings\"").ClickAsync();
        await WaitForStepAsync(page, "Fourth step");
        await Assertions.Expect(state).ToContainTextAsync("create → search → settings");

        // Back follows the visit history: to the step the jump was made from.
        await Command(page, "Previous").ClickAsync();
        await WaitForStepAsync(page, "Second step");

        await Command(page, "Complete").ClickAsync();
        await Assertions.Expect(page.Locator(".bit-bonboarding-root")).ToHaveCountAsync(0);
        await Assertions.Expect(state).ToContainTextAsync("Completed");

        await page.GetByRole(AriaRole.Button, new() { Name = "Start at the third step" }).ClickAsync();
        await WaitForStepAsync(page, "Third step");

        AssertNoConsoleErrors("the driven tour");
    }

    [TestMethod]
    public async Task Queued_tours_wait_their_turn_highest_priority_first()
    {
        var page = await OpenAsync("/lifecycle");
        var log = page.GetByRole(AriaRole.Log, new() { Name = "Queue events" });

        await page.GetByRole(AriaRole.Button, new() { Name = "Start the first tour" }).ClickAsync();
        await WaitForStepAsync(page, "The first tour");

        await page.GetByRole(AriaRole.Button, new() { Name = "Queue a priority 0 tour" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Queue a priority 5 tour" }).ClickAsync();
        await Assertions.Expect(log).ToContainTextAsync("TourQueued");

        await page.GetByRole(AriaRole.Button, new() { Name = "Start another only if nothing runs" }).ClickAsync();
        await Assertions.Expect(log).ToContainTextAsync("Skipped");

        // Still the first tour: neither queued start took the screen from it.
        await WaitForStepAsync(page, "The first tour");

        // Queued second, but the higher priority goes first.
        await page.Locator("[data-bonboarding-action='next']").ClickAsync();
        await WaitForStepAsync(page, "The priority 5 tour");

        await page.Locator("[data-bonboarding-action='next']").ClickAsync();
        await WaitForStepAsync(page, "The priority 0 tour");
    }

    [TestMethod]
    public async Task Remind_me_later_holds_the_announcement_back_from_StartOnceAsync_only()
    {
        var page = await OpenAsync("/lifecycle");
        var state = page.Locator(".bb-note[role='status']");

        await page.GetByRole(AriaRole.Button, new() { Name = "Offer the announcement" }).ClickAsync();
        var step = await WaitForStepAsync(page, "Three new things");

        await step.GetByRole(AriaRole.Button, new() { Name = "Remind me in 30 seconds" }).ClickAsync();
        await Assertions.Expect(page.Locator(".bit-bonboarding-root")).ToHaveCountAsync(0);
        await Assertions.Expect(state).ToContainTextAsync("Snoozed until");

        await page.GetByRole(AriaRole.Button, new() { Name = "Offer the announcement" }).ClickAsync();
        await Assertions.Expect(state).ToContainTextAsync("Not offered: snoozed until");
        Assert.IsTrue(await HostIsEmptyAsync(page), "A snoozed announcement was offered again.");

        // A start the user asks for is never held back by a snooze.
        await page.GetByRole(AriaRole.Button, new() { Name = "Show it anyway" }).ClickAsync();
        await WaitForStepAsync(page, "Three new things");
    }

    [TestMethod]
    public async Task Hooks_run_and_an_answer_written_to_the_state_unlocks_a_later_step()
    {
        var page = await OpenAsync("/lifecycle");
        var log = page.GetByRole(AriaRole.Log, new() { Name = "Hook calls" });

        await page.GetByRole(AriaRole.Button, new() { Name = "Run the hooked tour" }).ClickAsync();
        await WaitForStepAsync(page, "Loaded before it was shown");

        foreach (var hook in new[] { "OnStarted", "OnBeforeShow", "OnShown", "OnStepChanged" })
        {
            await Assertions.Expect(log).ToContainTextAsync(hook);
        }

        await page.Locator("[data-bonboarding-action='next']").ClickAsync();
        var connect = await WaitForStepAsync(page, "Connect a repository?");
        Assert.AreEqual("2 of 3", await connect.Locator(".bit-bonboarding-progress-text").TextContentAsync());

        // The answer goes into State, and the next step's When reads it: the tour grows by a step.
        await connect.GetByRole(AriaRole.Button, new() { Name = "Connect", Exact = true }).ClickAsync();
        var deploy = await WaitForStepAsync(page, "Deploy it");
        Assert.AreEqual("3 of 4", await deploy.Locator(".bit-bonboarding-progress-text").TextContentAsync());
        await Assertions.Expect(log).ToContainTextAsync("OnBeforeLeave");

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(log).ToContainTextAsync("OnEnded");

        AssertNoConsoleErrors("the hooked tour");
    }

    [TestMethod]
    public async Task A_broken_definition_is_listed_by_Validate_and_refused_by_StartAsync()
    {
        var page = await OpenAsync("/lifecycle");

        await page.GetByRole(AriaRole.Button, new() { Name = "Validate the broken tour" }).ClickAsync();
        var issues = page.Locator("tbody");
        foreach (var code in new[] { "duplicate-step-id", "goto-unknown-step", "no-way-forward", "overlay-opacity-out-of-range" })
        {
            await Assertions.Expect(issues.GetByText(code, new() { Exact = true })).ToBeVisibleAsync();
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Try to start it" }).ClickAsync();
        await Assertions.Expect(page.Locator(".bb-note-danger[role='alert']")).ToContainTextAsync("is not valid");
        Assert.IsTrue(await HostIsEmptyAsync(page), "An invalid tour was shown.");
    }
}
