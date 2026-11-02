using Bit.Bonboarding.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bonboarding.Tests.E2E;

/// <summary>
/// The guidance that lives on the page: beacons positioned by the browser layer on their targets, the hints they
/// open, a checklist that ticks itself off, the list of tours, and a held advance that says why.
/// </summary>
[TestClass]
public sealed class GuidanceTests : BonboardingTest
{
    private const string ImportBeacon = "[data-bonboarding-beacon='import-hint']";

    private async Task<IPage> OpenGuidanceAsync()
    {
        var page = await OpenAsync("/guidance");
        await Assertions.Expect(page.Locator(ImportBeacon)).ToHaveAttributeAsync("data-bonboarding-beacon-state", "visible");
        return page;
    }

    private static async Task<(double X, double Y)> CenterOfAsync(IPage page, string selector)
    {
        var box = await BoxOfAsync(page, selector);
        return (box.X + (box.Width / 2), box.Y + (box.Height / 2));
    }

    [TestMethod]
    public async Task A_beacon_sits_on_its_targets_corner_and_follows_it()
    {
        var page = await OpenGuidanceAsync();

        var target = await BoxOfAsync(page, "[data-bonboarding-anchor='import']");
        var beacon = await CenterOfAsync(page, ImportBeacon);

        // Top-end in a left-to-right page: the top-right corner.
        AssertNear(target.X + target.Width, beacon.X, 1.5, "The beacon's centre, horizontally");
        AssertNear(target.Y, beacon.Y, 1.5, "The beacon's centre, vertically");

        await page.Mouse.WheelAsync(0, 120);
        await page.WaitForFunctionAsync(
            $"() => Math.abs(document.querySelector(\"{ImportBeacon}\").getBoundingClientRect().y + 12 - document.querySelector(\"[data-bonboarding-anchor='import']\").getBoundingClientRect().y) < 1.5",
            null, new PageWaitForFunctionOptions { Timeout = 5_000 });

        AssertNoConsoleErrors("the guidance page");
    }

    [TestMethod]
    public async Task A_beacon_hides_with_its_target()
    {
        var page = await OpenGuidanceAsync();
        var usage = page.Locator("button.bit-bonboarding-beacon[aria-label='Take the usage tour']");

        await Assertions.Expect(usage).ToHaveAttributeAsync("data-bonboarding-beacon-state", "visible");

        await page.GetByRole(AriaRole.Button, new() { Name = "Hide the usage tile" }).ClickAsync();
        await Assertions.Expect(usage).ToHaveAttributeAsync("data-bonboarding-beacon-state", "hidden");
        await Assertions.Expect(usage).ToBeHiddenAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Show the usage tile" }).ClickAsync();
        await Assertions.Expect(usage).ToHaveAttributeAsync("data-bonboarding-beacon-state", "visible");
    }

    [TestMethod]
    public async Task A_read_hint_takes_its_beacon_away_for_good_and_focus_goes_to_the_target()
    {
        var page = await OpenGuidanceAsync();

        await page.Locator(ImportBeacon).ClickAsync();
        var hint = await WaitForStepAsync(page, "Import is here");

        // Non-modal: nothing blocks the page and the beacon says its popup is open.
        Assert.AreEqual(0, await page.Locator(".bit-bonboarding-blocker").CountAsync());
        Assert.AreEqual("false", await hint.GetAttributeAsync("aria-modal"));
        Assert.AreEqual("true", await page.Locator(ImportBeacon).GetAttributeAsync("aria-expanded"));

        await hint.Locator("[data-bonboarding-action='next']").ClickAsync();

        await Assertions.Expect(page.Locator(ImportBeacon)).ToHaveCountAsync(0);
        StringAssert.Contains(await FocusedAsync(page), "Import", "Focus went to the element the hint was about.");

        // Remembered across a reload.
        await page.ReloadAsync();
        await WaitForInteractiveAsync(page);
        await page.WaitForTimeoutAsync(500);
        Assert.AreEqual(0, await page.Locator(ImportBeacon).CountAsync());

        await page.GetByRole(AriaRole.Button, new() { Name = "Bring the beacons back" }).ClickAsync();
        await Assertions.Expect(page.Locator(ImportBeacon)).ToHaveAttributeAsync("data-bonboarding-beacon-state", "visible");
    }

    [TestMethod]
    public async Task Beacons_step_aside_while_a_tour_runs()
    {
        var page = await OpenGuidanceAsync();

        await page.Locator("button.bit-bonboarding-beacon[aria-label='Take the usage tour']").ClickAsync();
        await WaitForStepAsync(page, "Usage this month");

        await Assertions.Expect(page.Locator(ImportBeacon)).ToHaveCountAsync(0);

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(page.Locator(ImportBeacon)).ToHaveAttributeAsync("data-bonboarding-beacon-state", "visible");
    }

    [TestMethod]
    public async Task The_checklist_ticks_itself_off_and_remembers()
    {
        var page = await OpenAsync("/guidance");
        var checklist = page.Locator(".bit-bonboarding-checklist");
        await Assertions.Expect(checklist.Locator(".bit-bonboarding-checklist-count")).ToHaveTextAsync("0 of 3 done");

        // A signal from the application.
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Search projects" }).Last.FillAsync("billing");
        await Assertions.Expect(checklist.Locator("[data-bonboarding-item='search']")).ToHaveAttributeAsync("data-bonboarding-state", "done");

        // A condition.
        await page.GetByRole(AriaRole.Checkbox, new() { Name = "Turn on notifications" }).CheckAsync();
        await Assertions.Expect(checklist.Locator("[data-bonboarding-item='notifications']")).ToHaveAttributeAsync("data-bonboarding-state", "done");

        // A tour, started from the item itself.
        await checklist.Locator("[data-bonboarding-item='tour'] button").ClickAsync();
        var step = await WaitForStepAsync(page, "Projects start here");
        await step.Locator("[data-bonboarding-action='next']").ClickAsync();

        await Assertions.Expect(checklist.Locator(".bit-bonboarding-checklist-count")).ToHaveTextAsync("3 of 3 done");
        Assert.AreEqual("true", await checklist.GetAttributeAsync("data-bonboarding-complete"));

        // The signal and the tour are stored; the condition is the page's own state, which a reload resets.
        await page.ReloadAsync();
        await WaitForInteractiveAsync(page);
        await Assertions.Expect(checklist.Locator(".bit-bonboarding-checklist-count")).ToHaveTextAsync("2 of 3 done");
    }

    [TestMethod]
    public async Task The_tour_list_starts_what_it_lists()
    {
        var page = await OpenAsync("/guidance");
        var list = page.Locator(".bit-bonboarding-tour-list");

        await Assertions.Expect(list.Locator(".bit-bonboarding-tour-list-item")).ToHaveCountAsync(3);

        await list.GetByRole(AriaRole.Button, new() { Name = "Start Reading your usage" }).ClickAsync();
        await WaitForStepAsync(page, "Usage this month");
    }

    [TestMethod]
    public async Task A_held_advance_says_why()
    {
        var page = await OpenAsync("/flows");
        await page.GetByRole(AriaRole.Button, new() { Name = "Run the validating tour" }).ClickAsync();
        var step = await WaitForStepAsync(page, "Name your project");

        await step.Locator("[data-bonboarding-action='next']").ClickAsync();
        await Assertions.Expect(step.GetByRole(AriaRole.Alert)).ToHaveTextAsync("Give the project a name first.");

        await page.Locator("[data-bonboarding-anchor='project-name']").FillAsync("ab");
        await step.Locator("[data-bonboarding-action='next']").ClickAsync();
        await Assertions.Expect(step.GetByRole(AriaRole.Alert)).ToHaveTextAsync("Use at least three characters.");

        await page.Locator("[data-bonboarding-anchor='project-name']").FillAsync("Apollo");
        await step.Locator("[data-bonboarding-action='next']").ClickAsync();
        await WaitForStepAsync(page, "Thank you");
    }
}
