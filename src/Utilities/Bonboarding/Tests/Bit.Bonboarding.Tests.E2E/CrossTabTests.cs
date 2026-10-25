using Bit.Bonboarding.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bonboarding.Tests.E2E;

/// <summary>
/// The same site open in two tabs, which share localStorage: a decision taken in one is not undone by the other, and
/// progress made in one shows in the other.
/// </summary>
[TestClass]
public sealed class CrossTabTests : BonboardingTest
{
    private async Task<(IPage First, IPage Second)> OpenTwoTabsAsync(string path)
    {
        var context = await NewContextAsync(DefaultContextOptions());

        var first = await context.NewPageAsync();
        await GotoAsync(first, path);
        await WaitForInteractiveAsync(first);

        var second = await context.NewPageAsync();
        await GotoAsync(second, path);
        await WaitForInteractiveAsync(second);

        return (first, second);
    }

    [TestMethod]
    public async Task A_tour_dismissed_in_one_tab_stops_in_the_other()
    {
        var (first, second) = await OpenTwoTabsAsync("/");

        foreach (var page in new[] { first, second })
        {
            await page.BringToFrontAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Take the tour" }).ClickAsync();
            await WaitForStepAsync(page, "Welcome aboard");
        }

        await first.BringToFrontAsync();
        await first.Keyboard.PressAsync("Escape");
        await Assertions.Expect(first.Locator(".bit-bonboarding-root")).ToHaveCountAsync(0);

        // The other tab hears of it through the storage event and stops, without writing its own progress over it.
        await Assertions.Expect(second.Locator(".bit-bonboarding-root")).ToHaveCountAsync(0);

        var record = await second.EvaluateAsync<string>("() => localStorage.getItem('bit-bonboarding-demo:welcome')");
        StringAssert.Contains(record, "\"dismissed\":true");
    }

    [TestMethod]
    public async Task A_checklist_item_ticked_in_one_tab_is_ticked_in_the_other()
    {
        var (first, second) = await OpenTwoTabsAsync("/guidance");
        var item = "[data-bonboarding-item='search']";

        await Assertions.Expect(second.Locator(item)).ToHaveAttributeAsync("data-bonboarding-state", "todo");

        await first.BringToFrontAsync();
        await first.GetByRole(AriaRole.Textbox, new() { Name = "Search projects" }).Last.FillAsync("reports");
        await Assertions.Expect(first.Locator(item)).ToHaveAttributeAsync("data-bonboarding-state", "done");

        await Assertions.Expect(second.Locator(item)).ToHaveAttributeAsync("data-bonboarding-state", "done");
    }
}
