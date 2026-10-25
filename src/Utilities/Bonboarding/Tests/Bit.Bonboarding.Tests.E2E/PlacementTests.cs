using Bit.Bonboarding.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bonboarding.Tests.E2E;

/// <summary>
/// Placement in real layouts: a target partly scrolled out of its pane, logical sides in both directions, and a host
/// whose fixed positioning is relative to a transformed ancestor rather than to the viewport.
/// </summary>
[TestClass]
public sealed class PlacementTests : BonboardingTest
{
    private static async Task<IPage> StartPlacementTourAsync(IPage page, string placement = "Auto")
    {
        await page.Locator("select").First.SelectOptionAsync(placement);
        await page.GetByRole(AriaRole.Button, new() { Name = "Run the tour" }).ClickAsync();
        await WaitForStepAsync(page, "Top left");
        return page;
    }

    [TestMethod]
    public async Task The_spotlight_covers_only_the_part_of_the_target_its_pane_shows()
    {
        var page = await StartPlacementTourAsync(await OpenAsync("/placement"));

        // Scroll the pane so the target is half above its top edge.
        await page.EvaluateAsync(
            """
            () => {
              const pane = document.querySelector('.playground');
              const target = document.querySelector("[data-bonboarding-anchor='spot-nw']");
              pane.scrollTop = target.offsetTop + target.offsetHeight / 2;
            }
            """);
        await page.WaitForTimeoutAsync(100);
        await WaitForSettledAsync(page);

        var pane = await page.EvaluateAsync<double[]>(
            "() => { const p = document.querySelector('.playground'); const r = p.getBoundingClientRect(); return [r.top + p.clientTop]; }");
        var target = await BoxOfAsync(page, "[data-bonboarding-anchor='spot-nw']");
        var spotlight = await BoxOfAsync(page, ".bit-bonboarding-spotlight");

        // The visible half, plus the 8 pixel padding on every side.
        AssertNear(pane[0] - 8, spotlight.Y, 1.5, "The spotlight's top edge");
        AssertNear((target.Y + target.Height) - pane[0] + 16, spotlight.Height, 1.5, "The spotlight's height");
    }

    [TestMethod]
    public async Task A_logical_side_is_on_the_right_in_a_left_to_right_page()
    {
        var page = await StartPlacementTourAsync(await OpenAsync("/placement"), "InlineEnd");

        Assert.AreEqual("right", await page.Locator(".bit-bonboarding-popover").GetAttributeAsync("data-bonboarding-side"));

        var target = await BoxOfAsync(page, "[data-bonboarding-anchor='spot-nw']");
        var popover = await BoxOfAsync(page, ".bit-bonboarding-popover");
        Assert.IsGreaterThan(target.X + target.Width, popover.X);
    }

    [TestMethod]
    public async Task A_host_inside_a_transformed_ancestor_still_frames_its_target()
    {
        var page = await OpenAsync("/");

        // A transform on the body makes it the containing block of every fixed element inside it - the host
        // included - so once the page scrolls, viewport coordinates and fixed coordinates part ways.
        await page.EvaluateAsync("() => { document.body.style.transform = 'translateZ(0)'; window.scrollTo(0, 160); }");
        await page.WaitForTimeoutAsync(100);

        await page.GetByRole(AriaRole.Button, new() { Name = "Take the tour" }).ClickAsync();
        await WaitForStepAsync(page, "Welcome aboard");
        await page.Keyboard.PressAsync("ArrowRight");
        await WaitForStepAsync(page, "Create a project");

        var target = await BoxOfAsync(page, "[data-bonboarding-anchor='new-project']");
        var spotlight = await BoxOfAsync(page, ".bit-bonboarding-spotlight");

        AssertNear(target.X - 8, spotlight.X, 1.5, "The spotlight's left edge");
        AssertNear(target.Y - 8, spotlight.Y, 1.5, "The spotlight's top edge");
    }
}
