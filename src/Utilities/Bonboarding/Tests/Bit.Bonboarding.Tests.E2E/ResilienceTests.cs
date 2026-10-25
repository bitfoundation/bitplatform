using Bit.Bonboarding.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bonboarding.Tests.E2E;

/// <summary>
/// What a real page does to a tour: elements that render late, that are hidden or hidden again, that the user
/// has to reveal themselves, and steps that wait for the user to act. Everything here runs through
/// bit-bonboarding.ts, which no .NET test executes.
/// </summary>
[TestClass]
public sealed class ResilienceTests : BonboardingTest
{
    [TestMethod]
    public async Task A_step_waits_for_a_panel_the_user_reveals_while_it_waits()
    {
        var page = await OpenAsync("/dynamic");
        await page.GetByRole(AriaRole.Button, new() { Name = "Run the waiting tour" }).ClickAsync();
        await WaitForStepAsync(page, "Reveal the panel");

        await page.Locator("[data-bonboarding-action='next']").ClickAsync();
        await Assertions.Expect(page.Locator(".bit-bonboarding-waiting")).ToBeVisibleAsync();

        // Nothing blocks the page while the step waits, so the user can make its element appear.
        Assert.AreEqual(0, await page.Locator(".bit-bonboarding-blocker").CountAsync());
        await page.Locator("[data-bonboarding-anchor='reveal']").ClickAsync();

        await WaitForStepAsync(page, "There it is");

        var panel = await BoxOfAsync(page, "[data-bonboarding-anchor='late-panel']");
        var spotlight = await BoxOfAsync(page, ".bit-bonboarding-spotlight");
        AssertNear(panel.Y - 8, spotlight.Y, 1.5, "The spotlight's top edge");

        // The step's own interaction mode is back with its target: the page is blocked again.
        Assert.IsGreaterThan(0, await page.Locator(".bit-bonboarding-blocker").CountAsync());
        AssertNoConsoleErrors("the waiting tour");
    }

    [TestMethod]
    public async Task The_keyboard_is_not_trapped_while_a_step_waits()
    {
        var page = await OpenAsync("/dynamic");
        await page.GetByRole(AriaRole.Button, new() { Name = "Run the waiting tour" }).ClickAsync();
        await WaitForStepAsync(page, "Reveal the panel");
        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(page.Locator(".bit-bonboarding-waiting")).ToBeVisibleAsync();

        // The reveal button is reachable from the keyboard and works from it.
        await page.Locator("[data-bonboarding-anchor='reveal']").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await page.Keyboard.PressAsync("Shift+Tab");
        StringAssert.Contains(await FocusedAsync(page), "Reveal the panel");
        await page.Keyboard.PressAsync("Enter");

        await WaitForStepAsync(page, "There it is");
    }

    [TestMethod]
    public async Task The_waiting_card_can_be_closed()
    {
        var page = await OpenAsync("/dynamic");
        await page.GetByRole(AriaRole.Button, new() { Name = "Wait, then skip" }).ClickAsync();

        var close = page.Locator(".bit-bonboarding-waiting [data-bonboarding-action='close']");
        await Assertions.Expect(close).ToBeVisibleAsync();
        await close.ClickAsync();

        await Assertions.Expect(page.Locator(".bit-bonboarding-root")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task A_hidden_target_is_waited_for_until_it_shows()
    {
        var page = await OpenAsync("/");
        await page.GetByRole(AriaRole.Button, new() { Name = "Take the tour" }).ClickAsync();
        await WaitForStepAsync(page, "Welcome aboard");

        // The next step's element is in the DOM but not rendered, as inside a closed menu.
        await page.EvaluateAsync("() => document.querySelector(\"[data-bonboarding-anchor='new-project']\").style.display = 'none'");
        await page.Locator("[data-bonboarding-action='next']").ClickAsync();

        await Assertions.Expect(page.Locator(".bit-bonboarding-waiting")).ToBeVisibleAsync();
        Assert.AreEqual(0, await page.Locator(".bit-bonboarding-spotlight").CountAsync(), "There is nothing to put a spotlight around.");

        await page.EvaluateAsync("() => document.querySelector(\"[data-bonboarding-anchor='new-project']\").style.display = ''");
        await WaitForStepAsync(page, "Create a project");

        var target = await BoxOfAsync(page, "[data-bonboarding-anchor='new-project']");
        var spotlight = await BoxOfAsync(page, ".bit-bonboarding-spotlight");
        AssertNear(target.X - 8, spotlight.X, 1.5, "The spotlight's left edge");
    }

    [TestMethod]
    public async Task A_target_hidden_mid_step_is_waited_for_and_found_again()
    {
        var page = await OpenAsync("/");
        await page.GetByRole(AriaRole.Button, new() { Name = "Take the tour" }).ClickAsync();
        await WaitForStepAsync(page, "Welcome aboard");
        await page.Keyboard.PressAsync("ArrowRight");
        await WaitForStepAsync(page, "Create a project");

        // A panel collapsing, say: the element stays in the DOM, its box goes.
        await page.EvaluateAsync("() => document.querySelector(\"[data-bonboarding-anchor='new-project']\").style.visibility = 'hidden'");
        await Assertions.Expect(page.Locator(".bit-bonboarding-waiting")).ToBeVisibleAsync();

        await page.EvaluateAsync("() => document.querySelector(\"[data-bonboarding-anchor='new-project']\").style.visibility = ''");
        await WaitForStepAsync(page, "Create a project");
        Assert.AreEqual(1, await page.Locator(".bit-bonboarding-spotlight").CountAsync());
    }

    [TestMethod]
    public async Task The_spotlight_follows_a_target_that_moves_with_nothing_resized()
    {
        var page = await OpenAsync("/");

        // A fixed-height pane: content inserted above the target moves it without resizing the target, the
        // pane or the document, so no resize observer can report it.
        await page.EvaluateAsync("() => { const pane = document.querySelector('.app-window'); pane.style.height = pane.offsetHeight + 'px'; pane.style.overflow = 'hidden'; }");

        await page.GetByRole(AriaRole.Button, new() { Name = "Take the tour" }).ClickAsync();
        await WaitForStepAsync(page, "Welcome aboard");
        await page.Keyboard.PressAsync("ArrowRight");
        await WaitForStepAsync(page, "Create a project");

        var before = await BoxOfAsync(page, ".bit-bonboarding-spotlight");

        await page.EvaluateAsync(
            """
            () => {
              const banner = document.createElement('div');
              banner.style.height = '60px';
              const pane = document.querySelector('.app-window');
              pane.insertBefore(banner, pane.firstChild);
            }
            """);

        await page.WaitForFunctionAsync(
            $"() => Math.abs(document.querySelector('.bit-bonboarding-spotlight').getBoundingClientRect().y - {before.Y + 60}) < 1.5",
            null, new PageWaitForFunctionOptions { Timeout = 5_000 });

        var target = await BoxOfAsync(page, "[data-bonboarding-anchor='new-project']");
        await WaitForSettledAsync(page);
        var spotlight = await BoxOfAsync(page, ".bit-bonboarding-spotlight");
        AssertNear(target.Y - 8, spotlight.Y, 1.5, "The spotlight's top edge after the move");
    }

    [TestMethod]
    public async Task A_selector_with_a_hidden_first_match_points_at_the_visible_one()
    {
        var page = await OpenAsync("/");

        // A responsive layout's two copies of one control: the first is the hidden desktop copy.
        await page.EvaluateAsync(
            """
            () => {
              const original = document.querySelector("[data-bonboarding-anchor='new-project']");
              const hidden = original.cloneNode(true);
              hidden.style.display = 'none';
              original.parentElement.insertBefore(hidden, original);
            }
            """);

        await page.GetByRole(AriaRole.Button, new() { Name = "Take the tour" }).ClickAsync();
        await WaitForStepAsync(page, "Welcome aboard");
        await page.Keyboard.PressAsync("ArrowRight");
        await WaitForStepAsync(page, "Create a project");

        var visible =await page.Locator("[data-bonboarding-anchor='new-project']:visible").BoundingBoxAsync();
        var spotlight = await BoxOfAsync(page, ".bit-bonboarding-spotlight");
        AssertNear(visible!.X - 8, spotlight.X, 1.5, "The spotlight's left edge");
        AssertNear(visible.Width + 16, spotlight.Width, 1.5, "The spotlight's width");
    }

    [TestMethod]
    public async Task An_interactive_step_moves_on_when_the_user_uses_the_real_control()
    {
        var page = await OpenAsync("/dynamic");
        await page.GetByRole(AriaRole.Button, new() { Name = "Run the interactive tour" }).ClickAsync();
        await WaitForStepAsync(page, "Try it yourself");
        await page.Locator("[data-bonboarding-action='next']").ClickAsync();
        await WaitForStepAsync(page, "Now click this button");

        // The spotlight hole is click-through; the control itself is clicked, not a tour button.
        await page.Locator("[data-bonboarding-anchor='do-it']").ClickAsync();

        await WaitForStepAsync(page, "Nicely done");
        await Assertions.Expect(page.Locator("[data-bonboarding-anchor='do-it']")).ToContainTextAsync("Clicked 1 time");
    }
}
