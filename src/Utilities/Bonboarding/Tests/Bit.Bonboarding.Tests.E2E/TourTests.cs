using Bit.Bonboarding.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bonboarding.Tests.E2E;

/// <summary>
/// The welcome tour of the home page, driven the way a person drives it: the spotlight lands on the element each
/// step names, the popover sits where it was asked to, the keyboard moves the tour, focus is managed, and the
/// page is handed back exactly as it was found.
/// </summary>
[TestClass]
public sealed class TourTests : BonboardingTest
{
    private async Task<IPage> StartWelcomeTourAsync()
    {
        var page = await OpenAsync("/");
        await page.GetByRole(AriaRole.Button, new() { Name = "Take the tour" }).ClickAsync();
        await WaitForStepAsync(page, "Welcome aboard");
        return page;
    }

    [TestMethod]
    public async Task The_spotlight_frames_each_target_and_the_popover_sits_on_the_side_asked_for()
    {
        var page = await StartWelcomeTourAsync();

        // The first step has no target: a centred card, no spotlight.
        Assert.AreEqual(0, await page.Locator(".bit-bonboarding-spotlight").CountAsync());
        var card = await BoxOfAsync(page, ".bit-bonboarding-popover");
        AssertNear(1280 / 2.0, card.X + (card.Width / 2), 2, "The centred card's horizontal centre");

        await page.Locator("[data-bonboarding-action='next']").ClickAsync();
        await WaitForStepAsync(page, "Create a project");

        var target = await BoxOfAsync(page, "[data-bonboarding-anchor='new-project']");
        var spotlight = await BoxOfAsync(page, ".bit-bonboarding-spotlight");
        var popover = await BoxOfAsync(page, ".bit-bonboarding-popover");

        // The default spotlight padding is 8 pixels on every side.
        AssertNear(target.X - 8, spotlight.X, 1.5, "The spotlight's left edge");
        AssertNear(target.Y - 8, spotlight.Y, 1.5, "The spotlight's top edge");
        AssertNear(target.Width + 16, spotlight.Width, 1.5, "The spotlight's width");
        AssertNear(target.Height + 16, spotlight.Height, 1.5, "The spotlight's height");

        // Placement = Bottom: below the spotlight, a 12 pixel offset away, with its arrow pointing up.
        AssertNear(spotlight.Y + spotlight.Height + 12, popover.Y, 1.5, "The popover's top edge");
        Assert.AreEqual("bottom", await page.Locator(".bit-bonboarding-popover").GetAttributeAsync("data-bonboarding-side"));
        Assert.AreEqual(1, await page.Locator(".bit-bonboarding-arrow").CountAsync());

        AssertNoConsoleErrors("the welcome tour");
    }

    [TestMethod]
    public async Task The_arrow_keys_move_the_tour_and_escape_hands_focus_back()
    {
        var page = await StartWelcomeTourAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await WaitForStepAsync(page, "Create a project");

        await page.Keyboard.PressAsync("ArrowRight");
        await WaitForStepAsync(page, "Find anything");

        await page.Keyboard.PressAsync("ArrowLeft");
        await WaitForStepAsync(page, "Create a project");

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(page.Locator(".bit-bonboarding-root")).ToHaveCountAsync(0);

        // Focus goes back to the control that started the tour, so a keyboard user carries on from there.
        StringAssert.Contains(await FocusedAsync(page), "Take the tour");
    }

    [TestMethod]
    public async Task Focus_moves_into_each_step_and_tab_cannot_leave_a_modal_step()
    {
        var page = await StartWelcomeTourAsync();
        await page.Locator("[data-bonboarding-action='next']").ClickAsync();
        await WaitForStepAsync(page, "Create a project");

        // The primary button takes focus when a step is shown.
        StringAssert.Contains(await FocusedAsync(page), "Next");

        for (var i = 0; i < 8; i++)
        {
            await page.Keyboard.PressAsync(i % 3 == 2 ? "Shift+Tab" : "Tab");
            var inside = await page.EvaluateAsync<bool>("() => !!document.activeElement?.closest('.bit-bonboarding-popover')");
            Assert.IsTrue(inside, $"Tab press {i + 1} took focus out of a modal step, to {await FocusedAsync(page)}.");
        }
    }

    [TestMethod]
    public async Task Each_step_is_announced_politely()
    {
        var page = await StartWelcomeTourAsync();

        var live = page.Locator(".bit-bonboarding-live");
        Assert.AreEqual("polite", await live.GetAttributeAsync("aria-live"));
        await Assertions.Expect(live).ToContainTextAsync("Step 1 of 6");

        await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(live).ToContainTextAsync("Step 2 of 6");
        await Assertions.Expect(live).ToContainTextAsync("Create a project");
    }

    [TestMethod]
    public async Task Finishing_the_tour_leaves_nothing_behind()
    {
        var page = await StartWelcomeTourAsync();

        for (var i = 0; i < 6; i++)
        {
            await page.Locator("[data-bonboarding-action='next']").ClickAsync();
        }

        await Assertions.Expect(page.Locator(".bit-bonboarding-root")).ToHaveCountAsync(0);

        // The page is the page again: nothing intercepts a click on what the tour pointed at.
        var target = await BoxOfAsync(page, "[data-bonboarding-anchor='new-project']");
        var hit = await page.EvaluateAsync<string>(
            $"() => document.elementFromPoint({target.X + (target.Width / 2)}, {target.Y + (target.Height / 2)})?.closest('[data-bonboarding-anchor]')?.getAttribute('data-bonboarding-anchor') ?? ''");
        Assert.AreEqual("new-project", hit);

        AssertNoConsoleErrors("the finished welcome tour");
    }
}
